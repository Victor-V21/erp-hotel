using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? RTN { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    }

    public class PurchaseInvoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public string? CAINumber { get; set; }
        public string? SupplierRTN { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ISVAmount { get; set; }
        public decimal ISV15Amount { get; set; }
        public decimal ISV18Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Pendiente;
        public string? Notes { get; set; }

        public ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new List<PurchaseInvoiceItem>();
    }

    public class PurchaseInvoiceItem : BaseEntity
    {
        public Guid PurchaseInvoiceId { get; set; }
        public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; } = 0.15m;
    }
}


