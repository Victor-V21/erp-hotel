using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services;

namespace hotel_erp.Api.Database.Repositories
{
    public class CAIRepository : ICAIRepository
    {
        private readonly ApplicationDbContext _context;
        public CAIRepository(ApplicationDbContext context) => _context = context;

        public async Task<CAI?> GetByIdAsync(Guid id) => await _context.CAIs.FindAsync(id);
        public async Task<CAI?> GetByCAINumberAsync(string number) => await _context.CAIs.FirstOrDefaultAsync(c => c.CAINumber == number);
        public async Task<IEnumerable<CAI>> GetAllAsync() => await _context.CAIs.ToListAsync();
        public async Task<CAI?> GetActiveCAIAsync() => await _context.CAIs.Where(c => c.Status == hotel_erp.Api.Database.Entities.CAIStatus.Activo).FirstOrDefaultAsync();
        public async Task AddAsync(CAI c) { await _context.CAIs.AddAsync(c); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(CAI c) { _context.CAIs.Update(c); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var c = await _context.CAIs.FindAsync(id); if (c != null) { c.Status = CAIStatus.Desactivado; _context.CAIs.Update(c); await _context.SaveChangesAsync(); } }
    }

    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;
        public InvoiceRepository(ApplicationDbContext context) => _context = context;

        public async Task<Invoice?> GetByIdAsync(Guid id) => await _context.Invoices
            .Include(invoice => invoice.InvoiceItems)
            .Include(invoice => invoice.CAI)
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.Guest)
            .ThenInclude(guest => guest!.Reservations)
            .Include(invoice => invoice.PaymentApplications)
            .ThenInclude(application => application.Payment)
            .Include(invoice => invoice.CreditNotes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(invoice => invoice.Id == id);
        public async Task<Invoice?> GetByCorrelativeAsync(string correlative) => await WithBalances(_context.Invoices.Include(i => i.InvoiceItems)).AsSplitQuery().FirstOrDefaultAsync(i => i.CorrelativeNumber == correlative);
        public async Task<IEnumerable<Invoice>> GetAllAsync() => await WithBalances(_context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Customer).Include(i => i.Guest)).AsSplitQuery().ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime start, DateTime end) => await WithBalances(_context.Invoices.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Guest)).AsSplitQuery().ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId) => await WithBalances(_context.Invoices.Where(i => i.CustomerId == customerId).Include(i => i.InvoiceItems)).AsSplitQuery().ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByGuestAsync(Guid guestId) => await WithBalances(_context.Invoices.Where(i => i.GuestId == guestId).Include(i => i.InvoiceItems)).AsSplitQuery().ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByGuestDocumentAsync(string documentNumber) => await WithBalances(_context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Where(i => i.Guest != null && i.Guest.DocumentNumber == documentNumber)).AsSplitQuery().ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByAuthorizationAsync(Guid? caiId, Guid? documentAuthorizationId)
        {
            var query = WithBalances(_context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Guest));
            if (caiId.HasValue) query = query.Where(i => i.CAIId == caiId.Value);
            if (documentAuthorizationId.HasValue) query = query.Where(i => i.DocumentAuthorizationId == documentAuthorizationId.Value);
            return await query.AsSplitQuery().ToListAsync();
        }
        public async Task<IEnumerable<Invoice>> GetByOriginalInvoiceAsync(Guid originalInvoiceId) =>
            await WithBalances(_context.Invoices
                    .Where(invoice => invoice.OriginalInvoiceId == originalInvoiceId)
                    .Include(invoice => invoice.InvoiceItems)
                    .Include(invoice => invoice.CAI)
                    .Include(invoice => invoice.Customer)
                    .Include(invoice => invoice.Guest))
                .AsSplitQuery()
                .ToListAsync();

        private static IQueryable<Invoice> WithBalances(IQueryable<Invoice> query) => query
            .Include(invoice => invoice.PaymentApplications)
            .ThenInclude(application => application.Payment)
            .Include(invoice => invoice.CreditNotes);
        public async Task AddAsync(Invoice i) { await _context.Invoices.AddAsync(i); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Invoice i) { _context.Invoices.Update(i); await _context.SaveChangesAsync(); }
        public async Task DeleteInvoiceItemsAsync(Guid invoiceId)
        {
            var items = _context.InvoiceItems.Where(ii => ii.InvoiceId == invoiceId).ToList();
            foreach (var item in items)
            {
                item.IsDeleted = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetNextCorrelativeAsync(Guid caiId)
        {
            return await PostgresCorrelativeLock.ExecuteAsync(_context, $"cai:{caiId}", async () =>
            {
                var cai = await _context.CAIs.FirstOrDefaultAsync(c => c.Id == caiId) ?? throw new InvalidOperationException("CAI no encontrado");
                if (cai.Status != CAIStatus.Activo) throw new InvalidOperationException("El CAI no está activo");
                if (cai.DueDate < HondurasTime.Today) throw new InvalidOperationException("El CAI está vencido");

                var parts = cai.CurrentCorrelative.Split('-');
                if (parts.Length != 4) throw new InvalidOperationException("Formato de correlativo inválido");

                var hasIssuedDocuments = await _context.Invoices
                    .IgnoreQueryFilters()
                    .AnyAsync(invoice => invoice.CAIId == caiId);
                var sequential = !hasIssuedDocuments && cai.CurrentCorrelative == cai.InitialRange
                    ? int.Parse(parts[3])
                    : int.Parse(parts[3]) + 1;
                var finalSeq = int.Parse(cai.FinalRange.Split('-').Last());
                if (sequential > finalSeq)
                {
                    cai.Status = CAIStatus.Agotado;
                    await _context.SaveChangesAsync();
                    throw new CorrelativeStateException("El CAI ha agotado su rango de correlativos");
                }

                var newCorrelative = $"{parts[0]}-{parts[1]}-{parts[2]}-{sequential:D8}";

                cai.CurrentCorrelative = newCorrelative;
                _context.CAIs.Update(cai);
                await _context.SaveChangesAsync();
                return newCorrelative;
            });
        }
    }

    public class DocumentAuthorizationRepository : IDocumentAuthorizationRepository
    {
        private readonly ApplicationDbContext _context;
        public DocumentAuthorizationRepository(ApplicationDbContext context) => _context = context;

        public async Task<DocumentAuthorization?> GetByIdAsync(Guid id)
            => await _context.DocumentAuthorizations.FindAsync(id);

        public async Task<DocumentAuthorization?> GetActiveAsync(InvoiceDocumentType documentType)
            => await _context.DocumentAuthorizations
                .Where(a => a.DocumentType == documentType && a.Status == CAIStatus.Activo)
                .OrderBy(a => a.DueDate)
                .FirstOrDefaultAsync();

        public async Task<DocumentAuthorization?> GetByCAIAsync(InvoiceDocumentType documentType, string caiNumber)
            => await _context.DocumentAuthorizations.FirstOrDefaultAsync(a => a.DocumentType == documentType && a.CAINumber == caiNumber);

        public async Task<IEnumerable<DocumentAuthorization>> GetAllAsync()
            => await _context.DocumentAuthorizations.OrderBy(a => a.DocumentType).ThenBy(a => a.DueDate).ToListAsync();

        public async Task AddAsync(DocumentAuthorization authorization)
        {
            await _context.DocumentAuthorizations.AddAsync(authorization);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DocumentAuthorization authorization)
        {
            _context.DocumentAuthorizations.Update(authorization);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var authorization = await _context.DocumentAuthorizations.FindAsync(id);
            if (authorization == null) return;
            authorization.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
