using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;
using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Tests.Functions;

public class StripeWebhookFunctionTests
{
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ITicketPdfService _ticketPdf = Substitute.For<ITicketPdfService>();
    private readonly ILogger<StripeWebhookFunction> _logger = Substitute.For<ILogger<StripeWebhookFunction>>();
    private readonly StripeWebhookFunction _sut;

    public StripeWebhookFunctionTests()
    {
        _sut = new StripeWebhookFunction(_stripe, _storage, _email, _ticketPdf, _logger);
    }

    [Fact]
    public async Task Run_InvalidSignature_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateWebhookRequest("{}", "invalid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>())
            .Throws(new StripeException("Invalid signature"));

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_DuplicateEvent_ReturnsOkWithoutProcessing()
    {
        var stripeEvent = CreateStripeEvent("evt_dup", EventTypes.CheckoutSessionCompleted);
        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_dup").Returns(true);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().SaveWebhookEventAsync(Arg.Any<WebhookEventEntity>());
    }

    [Fact]
    public async Task Run_CheckoutCompleted_PaidSession_GeneratesTicketsAndSendsEmail()
    {
        var session = new Session
        {
            Id = "sess_123",
            PaymentStatus = "paid",
            CustomerEmail = "test@example.com",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", "order-123" },
                { "customerName", "Test User" }
            }
        };
        var stripeEvent = CreateStripeEvent("evt_1", EventTypes.CheckoutSessionCompleted, session);
        var order = new OrderEntity
        {
            PartitionKey = "balparental-2026",
            RowKey = "order-123",
            Email = "test@example.com",
            Name = "Test User",
            Quantity = 2,
            Status = "pending",
            StripeSessionId = "sess_123"
        };

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_1").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_123").Returns(order);
        _ticketPdf.GenerateTicketPdf(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 1, 2, 3 });

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        Assert.Equal("paid", order.Status);
        await _storage.Received(2).SaveTicketAsync(Arg.Any<TicketEntity>());
        await _email.Received(1).SendTicketConfirmationAsync(
            "test@example.com", "Test User", 2, Arg.Any<byte[]>());
        await _storage.Received(1).UpsertWebhookEventAsync(
            Arg.Is<WebhookEventEntity>(e => e.Result == "processed"));
    }

    [Fact]
    public async Task Run_CheckoutCompleted_UnpaidSession_SkipsTicketGeneration()
    {
        var session = new Session
        {
            Id = "sess_456",
            PaymentStatus = "unpaid",
            Metadata = new Dictionary<string, string> { { "orderId", "order-456" } }
        };
        var stripeEvent = CreateStripeEvent("evt_2", EventTypes.CheckoutSessionCompleted, session);

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_2").Returns(false);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().SaveTicketAsync(Arg.Any<TicketEntity>());
        await _email.DidNotReceive().SendTicketConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_CheckoutCompleted_NoOrderId_DoesNotFetchOrder()
    {
        var session = new Session
        {
            Id = "sess_789",
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string>()
        };
        var stripeEvent = CreateStripeEvent("evt_3", EventTypes.CheckoutSessionCompleted, session);

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_3").Returns(false);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().GetOrderByStripeSessionAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Run_CheckoutCompleted_OrderNotFound_SkipsTickets()
    {
        var session = new Session
        {
            Id = "sess_notfound",
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string> { { "orderId", "order-missing" } }
        };
        var stripeEvent = CreateStripeEvent("evt_4", EventTypes.CheckoutSessionCompleted, session);

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_4").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_notfound").Returns((OrderEntity?)null);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().SaveTicketAsync(Arg.Any<TicketEntity>());
    }

    [Fact]
    public async Task Run_PaymentFailed_UpdatesOrderToFailed()
    {
        var session = new Session
        {
            Id = "sess_fail",
            Metadata = new Dictionary<string, string> { { "orderId", "order-fail" } }
        };
        var stripeEvent = CreateStripeEvent("evt_5", EventTypes.CheckoutSessionAsyncPaymentFailed, session);
        var order = new OrderEntity
        {
            PartitionKey = "balparental-2026",
            RowKey = "order-fail",
            Status = "pending",
            StripeSessionId = "sess_fail"
        };

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_5").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_fail").Returns(order);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        Assert.Equal("failed", order.Status);
        await _storage.Received(1).UpdateOrderAsync(order);
    }

    [Fact]
    public async Task Run_UnhandledEventType_ReturnsOk()
    {
        var stripeEvent = new Event
        {
            Id = "evt_6",
            Type = "some.unknown.event",
            Data = new EventData()
        };
        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_6").Returns(false);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Run_ProcessingError_RecordsErrorInWebhookEvent()
    {
        var session = new Session
        {
            Id = "sess_err",
            PaymentStatus = "paid",
            Metadata = new Dictionary<string, string> { { "orderId", "order-err" } }
        };
        var stripeEvent = CreateStripeEvent("evt_7", EventTypes.CheckoutSessionCompleted, session);

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_7").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_err")
            .Returns(Task.FromException<OrderEntity?>(new InvalidOperationException("DB error")));

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.Received(1).UpsertWebhookEventAsync(
            Arg.Is<WebhookEventEntity>(e => e.Result.StartsWith("error:")));
    }

    private static Event CreateStripeEvent(string eventId, string eventType, Session? session = null)
    {
        return new Event
        {
            Id = eventId,
            Type = eventType,
            Data = new EventData
            {
                Object = session ?? new Session()
            }
        };
    }
}
