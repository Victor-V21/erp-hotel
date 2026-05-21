using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database.Entities;

namespace hotel_erp.Api.Database.Entities
{
    public class RoomType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }

    public class Room : BaseEntity
    {
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public Guid RoomTypeId { get; set; }
        public RoomType RoomType { get; set; } = null!;
        public RoomStatus Status { get; set; } = RoomStatus.Libre;
        public string? Observations { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Folio> Folios { get; set; } = new List<Folio>();
    }
}


