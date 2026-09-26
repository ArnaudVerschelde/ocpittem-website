using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OCPittem.Functions.Configuration;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class TableStorageService : IStorageService
{
    private readonly TableServiceClient _serviceClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _ordersTable;
    private readonly string _cookieOrdersTable;
    private readonly string _ticketsTable;
    private readonly string _webhookEventsTable;
    private readonly string _sponsorsTable;
    private readonly string _ticketPdfsContainer;
    private readonly string _sponsorLogosContainer;
    private readonly string _sponsorAttestationsContainer;

    public TableStorageService(string connectionString, StorageOptions options)
    {
        _serviceClient = new TableServiceClient(connectionString);
        _blobServiceClient = new BlobServiceClient(connectionString);
        _ordersTable = options.TableNameOrders;
        _cookieOrdersTable = options.TableNameCookieOrders;
        _ticketsTable = options.TableNameTickets;
        _webhookEventsTable = options.TableNameWebhookEvents;
        _sponsorsTable = options.TableNameSponsors;
        _ticketPdfsContainer = options.BlobContainerTickets;
        _sponsorLogosContainer = options.BlobContainerSponsorLogos;
        _sponsorAttestationsContainer = options.BlobContainerSponsorAttestations;
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

    public async Task<WebhookEventBeginResult> TryBeginWebhookEventAsync(
        string eventId,
        bool allowRetry,
        DateTimeOffset receivedUtc)
    {
        var table = await GetTableAsync(_webhookEventsTable);
        var newEvent = new WebhookEventEntity
        {
            PartitionKey = "Stripe",
            RowKey = eventId,
            ReceivedAt = receivedUtc.UtcDateTime,
            Result = "received",
        };

        try
        {
            await table.AddEntityAsync(newEvent);
            return WebhookEventBeginResult.Acquired;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
        }

        WebhookEventEntity existingEvent;
        try
        {
            existingEvent = (await table.GetEntityAsync<WebhookEventEntity>("Stripe", eventId)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return WebhookEventBeginResult.InProgress;
        }

        if (string.Equals(existingEvent.Result, "processed", StringComparison.Ordinal)
            || !allowRetry)
        {
            return WebhookEventBeginResult.AlreadyProcessed;
        }

        var claimExpired = existingEvent.ReceivedAt <= receivedUtc.UtcDateTime.AddMinutes(-5);
        var failedAttempt = existingEvent.Result.StartsWith("error:", StringComparison.Ordinal);
        if (!failedAttempt && !claimExpired)
            return WebhookEventBeginResult.InProgress;

        existingEvent.ReceivedAt = receivedUtc.UtcDateTime;
        existingEvent.ProcessedAt = null;
        existingEvent.Result = "received";

        try
        {
            await table.UpdateEntityAsync(existingEvent, existingEvent.ETag, TableUpdateMode.Replace);
            return WebhookEventBeginResult.Acquired;
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            return WebhookEventBeginResult.InProgress;
        }
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

    public async Task<SponsorRequestEntity?> GetSponsorRequestByStripeSessionAsync(string sessionId)
    {
        var table = await GetTableAsync(_sponsorsTable);
        await foreach (var entity in table.QueryAsync<SponsorRequestEntity>(e => e.StripeSessionId == sessionId))
            return entity;
        return null;
    }

    public async Task UpdateSponsorRequestAsync(SponsorRequestEntity request)
    {
        var table = await GetTableAsync(_sponsorsTable);
        await table.UpdateEntityAsync(request, request.ETag, TableUpdateMode.Replace);
    }

    public async Task<SponsorRequestEntity?> GetSponsorRequestByIdAsync(string requestId)
    {
        var table = await GetTableAsync(_sponsorsTable);
        try
        {
            var response = await table.GetEntityAsync<SponsorRequestEntity>("Sponsor", requestId);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TicketEntity>> GetTicketsByOrderIdAsync(string orderId)
    {
        var table = await GetTableAsync(_ticketsTable);
        var results = new List<TicketEntity>();
        await foreach (var entity in table.QueryAsync<TicketEntity>(e => e.PartitionKey == orderId))
            results.Add(entity);
        return results;
    }

    public async Task<string> SaveSponsorLogoAsync(string logoId, Stream stream, string contentType, string extension)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_sponsorLogosContainer);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = container.GetBlobClient($"{logoId}{extension}");
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddYears(5));
        return sasUri.ToString();
    }

    public async Task<string> SaveSponsorAttestationAsync(string requestId, byte[] pdf)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_sponsorAttestationsContainer);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = container.GetBlobClient($"{requestId}/sponsorattest.pdf");
        var headers = new BlobHttpHeaders { ContentType = "application/pdf" };
        using var stream = new MemoryStream(pdf);
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddYears(5));
        return sasUri.ToString();
    }

    public async Task<IReadOnlyList<string>> GetGalleryImageUrlsAsync(string containerName, TimeSpan sasLifetime)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);

        if (!await container.ExistsAsync())
            return Array.Empty<string>();

        var expiresOn = DateTimeOffset.UtcNow.Add(sasLifetime);
        var urls = new List<string>();

        await foreach (var blob in container.GetBlobsAsync())
        {
            if (!GalleryImageFilter.IsSupportedImage(blob.Name))
                continue;

            var blobClient = container.GetBlobClient(blob.Name);

            if (!blobClient.CanGenerateSasUri)
                continue;

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, expiresOn);
            urls.Add(sasUri.ToString());
        }

        return urls;
    }

    public async Task<IReadOnlyList<GalleryImageDto>> GetGalleryImagesAsync(string containerName, TimeSpan sasLifetime)
    {
        var containerClient = _blobServiceClient
            .GetBlobContainerClient(containerName);

        if (!await containerClient.ExistsAsync())
        {
            return Array.Empty<GalleryImageDto>();
        }

        var blobNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            blobNames.Add(blob.Name);
        }

        var images = new List<GalleryImageDto>();
        var expiresOn = DateTimeOffset.UtcNow.Add(sasLifetime);

        foreach (var originalBlobName in blobNames.Where(IsOriginalGalleryImage))
        {
            var category = GetCategory(originalBlobName);

            if (category is null)
            {
                continue;
            }

            var thumbnailBlobName =
                $"thumbnails/{Path.ChangeExtension(originalBlobName, ".webp")}"
                    .Replace('\\', '/');

            if (!blobNames.Contains(thumbnailBlobName))
            {
                continue;
            }

            var originalBlob = containerClient
                .GetBlobClient(originalBlobName);

            var thumbnailBlob = containerClient
                .GetBlobClient(thumbnailBlobName);

            var originalUrl = CreateReadSasUrl(
                originalBlob,
                expiresOn);

            var thumbnailUrl = CreateReadSasUrl(
                thumbnailBlob,
                expiresOn);

            images.Add(new GalleryImageDto(
                Name: Path.GetFileName(originalBlobName),
                Category: category,
                OriginalUrl: originalUrl,
                ThumbnailUrl: thumbnailUrl));
        }

        return images;
    }

    private static bool IsOriginalGalleryImage(string blobName)
    {
        if (blobName.StartsWith(
            "thumbnails/",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(blobName);

        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetCategory(string blobName)
    {
        var normalizedName = blobName
            .Replace('\\', '/')
            .ToLowerInvariant();

        if (normalizedName.StartsWith("fotograaf/"))
        {
            return "fotograaf";
        }

        if (normalizedName.StartsWith("photobooth/"))
        {
            return "photobooth";
        }

        if (normalizedName.StartsWith("sfeerbeelden/"))
        {
            return "sfeerbeelden";
        }

        return null;
    }

    private static string CreateReadSasUrl(BlobClient blobClient, DateTimeOffset expiresOn)
    {
        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                $"Er kan geen SAS-URL worden gegenereerd voor blob '{blobClient.Name}'.");
        }

        return blobClient
            .GenerateSasUri(BlobSasPermissions.Read, expiresOn)
            .ToString();
    }

    public async Task SaveCookieOrderAsync(CookieOrderEntity order)
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        await table.AddEntityAsync(order);
    }

    public async Task<CookieOrderEntity?> GetCookieOrderAsync(string orderId)
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        try
        {
            var response = await table.GetEntityAsync<CookieOrderEntity>(
                CookieSale2026Catalog.PartitionKey,
                orderId);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<CookieOrderEntity?> GetCookieOrderByStripeSessionAsync(string sessionId)
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        var filter = TableClient.CreateQueryFilter<CookieOrderEntity>(
            order => order.PartitionKey == CookieSale2026Catalog.PartitionKey
                && order.StripeCheckoutSessionId == sessionId);

        await foreach (var order in table.QueryAsync<CookieOrderEntity>(filter: filter, maxPerPage: 1))
            return order;

        return null;
    }

    public async Task<IReadOnlyList<CookieOrderEntity>> GetPaidCookieOrdersAsync()
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        var paidStatus = nameof(CookieOrderStatus.Paid);
        var filter = TableClient.CreateQueryFilter<CookieOrderEntity>(
            order => order.PartitionKey == CookieSale2026Catalog.PartitionKey
                && order.PaymentStatus == paidStatus);
        var orders = new List<CookieOrderEntity>();

        await foreach (var order in table.QueryAsync<CookieOrderEntity>(filter: filter))
            orders.Add(order);

        return orders;
    }

    public async Task SetCookieOrderStripeSessionAsync(string orderId, string sessionId)
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        var patch = new TableEntity(CookieSale2026Catalog.PartitionKey, orderId)
        {
            [nameof(CookieOrderEntity.StripeCheckoutSessionId)] = sessionId,
        };
        await table.UpdateEntityAsync(patch, ETag.All, TableUpdateMode.Merge);
    }

    public async Task<CookieOrderEntity?> TryMarkCookieOrderPaidAsync(
        string orderId,
        string sessionId,
        string paymentIntentId,
        DateTimeOffset paidUtc)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var order = await GetCookieOrderAsync(orderId);
            if (order is null)
                return null;

            if (order.PaymentStatus == nameof(CookieOrderStatus.Paid))
                return order;

            if (order.PaymentStatus != nameof(CookieOrderStatus.Pending))
                return null;

            order.PaymentStatus = nameof(CookieOrderStatus.Paid);
            order.PaidUtc = paidUtc;
            order.StripeCheckoutSessionId = sessionId;
            order.StripePaymentIntentId = paymentIntentId;

            try
            {
                var table = await GetTableAsync(_cookieOrdersTable);
                await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
                return order;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
            }
        }

        return await GetCookieOrderAsync(orderId);
    }

    public async Task TryMarkCookieOrderStatusAsync(string orderId, CookieOrderStatus status)
    {
        if (status is CookieOrderStatus.Pending or CookieOrderStatus.Paid)
            throw new ArgumentOutOfRangeException(nameof(status), status, "Only terminal non-paid statuses are supported.");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var order = await GetCookieOrderAsync(orderId);
            if (order is null || order.PaymentStatus != nameof(CookieOrderStatus.Pending))
                return;

            order.PaymentStatus = status.ToString();

            try
            {
                var table = await GetTableAsync(_cookieOrdersTable);
                await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
            }
        }
    }

    public async Task<CookieOrderEntity?> TryClaimCookieOrderConfirmationEmailAsync(
        string orderId,
        string claimId,
        DateTimeOffset claimedUtc)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var order = await GetCookieOrderAsync(orderId);
            if (order is null
                || order.PaymentStatus != nameof(CookieOrderStatus.Paid)
                || order.ConfirmationEmailSentUtc.HasValue)
            {
                return null;
            }

            var activeClaim = order.ConfirmationEmailClaimedUtc.HasValue
                && order.ConfirmationEmailClaimedUtc > claimedUtc.AddMinutes(-15);
            if (activeClaim)
                return null;

            order.ConfirmationEmailClaimId = claimId;
            order.ConfirmationEmailClaimedUtc = claimedUtc;

            try
            {
                var table = await GetTableAsync(_cookieOrdersTable);
                await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
                return order;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
            }
        }

        return null;
    }

    public async Task MarkCookieOrderConfirmationEmailSentAsync(
        string orderId,
        string claimId,
        DateTimeOffset sentUtc)
    {
        var order = await GetCookieOrderAsync(orderId);
        if (order is null
            || order.ConfirmationEmailSentUtc.HasValue
            || order.ConfirmationEmailClaimId != claimId)
        {
            return;
        }

        order.ConfirmationEmailSentUtc = sentUtc;
        await UpdateCookieOrderAsync(order);
    }

    public async Task ReleaseCookieOrderConfirmationEmailClaimAsync(
        string orderId,
        string claimId)
    {
        var order = await GetCookieOrderAsync(orderId);
        if (order is null
            || order.ConfirmationEmailSentUtc.HasValue
            || order.ConfirmationEmailClaimId != claimId)
        {
            return;
        }

        order.ConfirmationEmailClaimId = string.Empty;
        order.ConfirmationEmailClaimedUtc = null;
        await UpdateCookieOrderAsync(order);
    }

    private async Task UpdateCookieOrderAsync(CookieOrderEntity order)
    {
        var table = await GetTableAsync(_cookieOrdersTable);
        await table.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);
    }
}