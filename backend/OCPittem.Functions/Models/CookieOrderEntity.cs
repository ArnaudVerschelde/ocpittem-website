using Azure.Data.Tables;

namespace OCPittem.Functions.Models;

public class CookieOrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string OrderId { get; set; } = string.Empty;
    public string ConfirmationNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidUtc { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int CoteDorQuantity { get; set; }
    public int LotusQuantity { get; set; }
    public int TotalPackages { get; set; }
    public int TotalAmountCents { get; set; }
    public string PaymentStatus { get; set; } = nameof(CookieOrderStatus.Pending);
    public string StripeCheckoutSessionId { get; set; } = string.Empty;
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string ConfirmationEmailClaimId { get; set; } = string.Empty;
    public DateTimeOffset? ConfirmationEmailClaimedUtc { get; set; }
    public DateTimeOffset? ConfirmationEmailSentUtc { get; set; }
}
