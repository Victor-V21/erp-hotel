namespace hotel_erp.Application.DTOs
{
    public record BusinessSettingsDto
    {
        public string BusinessName { get; set; } = "Hotel ERP Honduras";
        public string RTN { get; set; } = "08019012345678";
        public string Address { get; set; } = "Santa Rosa de Copán, Honduras";
        public string Phone { get; set; } = "9999-0000";
        public string Email { get; set; } = "info@hotelerp.com";
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
        public string? BusinessName { get; set; }
        public string? RTN { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LogoBase64 { get; set; }
        public string? Footer { get; set; }
        public decimal? IsvRate { get; set; }
        public decimal? TouristTaxRate { get; set; }
        public string? PrintPrinterName { get; set; }
        public int? PrintWidth { get; set; }
        public int? PrintLogoHeight { get; set; }
        public string? PrintFontSize { get; set; }
        public int? PrintLineSpacing { get; set; }
        public bool? ShowLogo { get; set; }
        public bool? ShowHeader { get; set; }
        public bool? ShowFiscal { get; set; }
        public bool? ShowGuest { get; set; }
        public bool? ShowItems { get; set; }
        public bool? ShowTotals { get; set; }
        public bool? ShowPayment { get; set; }
        public bool? ShowFooter { get; set; }
        public string? HeaderAlign { get; set; }
        public string? SeparatorChar { get; set; }
        public int? MarginLeft { get; set; }
    }
}
