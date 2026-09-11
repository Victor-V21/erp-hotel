using System.Security.Claims;
using AutoMapper;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Database;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationRepository _repo;
        private readonly IBusinessSettingsRepository _settingsRepo;
        private readonly IDiscountRepository _discountRepo;
        private readonly TaxService _taxService;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;
        private readonly IMapper _mapper;

        public ReservationsController(
            IReservationRepository repo,
            IBusinessSettingsRepository settingsRepo,
            IDiscountRepository discountRepo,
            TaxService taxService,
            ApplicationDbContext context,
            AuditService auditService,
            IMapper mapper)
        {
            _repo = repo;
            _settingsRepo = settingsRepo;
            _discountRepo = discountRepo;
            _taxService = taxService;
            _context = context;
            _auditService = auditService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAll([FromQuery] string? status)
        {
            if (!string.IsNullOrEmpty(status))
                return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByStatusAsync(status)));
            return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetAllAsync()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetById(Guid id)
        {
            var reservation = await _repo.GetByIdAsync(id);
            if (reservation == null) return NotFound();
            return Ok(_mapper.Map<ReservationDto>(reservation));
        }

        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetByDateRange([FromQuery] DateOnly start, [FromQuery] DateOnly end)
            => Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByDateRangeAsync(start, end)));

        [HttpGet("calendar")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetCalendar([FromQuery] DateOnly? start, [FromQuery] DateOnly? end)
        {
            var from = start ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
            var to = end ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));
            return Ok(_mapper.Map<IEnumerable<ReservationDto>>(await _repo.GetByDateRangeAsync(from, to)));
        }

        [HttpPost]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationRequest request)
        {
            if (request.AdvancePayment > 0m)
                return BadRequest("Los anticipos estarán disponibles cuando puedan registrarse y aplicarse contablemente");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var roomLock = $"room-schedule:{request.RoomId:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({roomLock}));");

            var room = await _context.Rooms
                .Include(candidate => candidate.RoomType)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.RoomId);
            if (room is null) return NotFound("Habitación no encontrada");
            if (room.Status is RoomStatus.Mantenimiento or RoomStatus.Bloqueada)
                return Conflict($"La habitación no admite reservas: {room.Status}");
            if (request.Adults + request.Children > room.RoomType.Capacity)
                return BadRequest("La ocupación supera la capacidad de la habitación");

            var guest = await _context.Guests.SingleOrDefaultAsync(candidate => candidate.Id == request.GuestId);
            if (guest is null) return NotFound("Huésped no encontrado");
            if (await _repo.HasOverlapAsync(
                    request.RoomId,
                    request.CheckInDate,
                    request.CheckOutDate,
                    Guid.Empty))
                return Conflict("La habitación ya tiene una reserva activa que se superpone con las fechas");

            var reservation = new Reservation
            {
                Guest = guest,
                Room = room,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Adults = request.Adults,
                Children = request.Children,
                PaymentMethod = request.PaymentMethod,
                AdvancePayment = 0m,
                Notes = request.Notes,
                Status = ReservationStatus.Pendiente,
                Version = 1
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "CreateReservation",
                nameof(Reservation),
                reservation.Id,
                new { reservation.GuestId, reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate, reservation.Adults, reservation.Children, reservation.Version });
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, _mapper.Map<ReservationDto>(reservation));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Update(Guid id, [FromBody] UpdateReservationRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockReservationAsync(id);
            var reservation = await _context.Reservations
                .Include(candidate => candidate.Guest)
                .Include(candidate => candidate.Room)
                .SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (reservation == null) return NotFound();
            if (reservation.Status is not (ReservationStatus.Pendiente or ReservationStatus.Confirmada))
                return Conflict($"No se puede modificar una reserva en estado {reservation.Status}");
            if (reservation.Version != request.ExpectedVersion)
                return VersionConflict(reservation.Version);
            if (request.AdvancePayment is > 0m)
                return BadRequest("Los anticipos estarán disponibles cuando puedan registrarse y aplicarse contablemente");

            var roomId = request.RoomId ?? reservation.RoomId;
            var checkInDate = request.CheckInDate ?? reservation.CheckInDate;
            var checkOutDate = request.CheckOutDate ?? reservation.CheckOutDate;
            var adults = request.Adults ?? reservation.Adults;
            var children = request.Children ?? reservation.Children;
            if (checkOutDate <= checkInDate)
                return BadRequest("La fecha de salida debe ser posterior a la fecha de entrada");

            var roomIds = new[] { reservation.RoomId, roomId }.Distinct().Order().ToList();
            foreach (var lockedRoomId in roomIds)
                await LockRoomScheduleAsync(lockedRoomId);

            var room = await _context.Rooms
                .Include(candidate => candidate.RoomType)
                .SingleOrDefaultAsync(candidate => candidate.Id == roomId);
            if (room is null) return NotFound("Habitación no encontrada");
            if (room.Status is RoomStatus.Mantenimiento or RoomStatus.Bloqueada)
                return Conflict($"La habitación no admite reservas: {room.Status}");
            if (adults + children > room.RoomType.Capacity)
                return BadRequest("La ocupación supera la capacidad de la habitación");
            if (await _repo.HasOverlapAsync(roomId, checkInDate, checkOutDate, id))
                return Conflict("La habitación ya tiene una reserva activa que se superpone con las fechas");

            reservation.RoomId = roomId;
            reservation.Room = room;
            reservation.CheckInDate = checkInDate;
            reservation.CheckOutDate = checkOutDate;
            reservation.Adults = adults;
            reservation.Children = children;
            if (request.PaymentMethod != null) reservation.PaymentMethod = request.PaymentMethod;
            if (request.Notes != null) reservation.Notes = request.Notes;
            reservation.Version++;

            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "UpdateReservation",
                nameof(Reservation),
                reservation.Id,
                new { reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate, reservation.Adults, reservation.Children, reservation.Version });
            await transaction.CommitAsync();
            return Ok(new { reservation.Version });
        }

        [HttpPost("{id}/confirm")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Confirm(Guid id, [FromBody] ConfirmReservationRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockReservationAsync(id);
            var reservation = await _context.Reservations.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (reservation == null) return NotFound();

            if (reservation.Status == ReservationStatus.Confirmada)
            {
                await transaction.CommitAsync();
                return Ok(new { message = "La reserva ya estaba confirmada", reservation.Version });
            }
            if (reservation.Status != ReservationStatus.Pendiente)
                return Conflict($"No se puede confirmar una reserva en estado {reservation.Status}");
            if (reservation.Version != request.ExpectedVersion)
                return VersionConflict(reservation.Version);

            reservation.Status = ReservationStatus.Confirmada;
            reservation.Version++;
            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "ConfirmReservation",
                nameof(Reservation),
                reservation.Id,
                new { reservation.Status, reservation.Version });
            await transaction.CommitAsync();
            return Ok(new { message = "Reserva confirmada", reservation.Version });
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelReservationRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockReservationAsync(id);
            var reservation = await _context.Reservations.SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (reservation == null) return NotFound();

            if (reservation.Status == ReservationStatus.Cancelada)
            {
                await transaction.CommitAsync();
                return Ok(new { message = "La reserva ya estaba cancelada", reservation.Version });
            }
            if (reservation.Status is not (ReservationStatus.Pendiente or ReservationStatus.Confirmada))
                return Conflict($"No se puede cancelar una reserva en estado {reservation.Status}");
            if (reservation.Version != request.ExpectedVersion)
                return VersionConflict(reservation.Version);

            reservation.Status = ReservationStatus.Cancelada;
            reservation.Version++;
            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "CancelReservation",
                nameof(Reservation),
                reservation.Id,
                new { Reason = request.Reason.Trim(), reservation.Status, reservation.Version });
            await transaction.CommitAsync();
            return Ok(new { message = "Reserva cancelada", reservation.Version });
        }

        [HttpPost("checkin")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockReservationAsync(request.ReservationId);
            await LockRoomScheduleAsync(request.RoomId);

            var reservation = await _context.Reservations
                .Include(candidate => candidate.Guest)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.ReservationId);
            if (reservation == null) return NotFound("Reserva no encontrada");
            if (reservation.Status is not (ReservationStatus.Pendiente or ReservationStatus.Confirmada))
                return Conflict($"No se puede registrar check-in desde el estado {reservation.Status}");
            if (reservation.Version != request.ExpectedVersion)
                return VersionConflict(reservation.Version);
            if (reservation.RoomId != request.RoomId)
                return BadRequest("La habitación solicitada no coincide con la reserva");
            if (reservation.CheckOutDate <= reservation.CheckInDate)
                return BadRequest("La estadía debe ser al menos 1 noche");
            if (await _context.Folios.IgnoreQueryFilters().AnyAsync(folio => folio.ReservationId == reservation.Id))
                return Conflict("La reserva ya tiene un folio de estancia");

            // Load business settings for tax rates
            var settings = await _settingsRepo.GetAsync();
            if (settings != null)
            {
                _taxService.IsvRate = settings.IsvRate;
                _taxService.TouristTaxRate = settings.TouristTaxRate;
            }

            var room = await _context.Rooms
                .Include(candidate => candidate.RoomType)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.RoomId);
            if (room == null) return NotFound("Habitación no encontrada");
            if (room.Status != RoomStatus.Libre)
                return Conflict($"La habitación no está disponible físicamente: {room.Status}");
            if (reservation.Adults + reservation.Children > room.RoomType.Capacity)
                return BadRequest("La ocupación supera la capacidad de la habitación");
            if (await _repo.HasOverlapAsync(
                    request.RoomId,
                    reservation.CheckInDate,
                    reservation.CheckOutDate,
                    reservation.Id))
                return Conflict("Existe otra reserva activa que se superpone con la estancia");

            var nights = (reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber);
            var sellingPricePerNight = room.RoomType.PricePerNight;
            var taxResult = _taxService.CalculateFromSellingPrice(sellingPricePerNight, nights);

            // Apply discounts
            decimal totalDiscountPercent = 0;
            if (request.DiscountIds?.Any() == true)
            {
                var discounts = await _discountRepo.GetActiveAsync();
                var requestedDiscountIds = request.DiscountIds.Distinct().ToList();
                var selectedDiscounts = discounts.Where(d => requestedDiscountIds.Contains(d.Id)).OrderBy(d => d.Priority).ToList();
                if (selectedDiscounts.Count != requestedDiscountIds.Count)
                    return BadRequest("Uno o más descuentos no existen o no están activos");
                if (selectedDiscounts.Any(discount => discount.DiscountType != DiscountType.Porcentaje))
                    return BadRequest("El check-in solo admite descuentos porcentuales");
                foreach (var d in selectedDiscounts)
                {
                    totalDiscountPercent += d.Value;
                }
                if (totalDiscountPercent > 100m)
                    return BadRequest("La suma de descuentos no puede superar 100 %");
                taxResult = _taxService.ApplyDiscount(taxResult, totalDiscountPercent);
            }

            // Create folio
            var folio = new Folio
            {
                ReservationId = reservation.Id,
                GuestId = reservation.GuestId,
                RoomId = request.RoomId,
                OpeningDate = HondurasTime.Now,
                Status = FolioStatus.Abierto,
                TotalAmount = taxResult.Total
            };

            folio.FolioItems.Add(new FolioItem
            {
                Description = $"Hospedaje - {room.RoomNumber} x {nights} noche{(nights > 1 ? "s" : "")}",
                Quantity = 1,
                UnitPrice = taxResult.Subtotal,
                LineTotal = taxResult.Subtotal,
                IsExempt = false,
                ISVRate = _taxService.IsvRate,
                IsTouristTaxable = true
            });

            reservation.Status = ReservationStatus.CheckIn;
            reservation.Version++;
            room.Status = RoomStatus.Ocupada;
            _context.Folios.Add(folio);

            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "CheckIn",
                nameof(Reservation),
                reservation.Id,
                new { reservation.RoomId, reservation.CheckInDate, reservation.CheckOutDate, folio.Id, EstimatedTotal = folio.TotalAmount, reservation.Version });
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Check-in exitoso",
                folioId = folio.Id,
                version = reservation.Version,
                sellingPricePerNight,
                nights,
                subtotal = taxResult.Subtotal,
                isv = taxResult.ISV,
                isvRate = _taxService.IsvRate,
                touristTax = taxResult.TouristTax,
                touristTaxRate = _taxService.TouristTaxRate,
                discountPercent = totalDiscountPercent,
                discountAmount = taxResult.DiscountAmount,
                total = taxResult.Total
            });
        }

        [HttpPost("checkout/{id}")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> CheckOut(Guid id, [FromBody] CheckOutRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await LockReservationAsync(id);

            var reservation = await _context.Reservations
                .Include(candidate => candidate.Room)
                .Include(candidate => candidate.Folio)
                .SingleOrDefaultAsync(candidate => candidate.Id == id);
            if (reservation == null) return NotFound("Reserva no encontrada");
            if (reservation.Folio is null)
                return Conflict("La reserva no tiene un folio de estancia");

            var settlementInvoice = await _context.Invoices
                .SingleOrDefaultAsync(invoice => invoice.Id == request.InvoiceId);
            if (settlementInvoice is null
                || settlementInvoice.FolioId != reservation.Folio.Id
                || settlementInvoice.DocumentType != InvoiceDocumentType.Factura)
                return Conflict("La factura indicada no liquida el folio de esta reserva");

            if (reservation.Status == ReservationStatus.CheckOut
                && reservation.Folio.Status == FolioStatus.Cerrado)
            {
                await transaction.CommitAsync();
                return Ok(new { message = "El check-out ya estaba completado", invoiceId = settlementInvoice.Id });
            }
            if (reservation.Status != ReservationStatus.CheckIn || reservation.Folio.Status != FolioStatus.Abierto)
                return Conflict("La reserva y su folio no están abiertos para check-out");

            reservation.Folio.ClosingDate = HondurasTime.Now;
            reservation.Folio.Status = FolioStatus.Cerrado;
            reservation.Room.Status = RoomStatus.Limpieza;
            reservation.Status = ReservationStatus.CheckOut;
            reservation.Version++;
            await _context.SaveChangesAsync();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _auditService.LogAsync(
                userId,
                "CheckOut",
                nameof(Reservation),
                reservation.Id,
                new { reservation.Folio.Id, InvoiceId = settlementInvoice.Id, settlementInvoice.CorrelativeNumber, reservation.RoomId, reservation.Folio.TotalAmount, reservation.Version },
                settlementInvoice.CorrelativeNumber,
                settlementInvoice.PaymentMethod);
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Check-out exitoso, habitación liberada",
                invoiceId = settlementInvoice.Id
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = PermissionNames.ManageReservations)]
        public async Task<ActionResult> Delete(Guid id)
        {
            if (!await _context.Reservations.AnyAsync(candidate => candidate.Id == id))
                return NotFound();

            return BadRequest("Use la cancelación con motivo para preservar el historial de la reserva");
        }

        private async Task LockReservationAsync(Guid id)
        {
            var lockKey = $"reservation:{id:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({lockKey}));");
        }

        private async Task LockRoomScheduleAsync(Guid id)
        {
            var lockKey = $"room-schedule:{id:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({lockKey}));");
        }

        private ConflictObjectResult VersionConflict(int currentVersion) => Conflict(new
        {
            message = "La reserva cambió desde que fue consultada; actualice los datos antes de continuar",
            currentVersion
        });
    }
}
