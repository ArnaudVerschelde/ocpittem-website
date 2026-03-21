using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class TicketOrderFunction
{
    private readonly IStripeService _stripe;
    private readonly IStorageService _storage;
    private readonly ILogger<TicketOrderFunction> _logger;

    public TicketOrderFunction(IStripeService stripe, IStorageService storage, ILogger<TicketOrderFunction> logger)
    {
        _stripe = stripe;
        _storage = storage;
        _logger = logger;
    }

    [Function("CreateTicketCheckout")]
    public async Task<IActionResult> CreateCheckout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets/create-checkout")] HttpRequest req)
    {
        CreateCheckoutRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<CreateCheckoutRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig verzoek." });
        }

        if (body == null
            || string.IsNullOrWhiteSpace(body.Name)
            || string.IsNullOrWhiteSpace(body.Email))
        {
            return new BadRequestObjectResult(new { error = "Vul alle verplichte velden correct in." });
        }

        if (body.ToegangsticketCount < 0 || body.EtenPartyCount < 0
            || body.Drankkaart10Count < 0 || body.Drankkaart20Count < 0
            || body.VegetarischCount < 0)
        {
            return new BadRequestObjectResult(new { error = "Ongeldige aantallen." });
        }

        var totalTickets = body.ToegangsticketCount + body.EtenPartyCount;
        if (totalTickets < 1)
            return new BadRequestObjectResult(new { error = "Kies minstens 1 ticket (toegang of eten & party)." });

        if (totalTickets + body.Drankkaart10Count + body.Drankkaart20Count > 30)
            return new BadRequestObjectResult(new { error = "Maximum 30 items per bestelling." });

        if (body.VegetarischCount > body.EtenPartyCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal eten & party tickets." });

        var orderId = Guid.NewGuid().ToString();
        const string eventId = "balparental-2026";

        try
        {
            var checkout = await _stripe.CreateCheckoutSessionAsync(
                orderId, body.Email, body.Name,
                body.ToegangsticketCount, body.EtenPartyCount,
                body.Drankkaart10Count, body.Drankkaart20Count);

            var order = new OrderEntity
            {
                PartitionKey = eventId,
                RowKey = orderId,
                Email = body.Email,
                Name = body.Name,
                Quantity = totalTickets,
                ToegangsticketCount = body.ToegangsticketCount,
                EtenPartyCount = body.EtenPartyCount,
                VegetarischCount = body.VegetarischCount,
                Drankkaart10Count = body.Drankkaart10Count,
                Drankkaart20Count = body.Drankkaart20Count,
                Status = nameof(OrderStatus.Pending),
                StripeSessionId = checkout.SessionId,
            };

            await _storage.SaveOrderAsync(order);

            _logger.LogInformation("Checkout session created for order {OrderId}", orderId);

            return new OkObjectResult(new CreateCheckoutResponse(checkout.Url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for order {OrderId}", orderId);
            return new ObjectResult(new { error = "Er ging iets mis bij het starten van de betaling." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
