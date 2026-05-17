using System.Text;
using hotel_erp.Application.Interfaces;
using hotel_erp.Application.Services;
using hotel_erp.Domain.Entities;
using hotel_erp.Domain.Enums;
using hotel_erp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrintController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IBusinessSettingsRepository _settingsRepo;
        private readonly EscPosService _escPosService;

        public PrintController(
            IInvoiceRepository invoiceRepo,
            IBusinessSettingsRepository settingsRepo,
            EscPosService escPosService)
        {
            _invoiceRepo = invoiceRepo;
            _settingsRepo = settingsRepo;
            _escPosService = escPosService;
        }

        [HttpGet("printers")]
        public ActionResult GetPrinters()
        {
            var printers = RawPrinterHelper.GetInstalledPrinters();
            return Ok(printers);
        }

        [HttpGet("test")]
        public ActionResult TestPrinter([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Especifique el nombre de la impresora" });

            var (success, message) = RawPrinterHelper.TestPrinterConnection(name);
            return Ok(new { success, message });
        }

        [HttpGet("test-preview")]
        public async Task<ActionResult> GetTestPreview([FromQuery] int? width)
        {
            var settings = await _settingsRepo.GetAsync() ?? new BusinessSettings();
            if (width.HasValue) settings.PrintWidth = width.Value;
            var fake = BuildFakeInvoice(settings);
            var text = _escPosService.GeneratePreviewText(fake, settings, "Juan Perez", "15/05/2026", "18/05/2026");
            return Ok(new { text, logoBase64 = settings.LogoBase64 ?? "", printLogoHeight = settings.PrintLogoHeight, printWidth = settings.PrintWidth });
        }

        [HttpPost("test-print")]
        public async Task<ActionResult> PrintTest([FromQuery] int? width)
        {
            var settings = await _settingsRepo.GetAsync();
            if (settings == null)
                return BadRequest(new { message = "Configure los datos del negocio" });
            if (string.IsNullOrWhiteSpace(settings.PrintPrinterName))
                return BadRequest(new { message = "Configure la impresora en Configuracion" });
            if (width.HasValue) settings.PrintWidth = width.Value;

            try
            {
                var fake = BuildFakeInvoice(settings);
                var data = _escPosService.GenerateTwoCopyInvoice(fake, settings, "Juan Perez", "15/05/2026", "18/05/2026");
                RawPrinterHelper.SendBytesToPrinter(settings.PrintPrinterName, data);
                return Ok(new { message = "Factura de prueba enviada a imprimir" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("test-ruler")]
        public async Task<ActionResult> GetTestRuler()
        {
            var settings = await _settingsRepo.GetAsync();
            if (settings == null || string.IsNullOrWhiteSpace(settings.PrintPrinterName))
                return BadRequest(new { message = "Configure la impresora primero" });
            try
            {
                var data = _escPosService.BuildRulerData(settings, 100);
                RawPrinterHelper.SendBytesToPrinter(settings.PrintPrinterName, data);
                return Ok(new { message = "Regla de calibracion impresa. Observe hasta que numero llega el papel sin cortarse y anote ese valor en 'Ancho observado' en la pantalla de configuracion." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        private static Invoice BuildFakeInvoice(BusinessSettings settings)
        {
            return new Invoice
            {
                CAI = new CAI
                {
                    CAINumber = "A1B2-C3D4-E5F6-G7H8",
                    InitialRange = "001-001-01-00000001",
                    FinalRange = "001-001-01-00005000",
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
                    Status = CAIStatus.Activo
                },
                CorrelativeNumber = "001-001-01-00000001",
                InvoiceDate = HondurasTime.Now,
                CustomerName = "Juan Perez - Factura de Prueba",
                RTNCliente = "08019012345678",
                SubTotal = 840.34m,
                ISVAmount = 126.05m,
                TouristTaxAmount = 33.61m,
                DiscountsAmount = 0,
                TotalAmount = 1000.00m,
                PaymentMethod = "Efectivo",
                CashReceived = 1500,
                CashChange = 500,
                DocumentType = InvoiceDocumentType.Factura,
                Status = InvoiceStatus.Pagada,
                InvoiceItems = new List<InvoiceItem>
                {
                    new()
                    {
                        Description = "Habitación Sencilla|Hospedaje x3 noche(s)",
                        Quantity = 3,
                        UnitPrice = 333.33m,
                        LineTotal = 1000m,
                        ISVRate = 0.15m,
                        IsTouristTaxable = true,
                        IsExempt = false
                    }
                }
            };
        }

        [HttpGet("invoice/{id}/preview")]
        public async Task<ActionResult> GetPreview(Guid id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id);
            if (invoice == null) return NotFound(new { message = "Factura no encontrada" });

            var settings = await _settingsRepo.GetAsync() ?? new BusinessSettings();

            string? guestName = null, checkIn = null, checkOut = null;
            if (invoice.Guest != null)
            {
                guestName = $"{invoice.Guest.FirstName} {invoice.Guest.LastName}";
                var res = invoice.Guest.Reservations.FirstOrDefault();
                if (res != null) { checkIn = res.CheckInDate.ToString("dd/MM/yyyy"); checkOut = res.CheckOutDate.ToString("dd/MM/yyyy"); }
            }

            var text = _escPosService.GeneratePreviewText(invoice, settings, guestName, checkIn, checkOut);
            return Ok(new { text, logoBase64 = settings.LogoBase64 ?? "", printLogoHeight = settings.PrintLogoHeight, printWidth = settings.PrintWidth });
        }

        [HttpPost("invoice/{id}")]
        public async Task<ActionResult> PrintInvoice(Guid id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id);
            if (invoice == null) return NotFound(new { message = "Factura no encontrada" });

            var settings = await _settingsRepo.GetAsync();
            if (settings == null)
                return BadRequest(new { message = "Configure primero los datos del negocio en Configuracion" });

            var printerName = settings.PrintPrinterName;
            if (string.IsNullOrWhiteSpace(printerName))
                return BadRequest(new { message = "Configure el nombre de la impresora en Configuracion" });

            string? guestName = null, checkIn = null, checkOut = null;

            if (invoice.Guest != null)
            {
                guestName = $"{invoice.Guest.FirstName} {invoice.Guest.LastName}";
                var res = invoice.Guest.Reservations.FirstOrDefault();
                if (res != null)
                {
                    checkIn = res.CheckInDate.ToString("dd/MM/yyyy");
                    checkOut = res.CheckOutDate.ToString("dd/MM/yyyy");
                }
            }

            try
            {
                var data = _escPosService.GenerateTwoCopyInvoice(invoice, settings, guestName, checkIn, checkOut);
                RawPrinterHelper.SendBytesToPrinter(printerName, data);
                return Ok(new { message = "Factura enviada a imprimir", printer = printerName });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.Contains("StartDocPrinter"))
                    msg += ". Solucion: 1) Ejecute la terminal como Administrador. 2) Reinicie el servicio Spooler (PowerShell: Restart-Service Spooler).";
                return BadRequest(new { message = msg });
            }
        }
    }
}
