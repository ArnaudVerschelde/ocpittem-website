using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public interface IStorageService
{
    Task SaveOrderAsync(OrderEntity order);
    Task<OrderEntity?> GetOrderByStripeSessionAsync(string sessionId);
    Task UpdateOrderAsync(OrderEntity order);
    Task SaveTicketAsync(TicketEntity ticket);
    Task<bool> WebhookEventExistsAsync(string eventId);
    Task SaveWebhookEventAsync(WebhookEventEntity webhookEvent);
    Task<WebhookEventBeginResult> TryBeginWebhookEventAsync(
        string eventId,
        bool allowRetry,
        DateTimeOffset receivedUtc);
    Task SaveSponsorRequestAsync(SponsorRequestEntity request);
    Task UpsertWebhookEventAsync(WebhookEventEntity webhookEvent);
    Task<string> SaveTicketPdfAsync(string orderId, byte[] pdf);
    Task<TicketEntity?> GetTicketByIdAsync(string ticketId);
    Task MarkTicketScannedAsync(TicketEntity ticket);
    Task<IReadOnlyList<OrderEntity>> GetAllOrdersAsync();
    Task<IReadOnlyList<SponsorRequestEntity>> GetAllSponsorRequestsAsync();
    Task<SponsorRequestEntity?> GetSponsorRequestByStripeSessionAsync(string sessionId);
    Task UpdateSponsorRequestAsync(SponsorRequestEntity request);
    Task<string> SaveSponsorLogoAsync(string logoId, Stream stream, string contentType, string extension);
    Task<string> SaveSponsorAttestationAsync(string requestId, byte[] pdf);
    Task<SponsorRequestEntity?> GetSponsorRequestByIdAsync(string requestId);
    Task<IReadOnlyList<TicketEntity>> GetTicketsByOrderIdAsync(string orderId);
    Task<IReadOnlyList<string>> GetGalleryImageUrlsAsync(string containerName, TimeSpan sasLifetime);
    Task<IReadOnlyList<GalleryImageDto>> GetGalleryImagesAsync(string containerName,TimeSpan sasLifetime);
    Task SaveCookieOrderAsync(CookieOrderEntity order);
    Task<CookieOrderEntity?> GetCookieOrderAsync(string orderId);
    Task<CookieOrderEntity?> GetCookieOrderByStripeSessionAsync(string sessionId);
    Task<IReadOnlyList<CookieOrderEntity>> GetPaidCookieOrdersAsync();
    Task SetCookieOrderStripeSessionAsync(string orderId, string sessionId);
    Task<CookieOrderEntity?> TryMarkCookieOrderPaidAsync(
        string orderId,
        string sessionId,
        string paymentIntentId,
        DateTimeOffset paidUtc);
    Task TryMarkCookieOrderStatusAsync(string orderId, CookieOrderStatus status);
    Task<CookieOrderEntity?> TryClaimCookieOrderConfirmationEmailAsync(
        string orderId,
        string claimId,
        DateTimeOffset claimedUtc);
    Task MarkCookieOrderConfirmationEmailSentAsync(
        string orderId,
        string claimId,
        DateTimeOffset sentUtc);
    Task ReleaseCookieOrderConfirmationEmailClaimAsync(
        string orderId,
        string claimId);
}
