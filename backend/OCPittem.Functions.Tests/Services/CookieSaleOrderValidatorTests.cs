using OCPittem.Functions.Configuration;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class CookieSaleOrderValidatorTests
{
    private static readonly IReadOnlyList<string> Classes = ["Testklas"];

    [Theory]
    [InlineData(1, 0, 1100)]
    [InlineData(0, 1, 900)]
    [InlineData(2, 1, 3100)]
    public void TryValidate_ValidQuantities_CalculatesServerTotal(
        int coteDorQuantity,
        int lotusQuantity,
        int expectedTotalCents)
    {
        var request = CreateRequest(coteDorQuantity, lotusQuantity);

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out var order, out _);

        Assert.True(valid);
        Assert.NotNull(order);
        Assert.Equal(expectedTotalCents, order.TotalAmountCents);
    }

    [Fact]
    public void TryValidate_ZeroProducts_IsRejected()
    {
        var valid = CookieSaleOrderValidator.TryValidate(
            CreateRequest(0, 0), Classes, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_NegativeQuantity_IsRejected()
    {
        var valid = CookieSaleOrderValidator.TryValidate(
            CreateRequest(-1, 1), Classes, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_QuantityAboveMaximum_IsRejected()
    {
        var valid = CookieSaleOrderValidator.TryValidate(
            CreateRequest(CookieSale2026Catalog.MaximumTotalPackages, 1),
            Classes,
            out _,
            out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_InvalidEmail_IsRejected()
    {
        var request = CreateRequest(1, 0) with { Email = "geen-email" };

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_InvalidClass_IsRejected()
    {
        var request = CreateRequest(1, 0) with { ClassName = "Onbekend" };

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_ClassMatch_UsesCanonicalConfiguredValue()
    {
        var request = CreateRequest(1, 0) with { ClassName = "testKLAS" };

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out var order, out _);

        Assert.True(valid);
        Assert.Equal("Testklas", order!.ClassName);
    }

    [Fact]
    public void TryValidate_MissingStudentName_IsRejected()
    {
        var request = CreateRequest(1, 0) with { StudentName = "   " };

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryValidate_StudentName_IsTrimmed()
    {
        var request = CreateRequest(1, 0) with { StudentName = "  Leerling Test  " };

        var valid = CookieSaleOrderValidator.TryValidate(request, Classes, out var order, out _);

        Assert.True(valid);
        Assert.Equal("Leerling Test", order!.StudentName);
    }

    private static CreateCookieSaleCheckoutRequest CreateRequest(int coteDorQuantity, int lotusQuantity) =>
        new(
            Name: "Test Ouder",
            StudentName: "Test Leerling",
            Email: "ouder@example.com",
            ClassName: "Testklas",
            CoteDorQuantity: coteDorQuantity,
            LotusQuantity: lotusQuantity);
}
