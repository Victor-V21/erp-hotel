using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ApplicationDbContext _context;
        public SupplierRepository(ApplicationDbContext context) => _context = context;

        public async Task<Supplier?> GetByIdAsync(Guid id) => await _context.Suppliers.FindAsync(id);
        public async Task<Supplier?> GetByRTNAsync(string rtn) => await _context.Suppliers.FirstOrDefaultAsync(s => s.RTN == rtn);
        public async Task<IEnumerable<Supplier>> GetAllAsync() => await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
        public async Task<IEnumerable<Supplier>> SearchAsync(string term) => await _context.Suppliers.Where(s => s.Name.Contains(term) || (s.RTN != null && s.RTN.Contains(term))).ToListAsync();
        public async Task AddAsync(Supplier s) { await _context.Suppliers.AddAsync(s); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Supplier s) { _context.Suppliers.Update(s); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var s = await _context.Suppliers.FindAsync(id); if (s != null) { s.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }

    public class PurchaseInvoiceRepository : IPurchaseInvoiceRepository
    {
        private readonly ApplicationDbContext _context;
        public PurchaseInvoiceRepository(ApplicationDbContext context) => _context = context;

        public async Task<PurchaseInvoice?> GetByIdAsync(Guid id) => await _context.PurchaseInvoices.Include(pi => pi.Supplier).Include(pi => pi.PurchaseInvoiceItems).FirstOrDefaultAsync(pi => pi.Id == id);
        public async Task<IEnumerable<PurchaseInvoice>> GetAllAsync() => await _context.PurchaseInvoices.Include(pi => pi.Supplier).Include(pi => pi.PurchaseInvoiceItems).ToListAsync();
        public async Task<IEnumerable<PurchaseInvoice>> GetByDateRangeAsync(DateTime start, DateTime end) => await _context.PurchaseInvoices.Where(pi => pi.InvoiceDate >= start && pi.InvoiceDate <= end).Include(pi => pi.Supplier).Include(pi => pi.PurchaseInvoiceItems).ToListAsync();
        public async Task<IEnumerable<PurchaseInvoice>> GetBySupplierAsync(Guid supplierId) => await _context.PurchaseInvoices.Where(pi => pi.SupplierId == supplierId).Include(pi => pi.Supplier).Include(pi => pi.PurchaseInvoiceItems).ToListAsync();
        public async Task AddAsync(PurchaseInvoice pi) { await _context.PurchaseInvoices.AddAsync(pi); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(PurchaseInvoice pi) { _context.PurchaseInvoices.Update(pi); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var pi = await _context.PurchaseInvoices.FindAsync(id); if (pi != null) { pi.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }
}

