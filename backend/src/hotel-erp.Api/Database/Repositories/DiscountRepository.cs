using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly ApplicationDbContext _context;
        public DiscountRepository(ApplicationDbContext context) => _context = context;

        public async Task<Discount?> GetByIdAsync(Guid id) => await _context.Discounts.FindAsync(id);
        public async Task<IEnumerable<Discount>> GetAllAsync() => await _context.Discounts.OrderBy(d => d.Priority).ToListAsync();
        public async Task<IEnumerable<Discount>> GetActiveAsync() => await _context.Discounts.Where(d => d.IsActive).OrderBy(d => d.Priority).ToListAsync();
        public async Task AddAsync(Discount d) { await _context.Discounts.AddAsync(d); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Discount d) { _context.Discounts.Update(d); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var d = await _context.Discounts.FindAsync(id); if (d != null) { d.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }
}

