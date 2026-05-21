using ClosedXML.Excel;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPurchaseInvoiceRepository _purchaseRepo;

        public ReportsController(IInvoiceRepository invoiceRepo, IPurchaseInvoiceRepository purchaseRepo)
        {
            _invoiceRepo = invoiceRepo;
            _purchaseRepo = purchaseRepo;
        }

        [HttpGet("sar/sales-book")]
        public async Task<IActionResult> GetSalesBookXlsx([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var invoices = await _invoiceRepo.GetByDateRangeAsync(from, to.AddDays(1));
            var filtered = invoices.Where(i => i.DocumentType == InvoiceDocumentType.Factura || i.DocumentType == InvoiceDocumentType.NotaCredito || i.DocumentType == InvoiceDocumentType.NotaDebito);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Libro de Ventas");

            // Title
            ws.Cell("A1").Value = "LIBRO DE VENTAS SAR";
            ws.Range("A1:O1").Merge().Style.Font.Bold = true;
            ws.Cell("A2").Value = $"Del {from:dd/MM/yyyy} al {to:dd/MM/yyyy}";
            ws.Range("A2:O2").Merge().Style.Font.Italic = true;

            // Headers
            var headers = new[] {
                "No.", "Fecha Emisión", "Tipo Doc.", "No. Factura", "RTN Cliente",
                "Nombre Cliente", "Subtotal", "ISV 15%", "ISV 18%",
                "Exento", "Exonerado", "Total", "CAI", "Tipo Contribuyente", "Estado"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            int row = 5;
            int num = 1;
            foreach (var inv in filtered)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = inv.DocumentType.ToString();
                ws.Cell(row, 4).Value = inv.CorrelativeNumber;
                ws.Cell(row, 5).Value = inv.RTNCliente ?? "C/F";
                ws.Cell(row, 6).Value = inv.CustomerName;
                ws.Cell(row, 7).Value = (double)inv.SubTotal;
                ws.Cell(row, 8).Value = (double)inv.ISV15Amount;
                ws.Cell(row, 9).Value = (double)inv.ISV18Amount;
                ws.Cell(row, 10).Value = (double)inv.ExemptAmount;
                ws.Cell(row, 11).Value = (double)inv.ExoneratedAmount;
                ws.Cell(row, 12).Value = (double)inv.TotalAmount;
                ws.Cell(row, 13).Value = inv.CAINumberSnapshot ?? "";
                ws.Cell(row, 14).Value = inv.TaxpayerType.ToString();
                ws.Cell(row, 15).Value = inv.Status.ToString();

                for (int c = 7; c <= 12; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Libro_Ventas_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }

        [HttpGet("sar/purchases-book")]
        public async Task<IActionResult> GetPurchasesBookXlsx([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var purchases = await _purchaseRepo.GetByDateRangeAsync(from, to.AddDays(1));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Libro de Compras");

            ws.Cell("A1").Value = "LIBRO DE COMPRAS SAR";
            ws.Range("A1:M1").Merge().Style.Font.Bold = true;
            ws.Cell("A2").Value = $"Del {from:dd/MM/yyyy} al {to:dd/MM/yyyy}";
            ws.Range("A2:M2").Merge().Style.Font.Italic = true;

            var headers = new[] {
                "No.", "Fecha", "No. Factura", "RTN Proveedor", "Nombre Proveedor",
                "Subtotal", "ISV 15%", "ISV 18%", "Total",
                "No. CAI", "Tipo Compra", "Estado", "Notas"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            int row = 5;
            int num = 1;
            foreach (var p in purchases)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = p.InvoiceDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = p.InvoiceNumber;
                ws.Cell(row, 4).Value = p.SupplierRTN ?? p.Supplier?.RTN ?? "";
                ws.Cell(row, 5).Value = p.Supplier?.Name ?? "";
                ws.Cell(row, 6).Value = (double)p.SubTotal;
                ws.Cell(row, 7).Value = (double)p.ISV15Amount;
                ws.Cell(row, 8).Value = (double)p.ISV18Amount;
                ws.Cell(row, 9).Value = (double)p.TotalAmount;
                ws.Cell(row, 10).Value = p.CAINumber ?? "";
                ws.Cell(row, 11).Value = "Bienes/Servicios";
                ws.Cell(row, 12).Value = p.Status.ToString();
                ws.Cell(row, 13).Value = p.Notes ?? "";

                for (int c = 6; c <= 9; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

                row++;
            }

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Libro_Compras_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }
    }
}

