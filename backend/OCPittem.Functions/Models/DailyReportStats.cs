namespace OCPittem.Functions.Models;

public record DailyReportStats(
    int TotalOrders,
    int PaidOrders,
    int TotalToegangstickets,
    int TotalEtenPartyTickets,
    int TotalVegetarisch,
    int TotalDrankkaart10,
    int TotalDrankkaart20,
    decimal TotalRevenue,
    int TotalSponsorRequests,
    int PaidSponsorOrders,
    int TotalSponsorBrons,
    int TotalSponsorZilver,
    int TotalSponsorGoud,
    int TotalSponsorExtraEtenParty,
    int TotalSponsorExtraVegetarisch,
    int TotalSponsorExtraDrankkaart20,
    decimal TotalSponsorRevenue);
