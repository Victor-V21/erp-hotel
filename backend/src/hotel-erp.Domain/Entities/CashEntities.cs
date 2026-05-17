using hotel_erp.Domain.Common;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Domain.Entities
{
    public class CashRegister : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
    }

    public class CashMovement : BaseEntity
    {
        public Guid CashRegisterId { get; set; }
        public CashRegister CashRegister { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public CashMovementType MovementType { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public decimal BalanceAfter { get; set; }
        public Guid? ReferenceId { get; set; }
    }
}
