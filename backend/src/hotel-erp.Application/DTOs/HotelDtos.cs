namespace hotel_erp.Application.DTOs
{
    public record RoomTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
    }

    public record CreateRoomTypeRequest(string Name, string? Description, decimal PricePerNight, int Capacity);
    public record UpdateRoomTypeRequest(string? Name, string? Description, decimal? PricePerNight, int? Capacity);

    public record RoomDto
    {
        public Guid Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Observations { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
    }

    public record CreateRoomRequest(string RoomNumber, int Floor, Guid RoomTypeId, string? Observations);
    public record UpdateRoomRequest(string? RoomNumber, int? Floor, Guid? RoomTypeId, string? Status, string? Observations);

    public record ReservationDto
    {
        public Guid Id { get; set; }
        public Guid GuestId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal AdvancePayment { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public record CreateReservationRequest(Guid GuestId, Guid RoomId, DateOnly CheckInDate, DateOnly CheckOutDate, int Adults, int Children, string? PaymentMethod, decimal AdvancePayment, string? Notes);
    public record UpdateReservationRequest(Guid? RoomId, DateOnly? CheckInDate, DateOnly? CheckOutDate, int? Adults, int? Children, string? PaymentMethod, decimal? AdvancePayment, string? Notes);

    public record CheckInRequest(Guid ReservationId, Guid RoomId, List<Guid>? DiscountIds = null, string? PaymentMethod = null, decimal? CashReceived = null, decimal? CashChange = null);
    public record CheckOutRequest(Guid ReservationId, decimal? DiscountPercentage, string? DiscountReason);

    public record FolioDto
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public Guid GuestId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime OpeningDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<FolioItemDto> Items { get; set; } = new();
    }

    public record FolioItemDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsExempt { get; set; }
        public decimal ISVRate { get; set; }
        public bool IsTouristTaxable { get; set; }
        public decimal DiscountPercentage { get; set; }
    }

    public record AddFolioItemRequest(Guid FolioId, string Description, int Quantity, decimal UnitPrice, bool IsExempt, decimal ISVRate, bool IsTouristTaxable, decimal DiscountPercentage);
}
