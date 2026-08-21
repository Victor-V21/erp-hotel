using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class BusinessSettings : BaseEntity
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
        // Secciones visibles
        public bool ShowLogo { get; set; } = true;
        public bool ShowHeader { get; set; } = true;
        public bool ShowFiscal { get; set; } = true;
        public bool ShowGuest { get; set; } = true;
        public bool ShowItems { get; set; } = true;
        public bool ShowTotals { get; set; } = true;
        public bool ShowPayment { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        // Estilo
        public string HeaderAlign { get; set; } = "center";
        public string SeparatorChar { get; set; } = "-";
        public int MarginLeft { get; set; } = 0;
    }
}


