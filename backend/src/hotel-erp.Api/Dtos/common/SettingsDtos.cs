using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public record BusinessSettingsDto
    {
        public string BusinessName { get; set; } = "Hotel Maya Central";
        public string RTN { get; set; } = "08019012345678";
        public string Address { get; set; } = "Santa Rosa de Copán, Honduras";
        public string Phone { get; set; } = "9999-0000";
        public string Email { get; set; } = "info@hotelmayacentral.com";
        public string? LogoBase64 { get; set; }
        public string Footer { get; set; } = "¡Gracias por su preferencia!";
        public decimal IsvRate { get; set; } = 0.15m;
        public decimal TouristTaxRate { get; set; } = 0.04m;
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
        [StringLength(100, MinimumLength = 2)]
        public string? BusinessName { get; set; }
        [RegularExpression("^\\d{14}$")]
        public string? RTN { get; set; }
        [StringLength(250)]
        public string? Address { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }
        public string? LogoBase64 { get; set; }
        [StringLength(500)]
        public string? Footer { get; set; }
        [Range(0, 1)]
        public decimal? IsvRate { get; set; }
        [Range(0, 1)]
        public decimal? TouristTaxRate { get; set; }
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
}

