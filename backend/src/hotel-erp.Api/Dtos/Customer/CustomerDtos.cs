using System.ComponentModel.DataAnnotations;

namespace hotel_erp.Api.Dtos.Customer
{
    public record CustomerDto
    {
        public Guid Id { get; set; }
        public string? RTN { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string TaxpayerType { get; set; } = "Gravado";
        public string? ExonerationOrderNumber { get; set; }
        public string? SefinExonerationCertificateNumber { get; set; }
        public string? SagRegistryNumber { get; set; }
        public bool IsIsvExempt { get; set; }
        public bool IsTouristTaxExempt { get; set; }
        public DateOnly? ExonerationValidFrom { get; set; }
        public DateOnly? ExonerationValidTo { get; set; }
    }

    public record CreateCustomerRequest(
        [RegularExpression("^\\d{14}$")] string? RTN,
        [Required, StringLength(100, MinimumLength = 2)] string Name,
        [StringLength(250)] string? Address,
        [StringLength(20)] string? Phone,
        [EmailAddress, StringLength(100)] string? Email,
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")] string? TaxpayerType = null,
        string? ExonerationOrderNumber = null,
        string? SefinExonerationCertificateNumber = null,
        string? SagRegistryNumber = null,
        bool IsIsvExempt = false,
        bool IsTouristTaxExempt = false,
        DateOnly? ExonerationValidFrom = null,
        DateOnly? ExonerationValidTo = null);

    public record UpdateCustomerRequest(
        [RegularExpression("^\\d{14}$")] string? RTN,
        [StringLength(100, MinimumLength = 2)] string? Name,
        [StringLength(250)] string? Address,
        [StringLength(20)] string? Phone,
        [EmailAddress, StringLength(100)] string? Email,
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")] string? TaxpayerType = null,
        string? ExonerationOrderNumber = null,
        string? SefinExonerationCertificateNumber = null,
        string? SagRegistryNumber = null,
        bool? IsIsvExempt = null,
        bool? IsTouristTaxExempt = null,
        DateOnly? ExonerationValidFrom = null,
        DateOnly? ExonerationValidTo = null);

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
        public string TaxpayerType { get; set; } = "ConsumidorFinal";
        public string? ExonerationOrderNumber { get; set; }
        public string? SefinExonerationCertificateNumber { get; set; }
        public string? SagRegistryNumber { get; set; }
        public bool IsIsvExempt { get; set; }
        public bool IsTouristTaxExempt { get; set; }
        public DateOnly? ExonerationValidFrom { get; set; }
        public DateOnly? ExonerationValidTo { get; set; }
    }

    public record CreateGuestRequest(
        [Required, StringLength(50, MinimumLength = 2)] string FirstName,
        [Required, StringLength(50, MinimumLength = 2)] string LastName,
        [EmailAddress, StringLength(100)] string? Email,
        [StringLength(20)] string? Phone,
        DateOnly? DateOfBirth,
        [StringLength(50)] string? Nationality,
        [RegularExpression("^(DNI|Pasaporte|RTN|Otro)$")] string? DocumentType,
        [StringLength(50)] string? DocumentNumber,
        [StringLength(100)] string? Origin,
        bool HasVehicle,
        [StringLength(20)] string? VehiclePlate,
        [StringLength(100)] string? Company,
        [RegularExpression("^\\d{14}$")] string? GuestRTN,
        [StringLength(1000)] string? Preferences,
        [StringLength(50)] string? Classification,
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")] string? TaxpayerType = null,
        string? ExonerationOrderNumber = null,
        string? SefinExonerationCertificateNumber = null,
        string? SagRegistryNumber = null,
        bool IsIsvExempt = false,
        bool IsTouristTaxExempt = false,
        DateOnly? ExonerationValidFrom = null,
        DateOnly? ExonerationValidTo = null);

    public record UpdateGuestRequest(
        [StringLength(50, MinimumLength = 2)] string? FirstName,
        [StringLength(50, MinimumLength = 2)] string? LastName,
        [EmailAddress, StringLength(100)] string? Email,
        [StringLength(20)] string? Phone,
        DateOnly? DateOfBirth,
        [StringLength(50)] string? Nationality,
        [RegularExpression("^(DNI|Pasaporte|RTN|Otro)$")] string? DocumentType,
        [StringLength(50)] string? DocumentNumber,
        [StringLength(100)] string? Origin,
        bool? HasVehicle,
        [StringLength(20)] string? VehiclePlate,
        [StringLength(100)] string? Company,
        [RegularExpression("^\\d{14}$")] string? GuestRTN,
        [StringLength(1000)] string? Preferences,
        [StringLength(50)] string? Classification,
        [RegularExpression("^(ConsumidorFinal|Gravado|Exonerado)$")] string? TaxpayerType = null,
        string? ExonerationOrderNumber = null,
        string? SefinExonerationCertificateNumber = null,
        string? SagRegistryNumber = null,
        bool? IsIsvExempt = null,
        bool? IsTouristTaxExempt = null,
        DateOnly? ExonerationValidFrom = null,
        DateOnly? ExonerationValidTo = null);

    public record UpdateClassificationRequest([Required, StringLength(50, MinimumLength = 2)] string Classification);
}

