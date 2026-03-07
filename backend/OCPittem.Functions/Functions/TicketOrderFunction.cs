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
            || string.IsNullOrWhiteSpace(body.Email)
            || body.Quantity < 1
            || body.Quantity > 10)
        {
            return new BadRequestObjectResult(new { error = "Vul alle velden correct in (max 10 tickets)." });
        }

        var orderId = Guid.NewGuid().ToString();
        const string eventId = "balparental-2026";

        try
        {
            var checkoutUrl = await _stripe.CreateCheckoutSessionAsync(orderId, body.Email, body.Name, body.Quantity);

            var order = new OrderEntity
            {
                PartitionKey = eventId,
                RowKey = orderId,
                Email = body.Email,
                Name = body.Name,
                Quantity = body.Quantity,
                Status = "pending",
                StripeSessionId = "",
            };

            await _storage.SaveOrderAsync(order);

            _logger.LogInformation("Checkout session created for order {OrderId}", orderId);

            return new OkObjectResult(new CreateCheckoutResponse(checkoutUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for order {OrderId}", orderId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
