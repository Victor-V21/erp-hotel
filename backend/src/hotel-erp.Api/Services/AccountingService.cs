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

            var incomeAccount = await GetOrCreateAccountAsync("4101", "Ingresos por hospedaje", AccountType.Ingreso);
            var isvAccount = await GetOrCreateAccountAsync("2101", "ISV por pagar", AccountType.Pasivo);
            var touristTaxAccount = await GetOrCreateAccountAsync("2102", "Tasa turística por pagar", AccountType.Pasivo);

            var entry = new AccountingEntry
            {
                TransactionDate = DateOnly.FromDateTime(invoice.InvoiceDate),
                Description = $"{DocumentLabel(invoice.DocumentType)} {invoice.CorrelativeNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = invoice.Id
            };

            if (invoice.DocumentType == InvoiceDocumentType.NotaCredito)
            {
                if (!invoice.OriginalInvoiceId.HasValue)
                    throw new InvalidOperationException("La nota de crédito no tiene factura original");

                var originalTotal = await _context.Invoices
                    .Where(candidate => candidate.Id == invoice.OriginalInvoiceId.Value)
                    .Select(candidate => candidate.TotalAmount)
                    .SingleAsync();
                var paidAmount = await _context.PaymentApplications
                    .Where(application => application.InvoiceId == invoice.OriginalInvoiceId.Value
                        && application.Payment.Status != PaymentStatus.Anulado)
                    .SumAsync(application => application.Amount);
                var previouslyCredited = await _context.Invoices
                    .Where(candidate => candidate.OriginalInvoiceId == invoice.OriginalInvoiceId.Value
                        && candidate.DocumentType == InvoiceDocumentType.NotaCredito
                        && candidate.Id != invoice.Id)
                    .SumAsync(candidate => candidate.TotalAmount);
                var receivableBeforeCredit = Math.Max(
                    0m,
                    TaxService.RoundCurrency(originalTotal - paidAmount - previouslyCredited));
                var receivableReduction = Math.Min(invoice.TotalAmount, receivableBeforeCredit);
                var customerCredit = TaxService.RoundCurrency(invoice.TotalAmount - receivableReduction);

                AddLine(entry, incomeAccount.Id, invoice.SubTotal, 0m, "Reversión de ingreso");
                AddTaxLines(entry, isvAccount.Id, touristTaxAccount.Id, invoice, reverse: true);
                if (receivableReduction > 0m)
                {
                    var receivableAccount = await GetOrCreateAccountAsync("1103", "Cuentas por cobrar", AccountType.Activo);
                    AddLine(entry, receivableAccount.Id, 0m, receivableReduction, $"Reducción de cuenta por cobrar {invoice.OriginalCorrelativeNumber}");
                }
                if (customerCredit > 0m)
                {
                    var customerCreditAccount = await GetOrCreateAccountAsync("2105", "Saldos a favor de clientes", AccountType.Pasivo);
                    AddLine(entry, customerCreditAccount.Id, 0m, customerCredit, $"Saldo a favor por {invoice.OriginalCorrelativeNumber}");
                }
            }
            else
            {
                var debitAccount = await GetOrCreateAccountAsync("1103", "Cuentas por cobrar", AccountType.Activo);

                AddLine(
                    entry,
                    debitAccount.Id,
                    invoice.TotalAmount,
                    0m,
                    invoice.DocumentType == InvoiceDocumentType.NotaDebito
                        ? $"Cuenta por cobrar por {invoice.OriginalCorrelativeNumber}"
                        : $"Cuenta por cobrar por factura {invoice.CorrelativeNumber}");
                AddLine(entry, incomeAccount.Id, 0m, invoice.SubTotal, "Ingreso neto por hospedaje");
                AddTaxLines(entry, isvAccount.Id, touristTaxAccount.Id, invoice, reverse: false);
            }

            var debit = entry.EntryItems.Sum(i => i.Debit);
            var credit = entry.EntryItems.Sum(i => i.Credit);
            if (debit != credit)
                throw new InvalidOperationException($"Asiento contable descuadrado para factura {invoice.CorrelativeNumber}");

            await _context.AccountingEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        public async Task CreatePaymentEntryAsync(Payment payment)
        {
            var existingEntry = await _context.AccountingEntries
                .SingleOrDefaultAsync(entry => entry.ReferenceId == payment.Id);
            if (existingEntry is not null)
            {
                payment.AccountingEntryId = existingEntry.Id;
                return;
            }

            var receivableAccount = await GetOrCreateAccountAsync("1103", "Cuentas por cobrar", AccountType.Activo);
            var debitAccount = payment.Method switch
            {
                PaymentMethod.Efectivo => await GetOrCreateAccountAsync("1101", "Caja", AccountType.Activo),
                PaymentMethod.Transferencia => await GetOrCreateAccountAsync("1102", "Banco", AccountType.Activo),
                PaymentMethod.Tarjeta => await GetOrCreateAccountAsync("1104", "Cobros con tarjeta por liquidar", AccountType.Activo),
                _ => throw new InvalidOperationException("Método de pago no soportado")
            };
            var entry = new AccountingEntry
            {
                Id = Guid.CreateVersion7(),
                TransactionDate = DateOnly.FromDateTime(payment.PaymentDate),
                Description = $"Cobro {payment.PaymentNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = payment.Id
            };
            AddLine(entry, debitAccount.Id, payment.Amount, 0m, $"Ingreso por {payment.Method}");
            AddLine(entry, receivableAccount.Id, 0m, payment.Amount, "Aplicación a cuentas por cobrar");

            await _context.AccountingEntries.AddAsync(entry);
            payment.AccountingEntryId = entry.Id;
            await _context.SaveChangesAsync();
        }

        public async Task CreateRefundEntryAsync(Refund refund)
        {
            var existingEntry = await _context.AccountingEntries
                .SingleOrDefaultAsync(entry => entry.ReferenceId == refund.Id);
            if (existingEntry is not null)
            {
                refund.AccountingEntryId = existingEntry.Id;
                return;
            }

            var customerCreditAccount = await GetOrCreateAccountAsync("2105", "Saldos a favor de clientes", AccountType.Pasivo);
            var creditAccount = refund.Method switch
            {
                PaymentMethod.Efectivo => await GetOrCreateAccountAsync("1101", "Caja", AccountType.Activo),
                PaymentMethod.Transferencia => await GetOrCreateAccountAsync("1102", "Banco", AccountType.Activo),
                PaymentMethod.Tarjeta => await GetOrCreateAccountAsync("1104", "Cobros con tarjeta por liquidar", AccountType.Activo),
                _ => throw new InvalidOperationException("Método de devolución no soportado")
            };
            var entry = new AccountingEntry
            {
                Id = Guid.CreateVersion7(),
                TransactionDate = DateOnly.FromDateTime(refund.RefundDate),
                Description = $"Reembolso {refund.RefundNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = refund.Id
            };
            AddLine(entry, customerCreditAccount.Id, refund.Amount, 0m, "Cancelación de saldo a favor del cliente");
            AddLine(entry, creditAccount.Id, 0m, refund.Amount, $"Devolución por {refund.Method}");

            await _context.AccountingEntries.AddAsync(entry);
            refund.AccountingEntryId = entry.Id;
            await _context.SaveChangesAsync();
        }

        public async Task CreateCardSettlementEntryAsync(CardSettlement settlement)
        {
            var existingEntry = await _context.AccountingEntries
                .SingleOrDefaultAsync(entry => entry.ReferenceId == settlement.Id);
            if (existingEntry is not null)
            {
                settlement.AccountingEntryId = existingEntry.Id;
                return;
            }

            var cardClearingAccount = await GetOrCreateAccountAsync("1104", "Cobros con tarjeta por liquidar", AccountType.Activo);
            var bankAccount = await GetOrCreateAccountAsync("1102", "Banco", AccountType.Activo);
            var commissionAccount = await GetOrCreateAccountAsync("5201", "Comisiones por adquirencia", AccountType.Gasto);
            var withholdingAccount = await GetOrCreateAccountAsync("1105", "Retenciones sufridas por acreditar", AccountType.Activo);
            var entry = new AccountingEntry
            {
                Id = Guid.CreateVersion7(),
                TransactionDate = settlement.SettlementDate,
                Description = $"Liquidación de tarjeta {settlement.SettlementNumber}",
                EntryType = EntryType.Diario,
                ReferenceId = settlement.Id
            };
            if (settlement.BankDepositAmount > 0m)
                AddLine(entry, bankAccount.Id, settlement.BankDepositAmount, 0m, "Depósito neto del adquirente");
            if (settlement.CommissionAmount > 0m)
                AddLine(entry, commissionAccount.Id, settlement.CommissionAmount, 0m, "Comisión del adquirente");
            if (settlement.WithholdingAmount > 0m)
                AddLine(entry, withholdingAccount.Id, settlement.WithholdingAmount, 0m, "Retención sufrida pendiente de acreditar");
            AddLine(entry, cardClearingAccount.Id, 0m, settlement.GrossAmount, "Cancelación de cobros con tarjeta por liquidar");

            var debit = entry.EntryItems.Sum(item => item.Debit);
            var credit = entry.EntryItems.Sum(item => item.Credit);
            if (debit != credit)
                throw new InvalidOperationException($"Asiento contable descuadrado para liquidación {settlement.SettlementNumber}");

            await _context.AccountingEntries.AddAsync(entry);
            settlement.AccountingEntryId = entry.Id;
            await _context.SaveChangesAsync();
        }

        private static string DocumentLabel(InvoiceDocumentType type) => type switch
        {
            InvoiceDocumentType.NotaCredito => "Nota de crédito",
            InvoiceDocumentType.NotaDebito => "Nota de débito",
            _ => "Factura"
        };

        private static void AddTaxLines(
            AccountingEntry entry,
            Guid isvAccountId,
            Guid touristTaxAccountId,
            Invoice invoice,
            bool reverse)
        {
            if (invoice.ISVAmount > 0m)
                AddLine(entry, isvAccountId, reverse ? invoice.ISVAmount : 0m, reverse ? 0m : invoice.ISVAmount, reverse ? "Reversión de ISV" : "ISV por pagar");
            if (invoice.TouristTaxAmount > 0m)
                AddLine(entry, touristTaxAccountId, reverse ? invoice.TouristTaxAmount : 0m, reverse ? 0m : invoice.TouristTaxAmount, reverse ? "Reversión de tasa turística" : "Tasa turística por pagar");
        }

        private static void AddLine(AccountingEntry entry, Guid accountId, decimal debit, decimal credit, string description)
            => entry.EntryItems.Add(new EntryItem
            {
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                Description = description
            });

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
