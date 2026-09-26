namespace OCPittem.Functions.Models;

public sealed record CookieSaleConfirmationData(
    string OrderId,
    string ConfirmationNumber,
    string Name,
    string StudentName,
    string Email,
    string ClassName,
    int CoteDorQuantity,
    int LotusQuantity,
    int TotalPackages,
    int TotalAmountCents);
