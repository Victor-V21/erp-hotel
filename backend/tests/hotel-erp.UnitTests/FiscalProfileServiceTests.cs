using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Services;

namespace hotel_erp.UnitTests;

public class FiscalProfileServiceTests
{
    [Fact]
    public void ValidateForApproval_RejectsEmptyProfile()
    {
        var errors = FiscalProfileService.ValidateForApproval(new BusinessSettings());

        Assert.Contains(errors, error => error.Contains("razón social", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("RTN", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("dirección", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateForApproval_AcceptsCompleteProfile()
    {
        var settings = new BusinessSettings
        {
            BusinessName = "Hotel de prueba",
            RTN = "08011999123456",
            Address = "Tegucigalpa",
            IsvRate = 0.15m,
            TouristTaxRate = 0.04m
        };

        Assert.Empty(FiscalProfileService.ValidateForApproval(settings));
    }
}
