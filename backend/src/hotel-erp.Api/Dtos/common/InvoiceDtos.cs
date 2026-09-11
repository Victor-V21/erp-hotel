using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
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

    public record DocumentAuthorizationDto
    {
        public Guid Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string CAINumber { get; set; } = string.Empty;
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }
        public string InitialRange { get; set; } = string.Empty;
        public string FinalRange { get; set; } = string.Empty;
        public string CurrentCorrelative { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsExpiringSoon { get; set; }
        public bool HasAttachment { get; set; }
    }

    public record CreateDocumentAuthorizationRequest(
        [Required, RegularExpression("^(Factura|NotaCredito|NotaDebito)$")] string DocumentType,
        [Required, StringLength(50, MinimumLength = 10)] string CAINumber,
        [Required] DateOnly IssueDate,
        [Required] DateOnly DueDate,
        [Required, RegularExpression("^\\d{3}-\\d{3}-\\d{2}-\\d{8}$")] string InitialRange,
        [Required, RegularExpression("^\\d{3}-\\d{3}-\\d{2}-\\d{8}$")] string FinalRange) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate <= IssueDate)
                yield return new ValidationResult("La fecha de vencimiento debe ser posterior a la fecha de emisión", new[] { nameof(DueDate) });

            if (string.CompareOrdinal(FinalRange, InitialRange) < 0)
                yield return new ValidationResult("El rango final debe ser mayor o igual al rango inicial", new[] { nameof(FinalRange) });
        }
    }

    public record CreateCAIRequest(
        [Required, StringLength(50, MinimumLength = 10)] string CAINumber,
        [Required] DateOnly IssueDate,
        [Required] DateOnly DueDate,
        [Required, RegularExpression("^\\d{3}-\\d{3}-\\d{2}-\\d{8}$")] string InitialRange,
        [Required, RegularExpression("^\\d{3}-\\d{3}-\\d{2}-\\d{8}$")] string FinalRange) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate <= IssueDate)
                yield return new ValidationResult("La fecha de vencimiento debe ser posterior a la fecha de emisión", new[] { nameof(DueDate) });

            if (string.CompareOrdinal(FinalRange, InitialRange) < 0)
                yield return new ValidationResult("El rango final debe ser mayor o igual al rango inicial", new[] { nameof(FinalRange) });
        }
    }

    public record InvoiceDto
    {
        public Guid Id { get; set; }
        public Guid CAIId { get; set; }
        public Guid? DocumentAuthorizationId { get; set; }
        public string CAINumber { get; set; } = string.Empty;
        public string? CAINumberSnapshot { get; set; }
        public string? AuthorizationRangeSnapshot { get; set; }
        public DateTime? AuthorizationDueDateSnapshot { get; set; }
        public string CorrelativeNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? GuestId { get; set; }
        public Guid? FolioId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? RTNCliente { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ISVAmount { get; set; }
        public decimal ISV15Amount { get; set; }
        public decimal ISV18Amount { get; set; }
        public decimal TouristTaxAmount { get; set; }
        public decimal DiscountsAmount { get; set; }
        public Guid? AppliedDiscountId { get; set; }
        public string? AppliedDiscountNameSnapshot { get; set; }
        public decimal? AppliedDiscountPercentageSnapshot { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal CreditedAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal ExemptAmount { get; set; }
        public decimal ExoneratedAmount { get; set; }
        public string TaxpayerType { get; set; } = string.Empty;
        public string? ExonerationOrderNumber { get; set; }
        public string? SefinExonerationCertificateNumber { get; set; }
        public string? SagRegistryNumber { get; set; }
        public bool IsIsvExempt { get; set; }
        public bool IsTouristTaxExempt { get; set; }
        public Guid? OriginalInvoiceId { get; set; }
        public string? OriginalCorrelativeNumber { get; set; }
        public string? Reason { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public decimal? CashReceived { get; set; }
        public decimal? CashChange { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public record InvoiceItemDto
    {
        public Guid Id { get; set; }
        public Guid? OriginalInvoiceItemId { get; set; }
        public Guid? FolioItemId { get; set; }
        [Required, StringLength(300)]
        public string Description { get; set; } = string.Empty;
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        [Range(typeof(decimal), "0", "999999999")]
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        [Range(typeof(decimal), "0", "1")]
        public decimal ISVRate { get; set; }
        public bool IsTouristTaxable { get; set; }
        [Range(typeof(decimal), "0", "100")]
        public decimal DiscountPercentage { get; set; }
    }

    public record CreateInvoiceRequest
    {
        public Guid CAIId { get; set; }
        public Guid? DocumentAuthorizationId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? GuestId { get; set; }
        public Guid? FolioId { get; set; }
        public Guid? DiscountId { get; set; }
        public Guid? CashRegisterId { get; set; }
        [Range(typeof(decimal), "0", "999999999")]
        public decimal? CashReceived { get; set; }
        [RegularExpression("^\\d{14}$", ErrorMessage = "El RTN debe tener 14 dígitos numéricos")]
        public string? RTNCliente { get; set; }
        [Required, StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;
        [StringLength(300)]
        public string? CustomerAddress { get; set; }
        [RegularExpression("^(Efectivo|Tarjeta|Transferencia)$")]
        public string PaymentMethod { get; set; } = "Efectivo";
        [StringLength(100)]
        public string? PaymentReference { get; set; }
        [RegularExpression("^Factura$")]
        public string DocumentType { get; set; } = "Factura";
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")]
        public string? TaxpayerType { get; set; }
        [StringLength(50)]
        public string? ExonerationOrderNumber { get; set; }
        [StringLength(50)]
        public string? SefinExonerationCertificateNumber { get; set; }
        [StringLength(50)]
        public string? SagRegistryNumber { get; set; }
        public bool IsIsvExempt { get; set; }
        public bool IsTouristTaxExempt { get; set; }
        public Guid? OriginalInvoiceId { get; set; }
        [StringLength(250)]
        public string? Reason { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public record CreateCreditNoteRequest(
        [Required, NotEmptyGuid] Guid CAIId,
        [Required, NotEmptyGuid] Guid? DocumentAuthorizationId,
        [Required, NotEmptyGuid] Guid OriginalInvoiceId,
        [Required, StringLength(250)]
        string Reason,
        [Required, MinLength(1)]
        List<CreditNoteItemRequest> Items)
    {
        public Guid? GuestId { get; set; }
        public string DocumentType { get; set; } = "NotaCredito";
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")]
        public string? TaxpayerType { get; set; }
    }

    public record CreateDebitNoteRequest(
        [Required, NotEmptyGuid] Guid CAIId,
        [Required, NotEmptyGuid] Guid? DocumentAuthorizationId,
        [Required, NotEmptyGuid] Guid OriginalInvoiceId,
        [Required, StringLength(250)]
        string Reason,
        [Required, MinLength(1)]
        List<InvoiceItemDto> Items)
    {
        public Guid? GuestId { get; set; }
        public string DocumentType { get; set; } = "NotaDebito";
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")]
        public string? TaxpayerType { get; set; }
    }

    public record CreditNoteItemRequest(
        [Required, NotEmptyGuid] Guid OriginalInvoiceItemId,
        [Range(1, int.MaxValue)] int Quantity);

    public record TaxConfigurationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public bool IsActive { get; set; }
        public string? ApplicableTo { get; set; }
    }

    public record CreateTaxConfigurationRequest(
        [Required, StringLength(50, MinimumLength = 2)] string Name,
        [Range(0, 1)] decimal Rate,
        bool IsActive,
        [StringLength(100)] string? ApplicableTo);
}
