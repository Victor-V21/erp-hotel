namespace hotel_erp.Api.Database.Entities
{
    public class Reservation : BaseEntity
    {
        public Guid GuestId { get; set; }
        public Guest Guest { get; set; } = null!;
        public Guid RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal AdvancePayment { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Pendiente;
        public string? Notes { get; set; }
        public int Version { get; set; } = 1;

        public Folio? Folio { get; set; }
    }

    public class Folio : BaseEntity
    {
        public Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;
        public Guid GuestId { get; set; }
        public Guest Guest { get; set; } = null!;
        public Guid RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public DateTime OpeningDate { get; set; } = DateTime.UtcNow;
        public DateTime? ClosingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public FolioStatus Status { get; set; } = FolioStatus.Abierto;

        public ICollection<FolioItem> FolioItems { get; set; } = new List<FolioItem>();
        public Invoice? SettlementInvoice { get; set; }
    }

    public class FolioItem : BaseEntity
    {
        public Guid FolioId { get; set; }
        public Folio Folio { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; } = 0.15m;
        public bool IsTouristTaxable { get; set; }
        public decimal DiscountPercentage { get; set; }

        public InvoiceItem? InvoiceItem { get; set; }
    }
}
