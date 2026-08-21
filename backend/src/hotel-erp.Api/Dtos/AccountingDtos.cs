using System.ComponentModel.DataAnnotations;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Dtos.Accounting
{
    public record AccountingAccountDto
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public Guid? ParentAccountId { get; set; }
        public bool IsActive { get; set; }
        public decimal Balance { get; set; }
        public List<AccountingAccountDto> Children { get; set; } = new();
    }

    public record CreateAccountRequest(
        [Required, StringLength(20, MinimumLength = 2)] string AccountNumber,
        [Required, StringLength(100, MinimumLength = 2)] string AccountName,
        [Required] string AccountType,
        Guid? ParentAccountId = null);

    public record UpdateAccountRequest(
        [StringLength(100, MinimumLength = 2)] string? AccountName,
        string? AccountType,
        bool? IsActive);

    public record EntryItemDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
    }

    public record AccountingEntryDto
    {
        public Guid Id { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string EntryType { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<EntryItemDto> Items { get; set; } = new();
    }

    public record CreateEntryItemRequest(
        [Required] Guid AccountId,
        [Range(0, 10000000)] decimal Debit,
        [Range(0, 10000000)] decimal Credit,
        string? Description);

    public record CreateAccountingEntryRequest(
        [Required] DateOnly TransactionDate,
        [Required, StringLength(250, MinimumLength = 3)] string Description,
        string? EntryType,
        [Required, MinLength(2)] List<CreateEntryItemRequest> Items);

    public record TrialBalanceItem

    {
        public Guid AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal PreviousBalance { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public record AccountMovementDto
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
    }
}
