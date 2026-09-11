using hotel_erp.Api.Dtos.Common;

namespace hotel_erp.UnitTests;

public class StrongPasswordAttributeTests
{
    private readonly StrongPasswordAttribute _attribute = new();

    [Theory]
    [InlineData("SinNumeroAlguno", false)]
    [InlineData("sinmayuscula123", false)]
    [InlineData("SINMINUSCULA123", false)]
    [InlineData("ClaveHotel123", true)]
    public void IsValid_RequiresUppercaseLowercaseAndNumber(string password, bool expected)
    {
        Assert.Equal(expected, _attribute.IsValid(password));
    }
}
