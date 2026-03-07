using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class SponsorRequestEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Sponsor";
    public string RowKey { get; set; } = string.Empty;  // RequestId (GUID)
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
