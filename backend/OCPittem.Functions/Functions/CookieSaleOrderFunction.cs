using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Configuration;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class CookieSaleOrderFunction
{
    private readonly IStripeService _stripe;
    private readonly IStorageService _storage;
    private readonly ILogger<CookieSaleOrderFunction> _logger;
    private readonly IReadOnlyList<string> _allowedClasses;

    public CookieSaleOrderFunction(
        IStripeService stripe,
        IStorageService storage,
        ILogger<CookieSaleOrderFunction> logger)
        : this(stripe, storage, logger, CookieSale2026Catalog.AllowedClasses)
    {
    }

    internal CookieSaleOrderFunction(
        IStripeService stripe,
        IStorageService storage,
        ILogger<CookieSaleOrderFunction> logger,
        IReadOnlyList<string> allowedClasses)
    {
        _stripe = stripe;
        _storage = storage;
        _logger = logger;
        _allowedClasses = allowedClasses;
    }

    [Function("GetCookieSaleConfig")]
    public IActionResult GetConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cookie-sale/config")] HttpRequest req)
    {
        return new OkObjectResult(new CookieSalePublicConfigResponse(
            Enabled: _allowedClasses.Count > 0,
            EventDate: CookieSale2026Catalog.EventDate.ToString("yyyy-MM-dd"),
            MaximumTotalPackages: CookieSale2026Catalog.MaximumTotalPackages,
            Classes: _allowedClasses,
            Products:
            [
                new("coteDor", CookieSale2026Catalog.CoteDorLabel, CookieSale2026Catalog.CoteDorUnitPriceCents),
                new("lotus", CookieSale2026Catalog.LotusLabel, CookieSale2026Catalog.LotusUnitPriceCents),
            ]));
    }

    [Function("CreateCookieSaleCheckout")]
    public async Task<IActionResult> CreateCheckout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cookie-sale/create-checkout")] HttpRequest req)
    {
        if (_allowedClasses.Count == 0)
        {
            return new ObjectResult(new { error = "De koekjesverkoop is nog niet beschikbaar." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }

        CreateCookieSaleCheckoutRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<CreateCookieSaleCheckoutRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig verzoek." });
        }

        if (!CookieSaleOrderValidator.TryValidate(
                body,
                _allowedClasses,
                out var validatedOrder,
                out var validationError))
        {
            return new BadRequestObjectResult(new { error = validationError });
        }

        var orderId = Guid.NewGuid().ToString();
        var order = new CookieOrderEntity
        {
            PartitionKey = CookieSale2026Catalog.PartitionKey,
            RowKey = orderId,
            OrderId = orderId,
            ConfirmationNumber = ConfirmationNumberGenerator.Generate(),
            Name = validatedOrder!.Name,
            StudentName = validatedOrder.StudentName,
            Email = validatedOrder.Email,
            ClassName = validatedOrder.ClassName,
            CoteDorQuantity = validatedOrder.CoteDorQuantity,
            LotusQuantity = validatedOrder.LotusQuantity,
            TotalPackages = validatedOrder.TotalPackages,
            TotalAmountCents = validatedOrder.TotalAmountCents,
            PaymentStatus = nameof(CookieOrderStatus.Pending),
            CreatedUtc = DateTimeOffset.UtcNow,
        };

        var orderSaved = false;
        try
        {
            await _storage.SaveCookieOrderAsync(order);
            orderSaved = true;

            var checkout = await _stripe.CreateCookieSaleCheckoutSessionAsync(
                orderId,
                order.Email,
                order.CoteDorQuantity,
                order.LotusQuantity);

            try
            {
                await _storage.SetCookieOrderStripeSessionAsync(orderId, checkout.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cookie checkout session {SessionId} created but not stored for order {OrderId}",
                    checkout.SessionId,
                    orderId);
            }

            _logger.LogInformation(
                "Cookie checkout session created for order {OrderId} ({ConfirmationNumber})",
                orderId,
                order.ConfirmationNumber);
            return new OkObjectResult(new CreateCheckoutResponse(checkout.Url));
        }
        catch (Exception ex)
        {
            if (orderSaved)
            {
                try
                {
                    await _storage.TryMarkCookieOrderStatusAsync(orderId, CookieOrderStatus.Failed);
                }
                catch (Exception storageException)
                {
                    _logger.LogError(
                        storageException,
                        "Failed to mark cookie order {OrderId} as Failed after checkout error",
                        orderId);
                }
            }

            _logger.LogError(ex, "Failed to create cookie checkout for order {OrderId}", orderId);
            return new ObjectResult(new { error = "Er ging iets mis bij het starten van de betaling." })
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }
    }

    [Function("GetCookieSaleOrderStatus")]
    public async Task<IActionResult> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cookie-sale/order-status")] HttpRequest req)
    {
        var sessionId = req.Query["session_id"].ToString();
        if (string.IsNullOrWhiteSpace(sessionId))
            return new BadRequestObjectResult(new { error = "Betalingsreferentie ontbreekt." });

        var order = await _storage.GetCookieOrderByStripeSessionAsync(sessionId);
        if (order is null)
            return new NotFoundObjectResult(new { error = "Bestelling nog niet gevonden." });

        var isPaid = order.PaymentStatus == nameof(CookieOrderStatus.Paid);
        return new OkObjectResult(new CookieSaleOrderStatusResponse(
            order.PaymentStatus,
            isPaid ? order.ConfirmationNumber : null));
    }
}
