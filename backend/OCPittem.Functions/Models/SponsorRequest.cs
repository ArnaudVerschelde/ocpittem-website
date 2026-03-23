namespace OCPittem.Functions.Models;

public sealed record SponsorRequest(
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string Package,
    string Message,
    string EnterpriseNumber,
    int ExtraEtenPartyCount = 0,
    int ExtraVegetarischCount = 0,
    int ExtraDrankkaart20Count = 0,
    int IncludedVegetarischCount = 0);
