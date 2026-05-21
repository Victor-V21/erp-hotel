using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

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

        public async Task<Invoice?> GetByIdAsync(Guid id) => await _context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Customer).Include(i => i.Guest).ThenInclude(g => g.Reservations).FirstOrDefaultAsync(i => i.Id == id);
        public async Task<Invoice?> GetByCorrelativeAsync(string correlative) => await _context.Invoices.Include(i => i.InvoiceItems).FirstOrDefaultAsync(i => i.CorrelativeNumber == correlative);
        public async Task<IEnumerable<Invoice>> GetAllAsync() => await _context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Customer).Include(i => i.Guest).ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime start, DateTime end) => await _context.Invoices.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Guest).ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId) => await _context.Invoices.Where(i => i.CustomerId == customerId).Include(i => i.InvoiceItems).ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByGuestDocumentAsync(string documentNumber) => await _context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Where(i => i.Guest != null && i.Guest.DocumentNumber == documentNumber).ToListAsync();
        public async Task<IEnumerable<Invoice>> GetByAuthorizationAsync(Guid? caiId, Guid? documentAuthorizationId)
        {
            var query = _context.Invoices.Include(i => i.InvoiceItems).Include(i => i.CAI).Include(i => i.Guest).AsQueryable();
            if (caiId.HasValue) query = query.Where(i => i.CAIId == caiId.Value);
            if (documentAuthorizationId.HasValue) query = query.Where(i => i.DocumentAuthorizationId == documentAuthorizationId.Value);
            return await query.ToListAsync();
        }
        public async Task AddAsync(Invoice i) { await _context.Invoices.AddAsync(i); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Invoice i) { _context.Invoices.Update(i); await _context.SaveChangesAsync(); }
        public void DeleteInvoiceItems(Guid invoiceId)
        {
            var items = _context.InvoiceItems.Where(ii => ii.InvoiceId == invoiceId);
            _context.InvoiceItems.RemoveRange(items);
        }

        public async Task<string> GetNextCorrelativeAsync(Guid caiId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var cai = await _context.CAIs.FirstOrDefaultAsync(c => c.Id == caiId) ?? throw new InvalidOperationException("CAI no encontrado");
            if (cai.Status != CAIStatus.Activo) throw new InvalidOperationException("El CAI no está activo");
            if (cai.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("El CAI está vencido");

            var parts = cai.CurrentCorrelative.Split('-');
            if (parts.Length != 4) throw new InvalidOperationException("Formato de correlativo inválido");

            var sequential = int.Parse(parts[3]) + 1;
            var finalSeq = int.Parse(cai.FinalRange.Split('-').Last());
            if (sequential > finalSeq)
            {
                cai.Status = CAIStatus.Agotado;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException("El CAI ha agotado su rango de correlativos");
            }

            var newCorrelative = $"{parts[0]}-{parts[1]}-{parts[2]}-{sequential:D8}";

            cai.CurrentCorrelative = newCorrelative;
            _context.CAIs.Update(cai);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return newCorrelative;
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


