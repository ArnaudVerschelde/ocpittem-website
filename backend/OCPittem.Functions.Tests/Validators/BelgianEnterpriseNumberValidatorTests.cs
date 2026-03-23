using OCPittem.Functions.Validators;

namespace OCPittem.Functions.Tests.Validators;

public class BelgianEnterpriseNumberValidatorTests
{
    // -----------------------------------------------------------------------
    // Valid numbers (real KBO numbers with correct check digits)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("0403.227.515")]          // KBC Groep NV
    [InlineData("0202.239.951")]          // Proximus NV
    [InlineData("0471.938.850")]          // Belfius Bank NV
    [InlineData("0403227515")]            // no separators
    [InlineData("BE 0403.227.515")]       // with BE prefix + space
    [InlineData("BE0403227515")]          // compact with BE prefix
    [InlineData(" 0403.227.515 ")]        // leading/trailing spaces
    public void IsValid_ValidNumber_ReturnsTrue(string input)
    {
        Assert.True(BelgianEnterpriseNumberValidator.IsValid(input));
    }

    // -----------------------------------------------------------------------
    // Invalid check digit
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("0403.227.516")]   // last digit off by one
    [InlineData("0403.227.500")]   // check digit zeroed out
    [InlineData("0202.239.952")]   // wrong check digit
    public void IsValid_WrongCheckDigit_ReturnsFalse(string input)
    {
        Assert.False(BelgianEnterpriseNumberValidator.IsValid(input));
    }

    // -----------------------------------------------------------------------
    // Wrong length
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("040322751")]      // 9 digits
    [InlineData("04032275150")]    // 11 digits
    [InlineData("040322751500")]   // 12 digits
    public void IsValid_WrongLength_ReturnsFalse(string input)
    {
        Assert.False(BelgianEnterpriseNumberValidator.IsValid(input));
    }

    // -----------------------------------------------------------------------
    // Wrong first digit
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("2403227515")]   // starts with 2
    [InlineData("9403227515")]   // starts with 9
    public void IsValid_WrongFirstDigit_ReturnsFalse(string input)
    {
        Assert.False(BelgianEnterpriseNumberValidator.IsValid(input));
    }

    // -----------------------------------------------------------------------
    // Null / empty / non-numeric
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghij")]
    [InlineData("0403.ABC.515")]
    public void IsValid_NullOrInvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(BelgianEnterpriseNumberValidator.IsValid(input));
    }

    // -----------------------------------------------------------------------
    // Normalize
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("0403227515",       "0403.227.515")]
    [InlineData("0403.227.515",     "0403.227.515")]
    [InlineData("BE 0403.227.515",  "0403.227.515")]
    [InlineData("BE0403227515",     "0403.227.515")]
    [InlineData("0202239951",       "0202.239.951")]
    public void Normalize_ValidDigitString_ReturnsFormattedNumber(string input, string expected)
    {
        Assert.Equal(expected, BelgianEnterpriseNumberValidator.Normalize(input));
    }

    [Fact]
    public void Normalize_InvalidLength_ReturnsOriginalValue()
    {
        const string bad = "123";
        Assert.Equal(bad, BelgianEnterpriseNumberValidator.Normalize(bad));
    }
}
