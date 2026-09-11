using hotel_erp.Api.Services;

namespace hotel_erp.UnitTests;

public class TaxServiceTests
{
    private readonly TaxService _service = new()
    {
        IsvRate = 0.15m,
        TouristTaxRate = 0.04m
    };

    [Fact]
    public void CalculateFromNetAmount_SeparatesTaxesAndTotal()
    {
        var result = _service.CalculateFromNetAmount(100m);

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(15m, result.ISV);
        Assert.Equal(4m, result.TouristTax);
        Assert.Equal(119m, result.Total);
        Assert.Equal(100m, result.TaxableAmount);
    }

    [Fact]
    public void CalculateFromNetAmount_AppliesDiscountBeforeTaxes()
    {
        var result = _service.CalculateFromNetAmount(100m, discountPercentage: 10m);

        Assert.Equal(10m, result.DiscountAmount);
        Assert.Equal(90m, result.Subtotal);
        Assert.Equal(13.50m, result.ISV);
        Assert.Equal(3.60m, result.TouristTax);
        Assert.Equal(107.10m, result.Total);
    }

    [Fact]
    public void CalculateFromNetAmount_TracksExemptBase()
    {
        var result = _service.CalculateFromNetAmount(
            100m,
            isIsvExempt: true,
            isTouristTaxExempt: true);

        Assert.Equal(0m, result.ISV);
        Assert.Equal(0m, result.TouristTax);
        Assert.Equal(100m, result.ExoneratedAmount);
        Assert.Equal(0m, result.TaxableAmount);
    }

    [Fact]
    public void CalculateFromFinalPrice_ReconstructsIncludedTaxes()
    {
        var result = _service.CalculateFromFinalPrice(119m);

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(15m, result.ISV);
        Assert.Equal(4m, result.TouristTax);
        Assert.Equal(119m, result.Total);
    }

    [Fact]
    public void CalculateFromSellingPrice_PreservesFinalPriceAcrossNights()
    {
        var result = _service.CalculateFromSellingPrice(119m, 2);

        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(30m, result.ISV);
        Assert.Equal(8m, result.TouristTax);
        Assert.Equal(238m, result.Total);
    }

    [Fact]
    public void RoundCurrency_UsesAwayFromZeroForMidpoints()
    {
        Assert.Equal(1.01m, TaxService.RoundCurrency(1.005m));
        Assert.Equal(-1.01m, TaxService.RoundCurrency(-1.005m));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void CalculateFromNetAmount_RejectsInvalidInputs(int amount, int discount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.CalculateFromNetAmount(amount, discountPercentage: discount));
    }

    [Fact]
    public void CalculateFromSellingPrice_RejectsZeroNights()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.CalculateFromSellingPrice(119m, 0));
    }
}
