using Microsoft.EntityFrameworkCore;
using hotel_erp.Application.Interfaces;
using hotel_erp.Domain.Entities;

namespace hotel_erp.Infrastructure.Persistence.Repositories
{
    public class CAIRepository : ICAIRepository
    {
        private readonly ApplicationDbContext _context;
        public CAIRepository(ApplicationDbContext context) => _context = context;

        public async Task<CAI?> GetByIdAsync(Guid id) => await _context.CAIs.FindAsync(id);
        public async Task<CAI?> GetByCAINumberAsync(string number) => await _context.CAIs.FirstOrDefaultAsync(c => c.CAINumber == number);
        public async Task<IEnumerable<CAI>> GetAllAsync() => await _context.CAIs.ToListAsync();
        public async Task<CAI?> GetActiveCAIAsync() => await _context.CAIs.Where(c => c.Status == Domain.Enums.CAIStatus.Activo).FirstOrDefaultAsync();
        public async Task AddAsync(CAI c) { await _context.CAIs.AddAsync(c); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(CAI c) { _context.CAIs.Update(c); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var c = await _context.CAIs.FindAsync(id); if (c != null) { _context.CAIs.Remove(c); await _context.SaveChangesAsync(); } }
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
        public async Task AddAsync(Invoice i) { await _context.Invoices.AddAsync(i); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Invoice i) { _context.Invoices.Update(i); await _context.SaveChangesAsync(); }
        public void DeleteInvoiceItems(Guid invoiceId)
        {
            var items = _context.InvoiceItems.Where(ii => ii.InvoiceId == invoiceId);
            _context.InvoiceItems.RemoveRange(items);
        }

        public async Task<string> GetNextCorrelativeAsync(Guid caiId)
        {
            var cai = await _context.CAIs.FindAsync(caiId) ?? throw new InvalidOperationException("CAI no encontrado");

            // Parse current correlative and increment
            var parts = cai.CurrentCorrelative.Split('-');
            if (parts.Length != 4) throw new InvalidOperationException("Formato de correlativo inválido");

            var sequential = int.Parse(parts[3]) + 1;
            var newCorrelative = $"{parts[0]}-{parts[1]}-{parts[2]}-{sequential:D8}";

            // Validate range
            var initialSeq = int.Parse(parts[3]); // This should be based on InitialRange
            // For now, just increment
            cai.CurrentCorrelative = newCorrelative;
            _context.CAIs.Update(cai);
            await _context.SaveChangesAsync();

            return newCorrelative;
        }
    }
}
