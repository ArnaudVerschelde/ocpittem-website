using Azure;
using Azure.Data.Tables;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class TableStorageService : IStorageService
{
    private readonly TableServiceClient _serviceClient;
    private readonly string _ordersTable;
    private readonly string _ticketsTable;
    private readonly string _webhookEventsTable;
    private readonly string _sponsorsTable;

    public TableStorageService(string connectionString, StorageOptions options)
    {
        _serviceClient = new TableServiceClient(connectionString);
        _ordersTable = options.TableNameOrders;
        _ticketsTable = options.TableNameTickets;
        _webhookEventsTable = options.TableNameWebhookEvents;
        _sponsorsTable = options.TableNameSponsors;
    }

    private async Task<TableClient> GetTableAsync(string tableName)
    {
        var client = _serviceClient.GetTableClient(tableName);
        await client.CreateIfNotExistsAsync();
        return client;
    }

    public async Task SaveOrderAsync(OrderEntity order)
    {
        var table = await GetTableAsync(_ordersTable);
        await table.AddEntityAsync(order);
    }

    public async Task<OrderEntity?> GetOrderByStripeSessionAsync(string sessionId)
    {
        var table = await GetTableAsync(_ordersTable);
        var results = table.QueryAsync<OrderEntity>(e => e.StripeSessionId == sessionId);

        await foreach (var entity in results)
        {
            return entity;
        }

        return null;
    }

    public async Task UpdateOrderAsync(OrderEntity order)
    {
        var table = await GetTableAsync(_ordersTable);
        await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
    }

    public async Task SaveTicketAsync(TicketEntity ticket)
    {
        var table = await GetTableAsync(_ticketsTable);
        await table.AddEntityAsync(ticket);
    }

    public async Task<bool> WebhookEventExistsAsync(string eventId)
    {
        var table = await GetTableAsync(_webhookEventsTable);
        try
        {
            await table.GetEntityAsync<WebhookEventEntity>("Stripe", eventId);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task SaveWebhookEventAsync(WebhookEventEntity webhookEvent)
    {
        var table = await GetTableAsync(_webhookEventsTable);
        await table.AddEntityAsync(webhookEvent);
    }

    public async Task SaveSponsorRequestAsync(SponsorRequestEntity request)
    {
        var table = await GetTableAsync(_sponsorsTable);
        await table.AddEntityAsync(request);
    }

    public async Task UpsertWebhookEventAsync(WebhookEventEntity webhookEvent)
    {
        var table = await GetTableAsync(_webhookEventsTable);
        await table.UpsertEntityAsync(webhookEvent, TableUpdateMode.Replace);
    }
}
