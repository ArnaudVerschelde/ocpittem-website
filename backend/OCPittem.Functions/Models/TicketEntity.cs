using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class TicketEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;  // OrderId
    public string RowKey { get; set; } = string.Empty;         // TicketId
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string QrPayload { get; set; } = string.Empty;
    public DateTime? ScannedAt { get; set; }
    public string PdfBlobUrl { get; set; } = string.Empty;
}
