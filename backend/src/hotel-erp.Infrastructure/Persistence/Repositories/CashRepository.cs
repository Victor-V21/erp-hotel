using Microsoft.EntityFrameworkCore;
using hotel_erp.Application.Interfaces;
using hotel_erp.Domain.Entities;

namespace hotel_erp.Infrastructure.Persistence.Repositories
{
    public class CashRegisterRepository : ICashRegisterRepository
    {
        private readonly ApplicationDbContext _context;
        public CashRegisterRepository(ApplicationDbContext context) => _context = context;

        public async Task<CashRegister?> GetByIdAsync(Guid id) => await _context.CashRegisters.FindAsync(id);
        public async Task<IEnumerable<CashRegister>> GetAllAsync() => await _context.CashRegisters.ToListAsync();
        public async Task AddAsync(CashRegister cr) { await _context.CashRegisters.AddAsync(cr); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(CashRegister cr) { _context.CashRegisters.Update(cr); await _context.SaveChangesAsync(); }
    }

    public class CashMovementRepository : ICashMovementRepository
    {
        private readonly ApplicationDbContext _context;
        public CashMovementRepository(ApplicationDbContext context) => _context = context;

        public async Task<CashMovement?> GetByIdAsync(Guid id) => await _context.CashMovements.Include(cm => cm.CashRegister).Include(cm => cm.User).FirstOrDefaultAsync(cm => cm.Id == id);
        public async Task<IEnumerable<CashMovement>> GetByRegisterAsync(Guid cashRegisterId) => await _context.CashMovements.Include(cm => cm.User).Where(cm => cm.CashRegisterId == cashRegisterId).OrderByDescending(cm => cm.MovementDate).ToListAsync();
        public async Task<IEnumerable<CashMovement>> GetByDateRangeAsync(DateTime start, DateTime end) => await _context.CashMovements.Include(cm => cm.CashRegister).Include(cm => cm.User).Where(cm => cm.MovementDate >= start && cm.MovementDate <= end).ToListAsync();
        public async Task AddAsync(CashMovement cm) { await _context.CashMovements.AddAsync(cm); await _context.SaveChangesAsync(); }
    }

    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        public AuditLogRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(AuditLog al) { await _context.AuditLogs.AddAsync(al); await _context.SaveChangesAsync(); }
        public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId) => await _context.AuditLogs.Where(al => al.UserId == userId).OrderByDescending(al => al.Timestamp).ToListAsync();
        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime start, DateTime end) => await _context.AuditLogs.Where(al => al.Timestamp >= start && al.Timestamp <= end).OrderByDescending(al => al.Timestamp).ToListAsync();
    }
}
