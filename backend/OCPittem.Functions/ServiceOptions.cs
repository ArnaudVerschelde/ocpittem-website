namespace OCPittem.Functions;

public class StripeOptions
{
    public string SecretKey { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
    public string TicketPriceId { get; init; } = "";
}

public class MailjetOptions
{
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "";
}

public class EmailOptions
{
    public bool Enabled { get; init; }
}

public class AppOptions
{
    public string FrontendUrl { get; init; } = "http://localhost:5173";
    public string ContactEmail { get; init; } = "";
}

public class StorageOptions
{
    public string TableNameOrders { get; init; } = "Orders";
    public string TableNameTickets { get; init; } = "Tickets";
    public string TableNameWebhookEvents { get; init; } = "WebhookEvents";
    public string TableNameSponsors { get; init; } = "SponsorRequests";
}
