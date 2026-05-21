using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Common
{
    public record RoomTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
    }

    public record CreateRoomTypeRequest(
        [Required, StringLength(50, MinimumLength = 2)] string Name,
        [StringLength(250)] string? Description,
        [Range(0.01, 999999.99)] decimal PricePerNight,
        [Range(1, 50)] int Capacity);

    public record UpdateRoomTypeRequest(
        [StringLength(50, MinimumLength = 2)] string? Name,
        [StringLength(250)] string? Description,
        [Range(0.01, 999999.99)] decimal? PricePerNight,
        [Range(1, 50)] int? Capacity);

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

    public record CreateRoomRequest(
        [Required, StringLength(10, MinimumLength = 1)] string RoomNumber,
        [Range(0, 200)] int Floor,
        [NotEmptyGuid] Guid RoomTypeId,
        [StringLength(500)] string? Observations);

    public record UpdateRoomRequest(
        [StringLength(10, MinimumLength = 1)] string? RoomNumber,
        [Range(0, 200)] int? Floor,
        Guid? RoomTypeId,
        [RegularExpression("^(Libre|Ocupada|Limpieza|Mantenimiento|Reservada|Bloqueada)$")] string? Status,
        [StringLength(500)] string? Observations);

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

    public record CreateReservationRequest(
        [NotEmptyGuid] Guid GuestId,
        [NotEmptyGuid] Guid RoomId,
        [Required] DateOnly CheckInDate,
        [Required] DateOnly CheckOutDate,
        [Range(1, 20)] int Adults,
        [Range(0, 20)] int Children,
        [RegularExpression("^(Efectivo|Tarjeta|Transferencia)$")] string? PaymentMethod,
        [Range(0, 999999.99)] decimal AdvancePayment,
        [StringLength(1000)] string? Notes) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CheckOutDate <= CheckInDate)
                yield return new ValidationResult("La fecha de salida debe ser posterior a la fecha de entrada", new[] { nameof(CheckOutDate) });
        }
    }

    public record UpdateReservationRequest(
        Guid? RoomId,
        DateOnly? CheckInDate,
        DateOnly? CheckOutDate,
        [Range(1, 20)] int? Adults,
        [Range(0, 20)] int? Children,
        [RegularExpression("^(Efectivo|Tarjeta|Transferencia)$")] string? PaymentMethod,
        [Range(0, 999999.99)] decimal? AdvancePayment,
        [StringLength(1000)] string? Notes) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CheckInDate.HasValue && CheckOutDate.HasValue && CheckOutDate <= CheckInDate)
                yield return new ValidationResult("La fecha de salida debe ser posterior a la fecha de entrada", new[] { nameof(CheckOutDate) });
        }
    }

    public record CheckInRequest(
        [NotEmptyGuid] Guid ReservationId,
        [NotEmptyGuid] Guid RoomId,
        List<Guid>? DiscountIds = null,
        [RegularExpression("^(Efectivo|Tarjeta|Transferencia)$")] string? PaymentMethod = null,
        [Range(0, 999999.99)] decimal? CashReceived = null,
        [Range(0, 999999.99)] decimal? CashChange = null);

    public record CheckOutRequest(
        [NotEmptyGuid] Guid ReservationId,
        [Range(0, 100)] decimal? DiscountPercentage,
        [StringLength(250)] string? DiscountReason);

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

    public record AddFolioItemRequest(
        [NotEmptyGuid] Guid FolioId,
        [Required, StringLength(250, MinimumLength = 2)] string Description,
        [Range(1, 1000)] int Quantity,
        [Range(0, 999999.99)] decimal UnitPrice,
        bool IsExempt,
        [Range(0, 1)] decimal ISVRate,
        bool IsTouristTaxable,
        [Range(0, 100)] decimal DiscountPercentage);
}

