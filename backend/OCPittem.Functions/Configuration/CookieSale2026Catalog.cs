namespace OCPittem.Functions.Configuration;

public static class CookieSale2026Catalog
{
    public const string Flow = "cookie-sale-2026";
    public const string PartitionKey = "COOKIE_SALE_2026";
    public const string CoteDorLabel = "Côte d'Or pakket";
    public const string LotusLabel = "Lotus pakket";
    public const int CoteDorUnitPriceCents = 1100;
    public const int LotusUnitPriceCents = 900;
    public const int MaximumTotalPackages = 50;

    public static DateOnly EventDate { get; } = new(2026, 12, 10);

    // TODO: Vul hier de definitieve, door de school bevestigde klassenlijst in.
    // Dit is de enige autoritatieve bron; frontend en backend gebruiken deze lijst via de config-API.
    public static IReadOnlyList<string> AllowedClasses { get; } = [
        "Kleuter 1A",
        "Kleuter 1B",
        "Kleuter 1C",
        ];

    public static bool IsOrderingAvailable => AllowedClasses.Count > 0;

    public static int CalculateTotalAmountCents(int coteDorQuantity, int lotusQuantity) =>
        checked(coteDorQuantity * CoteDorUnitPriceCents + lotusQuantity * LotusUnitPriceCents);
}
