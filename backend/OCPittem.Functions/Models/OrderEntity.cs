using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;  // EventId (e.g. "balparental-2026")
    public string RowKey { get; set; } = string.Empty;         // OrderId (GUID)
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string StripeSessionId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "pending"; // pending, paid, failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
