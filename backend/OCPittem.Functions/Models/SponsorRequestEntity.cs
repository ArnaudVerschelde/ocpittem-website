using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class SponsorRequestEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Sponsor";
    public string RowKey { get; set; } = string.Empty;  // RequestId (GUID)
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string StripeSessionId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Package { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;   // kept for backwards-compat with existing rows
    public string EnterpriseNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int ExtraEtenPartyCount { get; set; }
    public int ExtraVegetarischCount { get; set; }
    public int ExtraDrankkaart20Count { get; set; }
    public int IncludedVegetarischCount { get; set; }
    public bool SponsorAttends { get; set; }
    public int SponsorAttendeesCount { get; set; }
    public string LogoUrl { get; set; } = string.Empty;
    public string PdfBlobUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
