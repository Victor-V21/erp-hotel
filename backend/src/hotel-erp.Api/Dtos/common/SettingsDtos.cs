using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public record BusinessSettingsDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string RTN { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? LogoBase64 { get; set; }
        public string Footer { get; set; } = "¡Gracias por su preferencia!";
        public decimal IsvRate { get; set; }
        public decimal TouristTaxRate { get; set; }
        public string FiscalProfileStatus { get; set; } = "Borrador";
        public int FiscalProfileVersion { get; set; } = 1;
        public DateOnly? FiscalValidFrom { get; set; }
        public DateOnly? FiscalValidUntil { get; set; }
        public DateTime? FiscalApprovedAt { get; set; }
        public Guid? FiscalApprovedByUserId { get; set; }
        public string? FiscalApprovalNote { get; set; }
        public DateTime? FiscalRetiredAt { get; set; }
        public Guid? FiscalRetiredByUserId { get; set; }
        public string? FiscalRetirementReason { get; set; }
        public string? PrintPrinterName { get; set; }
        public int PrintWidth { get; set; } = 46;
        public int PrintLogoHeight { get; set; } = 40;
        public string PrintFontSize { get; set; } = "condensed";
        public int PrintLineSpacing { get; set; } = 1;
        public bool ShowLogo { get; set; } = true;
        public bool ShowHeader { get; set; } = true;
        public bool ShowFiscal { get; set; } = true;
        public bool ShowGuest { get; set; } = true;
        public bool ShowItems { get; set; } = true;
        public bool ShowTotals { get; set; } = true;
        public bool ShowPayment { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public string HeaderAlign { get; set; } = "center";
        public string SeparatorChar { get; set; } = "-";
        public int MarginLeft { get; set; } = 0;
    }

    public record UpdateBusinessSettingsRequest
    {
        [StringLength(100)]
        public string? BusinessName { get; set; }
        [RegularExpression("^\\d{14}$")]
        public string? RTN { get; set; }
        [StringLength(250)]
        public string? Address { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [StringLength(100), RegularExpression("^$|^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", ErrorMessage = "El correo electrónico no tiene un formato válido")]
        public string? Email { get; set; }
        public string? LogoBase64 { get; set; }
        [StringLength(500)]
        public string? Footer { get; set; }
        [Range(0, 1)]
        public decimal? IsvRate { get; set; }
        [Range(0, 1)]
        public decimal? TouristTaxRate { get; set; }
        public DateOnly? FiscalValidFrom { get; set; }
        public DateOnly? FiscalValidUntil { get; set; }
        [StringLength(100)]
        public string? PrintPrinterName { get; set; }
        [Range(28, 100)]
        public int? PrintWidth { get; set; }
        [Range(20, 200)]
        public int? PrintLogoHeight { get; set; }
        [RegularExpression("^(condensed|normal)$")]
        public string? PrintFontSize { get; set; }
        [Range(0, 2)]
        public int? PrintLineSpacing { get; set; }
        public bool? ShowLogo { get; set; }
        public bool? ShowHeader { get; set; }
        public bool? ShowFiscal { get; set; }
        public bool? ShowGuest { get; set; }
        public bool? ShowItems { get; set; }
        public bool? ShowTotals { get; set; }
        public bool? ShowPayment { get; set; }
        public bool? ShowFooter { get; set; }
        [RegularExpression("^(center|left)$")]
        public string? HeaderAlign { get; set; }
        [StringLength(1, MinimumLength = 1)]
        public string? SeparatorChar { get; set; }
        [Range(0, 8)]
        public int? MarginLeft { get; set; }
    }

    public record ApproveFiscalProfileRequest(
        [Required] DateOnly ValidFrom,
        DateOnly? ValidUntil,
        [Required, StringLength(500, MinimumLength = 10)] string ApprovalNote) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ValidUntil.HasValue && ValidUntil.Value < ValidFrom)
                yield return new ValidationResult("La vigencia final no puede ser anterior a la inicial", [nameof(ValidUntil)]);
        }
    }

    public record RetireFiscalProfileRequest(
        [Required, StringLength(500, MinimumLength = 10)] string Reason);
}
