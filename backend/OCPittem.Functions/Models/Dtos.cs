namespace OCPittem.Functions.Models;

public record CreateCheckoutRequest(string Name, string Email, int Quantity);

public record CreateCheckoutResponse(string CheckoutUrl);

public record ContactRequest(string Name, string Email, string Subject, string Message);

public record ContactResponse(bool Success);

public record SponsorRequest(
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string Package,
    string Message
);

public record SponsorResponse(bool Success);
