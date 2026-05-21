using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class AccountingAccount : BaseEntity
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public Guid? ParentAccountId { get; set; }
        public AccountingAccount? ParentAccount { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<AccountingAccount> ChildAccounts { get; set; } = new List<AccountingAccount>();
        public ICollection<EntryItem> EntryItems { get; set; } = new List<EntryItem>();
    }

    public class AccountingEntry : BaseEntity
    {
        public DateOnly TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public EntryType EntryType { get; set; } = EntryType.Diario;
        public Guid? ReferenceId { get; set; }

        public ICollection<EntryItem> EntryItems { get; set; } = new List<EntryItem>();
    }

    public class EntryItem : BaseEntity
    {
        public Guid EntryId { get; set; }
        public AccountingEntry Entry { get; set; } = null!;
        public Guid AccountId { get; set; }
        public AccountingAccount Account { get; set; } = null!;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
    }
}


