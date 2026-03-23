namespace OCPittem.Functions.Models;

public sealed record SponsorRequest(
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    string Package,
    string EnterpriseNumber,
    string Street,
    string HouseNumber,
    string PostalCode,
    string City,
    int ExtraEtenPartyCount = 0,
    int ExtraVegetarischCount = 0,
    int ExtraDrankkaart20Count = 0,
    int IncludedVegetarischCount = 0,
    bool SponsorAttends = false,
    int SponsorAttendeesCount = 0);
