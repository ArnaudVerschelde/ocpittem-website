namespace OCPittem.Functions.Models;

public sealed record CreateCookieSaleCheckoutRequest(
    string Name,
    string StudentName,
    string Email,
    string ClassName,
    int CoteDorQuantity,
    int LotusQuantity);
