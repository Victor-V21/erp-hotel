namespace hotel_erp.Api.Database.Entities
{
    public enum PaymentMethod
    {
        Efectivo,
        Tarjeta,
        Transferencia
    }

    public enum PaymentStatus
    {
        Confirmado,
        Anulado,
        ParcialmenteReembolsado,
        Reembolsado
    }

    public enum RefundStatus
    {
        Confirmado,
        Anulado
    }

    public enum CardSettlementStatus
    {
        Confirmado,
        Anulado
    }

    public class Payment : BaseEntity
    {
        public string PaymentNumber { get; set; } = string.Empty;
        public Guid RecordedByUserId { get; set; }
        public User RecordedByUser { get; set; } = null!;
        public PaymentMethod Method { get; set; }
        public string Currency { get; set; } = "HNL";
        public decimal Amount { get; set; }
        public Nullable<decimal> CashReceived { get; set; }
        public Nullable<decimal> CashChange { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public PaymentStatus Status { get; set; } = PaymentStatus.Confirmado;
        public Nullable<Guid> CashRegisterId { get; set; }
        public CashRegister CashRegister { get; set; } = null!;
        public Nullable<Guid> AccountingEntryId { get; set; }
        public AccountingEntry AccountingEntry { get; set; } = null!;
        public ICollection<PaymentApplication> Applications { get; set; } = new List<PaymentApplication>();
        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
        public ICollection<CardSettlementApplication> CardSettlementApplications { get; set; } = new List<CardSettlementApplication>();
    }

    public class PaymentApplication : BaseEntity
    {
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class Refund : BaseEntity
    {
        public string RefundNumber { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
        public Guid RecordedByUserId { get; set; }
        public User RecordedByUser { get; set; } = null!;
        public PaymentMethod Method { get; set; }
        public string Currency { get; set; } = "HNL";
        public decimal Amount { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime RefundDate { get; set; } = DateTime.UtcNow;
        public RefundStatus Status { get; set; } = RefundStatus.Confirmado;
        public Guid? CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }
        public Guid? AccountingEntryId { get; set; }
        public AccountingEntry? AccountingEntry { get; set; }

        public ICollection<RefundApplication> Applications { get; set; } = new List<RefundApplication>();
    }

    public class RefundApplication : BaseEntity
    {
        public Guid RefundId { get; set; }
        public Refund Refund { get; set; } = null!;
        public Guid CreditNoteId { get; set; }
        public Invoice CreditNote { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class CardSettlement : BaseEntity
    {
        public string SettlementNumber { get; set; } = string.Empty;
        public Guid RecordedByUserId { get; set; }
        public User RecordedByUser { get; set; } = null!;
        public string Currency { get; set; } = "HNL";
        public decimal GrossAmount { get; set; }
        public decimal BankDepositAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal WithholdingAmount { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public DateOnly SettlementDate { get; set; }
        public CardSettlementStatus Status { get; set; } = CardSettlementStatus.Confirmado;
        public Guid? AccountingEntryId { get; set; }
        public AccountingEntry? AccountingEntry { get; set; }

        public ICollection<CardSettlementApplication> Applications { get; set; } = new List<CardSettlementApplication>();
    }

    public class CardSettlementApplication : BaseEntity
    {
        public Guid CardSettlementId { get; set; }
        public CardSettlement CardSettlement { get; set; } = null!;
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
