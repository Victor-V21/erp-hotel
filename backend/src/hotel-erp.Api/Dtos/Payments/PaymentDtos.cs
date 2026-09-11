using System.ComponentModel.DataAnnotations;
using hotel_erp.Api.Dtos.Common;

namespace hotel_erp.Api.Dtos.Payments
{
    public record PaymentApplicationRequest(
        [NotEmptyGuid] Guid InvoiceId,
        [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount);

    public record CreatePaymentRequest
    {
        [Required, RegularExpression("^(Efectivo|Tarjeta|Transferencia)$")]
        public string Method { get; set; } = string.Empty;

        [Required, RegularExpression("^HNL$")]
        public string Currency { get; set; } = "HNL";

        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Amount { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? CashReceived { get; set; }

        public Guid? CashRegisterId { get; set; }

        [StringLength(100)]
        public string? ExternalReference { get; set; }

        [Required, MinLength(1)]
        public List<PaymentApplicationRequest> Applications { get; set; } = new();
    }

    public record PaymentApplicationDto
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public string CorrelativeNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public record RefundApplicationRequest(
        [NotEmptyGuid] Guid CreditNoteId,
        [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount);

    public record CreateRefundRequest
    {
        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Amount { get; set; }

        public Guid? CashRegisterId { get; set; }

        [StringLength(100)]
        public string? ExternalReference { get; set; }

        [Required, StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; } = string.Empty;

        [Required, MinLength(1)]
        public List<RefundApplicationRequest> Applications { get; set; } = new();
    }

    public record RefundApplicationDto
    {
        public Guid Id { get; set; }
        public Guid CreditNoteId { get; set; }
        public string CreditNoteCorrelativeNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public record RefundDto
    {
        public Guid Id { get; set; }
        public string RefundNumber { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public Guid RecordedByUserId { get; set; }
        public string RecordedByUserName { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Currency { get; set; } = "HNL";
        public decimal Amount { get; set; }
        public string? ExternalReference { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime RefundDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public Guid? AccountingEntryId { get; set; }
        public List<RefundApplicationDto> Applications { get; set; } = new();
    }

    public record PaymentDto
    {
        public Guid Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public Guid RecordedByUserId { get; set; }
        public string RecordedByUserName { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Currency { get; set; } = "HNL";
        public decimal Amount { get; set; }
        public decimal? CashReceived { get; set; }
        public decimal? CashChange { get; set; }
        public string? ExternalReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public Guid? AccountingEntryId { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal CardSettledAmount { get; set; }
        public List<PaymentApplicationDto> Applications { get; set; } = new();
        public List<RefundDto> Refunds { get; set; } = new();
    }

    public record CardSettlementApplicationRequest(
        [NotEmptyGuid] Guid PaymentId,
        [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Amount);

    public record CreateCardSettlementRequest
    {
        [Required, RegularExpression("^HNL$")]
        public string Currency { get; set; } = "HNL";

        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal GrossAmount { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal BankDepositAmount { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal CommissionAmount { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal WithholdingAmount { get; set; }

        [Required, StringLength(100, MinimumLength = 3)]
        public string ExternalReference { get; set; } = string.Empty;

        public DateOnly SettlementDate { get; set; }

        [Required, MinLength(1)]
        public List<CardSettlementApplicationRequest> Applications { get; set; } = new();
    }

    public record CardSettlementApplicationDto
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public record CardSettlementDto
    {
        public Guid Id { get; set; }
        public string SettlementNumber { get; set; } = string.Empty;
        public Guid RecordedByUserId { get; set; }
        public string RecordedByUserName { get; set; } = string.Empty;
        public string Currency { get; set; } = "HNL";
        public decimal GrossAmount { get; set; }
        public decimal BankDepositAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal WithholdingAmount { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public DateOnly SettlementDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? AccountingEntryId { get; set; }
        public List<CardSettlementApplicationDto> Applications { get; set; } = new();
    }

    public record EligibleCardPaymentDto
    {
        public Guid Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal AvailableAmount { get; set; }
    }
}
