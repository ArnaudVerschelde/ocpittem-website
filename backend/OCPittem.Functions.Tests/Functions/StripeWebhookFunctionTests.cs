using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly ISponsorAttestationService _attestation = Substitute.For<ISponsorAttestationService>();
    private readonly ILogger<StripeWebhookFunction> _logger = Substitute.For<ILogger<StripeWebhookFunction>>();
    private readonly StripeWebhookFunction _sut;

    public StripeWebhookFunctionTests()
    {
        var options = Options.Create(new AppOptions { TicketHmacSecret = "test-hmac-secret" });
        _sut = new StripeWebhookFunction(_stripe, _storage, _email, _ticketPdf, _attestation, options, _logger);
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
        _storage.TryBeginWebhookEventAsync(
                "evt_dup",
                false,
                Arg.Any<DateTimeOffset>())
            .Returns(WebhookEventBeginResult.AlreadyProcessed);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().UpsertWebhookEventAsync(Arg.Any<WebhookEventEntity>());
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
            Status = nameof(OrderStatus.Pending),
            ToegangsticketCount = 1,
            EtenPartyCount = 1,
            StripeSessionId = "sess_123"
        };

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_1").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_123").Returns(order);
        _storage.SaveTicketPdfAsync(Arg.Any<string>(), Arg.Any<byte[]>()).Returns("https://blob/order-123/tickets.pdf");
        _ticketPdf.GenerateTicketsPdf(
                Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 1, 2, 3 });

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        Assert.Equal(nameof(OrderStatus.Paid), order.Status);
        await _storage.Received(2).SaveTicketAsync(Arg.Any<TicketEntity>());
        await _storage.Received(1).SaveTicketPdfAsync(Arg.Any<string>(), Arg.Any<byte[]>());
        await _email.Received(1).SendTicketConfirmationAsync(
            "test@example.com", "Test User",
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<byte[]>());
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
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<byte[]>());
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
            Status = nameof(OrderStatus.Pending),
            StripeSessionId = "sess_fail"
        };

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_5").Returns(false);
        _storage.GetOrderByStripeSessionAsync("sess_fail").Returns(order);

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        Assert.Equal(nameof(OrderStatus.Failed), order.Status);
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

    [Fact]
    public async Task Run_CookieProcessingError_ReturnsServerErrorForStripeRetry()
    {
        var stripeEvent = CreateStripeEvent(
            "evt_cookie_error",
            EventTypes.CheckoutSessionCompleted,
            CreatePaidCookieSession());
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.GetCookieOrderAsync("cookie-order-1")
            .Returns(Task.FromException<CookieOrderEntity?>(new InvalidOperationException("DB error")));

        var result = await _sut.Run(HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig"));

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        await _storage.Received(1).UpsertWebhookEventAsync(
            Arg.Is<WebhookEventEntity>(entity => entity.Result.StartsWith("error:")));
    }

    [Fact]
    public async Task Run_CookieCheckoutCompleted_MarksPaidAndSendsOneConfirmation()
    {
        var session = CreatePaidCookieSession();
        var stripeEvent = CreateStripeEvent("evt_cookie_paid", EventTypes.CheckoutSessionCompleted, session);
        var order = CreateCookieOrder();
        var paidOrder = CreateCookieOrder();
        paidOrder.PaymentStatus = nameof(CookieOrderStatus.Paid);

        var req = HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig");
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.WebhookEventExistsAsync("evt_cookie_paid").Returns(false);
        _storage.GetCookieOrderAsync("cookie-order-1").Returns(order);
        _storage.TryMarkCookieOrderPaidAsync(
                "cookie-order-1",
                "cs_cookie",
                "pi_cookie",
                Arg.Any<DateTimeOffset>())
            .Returns(paidOrder);
        _storage.TryClaimCookieOrderConfirmationEmailAsync(
                "cookie-order-1",
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>())
            .Returns(call =>
            {
                paidOrder.ConfirmationEmailClaimId = call.ArgAt<string>(1);
                paidOrder.ConfirmationEmailClaimedUtc = call.ArgAt<DateTimeOffset>(2);
                return paidOrder;
            });

        var result = await _sut.Run(req);

        Assert.IsType<OkResult>(result);
        await _storage.Received(1).TryMarkCookieOrderPaidAsync(
            "cookie-order-1",
            "cs_cookie",
            "pi_cookie",
            Arg.Any<DateTimeOffset>());
        await _email.Received(1).SendCookieSaleConfirmationAsync(
            Arg.Is<CookieSaleConfirmationData>(data =>
                data.ConfirmationNumber == "KV26-23456789ABCD"
                && data.StudentName == "Test Leerling"
                && data.TotalAmountCents == 3100));
        await _storage.Received(1).MarkCookieOrderConfirmationEmailSentAsync(
            "cookie-order-1",
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Run_CookieDuplicateSuccessEvent_DoesNotSendDuplicateEmail()
    {
        var firstEvent = CreateStripeEvent(
            "evt_cookie_first",
            EventTypes.CheckoutSessionCompleted,
            CreatePaidCookieSession());
        var duplicateEvent = CreateStripeEvent(
            "evt_cookie_second",
            EventTypes.CheckoutSessionAsyncPaymentSucceeded,
            CreatePaidCookieSession());
        var paidOrder = CreateCookieOrder();
        paidOrder.PaymentStatus = nameof(CookieOrderStatus.Paid);

        _storage.GetCookieOrderAsync("cookie-order-1").Returns(paidOrder);
        _storage.TryMarkCookieOrderPaidAsync(
                "cookie-order-1",
                "cs_cookie",
                "pi_cookie",
                Arg.Any<DateTimeOffset>())
            .Returns(paidOrder);
        _storage.TryClaimCookieOrderConfirmationEmailAsync(
                "cookie-order-1",
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>())
            .Returns(paidOrder, (CookieOrderEntity?)null);

        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>())
            .Returns(firstEvent, duplicateEvent);

        await _sut.Run(HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig"));
        await _sut.Run(HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig"));

        await _email.Received(1).SendCookieSaleConfirmationAsync(
            Arg.Any<CookieSaleConfirmationData>());
    }

    [Fact]
    public async Task Run_CookieCheckoutCompleted_Unpaid_DoesNotMarkPaid()
    {
        var session = CreatePaidCookieSession();
        session.PaymentStatus = "unpaid";
        var stripeEvent = CreateStripeEvent(
            "evt_cookie_unpaid",
            EventTypes.CheckoutSessionCompleted,
            session);
        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);

        var result = await _sut.Run(HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig"));

        Assert.IsType<OkResult>(result);
        await _storage.DidNotReceive().TryMarkCookieOrderPaidAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Run_SponsorCheckoutCompleted_StillUsesSponsorRoute()
    {
        var session = new Session
        {
            Id = "sess_sponsor",
            PaymentStatus = "paid",
            CustomerEmail = "sponsor@example.com",
            Metadata = new Dictionary<string, string>
            {
                ["requestId"] = "sponsor-1",
                ["customerName"] = "Sponsor NV",
                ["orderType"] = "sponsor",
            },
        };
        var sponsor = new SponsorRequestEntity
        {
            PartitionKey = "Sponsor",
            RowKey = "sponsor-1",
            CompanyName = "Sponsor NV",
            Email = "sponsor@example.com",
            Package = "brons",
            Status = "Pending",
            StripeSessionId = "sess_sponsor",
        };
        var stripeEvent = CreateStripeEvent(
            "evt_sponsor_regression",
            EventTypes.CheckoutSessionCompleted,
            session);

        _stripe.ConstructWebhookEvent(Arg.Any<string>(), Arg.Any<string>()).Returns(stripeEvent);
        _storage.GetSponsorRequestByStripeSessionAsync("sess_sponsor").Returns(sponsor);
        _attestation.GenerateAttestationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<decimal>(),
                Arg.Any<DateTime>())
            .Returns([1, 2, 3]);
        _storage.SaveSponsorAttestationAsync("sponsor-1", Arg.Any<byte[]>())
            .Returns("https://blob/sponsor-1/attest.pdf");

        var result = await _sut.Run(HttpRequestHelper.CreateWebhookRequest("{}", "valid-sig"));

        Assert.IsType<OkResult>(result);
        Assert.Equal("Paid", sponsor.Status);
        await _storage.Received(1).UpdateSponsorRequestAsync(sponsor);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            "sponsor@example.com",
            "Sponsor NV",
            "brons",
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]?>(),
            Arg.Any<byte[]?>());
    }

    private static Session CreatePaidCookieSession() => new()
    {
        Id = "cs_cookie",
        PaymentStatus = "paid",
        AmountTotal = 3100,
        Currency = "eur",
        PaymentIntentId = "pi_cookie",
        Metadata = new Dictionary<string, string>
        {
            ["flow"] = "cookie-sale-2026",
            ["orderId"] = "cookie-order-1",
        },
    };

    private static CookieOrderEntity CreateCookieOrder() => new()
    {
        PartitionKey = "COOKIE_SALE_2026",
        RowKey = "cookie-order-1",
        OrderId = "cookie-order-1",
        ConfirmationNumber = "KV26-23456789ABCD",
        Name = "Test Ouder",
        StudentName = "Test Leerling",
        Email = "ouder@example.com",
        ClassName = "Testklas",
        CoteDorQuantity = 2,
        LotusQuantity = 1,
        TotalPackages = 3,
        TotalAmountCents = 3100,
        PaymentStatus = nameof(CookieOrderStatus.Pending),
    };

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
