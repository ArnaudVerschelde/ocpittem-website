using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class TableStorageService : IStorageService
{
    private readonly TableServiceClient _serviceClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _ordersTable;
    private readonly string _ticketsTable;
    private readonly string _webhookEventsTable;
    private readonly string _sponsorsTable;
    private readonly string _ticketPdfsContainer;

    public TableStorageService(string connectionString, StorageOptions options)
    {
        _serviceClient = new TableServiceClient(connectionString);
        _blobServiceClient = new BlobServiceClient(connectionString);
        _ordersTable = options.TableNameOrders;
        _ticketsTable = options.TableNameTickets;
        _webhookEventsTable = options.TableNameWebhookEvents;
        _sponsorsTable = options.TableNameSponsors;
        _ticketPdfsContainer = options.BlobContainerTickets;
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

    public async Task<string> SaveTicketPdfAsync(string orderId, byte[] pdf)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_ticketPdfsContainer);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = container.GetBlobClient($"{orderId}/tickets.pdf");
        using var stream = new MemoryStream(pdf);
        await blobClient.UploadAsync(stream, overwrite: true);
        return blobClient.Uri.ToString();
    }

    public async Task<TicketEntity?> GetTicketByIdAsync(string ticketId)
    {
        var table = await GetTableAsync(_ticketsTable);
        var results = table.QueryAsync<TicketEntity>(e => e.RowKey == ticketId);

        await foreach (var entity in results)
        {
            return entity;
        }

        return null;
    }

    public async Task MarkTicketScannedAsync(TicketEntity ticket)
    {
        ticket.ScannedAt = DateTime.UtcNow;
        var table = await GetTableAsync(_ticketsTable);
        await table.UpdateEntityAsync(ticket, ticket.ETag, TableUpdateMode.Replace);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllOrdersAsync()
    {
        var table = await GetTableAsync(_ordersTable);
        var results = new List<OrderEntity>();
        await foreach (var entity in table.QueryAsync<OrderEntity>())
            results.Add(entity);
        return results;
    }

    public async Task<IReadOnlyList<SponsorRequestEntity>> GetAllSponsorRequestsAsync()
    {
        var table = await GetTableAsync(_sponsorsTable);
        var results = new List<SponsorRequestEntity>();
        await foreach (var entity in table.QueryAsync<SponsorRequestEntity>(e => e.PartitionKey == "Sponsor"))
            results.Add(entity);
        return results;
    }
}
