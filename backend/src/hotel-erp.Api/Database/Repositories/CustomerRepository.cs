using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;
        public CustomerRepository(ApplicationDbContext context) => _context = context;

        public async Task<Customer?> GetByIdAsync(Guid id) => await _context.Customers.FindAsync(id);
        public async Task<Customer?> GetByRTNAsync(string rtn) => await _context.Customers.FirstOrDefaultAsync(c => c.RTN == rtn);
        public async Task<IEnumerable<Customer>> GetAllAsync() => await _context.Customers.ToListAsync();
        public async Task<IEnumerable<Customer>> SearchAsync(string term) => await _context.Customers.Where(c => c.Name.Contains(term) || (c.RTN != null && c.RTN.Contains(term))).ToListAsync();
        public async Task AddAsync(Customer c) { await _context.Customers.AddAsync(c); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Customer c) { _context.Customers.Update(c); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var c = await _context.Customers.FindAsync(id); if (c != null) { c.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }

    public class GuestRepository : IGuestRepository
    {
        private readonly ApplicationDbContext _context;
        public GuestRepository(ApplicationDbContext context) => _context = context;

        public async Task<Guest?> GetByIdAsync(Guid id) => await _context.Guests.FindAsync(id);
        public async Task<Guest?> GetByDocumentNumberAsync(string doc) => await _context.Guests.FirstOrDefaultAsync(g => g.DocumentNumber == doc);
        public async Task<IEnumerable<Guest>> GetAllAsync() => await _context.Guests.ToListAsync();
        public async Task<IEnumerable<Guest>> SearchAsync(string term) => await _context.Guests.Where(g => (g.FirstName + " " + g.LastName).Contains(term) || (g.DocumentNumber != null && g.DocumentNumber.Contains(term))).ToListAsync();
        public async Task AddAsync(Guest g) { await _context.Guests.AddAsync(g); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Guest g) { _context.Guests.Update(g); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var g = await _context.Guests.FindAsync(id); if (g != null) { g.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }
}

