using System.Text.RegularExpressions;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class ConfirmationNumberGeneratorTests
{
    [Fact]
    public void Generate_ReturnsExpectedFormat()
    {
        var confirmationNumber = ConfirmationNumberGenerator.Generate();

        Assert.Matches(new Regex("^KV26-[23456789A-HJ-NP-Z]{12}$"), confirmationNumber);
    }

    [Fact]
    public void Generate_ReturnsDistinctValues()
    {
        var values = Enumerable.Range(0, 1000)
            .Select(_ => ConfirmationNumberGenerator.Generate())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(1000, values.Count);
    }
}
