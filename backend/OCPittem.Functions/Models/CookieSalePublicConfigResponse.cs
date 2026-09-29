namespace OCPittem.Functions.Models;

public sealed record CookieSaleProductResponse(
    string Id,
    string Label,
    int UnitPriceCents);

public sealed record CookieSalePublicConfigResponse(
    bool Enabled,
    string EventDate,
    int MaximumTotalPackages,
    IReadOnlyList<string> Classes,
    IReadOnlyList<string> StaffCategories,
    IReadOnlyList<CookieSaleProductResponse> Products);
