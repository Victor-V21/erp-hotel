using hotel_erp.Api.Dtos.Common;
using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Cash
{
    public record CashRegisterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsOpen { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? LastMovementDate { get; set; }
    }

    public record CreateCashRegisterRequest(
        [Required, StringLength(50, MinimumLength = 2)] string Name,
        [StringLength(250)] string? Description);

    public record OpenCashRegisterRequest(
        [Range(0, 999999.99)] decimal InitialAmount);

    public record CloseCashRegisterRequest(
        [Range(0, 999999.99)] decimal CountedAmount,
        [StringLength(500)] string? Notes);

    public record CashRegisterOperationDto
    {
        public Guid MovementId { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? CountedAmount { get; set; }
        public decimal? Difference { get; set; }
    }
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
        public Guid? ReferenceId { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? CountedAmount { get; set; }
        public decimal? Difference { get; set; }
        public string? Notes { get; set; }
    }

    public record CreateCashMovementRequest(
        [NotEmptyGuid] Guid CashRegisterId,
        [Required, RegularExpression("^(Apertura|Cierre|Ingreso|Egreso|Arqueo)$")] string MovementType,
        [Range(0.01, 999999.99)] decimal Amount,
        [StringLength(250)] string? Description,
        Guid? ReferenceId);
}
