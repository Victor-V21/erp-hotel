using ClosedXML.Excel;
using hotel_erp.Api.Authorization;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    public record TaxSummaryDto(
        DateTime From,
        DateTime To,
        decimal SalesTaxed15,
        decimal SalesTaxed18,
        decimal SalesExempt,
        decimal IsvCollected15,
        decimal IsvCollected18,
        decimal IsvOnPurchases,
        decimal NetIsvToPay,
        decimal TouristTaxCollected
    );

    public record FinancialStatementLineDto(string AccountNumber, string AccountName, decimal Amount);

    public record IncomeStatementDto(
        DateTime From,
        DateTime To,
        List<FinancialStatementLineDto> Revenues,
        decimal TotalRevenues,
        List<FinancialStatementLineDto> Expenses,
        decimal TotalExpenses,
        decimal NetIncome
    );

    public record BalanceSheetDto(
        DateTime AsOfDate,
        List<FinancialStatementLineDto> Assets,
        decimal TotalAssets,
        List<FinancialStatementLineDto> Liabilities,
        decimal TotalLiabilities,
        List<FinancialStatementLineDto> Equity,
        decimal TotalEquity,
        decimal TotalLiabilitiesAndEquity,
        bool IsBalanced
    );

    [ApiController]
    [Route("api/reports")]
    [Authorize(Policy = PermissionNames.ViewReports)]
    public class ReportsController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPurchaseInvoiceRepository _purchaseRepo;
        private readonly IAccountingRepository _accountingRepo;

        public ReportsController(
            IInvoiceRepository invoiceRepo,
            IPurchaseInvoiceRepository purchaseRepo,
            IAccountingRepository accountingRepo)
        {
            _invoiceRepo = invoiceRepo;
            _purchaseRepo = purchaseRepo;
            _accountingRepo = accountingRepo;
        }

        [HttpGet("tax-summary")]
        public async Task<IActionResult> GetTaxSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var invoices = await _invoiceRepo.GetByDateRangeAsync(from, to.AddDays(1));
            var purchases = await _purchaseRepo.GetByDateRangeAsync(from, to.AddDays(1));

            var salesTaxed15 = invoices.Where(i => i.ISV15Amount > 0).Sum(i => i.SubTotal);
            var salesTaxed18 = invoices.Where(i => i.ISV18Amount > 0).Sum(i => i.ISV18Amount > 0 ? i.ISV18Amount / 0.18m : 0m);
            var salesExempt = invoices.Sum(i => i.ExemptAmount);
            var isvCollected15 = invoices.Sum(i => i.ISV15Amount);
            var isvCollected18 = invoices.Sum(i => i.ISV18Amount);
            var isvOnPurchases = purchases.Sum(p => p.ISV15Amount + p.ISV18Amount);
            var netIsvToPay = (isvCollected15 + isvCollected18) - isvOnPurchases;
            var touristTaxCollected = invoices.Sum(i => i.TouristTaxAmount);

            var result = new TaxSummaryDto(
                from, to,
                salesTaxed15, salesTaxed18, salesExempt,
                isvCollected15, isvCollected18,
                isvOnPurchases, netIsvToPay,
                touristTaxCollected
            );

            return Ok(result);
        }

        [HttpGet("financial/income-statement")]
        public async Task<ActionResult<IncomeStatementDto>> GetIncomeStatement([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var fromDate = DateOnly.FromDateTime(from);
            var toDate = DateOnly.FromDateTime(to);
            var accounts = await _accountingRepo.GetAllAccountsAsync();
            var revenues = new List<FinancialStatementLineDto>();
            var expenses = new List<FinancialStatementLineDto>();

            foreach (var account in accounts.Where(a => a.IsActive))
            {
                var movements = await _accountingRepo.GetAccountMovementsAsync(account.Id);
                var periodMovements = movements.Where(m => m.Entry != null && m.Entry.TransactionDate >= fromDate && m.Entry.TransactionDate <= toDate).ToList();

                if (periodMovements.Count == 0) continue;

                var totalDebit = periodMovements.Sum(m => m.Debit);
                var totalCredit = periodMovements.Sum(m => m.Credit);

                if (account.AccountType == AccountType.Ingreso)
                {
                    var balance = totalCredit - totalDebit;
                    if (balance != 0)
                        revenues.Add(new FinancialStatementLineDto(account.AccountNumber, account.AccountName, balance));
                }
                else if (account.AccountType == AccountType.Gasto)
                {
                    var balance = totalDebit - totalCredit;
                    if (balance != 0)
                        expenses.Add(new FinancialStatementLineDto(account.AccountNumber, account.AccountName, balance));
                }
            }

            var totalRevenues = revenues.Sum(r => r.Amount);
            var totalExpenses = expenses.Sum(e => e.Amount);
            var netIncome = totalRevenues - totalExpenses;

            return Ok(new IncomeStatementDto(from, to, revenues, totalRevenues, expenses, totalExpenses, netIncome));
        }

        [HttpGet("financial/balance-sheet")]
        public async Task<ActionResult<BalanceSheetDto>> GetBalanceSheet([FromQuery] DateTime? asOfDate)
        {
            var date = asOfDate ?? DateTime.UtcNow;
            var asOf = DateOnly.FromDateTime(date);
            var accounts = await _accountingRepo.GetAllAccountsAsync();
            var assets = new List<FinancialStatementLineDto>();
            var liabilities = new List<FinancialStatementLineDto>();
            var equity = new List<FinancialStatementLineDto>();

            decimal currentPeriodIncome = 0;

            foreach (var account in accounts.Where(a => a.IsActive))
            {
                var movements = await _accountingRepo.GetAccountMovementsAsync(account.Id);
                var periodMovements = movements.Where(m => m.Entry != null && m.Entry.TransactionDate <= asOf).ToList();

                var totalDebit = periodMovements.Sum(m => m.Debit);
                var totalCredit = periodMovements.Sum(m => m.Credit);

                switch (account.AccountType)
                {
                    case AccountType.Activo:
                        var assetBal = totalDebit - totalCredit;
                        if (assetBal != 0)
                            assets.Add(new FinancialStatementLineDto(account.AccountNumber, account.AccountName, assetBal));
                        break;
                    case AccountType.Pasivo:
                        var liabBal = totalCredit - totalDebit;
                        if (liabBal != 0)
                            liabilities.Add(new FinancialStatementLineDto(account.AccountNumber, account.AccountName, liabBal));
                        break;
                    case AccountType.Patrimonio:
                        var eqBal = totalCredit - totalDebit;
                        if (eqBal != 0)
                            equity.Add(new FinancialStatementLineDto(account.AccountNumber, account.AccountName, eqBal));
                        break;
                    case AccountType.Ingreso:
                        currentPeriodIncome += (totalCredit - totalDebit);
                        break;
                    case AccountType.Gasto:
                        currentPeriodIncome -= (totalDebit - totalCredit);
                        break;
                }
            }


            if (currentPeriodIncome != 0)
            {
                equity.Add(new FinancialStatementLineDto("3102", "Utilidad / (Pérdida) del Período", currentPeriodIncome));
            }

            var totalAssets = assets.Sum(a => a.Amount);
            var totalLiabilities = liabilities.Sum(l => l.Amount);
            var totalEquity = equity.Sum(e => e.Amount);
            var totalLiabEquity = totalLiabilities + totalEquity;
            var isBalanced = Math.Abs(totalAssets - totalLiabEquity) < 0.05m;

            return Ok(new BalanceSheetDto(
                date,
                assets, totalAssets,
                liabilities, totalLiabilities,
                equity, totalEquity,
                totalLiabEquity,
                isBalanced
            ));
        }

        [HttpGet("sar/sales-book")]
        public async Task<IActionResult> GetSalesBookXlsx([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var invoices = await _invoiceRepo.GetByDateRangeAsync(from, to.AddDays(1));
            var filtered = invoices.Where(i => i.DocumentType == InvoiceDocumentType.Factura || i.DocumentType == InvoiceDocumentType.NotaCredito || i.DocumentType == InvoiceDocumentType.NotaDebito);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Libro de Ventas");

            ws.Cell("A1").Value = "LIBRO DE VENTAS SAR — HOTEL MAYA CENTRAL";
            ws.Range("A1:L1").Merge().Style.Font.Bold = true;
            ws.Cell("A2").Value = $"Del {from:dd/MM/yyyy} al {to:dd/MM/yyyy}";
            ws.Range("A2:L2").Merge().Style.Font.Italic = true;

            var headers = new[] {
                "Fecha", "Correlativo", "CAI", "RTN Cliente", "Nombre Cliente",
                "Gravado 15%", "Gravado 18%", "Exento", "ISV 15%", "ISV 18%",
                "Total", "Tipo Documento"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            int row = 5;
            decimal totalGravado15 = 0, totalGravado18 = 0, totalExento = 0;
            decimal totalISV15 = 0, totalISV18 = 0, totalTotal = 0;

            foreach (var inv in filtered)
            {
                var gravado15 = inv.ISV15Amount > 0 ? Math.Round(inv.ISV15Amount / 0.15m, 2) : 0m;
                var gravado18 = inv.ISV18Amount > 0 ? Math.Round(inv.ISV18Amount / 0.18m, 2) : 0m;

                ws.Cell(row, 1).Value = inv.InvoiceDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = inv.CorrelativeNumber;
                ws.Cell(row, 3).Value = inv.CAINumberSnapshot ?? "";
                ws.Cell(row, 4).Value = inv.RTNCliente ?? "C/F";
                ws.Cell(row, 5).Value = inv.CustomerName;
                ws.Cell(row, 6).Value = (double)gravado15;
                ws.Cell(row, 7).Value = (double)gravado18;
                ws.Cell(row, 8).Value = (double)inv.ExemptAmount;
                ws.Cell(row, 9).Value = (double)inv.ISV15Amount;
                ws.Cell(row, 10).Value = (double)inv.ISV18Amount;
                ws.Cell(row, 11).Value = (double)inv.TotalAmount;
                ws.Cell(row, 12).Value = inv.DocumentType.ToString();

                for (int c = 6; c <= 11; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

                totalGravado15 += gravado15;
                totalGravado18 += gravado18;
                totalExento += inv.ExemptAmount;
                totalISV15 += inv.ISV15Amount;
                totalISV18 += inv.ISV18Amount;
                totalTotal += inv.TotalAmount;

                row++;
            }

            ws.Cell(row, 1).Value = "TOTALES";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 6).Value = (double)totalGravado15;
            ws.Cell(row, 7).Value = (double)totalGravado18;
            ws.Cell(row, 8).Value = (double)totalExento;
            ws.Cell(row, 9).Value = (double)totalISV15;
            ws.Cell(row, 10).Value = (double)totalISV18;
            ws.Cell(row, 11).Value = (double)totalTotal;
            for (int c = 6; c <= 11; c++)
            {
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, c).Style.Font.Bold = true;
            }
            for (int c = 1; c <= 12; c++)
                ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thin;

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

            ws.Cell("A1").Value = "LIBRO DE COMPRAS SAR — HOTEL MAYA CENTRAL";
            ws.Range("A1:H1").Merge().Style.Font.Bold = true;
            ws.Cell("A2").Value = $"Del {from:dd/MM/yyyy} al {to:dd/MM/yyyy}";
            ws.Range("A2:H2").Merge().Style.Font.Italic = true;

            var headers = new[] {
                "Fecha", "No. Factura", "RTN Proveedor", "Nombre Proveedor",
                "Gravado", "ISV", "Total", "Con Factura Fiscal"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            int row = 5;
            decimal totalGravado = 0, totalISV = 0, totalTotal = 0;

            foreach (var p in purchases)
            {
                var gravado = p.TotalAmount - (p.ISV15Amount + p.ISV18Amount);
                var isv = p.ISV15Amount + p.ISV18Amount;
                var conFactura = !string.IsNullOrWhiteSpace(p.CAINumber) ? "Sí" : "No";

                ws.Cell(row, 1).Value = p.InvoiceDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = p.InvoiceNumber;
                ws.Cell(row, 3).Value = p.SupplierRTN ?? p.Supplier?.RTN ?? "";
                ws.Cell(row, 4).Value = p.Supplier?.Name ?? "";
                ws.Cell(row, 5).Value = (double)gravado;
                ws.Cell(row, 6).Value = (double)isv;
                ws.Cell(row, 7).Value = (double)p.TotalAmount;
                ws.Cell(row, 8).Value = conFactura;

                for (int c = 5; c <= 7; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

                totalGravado += gravado;
                totalISV += isv;
                totalTotal += p.TotalAmount;

                row++;
            }

            ws.Cell(row, 1).Value = "TOTALES";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = (double)totalGravado;
            ws.Cell(row, 6).Value = (double)totalISV;
            ws.Cell(row, 7).Value = (double)totalTotal;
            for (int c = 5; c <= 7; c++)
            {
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, c).Style.Font.Bold = true;
            }
            for (int c = 1; c <= 8; c++)
                ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Libro_Compras_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
        }
    }
}
