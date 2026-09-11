using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services;

namespace hotel_erp.UnitTests;

public class EscPosServiceTests
{
    [Fact]
    public void BuildLines_DoesNotInventCashPaymentForPendingInvoice()
    {
        var invoice = new Invoice
        {
            CAI = new CAI
            {
                CAINumber = "CAI-FACTURA",
                InitialRange = "001-001-01-00000001",
                FinalRange = "001-001-01-00000100",
                DueDate = new DateOnly(2026, 12, 31)
            },
            CorrelativeNumber = "001-001-01-00000001",
            DocumentType = InvoiceDocumentType.Factura,
            CustomerName = "Cliente",
            SubTotal = 100m,
            ISV15Amount = 15m,
            ISVAmount = 15m,
            TotalAmount = 115m,
            PaymentMethod = null
        };

        var lines = new EscPosService().BuildLines(
            invoice,
            new BusinessSettings { PrintWidth = 46, ShowPayment = true },
            null,
            null,
            null,
            "ORIGINAL: CLIENTE",
            includePago: true);

        Assert.Contains("Condición de pago: Pendiente de cobro", lines);
        Assert.DoesNotContain(lines, line => line == "Pago: Efectivo");
    }

    [Fact]
    public void BuildLines_UsesFiscalSnapshotAndIdentifiesCreditNote()
    {
        var invoice = new Invoice
        {
            CAI = new CAI
            {
                CAINumber = "CAI-GENERAL-INCORECTO",
                InitialRange = "001-001-01-00000001",
                FinalRange = "001-001-01-00000100",
                DueDate = new DateOnly(2026, 12, 31)
            },
            CAINumberSnapshot = "CAI-NOTA-CREDITO",
            AuthorizationRangeSnapshot = "003-001-03-00000020 - 003-001-03-00000040",
            AuthorizationDueDateSnapshot = new DateTime(2027, 1, 31),
            CorrelativeNumber = "003-001-03-00000020",
            DocumentType = InvoiceDocumentType.NotaCredito,
            OriginalInvoiceId = Guid.NewGuid(),
            OriginalCorrelativeNumber = "001-001-01-00000005",
            Reason = "Devolución",
            CustomerName = "Cliente",
            SubTotal = 100m,
            ISV15Amount = 15m,
            ISV18Amount = 18m,
            ISVAmount = 33m,
            TotalAmount = 133m
        };

        var lines = new EscPosService().BuildLines(
            invoice,
            new BusinessSettings { PrintWidth = 46, ShowFiscal = true, ShowTotals = true },
            null,
            null,
            null,
            "ORIGINAL: CLIENTE",
            includePago: true);

        Assert.Contains(lines, line => line.Contains("NOTA DE CRÉDITO"));
        Assert.Contains("CAI: CAI-NOTA-CREDITO", lines);
        Assert.Contains(lines, line => line.Contains("003-001-03-00000020 al 003-001-03-00000040"));
        Assert.Contains(lines, line => line.Contains("Fecha Límite de Emisión: 31/01/2027"));
        Assert.Contains(lines, line => line.Contains("Documento afectado: 001-001-01-00000005"));
        Assert.Contains(lines, line => line.Contains("ISV 15%:"));
        Assert.Contains(lines, line => line.Contains("ISV 18%:"));
        Assert.DoesNotContain(lines, line => line.Contains("CAI-GENERAL-INCORECTO"));
        Assert.DoesNotContain(lines, line => line.StartsWith("Pago:"));
    }
}
