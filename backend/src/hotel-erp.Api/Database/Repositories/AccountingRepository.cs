using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services.Interfaces;

namespace hotel_erp.Api.Database.Repositories
{
    public class AccountingRepository : IAccountingRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AccountingAccount>> GetAllAccountsAsync()
        {
            return await _context.AccountingAccounts
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<AccountingAccount?> GetAccountByIdAsync(Guid id)
        {
            return await _context.AccountingAccounts
                .Include(a => a.ChildAccounts)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<AccountingEntry>> GetAllEntriesAsync()
        {
            return await _context.AccountingEntries
                .Include(e => e.EntryItems)
                    .ThenInclude(ei => ei.Account)
                .OrderByDescending(e => e.TransactionDate)
                .ThenByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<AccountingEntry?> GetEntryByIdAsync(Guid id)
        {
            return await _context.AccountingEntries
                .Include(e => e.EntryItems)
                    .ThenInclude(ei => ei.Account)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddAccountAsync(AccountingAccount account)
        {
            await _context.AccountingAccounts.AddAsync(account);
            await _context.SaveChangesAsync();
        }

        public async Task AddEntryAsync(AccountingEntry entry)
        {
            await _context.AccountingEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetAccountBalanceAsync(Guid accountId)
        {
            var account = await _context.AccountingAccounts.FindAsync(accountId);
            if (account == null) return 0;

            var entries = await _context.EntryItems
                .Where(ei => ei.AccountId == accountId)
                .ToListAsync();

            var totalDebit = entries.Sum(e => e.Debit);
            var totalCredit = entries.Sum(e => e.Credit);

            return account.AccountType switch
            {
                AccountType.Activo or AccountType.Gasto => totalDebit - totalCredit,
                _ => totalCredit - totalDebit,
            };
        }

        public async Task<Dictionary<Guid, decimal>> GetAllAccountBalancesAsync()
        {
            var accounts = await _context.AccountingAccounts.AsNoTracking().ToListAsync();
            var allItems = await _context.EntryItems.AsNoTracking()
                .GroupBy(ei => ei.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    TotalDebit = g.Sum(ei => ei.Debit),
                    TotalCredit = g.Sum(ei => ei.Credit)
                })
                .ToListAsync();

            var itemLookup = allItems.ToDictionary(x => x.AccountId, x => (x.TotalDebit, x.TotalCredit));
            var balances = new Dictionary<Guid, decimal>();

            foreach (var account in accounts)
            {
                if (!itemLookup.TryGetValue(account.Id, out var totals))
                {
                    balances[account.Id] = 0;
                    continue;
                }

                balances[account.Id] = account.AccountType switch
                {
                    AccountType.Activo or AccountType.Gasto => totals.TotalDebit - totals.TotalCredit,
                    _ => totals.TotalCredit - totals.TotalDebit,
                };
            }

            return balances;
        }

        public async Task<List<EntryItem>> GetAccountMovementsAsync(Guid accountId)
        {
            return await _context.EntryItems
                .Include(ei => ei.Entry)
                .Where(ei => ei.AccountId == accountId && !ei.Entry.IsDeleted)
                .OrderBy(ei => ei.Entry.TransactionDate)
                .ThenBy(ei => ei.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, (decimal Debit, decimal Credit)>> GetAccountTotalsAsync()
        {
            var allItems = await _context.EntryItems.AsNoTracking()
                .Where(ei => !ei.Entry.IsDeleted)
                .GroupBy(ei => ei.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    TotalDebit = g.Sum(ei => ei.Debit),
                    TotalCredit = g.Sum(ei => ei.Credit)
                })
                .ToListAsync();

            return allItems.ToDictionary(x => x.AccountId, x => (x.TotalDebit, x.TotalCredit));
        }

        public async Task UpdateAccountAsync(AccountingAccount account)
        {
            _context.AccountingAccounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(AccountingAccount account)
        {
            account.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AccountNumberExistsAsync(string number)
        {
            return await _context.AccountingAccounts.AnyAsync(a => a.AccountNumber == number);
        }

        public async Task DeleteAsync(Guid entryId)
        {
            var entry = await _context.AccountingEntries.Include(e => e.EntryItems).FirstOrDefaultAsync(e => e.Id == entryId);
            if (entry != null)
            {
                entry.IsDeleted = true;
                foreach(var item in entry.EntryItems)
                {
                    item.IsDeleted = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
