namespace hotel_erp.Application.DTOs
{
    public record CAIDto
    {
        public Guid Id { get; set; }
        public string CAINumber { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }
        public string InitialRange { get; set; } = string.Empty;
        public string FinalRange { get; set; } = string.Empty;
        public string CurrentCorrelative { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsExpiringSoon { get; set; }
    }

    public record CreateCAIRequest(string CAINumber, DateOnly IssueDate, DateOnly DueDate, string InitialRange, string FinalRange);

    public record InvoiceDto
    {
        public Guid Id { get; set; }
        public Guid CAIId { get; set; }
        public string CAINumber { get; set; } = string.Empty;
        public string CorrelativeNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? RTNCliente { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ISVAmount { get; set; }
        public decimal TouristTaxAmount { get; set; }
        public decimal DiscountsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public record InvoiceItemDto
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; }
        public bool IsTouristTaxable { get; set; }
        public decimal DiscountPercentage { get; set; }
    }

    public record CreateInvoiceRequest
    {
        public Guid CAIId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? GuestId { get; set; }
        public string? RTNCliente { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string DocumentType { get; set; } = "Factura";
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public record TaxConfigurationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public bool IsActive { get; set; }
        public string? ApplicableTo { get; set; }
    }

    public record CreateTaxConfigurationRequest(string Name, decimal Rate, bool IsActive, string? ApplicableTo);
}
