namespace OCPittem.Functions.Models;

public sealed record CreateCheckoutRequest(
    string Name,
    string Email,
    int ToegangsticketCount,
    int EtenPartyCount,
    int VegetarischCount,
    int Drankkaart10Count,
    int Drankkaart20Count);
