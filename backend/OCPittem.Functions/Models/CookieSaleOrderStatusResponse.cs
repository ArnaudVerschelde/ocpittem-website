namespace OCPittem.Functions.Models;

public sealed record CookieSaleOrderStatusResponse(
    string PaymentStatus,
    string? ConfirmationNumber);
