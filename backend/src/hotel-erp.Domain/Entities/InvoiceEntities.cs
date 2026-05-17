using hotel_erp.Domain.Common;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Domain.Entities
{
    public class CAI : BaseEntity
    {
        public string CAINumber { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }
        public string InitialRange { get; set; } = string.Empty;
        public string FinalRange { get; set; } = string.Empty;
        public string CurrentCorrelative { get; set; } = string.Empty;
        public CAIStatus Status { get; set; } = CAIStatus.Activo;

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    public class Invoice : BaseEntity
    {
        public Guid CAIId { get; set; }
        public CAI CAI { get; set; } = null!;
        public string CorrelativeNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public Guid? GuestId { get; set; }
        public Guest? Guest { get; set; }
        public string? RTNCliente { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ISVAmount { get; set; }
        public decimal TouristTaxAmount { get; set; }
        public decimal DiscountsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceDocumentType DocumentType { get; set; } = InvoiceDocumentType.Factura;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Emitida;

        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

        public string? PaymentMethod { get; set; }
        public decimal? CashReceived { get; set; }
        public decimal? CashChange { get; set; }
    }

    public class InvoiceItem : BaseEntity
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; } = 0.15m;
        public bool IsTouristTaxable { get; set; }
        public decimal DiscountPercentage { get; set; }
    }

    public class TaxConfiguration : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ApplicableTo { get; set; }
    }
}
