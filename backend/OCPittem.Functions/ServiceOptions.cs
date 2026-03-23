namespace OCPittem.Functions;

public class StripeOptions
{
    public string SecretKey { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
    public string PriceIdToegangsticket { get; init; } = "";
    public string PriceIdEtenParty { get; init; } = "";
    public string PriceIdDrankkaart10 { get; init; } = "";
    public string PriceIdDrankkaart20 { get; init; } = "";
    public string PriceIdSponsorBrons { get; init; } = "";
    public string PriceIdSponsorZilver { get; init; } = "";
    public string PriceIdSponsorGoud { get; init; } = "";
}

public class MailjetOptions
{
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "";
    public string ContactFromEmail { get; init; } = "";
    public string ContactFromName { get; init; } = "";
    public string TicketFromEmail { get; init; } = "";
    public string TicketFromName { get; init; } = "";
}

public class EmailOptions
{
    public bool Enabled { get; init; }
}

public class AppOptions
{
    public string FrontendUrl { get; init; } = "http://localhost:5173";
    public string ContactEmail { get; init; } = "";
    public string TicketHmacSecret { get; init; } = "";
    public string ReportRecipients { get; init; } = "";

    public IReadOnlyList<string> GetReportRecipients() =>
        ReportRecipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public class StorageOptions
{
    public string TableNameOrders { get; init; } = "Orders";
    public string TableNameTickets { get; init; } = "Tickets";
    public string TableNameWebhookEvents { get; init; } = "WebhookEvents";
    public string TableNameSponsors { get; init; } = "SponsorRequests";
    public string BlobContainerTickets { get; init; } = "ticket-pdfs";
    public string BlobContainerSponsorLogos { get; init; } = "sponsor-logos";
}
