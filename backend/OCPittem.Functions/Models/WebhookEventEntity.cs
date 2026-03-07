using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class WebhookEventEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Stripe";
    public string RowKey { get; set; } = string.Empty;  // StripeEventId
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string Result { get; set; } = string.Empty;
}
