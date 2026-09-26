namespace OCPittem.Functions.Models;

public sealed record CreateCookieSaleCheckoutRequest(
    string Name,
    string Email,
    string ClassName,
    int CoteDorQuantity,
    int LotusQuantity);
