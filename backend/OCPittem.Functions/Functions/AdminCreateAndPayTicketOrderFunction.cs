using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class AdminCreateAndPayTicketOrderFunction
{
    private const string EventId = "balparental-2026";

    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly ITicketPdfService _ticketPdf;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AdminCreateAndPayTicketOrderFunction> _logger;

    public AdminCreateAndPayTicketOrderFunction(
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        IOptions<AppOptions> appOptions,
        ILogger<AdminCreateAndPayTicketOrderFunction> logger)
    {
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    [Function("AdminCreateAndPayTicketOrder")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "manage/tickets/create-paid")] HttpRequest req)
    {
        var overrideEmail = req.Query["overrideEmail"].ToString();
        var isTestMode = !string.IsNullOrWhiteSpace(overrideEmail);

        CreateCheckoutRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<CreateCheckoutRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig JSON-verzoek." });
        }

        if (body == null
            || string.IsNullOrWhiteSpace(body.Name)
            || string.IsNullOrWhiteSpace(body.Email))
        {
            return new BadRequestObjectResult(new { error = "Vul alle verplichte velden in." });
        }

        if (body.ToegangsticketCount < 0 || body.EtenPartyCount < 0
            || body.Drankkaart10Count < 0 || body.Drankkaart20Count < 0
            || body.VegetarischCount < 0)
            return new BadRequestObjectResult(new { error = "Ongeldige aantallen." });

        var totalTickets = body.ToegangsticketCount + body.EtenPartyCount;
        if (totalTickets < 1)
            return new BadRequestObjectResult(new { error = "Kies minstens 1 ticket (toegang of eten & party)." });

        if (body.VegetarischCount > body.EtenPartyCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal eten & party tickets." });

        var orderId = Guid.NewGuid().ToString();
        var customerName = body.Name.Trim();

        // Tickets aanmaken
        var pdfTickets = new List<TicketPdfData>();

        for (int i = 0; i < body.ToegangsticketCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            if (!isTestMode)
                await _storage.SaveTicketAsync(new TicketEntity
                {
                    PartitionKey = orderId, RowKey = ticketId,
                    QrPayload = qrPayload, TicketType = nameof(TicketKind.Toegang), IsVegetarisch = false,
                });
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.Toegang), false));
        }

        for (int i = 0; i < body.EtenPartyCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            var isVeg = i < body.VegetarischCount;
            if (!isTestMode)
                await _storage.SaveTicketAsync(new TicketEntity
                {
                    PartitionKey = orderId, RowKey = ticketId,
                    QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
                });
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        // Ticket PDF genereren en opslaan
        byte[]? combinedPdf = pdfTickets.Count > 0
            ? _ticketPdf.GenerateTicketsPdf(pdfTickets, customerName, "Bal Parental 2026")
            : null;

        var order = new OrderEntity
        {
            PartitionKey = EventId,
            RowKey = orderId,
            Email = body.Email.Trim(),
            Name = customerName,
            Quantity = totalTickets,
            ToegangsticketCount = body.ToegangsticketCount,
            EtenPartyCount = body.EtenPartyCount,
            VegetarischCount = body.VegetarischCount,
            Drankkaart10Count = body.Drankkaart10Count,
            Drankkaart20Count = body.Drankkaart20Count,
            Status = nameof(OrderStatus.Paid),
        };

        if (combinedPdf != null && !isTestMode)
        {
            var blobUrl = await _storage.SaveTicketPdfAsync(orderId, combinedPdf);
            order.PdfBlobUrl = blobUrl;
            _logger.LogInformation("Ticket PDF saved for manual order {OrderId}", orderId);
        }

        if (!isTestMode)
        {
            await _storage.SaveOrderAsync(order);
            _logger.LogInformation("Manually created paid ticket order {OrderId} ({Name})", orderId, customerName);
        }
        else
        {
            _logger.LogInformation(
                "[TEST] Dry-run create for {Name} ({TicketCount} tickets) \u2014 geen opslag in storage",
                customerName, pdfTickets.Count);
        }

        var recipientEmail = isTestMode ? overrideEmail : order.Email;

        await _email.SendTicketConfirmationAsync(
            recipientEmail, customerName,
            body.ToegangsticketCount, body.EtenPartyCount, body.VegetarischCount,
            body.Drankkaart10Count, body.Drankkaart20Count,
            pdfTickets, combinedPdf);

        var contactEmail = isTestMode
            ? overrideEmail
            : string.IsNullOrEmpty(_appOptions.ContactEmail)
                ? "oudercomitepittem@gmail.com"
                : _appOptions.ContactEmail;

        await _email.SendContactNotificationAsync(
            customerName, order.Email,
            $"Bestelling betaald (manueel aangemaakt): {customerName}",
            $"Naam: {customerName}\nE-mail: {order.Email}\nToegangstickets: {body.ToegangsticketCount}\nEten & Party: {body.EtenPartyCount}\nVegetarisch: {body.VegetarischCount}\nDrankkaarten \u20ac10: {body.Drankkaart10Count}\nDrankkaarten \u20ac20: {body.Drankkaart20Count}",
            contactEmail);

        _logger.LogInformation(
            "Ticket order {OrderId} ({Name}) manually created and marked as Paid{TestMode}",
            orderId, customerName, isTestMode ? $" [TEST \u2192 {overrideEmail}]" : "");

        return new OkObjectResult(new
        {
            message = isTestMode
                ? $"[TEST] Dry-run voltooid voor {customerName} \u2014 geen opslag, e-mail verstuurd naar {overrideEmail}."
                : $"Bestelling voor {customerName} succesvol aangemaakt en gemarkeerd als betaald.",
            orderId = isTestMode ? (string?)null : orderId,
            ticketsGenerated = pdfTickets.Count,
            recipient = recipientEmail,
            testMode = isTestMode
        });
    }
}
