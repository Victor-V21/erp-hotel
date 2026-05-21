using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public record SupplierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RTN { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateSupplierRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [RegularExpression("^\\d{14}$")] string? RTN,
        [StringLength(100)] string? ContactPerson,
        [StringLength(20)] string? Phone,
        [EmailAddress, StringLength(100)] string? Email,
        [StringLength(250)] string? Address);

    public record UpdateSupplierRequest(
        [StringLength(100, MinimumLength = 2)] string? Name,
        [RegularExpression("^\\d{14}$")] string? RTN,
        [StringLength(100)] string? ContactPerson,
        [StringLength(20)] string? Phone,
        [EmailAddress, StringLength(100)] string? Email,
        [StringLength(250)] string? Address,
        bool? IsActive = null);

    public record PurchaseInvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string? CAINumber { get; set; }
        public string? SupplierRTN { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ISVAmount { get; set; }
        public decimal ISV15Amount { get; set; }
        public decimal ISV18Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public record PurchaseInvoiceItemDto
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; }
    }

    public record CreatePurchaseInvoiceRequest(
        [Required, StringLength(50)] string InvoiceNumber,
        [Required] Guid SupplierId,
        [Required] DateTime InvoiceDate,
        [StringLength(50)] string? CAINumber,
        [RegularExpression("^\\d{14}$")] string? SupplierRTN,
        [StringLength(500)] string? Notes,
        [Required, MinLength(1)] List<PurchaseInvoiceItemDto> Items);
}

