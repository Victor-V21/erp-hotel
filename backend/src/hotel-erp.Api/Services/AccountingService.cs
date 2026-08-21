using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services
{
    public class AccountingService : IAccountingService
    {
        private readonly ApplicationDbContext _context;

        public AccountingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateInvoiceEntryAsync(Invoice invoice)
        {
            if (await _context.AccountingEntries.AnyAsync(e => e.ReferenceId == invoice.Id))
                return;

            var paymentAccount = await GetOrCreateAccountAsync(
                invoice.PaymentMethod == "Transferencia" || invoice.PaymentMethod == "Tarjeta" ? "1102" : "1101",
                invoice.PaymentMethod == "Transferencia" || invoice.PaymentMethod == "Tarjeta" ? "Banco" : "Caja",
                AccountType.Activo);
            var incomeAccount = await GetOrCreateAccountAsync("4101", "Ingresos por hospedaje", AccountType.Ingreso);
            var isvAccount = await GetOrCreateAccountAsync("2101", "ISV por pagar", AccountType.Pasivo);
            var touristTaxAccount = await GetOrCreateAccountAsync("2102", "Tasa turística por pagar", AccountType.Pasivo);

            var entry = new AccountingEntry
            {
                TransactionDate = DateOnly.FromDateTime(invoice.InvoiceDate),
                Description = $"Factura {invoice.CorrelativeNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = invoice.Id
            };

            entry.EntryItems.Add(new EntryItem
            {
                AccountId = paymentAccount.Id,
                Debit = invoice.TotalAmount,
                Credit = 0,
                Description = $"Cobro factura {invoice.CorrelativeNumber}"
            });

            entry.EntryItems.Add(new EntryItem
            {
                AccountId = incomeAccount.Id,
                Debit = 0,
                Credit = invoice.SubTotal,
                Description = "Ingreso neto por hospedaje"
            });

            if (invoice.ISVAmount > 0)
            {
                entry.EntryItems.Add(new EntryItem
                {
                    AccountId = isvAccount.Id,
                    Debit = 0,
                    Credit = invoice.ISVAmount,
                    Description = "ISV por pagar"
                });
            }

            if (invoice.TouristTaxAmount > 0)
            {
                entry.EntryItems.Add(new EntryItem
                {
                    AccountId = touristTaxAccount.Id,
                    Debit = 0,
                    Credit = invoice.TouristTaxAmount,
                    Description = "Tasa turística por pagar"
                });
            }

            var debit = entry.EntryItems.Sum(i => i.Debit);
            var credit = entry.EntryItems.Sum(i => i.Credit);
            if (debit != credit)
                throw new InvalidOperationException($"Asiento contable descuadrado para factura {invoice.CorrelativeNumber}");

            await _context.AccountingEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        private async Task<AccountingAccount> GetOrCreateAccountAsync(string number, string name, AccountType type)
        {
            var account = await _context.AccountingAccounts.FirstOrDefaultAsync(a => a.AccountNumber == number);
            if (account != null) return account;

            account = new AccountingAccount
            {
                AccountNumber = number,
                AccountName = name,
                AccountType = type,
                IsActive = true
            };
            await _context.AccountingAccounts.AddAsync(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task CreatePurchaseEntryAsync(PurchaseInvoice purchaseInvoice)
        {
            if (await _context.AccountingEntries.AnyAsync(e => e.ReferenceId == purchaseInvoice.Id))
                return;

            var expenseAccount = await GetOrCreateAccountAsync("5109", "Gastos varios", AccountType.Gasto);
            var isvAccount = await GetOrCreateAccountAsync("2101", "ISV por pagar", AccountType.Pasivo);
            var supplierAccount = await GetOrCreateAccountAsync("2103", "Proveedores", AccountType.Pasivo);

            var entry = new AccountingEntry
            {
                TransactionDate = DateOnly.FromDateTime(purchaseInvoice.InvoiceDate),
                Description = $"Compra {purchaseInvoice.InvoiceNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = purchaseInvoice.Id
            };

            entry.EntryItems.Add(new EntryItem
            {
                AccountId = expenseAccount.Id,
                Debit = purchaseInvoice.SubTotal,
                Credit = 0,
                Description = $"Compra {purchaseInvoice.InvoiceNumber}"
            });

            if (purchaseInvoice.ISVAmount > 0)
            {
                entry.EntryItems.Add(new EntryItem
                {
                    AccountId = isvAccount.Id,
                    Debit = purchaseInvoice.ISVAmount,
                    Credit = 0,
                    Description = "ISV crédito fiscal"
                });
            }

            entry.EntryItems.Add(new EntryItem
            {
                AccountId = supplierAccount.Id,
                Debit = 0,
                Credit = purchaseInvoice.TotalAmount,
                Description = $"Proveedor factura {purchaseInvoice.InvoiceNumber}"
            });

            await _context.AccountingEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }


        public async Task DeleteEntryByReferenceIdAsync(Guid referenceId)
        {
            var entry = await _context.AccountingEntries.FirstOrDefaultAsync(e => e.ReferenceId == referenceId);
            if (entry != null)
            {
                entry.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}

