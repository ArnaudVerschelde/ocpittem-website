namespace OCPittem.Functions.Models;

public sealed record SponsorRequest(
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string Package,
    string Message);
