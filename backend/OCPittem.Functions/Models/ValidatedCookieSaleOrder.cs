namespace OCPittem.Functions.Models;

public sealed record ValidatedCookieSaleOrder(
    string Name,
    string Email,
    string ClassName,
    int CoteDorQuantity,
    int LotusQuantity,
    int TotalPackages,
    int TotalAmountCents);
