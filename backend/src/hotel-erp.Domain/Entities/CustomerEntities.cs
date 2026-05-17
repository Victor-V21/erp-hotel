using hotel_erp.Domain.Common;

namespace hotel_erp.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public string? RTN { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    public class Guest : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? Origin { get; set; }
        public bool HasVehicle { get; set; }
        public string? VehiclePlate { get; set; }
        public string? Company { get; set; }
        public string? RTN { get; set; }
        public string? Preferences { get; set; }
        public string? Classification { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Folio> Folios { get; set; } = new List<Folio>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
