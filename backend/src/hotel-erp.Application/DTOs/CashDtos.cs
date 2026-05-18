using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Application.DTOs
{
    public record CashRegisterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateCashRegisterRequest(
        [Required, StringLength(50, MinimumLength = 2)] string Name,
        [StringLength(250)] string? Description);

    public record OpenCashRegisterRequest(
        [NotEmptyGuid] Guid CashRegisterId,
        [Range(0, 999999.99)] decimal InitialAmount);

    public record CloseCashRegisterRequest(
        [NotEmptyGuid] Guid CashRegisterId,
        [Range(0, 999999.99)] decimal ExpectedAmount,
        [Range(0, 999999.99)] decimal CountedAmount,
        [StringLength(500)] string? Notes);
    public record CashMovementDto
    {
        public Guid Id { get; set; }
        public Guid CashRegisterId { get; set; }
        public string CashRegisterName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime MovementDate { get; set; }
        public decimal BalanceAfter { get; set; }
    }

    public record CreateCashMovementRequest(
        [NotEmptyGuid] Guid CashRegisterId,
        [Required, RegularExpression("^(Apertura|Cierre|Ingreso|Egreso|Arqueo)$")] string MovementType,
        [Range(0.01, 999999.99)] decimal Amount,
        [StringLength(250)] string? Description,
        Guid? ReferenceId);
}
