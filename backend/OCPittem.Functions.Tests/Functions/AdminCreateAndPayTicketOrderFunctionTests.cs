using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Functions;

public class AdminCreateAndPayTicketOrderFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ITicketPdfService _ticketPdf = Substitute.For<ITicketPdfService>();
    private readonly ILogger<AdminCreateAndPayTicketOrderFunction> _logger =
        Substitute.For<ILogger<AdminCreateAndPayTicketOrderFunction>>();
    private readonly AdminCreateAndPayTicketOrderFunction _sut;

    public AdminCreateAndPayTicketOrderFunctionTests()
    {
        var options = Options.Create(new AppOptions
        {
            ContactEmail = "oc@ocpittem.be",
            TicketHmacSecret = "test-secret-key"
        });
        _sut = new AdminCreateAndPayTicketOrderFunction(_storage, _email, _ticketPdf, options, _logger);

        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });
        _storage.SaveTicketPdfAsync(Arg.Any<string>(), Arg.Any<byte[]>())
            .Returns("https://blob/tickets.pdf");
    }

    private static CreateCheckoutRequest ValidRequest(
        int toegang = 2, int etenParty = 2, int veg = 0, int dk10 = 0, int dk20 = 0) => new(
        Name: "Jan Janssen",
        Email: "jan@test.be",
        ToegangsticketCount: toegang,
        EtenPartyCount: etenParty,
        VegetarischCount: veg,
        Drankkaart10Count: dk10,
        Drankkaart20Count: dk20);

    private static HttpRequest BuildRequest(CreateCheckoutRequest? body, string? overrideEmail = null)
    {
        var context = new DefaultHttpContext();
        if (overrideEmail != null)
            context.Request.QueryString = new QueryString($"?overrideEmail={overrideEmail}");

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentType = "application/json";
        }
        return context.Request;
    }

    // ── Validatie ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_NullBody_ReturnsBadRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream("null"u8.ToArray());
        context.Request.ContentType = "application/json";

        var result = await _sut.Run(context.Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("", "jan@test.be")]
    [InlineData("Jan", "")]
    public async Task Run_MissingRequiredField_ReturnsBadRequest(string name, string email)
    {
        var body = new CreateCheckoutRequest(name, email, 2, 0, 0, 0, 0);

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_NoTickets_ReturnsBadRequest()
    {
        var body = ValidRequest(toegang: 0, etenParty: 0);

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_NegativeCount_ReturnsBadRequest()
    {
        var body = ValidRequest() with { Drankkaart20Count = -1 };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_VegetarischGreaterThanEtenParty_ReturnsBadRequest()
    {
        var body = ValidRequest(etenParty: 1, veg: 2);

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_ValidRequest_ReturnsOk()
    {
        var result = await _sut.Run(BuildRequest(ValidRequest()));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Run_ValidRequest_SavesOrderWithStatusPaid()
    {
        await _sut.Run(BuildRequest(ValidRequest()));

        await _storage.Received(1).SaveOrderAsync(
            Arg.Is<OrderEntity>(o =>
                o.Status == nameof(OrderStatus.Paid) &&
                o.Name == "Jan Janssen" &&
                o.Email == "jan@test.be" &&
                o.ToegangsticketCount == 2 &&
                o.EtenPartyCount == 2));
    }

    [Fact]
    public async Task Run_ValidRequest_GeneratesAllTickets()
    {
        await _sut.Run(BuildRequest(ValidRequest(toegang: 3, etenParty: 2)));

        await _storage.Received(5).SaveTicketAsync(Arg.Any<TicketEntity>());
        _ticketPdf.Received(1).GenerateTicketsPdf(
            Arg.Is<IReadOnlyList<TicketPdfData>>(list => list.Count == 5),
            "Jan Janssen", "Bal Parental 2026");
    }

    [Fact]
    public async Task Run_ValidRequest_GeneratesVegetarischTickets()
    {
        await _sut.Run(BuildRequest(ValidRequest(toegang: 0, etenParty: 3, veg: 2)));

        await _storage.Received(2).SaveTicketAsync(
            Arg.Is<TicketEntity>(t => t.TicketType == nameof(TicketKind.EtenParty) && t.IsVegetarisch));
        await _storage.Received(1).SaveTicketAsync(
            Arg.Is<TicketEntity>(t => t.TicketType == nameof(TicketKind.EtenParty) && !t.IsVegetarisch));
    }

    [Fact]
    public async Task Run_ValidRequest_SendsTicketConfirmationToCustomer()
    {
        await _sut.Run(BuildRequest(ValidRequest(toegang: 2, etenParty: 1, veg: 1, dk10: 1, dk20: 2)));

        await _email.Received(1).SendTicketConfirmationAsync(
            "jan@test.be", "Jan Janssen", 2, 1, 1, 1, 2,
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_ValidRequest_SendsContactNotificationWithManueel()
    {
        await _sut.Run(BuildRequest(ValidRequest()));

        await _email.Received(1).SendContactNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("manueel aangemaakt")),
            Arg.Any<string>(), "oc@ocpittem.be");
    }

    [Fact]
    public async Task Run_ValidRequest_ResponseContainsOrderIdAndDetails()
    {
        var result = await _sut.Run(BuildRequest(ValidRequest(toegang: 3, etenParty: 2)));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"ticketsGenerated\":5", json);
        Assert.Contains("\"testMode\":false", json);
        Assert.Contains("orderId", json);
    }

    // ── Test mode ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WithOverrideEmail_SendsToOverrideNotCustomer()
    {
        const string overrideEmail = "test@ocpittem.be";

        await _sut.Run(BuildRequest(ValidRequest(), overrideEmail));

        await _email.Received(1).SendTicketConfirmationAsync(
            overrideEmail, Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<byte[]?>());
        await _email.DidNotReceive().SendTicketConfirmationAsync(
            "jan@test.be", Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Run_WithOverrideEmail_DoesNotSaveAnythingToStorage()
    {
        const string overrideEmail = "test@ocpittem.be";

        await _sut.Run(BuildRequest(ValidRequest(toegang: 2, etenParty: 2), overrideEmail));

        await _storage.DidNotReceive().SaveOrderAsync(Arg.Any<OrderEntity>());
        await _storage.DidNotReceive().SaveTicketAsync(Arg.Any<TicketEntity>());
        await _storage.DidNotReceive().SaveTicketPdfAsync(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_WithOverrideEmail_ResponseHasNullOrderId()
    {
        var result = await _sut.Run(BuildRequest(ValidRequest(), "test@ocpittem.be"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"orderId\":null", json);
        Assert.Contains("\"testMode\":true", json);
    }
}
