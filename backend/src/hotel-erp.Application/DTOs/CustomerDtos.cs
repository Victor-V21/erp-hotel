namespace hotel_erp.Application.DTOs
{
    public record CustomerDto
    {
        public Guid Id { get; set; }
        public string? RTN { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public record CreateCustomerRequest(string? RTN, string Name, string? Address, string? Phone, string? Email);
    public record UpdateCustomerRequest(string? RTN, string? Name, string? Address, string? Phone, string? Email);

    public record GuestDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
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
        public string? GuestRTN { get; set; }
        public string? Preferences { get; set; }
        public string? Classification { get; set; }
    }

    public record CreateGuestRequest(string FirstName, string LastName, string? Email, string? Phone, DateOnly? DateOfBirth, string? Nationality, string? DocumentType, string? DocumentNumber, string? Origin, bool HasVehicle, string? VehiclePlate, string? Company, string? GuestRTN, string? Preferences, string? Classification);
    public record UpdateGuestRequest(string? FirstName, string? LastName, string? Email, string? Phone, DateOnly? DateOfBirth, string? Nationality, string? DocumentType, string? DocumentNumber, string? Origin, bool? HasVehicle, string? VehiclePlate, string? Company, string? GuestRTN, string? Preferences, string? Classification);
    public record UpdateClassificationRequest(string Classification);
}
