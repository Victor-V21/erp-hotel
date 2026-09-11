using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class RoomTypeRepository : IRoomTypeRepository
    {
        private readonly ApplicationDbContext _context;
        public RoomTypeRepository(ApplicationDbContext context) => _context = context;

        public async Task<RoomType?> GetByIdAsync(Guid id) => await _context.RoomTypes.FindAsync(id);
        public async Task<RoomType?> GetByNameAsync(string name) => await _context.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == name);
        public async Task<IEnumerable<RoomType>> GetAllAsync() => await _context.RoomTypes.ToListAsync();
        public async Task AddAsync(RoomType rt) { await _context.RoomTypes.AddAsync(rt); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(RoomType rt) { _context.RoomTypes.Update(rt); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var rt = await _context.RoomTypes.FindAsync(id); if (rt != null) { rt.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }

    public class RoomRepository : IRoomRepository
    {
        private readonly ApplicationDbContext _context;
        public RoomRepository(ApplicationDbContext context) => _context = context;

        public async Task<Room?> GetByIdAsync(Guid id) => await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == id);
        public async Task<Room?> GetByRoomNumberAsync(string roomNumber) => await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
        public async Task<IEnumerable<Room>> GetAllAsync() => await _context.Rooms.Include(r => r.RoomType).ToListAsync();
        public async Task<IEnumerable<Room>> GetByStatusAsync(string status) => await _context.Rooms.Include(r => r.RoomType).Where(r => r.Status.ToString() == status).ToListAsync();

        public async Task<IEnumerable<Room>> GetAvailableAsync(DateOnly checkIn, DateOnly checkOut)
        {
            var occupiedRoomIds = await _context.Reservations
                .Where(res => res.Status != ReservationStatus.Cancelada
                    && res.Status != ReservationStatus.CheckOut
                    && res.CheckInDate < checkOut && res.CheckOutDate > checkIn)
                .Select(res => res.RoomId)
                .Distinct()
                .ToListAsync();

            return await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => !occupiedRoomIds.Contains(r.Id)
                    && r.Status != RoomStatus.Mantenimiento
                    && r.Status != RoomStatus.Bloqueada)
                .ToListAsync();
        }

        public async Task AddAsync(Room room) { await _context.Rooms.AddAsync(room); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Room room) { _context.Rooms.Update(room); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var r = await _context.Rooms.FindAsync(id); if (r != null) { r.IsDeleted = true; await _context.SaveChangesAsync(); } }
    }
}
