using hotel_erp.Api.Services;

namespace hotel_erp.UnitTests;

public class InvoiceCalculationServiceTests
{
    [Fact]
    public void Calculate_SeparatesTaxBasesAndRoundsEachLine()
    {
        var result = InvoiceCalculationService.Calculate(
        [
            new("Hospedaje", 2, 100m, false, 0.15m, true, 10m),
            new("Bebida alcohólica", 1, 100m, false, 0.18m, false, 0m),
            new("Concepto exento", 1, 50m, true, 0m, false, 0m)
        ]);

        Assert.Equal(330m, result.SubTotal);
        Assert.Equal(27m, result.ISV15Amount);
        Assert.Equal(18m, result.ISV18Amount);
        Assert.Equal(45m, result.ISVAmount);
        Assert.Equal(7.20m, result.TouristTaxAmount);
        Assert.Equal(20m, result.DiscountsAmount);
        Assert.Equal(382.20m, result.TotalAmount);
        Assert.Equal(280m, result.TaxableAmount);
        Assert.Equal(50m, result.ExemptAmount);
        Assert.Equal(0m, result.ExoneratedAmount);
    }

    [Fact]
    public void Calculate_TracksExoneratedBaseWithoutCreatingIsv()
    {
        var result = InvoiceCalculationService.Calculate(
            [new("Hospedaje exonerado", 1, 100m, false, 0.15m, true, 0m)],
            isIsvExempt: true,
            isTouristTaxExempt: true);

        Assert.Equal(100m, result.SubTotal);
        Assert.Equal(0m, result.ISVAmount);
        Assert.Equal(0m, result.TouristTaxAmount);
        Assert.Equal(100m, result.ExoneratedAmount);
        Assert.Equal(100m, result.TotalAmount);
    }

    [Theory]
    [InlineData(0, 100, 0.15, 0)]
    [InlineData(1, -1, 0.15, 0)]
    [InlineData(1, 100, 0.12, 0)]
    [InlineData(1, 100, 0.15, 101)]
    public void Calculate_RejectsInvalidFiscalLines(int quantity, int unitPrice, double rate, int discount)
    {
        Assert.Throws<InvoiceCalculationException>(() =>
            InvoiceCalculationService.Calculate(
                [new("Línea inválida", quantity, unitPrice, false, (decimal)rate, false, discount)]));
    }
}
