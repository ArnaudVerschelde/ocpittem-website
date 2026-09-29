namespace OCPittem.Functions.Models;

public sealed record ValidatedCookieSaleOrder(
    string Name,
    string OrderType,
    string? StudentName,
    string Email,
    string? ClassName,
    string? StaffCategory,
    int CoteDorQuantity,
    int LotusQuantity,
    int TotalPackages,
    int TotalAmountCents);
