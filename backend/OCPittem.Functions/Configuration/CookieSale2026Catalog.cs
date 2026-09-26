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

    public static IReadOnlyList<string> AllowedClasses { get; } = [
        "Kleuter K1A - Nele De Brabandere",
        "Kleuter K1B - Lies Duyck & Eva Bernard",
        "Kleuter K1C - Fien Verhulst",
        "Kleuter K2A - Ellen Beeuwsaert",
        "Kleuter K2B - Dominique Delacauw",
        "Kleuter K3A - Emma Vervaeke & Hanne Devisch",
        "Kleuter K3B - Amandine Buyse & Hanne Devisch",
        "Lager L1A - Fien Delaert (Tanita Eechkhout)",
        "Lager L1B - Nathalie Vansteelant",
        "Lager L2A - Amber Vanluchene",
        "Lager L2B - Ine Vandeputte",
        "Lager L3A - Amber Lambert",
        "Lager L3B - Laura Vanderhaeghen",
        "Lager L4A - Eveline Demeyer & Femke Debie",
        "Lager L4B - Charlotte Vergote & Romy Heyman",
        "Lager L5A - Eveline Ooghe",
        "Lager L5B - Emmely Vanthuyne & Charlotte Vandenbussche",
        "Lager L6A - Fauve Farrazijn & Romy Heyman",
        "Lager L6B - Kelly Quintyn"
        ];

    public static bool IsOrderingAvailable => AllowedClasses.Count > 0;

    public static int CalculateTotalAmountCents(int coteDorQuantity, int lotusQuantity) =>
        checked(coteDorQuantity * CoteDorUnitPriceCents + lotusQuantity * LotusUnitPriceCents);
}
