using System.Text.Json;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Tests.Models;

/// <summary>
/// Pins the wire format (property names/casing) the React frontend relies on, so a
/// serializer change in the Worker 2.x / ASP.NET Core 10 stack is caught by tests.
/// ASP.NET Core's MVC result executor uses JsonSerializerDefaults.Web (camelCase).
/// </summary>
public class CookieSaleDtoJsonContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PublicConfig_SerializesExpectedShape()
    {
        var dto = new CookieSalePublicConfigResponse(
            true, "2026-11-01", 10, ["1A"], ["Leerkracht"], [new CookieSaleProductResponse("lotus", "Lotus", 900)]);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto, Web));
        var root = doc.RootElement;

        Assert.True(root.GetProperty("enabled").GetBoolean());
        Assert.Equal("2026-11-01", root.GetProperty("eventDate").GetString());
        Assert.Equal(10, root.GetProperty("maximumTotalPackages").GetInt32());
        Assert.Equal("1A", root.GetProperty("classes")[0].GetString());
        Assert.Equal("Leerkracht", root.GetProperty("staffCategories")[0].GetString());
        var product = root.GetProperty("products")[0];
        Assert.Equal("lotus", product.GetProperty("id").GetString());
        Assert.Equal("Lotus", product.GetProperty("label").GetString());
        Assert.Equal(900, product.GetProperty("unitPriceCents").GetInt32());
    }

    [Fact]
    public void OrderStatus_SerializesExpectedShape()
    {
        var dto = new CookieSaleOrderStatusResponse("Paid", "KV26-23456789ABCD");

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(dto, Web));

        Assert.Equal("Paid", doc.RootElement.GetProperty("paymentStatus").GetString());
        Assert.Equal("KV26-23456789ABCD", doc.RootElement.GetProperty("confirmationNumber").GetString());
        Assert.Equal(2, doc.RootElement.EnumerateObject().Count());
    }
}
