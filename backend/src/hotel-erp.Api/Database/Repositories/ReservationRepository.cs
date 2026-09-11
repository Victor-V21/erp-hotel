using Microsoft.EntityFrameworkCore;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;
        public ReservationRepository(ApplicationDbContext context) => _context = context;

        public async Task<Reservation?> GetByIdAsync(Guid id) => await _context.Reservations.Include(r => r.Guest).Include(r => r.Room).FirstOrDefaultAsync(r => r.Id == id);
        public async Task<IEnumerable<Reservation>> GetAllAsync() => await _context.Reservations.Include(r => r.Guest).Include(r => r.Room).ToListAsync();
        public async Task<IEnumerable<Reservation>> GetByGuestAsync(Guid guestId) => await _context.Reservations.Include(r => r.Room).Where(r => r.GuestId == guestId).ToListAsync();
        public async Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId) => await _context.Reservations.Include(r => r.Guest).Where(r => r.RoomId == roomId).ToListAsync();
        public async Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateOnly start, DateOnly end) => await _context.Reservations.Include(r => r.Guest).Include(r => r.Room).Where(r => r.CheckInDate < end && r.CheckOutDate > start).ToListAsync();
        public async Task<IEnumerable<Reservation>> GetByStatusAsync(string status) => await _context.Reservations.Include(r => r.Guest).Include(r => r.Room).Where(r => r.Status.ToString() == status).ToListAsync();
        public async Task AddAsync(Reservation r) { await _context.Reservations.AddAsync(r); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Reservation r) { _context.Reservations.Update(r); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Guid id) { var r = await _context.Reservations.FindAsync(id); if (r != null) { r.IsDeleted = true; await _context.SaveChangesAsync(); } }
        public async Task<bool> HasOverlapAsync(Guid roomId, DateOnly checkIn, DateOnly checkOut, Guid excludeReservationId)
            => await _context.Reservations.AnyAsync(r => r.Id != excludeReservationId
                && r.RoomId == roomId
                && r.Status != ReservationStatus.Cancelada
                && r.Status != ReservationStatus.CheckOut
                && r.CheckInDate < checkOut && r.CheckOutDate > checkIn);
    }

    public class FolioRepository : IFolioRepository
    {
        private readonly ApplicationDbContext _context;
        public FolioRepository(ApplicationDbContext context) => _context = context;

        public async Task<Folio?> GetByIdAsync(Guid id) => await HistoricalFolios().FirstOrDefaultAsync(f => f.Id == id);
        public async Task<Folio?> GetByReservationAsync(Guid reservationId) => await HistoricalFolios().FirstOrDefaultAsync(f => f.ReservationId == reservationId);
        public async Task<IEnumerable<Folio>> GetAllAsync() => await HistoricalFolios().ToListAsync();
        public async Task AddAsync(Folio f) { await _context.Folios.AddAsync(f); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Folio f) { _context.Folios.Update(f); await _context.SaveChangesAsync(); }

        private IQueryable<Folio> HistoricalFolios() => _context.Folios
            .IgnoreQueryFilters()
            .Where(folio => !folio.IsDeleted)
            .Include(folio => folio.FolioItems.Where(item => !item.IsDeleted))
            .Include(folio => folio.Guest)
            .Include(folio => folio.Room)
            .AsSplitQuery();
    }
}
