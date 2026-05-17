namespace hotel_erp.Application.DTOs
{
    public record CashRegisterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateCashRegisterRequest(string Name, string? Description);
    public record OpenCashRegisterRequest(Guid CashRegisterId, decimal InitialAmount);
    public record CloseCashRegisterRequest(Guid CashRegisterId, decimal ExpectedAmount, decimal CountedAmount, string? Notes);
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

    public record CreateCashMovementRequest(Guid CashRegisterId, string MovementType, decimal Amount, string? Description, Guid? ReferenceId);
}
