using ClosedXML.Excel;
using hotel_erp.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/data/export")]
    [Authorize]
    public class DataExportController : ControllerBase
    {
        private readonly IGuestRepository _guestRepo;
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IReservationRepository _reservationRepo;
        private readonly IRoomRepository _roomRepo;

        public DataExportController(
            IGuestRepository guestRepo,
            IInvoiceRepository invoiceRepo,
            IReservationRepository reservationRepo,
            IRoomRepository roomRepo)
        {
            _guestRepo = guestRepo;
            _invoiceRepo = invoiceRepo;
            _reservationRepo = reservationRepo;
            _roomRepo = roomRepo;
        }

        [HttpGet("guests")]
        public async Task<IActionResult> ExportGuestsXlsx()
        {
            var guests = await _guestRepo.GetAllAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Huéspedes");

            ws.Cell("A1").Value = "No.";
            ws.Cell("B1").Value = "Nombre";
            ws.Cell("C1").Value = "Apellido";
            ws.Cell("D1").Value = "DNI";
            ws.Cell("E1").Value = "RTN";
            ws.Cell("F1").Value = "Teléfono";
            ws.Cell("G1").Value = "Email";
            ws.Cell("H1").Value = "Nacionalidad";
            ws.Cell("I1").Value = "Procedencia";
            ws.Cell("J1").Value = "Empresa";
            ws.Cell("K1").Value = "Clasificación";
            ws.Cell("L1").Value = "Tipo Contribuyente";
            ws.Cell("M1").Value = "Creado";

            var header = ws.Range("A1:M1");
            header.Style.Font.Bold = true;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var g in guests)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = g.FirstName;
                ws.Cell(row, 3).Value = g.LastName;
                ws.Cell(row, 4).Value = g.DocumentNumber ?? "";
                ws.Cell(row, 5).Value = g.RTN ?? "";
                ws.Cell(row, 6).Value = g.Phone ?? "";
                ws.Cell(row, 7).Value = g.Email ?? "";
                ws.Cell(row, 8).Value = g.Nationality ?? "";
                ws.Cell(row, 9).Value = g.Origin ?? "";
                ws.Cell(row, 10).Value = g.Company ?? "";
                ws.Cell(row, 11).Value = g.Classification ?? "";
                ws.Cell(row, 12).Value = g.TaxpayerType.ToString();
                ws.Cell(row, 13).Value = g.CreatedAt.ToString("dd/MM/yyyy");
                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "huespedes.xlsx");
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> ExportInvoicesXlsx([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var invoices = from.HasValue && to.HasValue
                ? await _invoiceRepo.GetByDateRangeAsync(from.Value, to.Value.AddDays(1))
                : await _invoiceRepo.GetAllAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Facturas");

            ws.Cell("A1").Value = "No.";
            ws.Cell("B1").Value = "Correlativo";
            ws.Cell("C1").Value = "Fecha";
            ws.Cell("D1").Value = "Cliente";
            ws.Cell("E1").Value = "RTN";
            ws.Cell("F1").Value = "Tipo Doc.";
            ws.Cell("G1").Value = "Subtotal";
            ws.Cell("H1").Value = "ISV 15%";
            ws.Cell("I1").Value = "ISV 18%";
            ws.Cell("J1").Value = "Tasa Tur.";
            ws.Cell("K1").Value = "Total";
            ws.Cell("L1").Value = "Estado";
            ws.Cell("M1").Value = "Tipo Contrib.";
            ws.Cell("N1").Value = "CAI";

            var header = ws.Range("A1:N1");
            header.Style.Font.Bold = true;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var inv in invoices)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = inv.CorrelativeNumber;
                ws.Cell(row, 3).Value = inv.InvoiceDate.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = inv.CustomerName;
                ws.Cell(row, 5).Value = inv.RTNCliente ?? "C/F";
                ws.Cell(row, 6).Value = inv.DocumentType.ToString();
                ws.Cell(row, 7).Value = (double)inv.SubTotal;
                ws.Cell(row, 8).Value = (double)inv.ISV15Amount;
                ws.Cell(row, 9).Value = (double)inv.ISV18Amount;
                ws.Cell(row, 10).Value = (double)inv.TouristTaxAmount;
                ws.Cell(row, 11).Value = (double)inv.TotalAmount;
                ws.Cell(row, 12).Value = inv.Status.ToString();
                ws.Cell(row, 13).Value = inv.TaxpayerType.ToString();
                ws.Cell(row, 14).Value = inv.CAINumberSnapshot ?? "";

                for (int c = 7; c <= 11; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            var suffix = from.HasValue ? $"{from:yyyyMMdd}_{to:yyyyMMdd}" : "todas";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"facturas_{suffix}.xlsx");
        }

        [HttpGet("reservations")]
        public async Task<IActionResult> ExportReservationsXlsx([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var allReservations = await _reservationRepo.GetAllAsync();
            var reservations = from.HasValue && to.HasValue
                ? allReservations.Where(r => r.CheckInDate >= DateOnly.FromDateTime(from.Value) && r.CheckInDate <= DateOnly.FromDateTime(to.Value))
                : allReservations;

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reservaciones");

            ws.Cell("A1").Value = "No.";
            ws.Cell("B1").Value = "Huésped";
            ws.Cell("C1").Value = "Habitación";
            ws.Cell("D1").Value = "Check-In";
            ws.Cell("E1").Value = "Check-Out";
            ws.Cell("F1").Value = "Adultos";
            ws.Cell("G1").Value = "Niños";
            ws.Cell("H1").Value = "Método Pago";
            ws.Cell("I1").Value = "Adelanto";
            ws.Cell("J1").Value = "Estado";
            ws.Cell("K1").Value = "Creado";

            var header = ws.Range("A1:K1");
            header.Style.Font.Bold = true;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var r in reservations)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = r.Guest != null ? $"{r.Guest.FirstName} {r.Guest.LastName}" : "";
                ws.Cell(row, 3).Value = r.Room?.RoomNumber ?? "";
                ws.Cell(row, 4).Value = r.CheckInDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 5).Value = r.CheckOutDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value = r.Adults;
                ws.Cell(row, 7).Value = r.Children;
                ws.Cell(row, 8).Value = r.PaymentMethod ?? "";
                ws.Cell(row, 9).Value = (double)r.AdvancePayment;
                ws.Cell(row, 10).Value = r.Status.ToString();
                ws.Cell(row, 11).Value = r.CreatedAt.ToString("dd/MM/yyyy");
                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reservaciones.xlsx");
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> ExportRoomsXlsx()
        {
            var rooms = await _roomRepo.GetAllAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Habitaciones");

            ws.Cell("A1").Value = "No.";
            ws.Cell("B1").Value = "Habitación";
            ws.Cell("C1").Value = "Piso";
            ws.Cell("D1").Value = "Tipo";
            ws.Cell("E1").Value = "Precio/Noche";
            ws.Cell("F1").Value = "Capacidad";
            ws.Cell("G1").Value = "Estado";

            var header = ws.Range("A1:G1");
            header.Style.Font.Bold = true;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var r in rooms)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = r.RoomNumber;
                ws.Cell(row, 3).Value = r.Floor;
                ws.Cell(row, 4).Value = r.RoomType?.Name ?? "";
                ws.Cell(row, 5).Value = (double)(r.RoomType?.PricePerNight ?? 0);
                ws.Cell(row, 6).Value = r.RoomType?.Capacity ?? 0;
                ws.Cell(row, 7).Value = r.Status.ToString();
                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "habitaciones.xlsx");
        }
    }
}

