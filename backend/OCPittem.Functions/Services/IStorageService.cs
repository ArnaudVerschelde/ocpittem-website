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
    Task SaveSponsorRequestAsync(SponsorRequestEntity request);
    Task UpsertWebhookEventAsync(WebhookEventEntity webhookEvent);
    Task<string> SaveTicketPdfAsync(string orderId, byte[] pdf);
    Task<TicketEntity?> GetTicketByIdAsync(string ticketId);
    Task MarkTicketScannedAsync(TicketEntity ticket);
    Task<IReadOnlyList<OrderEntity>> GetAllOrdersAsync();
    Task<IReadOnlyList<SponsorRequestEntity>> GetAllSponsorRequestsAsync();
    Task<SponsorRequestEntity?> GetSponsorRequestByStripeSessionAsync(string sessionId);
    Task UpdateSponsorRequestAsync(SponsorRequestEntity request);
}
