using hotel_erp.Api.Database;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Dashboard;
using hotel_erp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var today = HondurasTime.Today;
            var todayStart = HondurasTime.Now.Date;
            var todayEnd = todayStart.AddDays(1);

            var occupiedStatuses = new[] { RoomStatus.Ocupada };
            var occupiedRooms = await _context.Rooms.CountAsync(r => occupiedStatuses.Contains(r.Status));
            var freeRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Libre);

            var pendingCheckIns = await _context.Reservations.CountAsync(r => r.CheckInDate == today && r.Status == ReservationStatus.Confirmada);
            var pendingCheckOuts = await _context.Reservations.CountAsync(r => r.CheckOutDate == today && r.Status == ReservationStatus.CheckIn);

            var invoiceQuery = _context.Invoices.AsNoTracking().Include(i => i.Guest).Include(i => i.Customer);
            var recentInvoices = await invoiceQuery
                .OrderByDescending(i => i.InvoiceDate)
                .Take(5)
                .Select(i => new DashboardInvoiceDto
                {
                    CorrelativeNumber = i.CorrelativeNumber,
                    CustomerName = i.CustomerName,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status.ToString(),
                    InvoiceDate = i.InvoiceDate
                })
                .ToListAsync();

            var upcomingReservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Guest)
                .Include(r => r.Room)
                .Where(r => r.Status == ReservationStatus.Confirmada || r.Status == ReservationStatus.Pendiente)
                .OrderBy(r => r.CheckInDate)
                .Take(5)
                .Select(r => new DashboardReservationDto
                {
                    GuestName = r.Guest.FirstName + " " + r.Guest.LastName,
                    RoomNumber = r.Room.RoomNumber,
                    Status = r.Status.ToString(),
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate
                })
                .ToListAsync();

            var totalInvoicesToday = await _context.Invoices.CountAsync(i => i.InvoiceDate >= todayStart && i.InvoiceDate < todayEnd);
            var revenueToday = await _context.Invoices
                .Where(i => i.InvoiceDate >= todayStart && i.InvoiceDate < todayEnd)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            var expiringCAIs = await _context.CAIs
                .AsNoTracking()
                .Where(c => c.Status == CAIStatus.Activo && c.DueDate <= today.AddDays(30))
                .OrderBy(c => c.DueDate)
                .ToListAsync();

            var activeDocAuth = await _context.DocumentAuthorizations
                .AsNoTracking()
                .Where(d => d.Status == CAIStatus.Activo)
                .OrderBy(d => d.DueDate)
                .ToListAsync();

            var cashRegister = await _context.CashRegisters.AsNoTracking().FirstOrDefaultAsync(cr => cr.IsActive);
            decimal? cashBalance = null;
            string? cashName = null;
            if (cashRegister != null)
            {
                cashBalance = await _context.CashMovements
                    .Where(cm => cm.CashRegisterId == cashRegister.Id)
                    .OrderByDescending(cm => cm.CreatedAt)
                    .ThenByDescending(cm => cm.Id)
                    .Select(cm => (decimal?)cm.BalanceAfter)
                    .FirstOrDefaultAsync() ?? 0;
                cashName = cashRegister.Name;
            }

            var alerts = new List<DashboardAlertDto>();
            if (expiringCAIs.Count > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Title = "CAI por vencer",
                    Description = $"{expiringCAIs.Count} CAI(s) vencen pronto. El más cercano vence el {expiringCAIs[0].DueDate:dd/MM/yyyy}.",
                    Severity = "warning"
                });
            }
            if (activeDocAuth.Any(d => d.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))))
            {
                alerts.Add(new DashboardAlertDto
                {
                    Title = "Autorización fiscal próxima a vencer",
                    Description = $"{activeDocAuth.Count(d => d.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))} autorización(es) requieren revisión.",
                    Severity = "warning"
                });
            }
            if (await _context.Products.AnyAsync(p => p.IsActive && p.CurrentStock <= p.MinStockLevel))
            {
                alerts.Add(new DashboardAlertDto
                {
                    Title = "Stock bajo",
                    Description = "Hay productos por debajo del stock mínimo.",
                    Severity = "danger"
                });
            }

            var summary = new DashboardSummaryDto
            {
                OccupiedRooms = occupiedRooms,
                FreeRooms = freeRooms,
                PendingCheckIns = pendingCheckIns,
                PendingCheckOuts = pendingCheckOuts,
                Cash = cashRegister == null
                    ? null
                    : new DashboardCashDto { RegisterName = cashName!, Balance = cashBalance ?? 0 },
                Stats = new List<DashboardStatDto>
                {
                    new() { Title = "Habitaciones ocupadas", Value = occupiedRooms.ToString(), Trend = "Operativo" },
                    new() { Title = "Habitaciones libres", Value = freeRooms.ToString(), Trend = "Operativo" },
                    new() { Title = "Reservas hoy", Value = pendingCheckIns.ToString(), Trend = "Por confirmar" },
                    new() { Title = "Ingresos del día", Value = $"L {revenueToday:N2}", Trend = $"{totalInvoicesToday} factura(s)" },
                },
                Alerts = alerts,
                RecentInvoices = recentInvoices,
                UpcomingReservations = upcomingReservations
            };

            return Ok(summary);
        }
    }
}
