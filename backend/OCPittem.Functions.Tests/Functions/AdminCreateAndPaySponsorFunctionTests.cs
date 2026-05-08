using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Functions;

public class AdminCreateAndPaySponsorFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ITicketPdfService _ticketPdf = Substitute.For<ITicketPdfService>();
    private readonly ISponsorAttestationService _attestation = Substitute.For<ISponsorAttestationService>();
    private readonly ILogger<AdminCreateAndPaySponsorFunction> _logger =
        Substitute.For<ILogger<AdminCreateAndPaySponsorFunction>>();
    private readonly AdminCreateAndPaySponsorFunction _sut;

    public AdminCreateAndPaySponsorFunctionTests()
    {
        var options = Options.Create(new AppOptions
        {
            ContactEmail = "oc@ocpittem.be",
            TicketHmacSecret = "test-secret-key"
        });
        _sut = new AdminCreateAndPaySponsorFunction(_storage, _email, _ticketPdf, _attestation, options, _logger);

        _attestation.GenerateAttestationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<DateTime>())
            .Returns(new byte[] { 1, 2, 3 });

        _storage.SaveTicketPdfAsync(Arg.Any<string>(), Arg.Any<byte[]>())
            .Returns("https://blob/tickets.pdf");
        _storage.SaveSponsorAttestationAsync(Arg.Any<string>(), Arg.Any<byte[]>())
            .Returns("https://blob/attest.pdf");
    }

    private static SponsorRequest ValidRequest(string package = "zilver") => new(
        CompanyName: "Testbedrijf BV",
        ContactName: "Jan Janssen",
        Email: "jan@testbedrijf.be",
        Phone: "0499123456",
        Package: package,
        EnterpriseNumber: "0403.227.515",
        Street: "Teststraat",
        HouseNumber: "12",
        PostalCode: "8740",
        City: "Pittem");

    private static HttpRequest BuildRequest(SponsorRequest? body, string? overrideEmail = null)
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
    [InlineData("", "Jan", "jan@test.be", "zilver", "0403.227.515", "Straat", "1", "8740", "Pittem")]
    [InlineData("BV", "", "jan@test.be", "zilver", "0403.227.515", "Straat", "1", "8740", "Pittem")]
    [InlineData("BV", "Jan", "", "zilver", "0403.227.515", "Straat", "1", "8740", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "", "0403.227.515", "Straat", "1", "8740", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "", "Straat", "1", "8740", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "0403.227.515", "", "1", "8740", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "0403.227.515", "Straat", "", "8740", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "0403.227.515", "Straat", "1", "", "Pittem")]
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "0403.227.515", "Straat", "1", "8740", "")]
    public async Task Run_MissingRequiredField_ReturnsBadRequest(
        string company, string contact, string email, string package,
        string enterprise, string street, string house, string postal, string city)
    {
        var body = new SponsorRequest(company, contact, email, "", package, enterprise, street, house, postal, city);

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidEnterpriseNumber_ReturnsBadRequest()
    {
        var body = ValidRequest() with { EnterpriseNumber = "0000.000.000" };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidPostalCode_ReturnsBadRequest()
    {
        var body = ValidRequest() with { PostalCode = "874" };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidPackage_ReturnsBadRequest()
    {
        var body = ValidRequest() with { Package = "platinum" };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_ExtraVegetarischGreaterThanExtraEtenParty_ReturnsBadRequest()
    {
        var body = ValidRequest() with { ExtraEtenPartyCount = 1, ExtraVegetarischCount = 2 };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_IncludedVegetarischGreaterThanIncludedTickets_ReturnsBadRequest()
    {
        // zilver = 2 included tickets, maar 3 vegetarisch
        var body = ValidRequest("zilver") with { IncludedVegetarischCount = 3 };

        var result = await _sut.Run(BuildRequest(body));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_ValidRequest_ReturnsOk()
    {
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        var result = await _sut.Run(BuildRequest(ValidRequest()));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Run_ValidRequest_SavesSponsorRequestWithStatusPaid()
    {
        var result = await _sut.Run(BuildRequest(ValidRequest()));

        await _storage.Received(1).SaveSponsorRequestAsync(
            Arg.Is<SponsorRequestEntity>(e =>
                e.Status == "Paid" &&
                e.CompanyName == "Testbedrijf BV" &&
                e.Package == "zilver"));
    }

    [Fact]
    public async Task Run_ZilverPackage_GeneratesTwoTickets()
    {
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(BuildRequest(ValidRequest("zilver")));

        await _storage.Received(2).SaveTicketAsync(Arg.Any<TicketEntity>());
        _ticketPdf.Received(1).GenerateTicketsPdf(
            Arg.Is<IReadOnlyList<TicketPdfData>>(list => list.Count == 2),
            "Testbedrijf BV", "Bal Parental 2026");
    }

    [Fact]
    public async Task Run_GoudPackage_GeneratesFourTickets()
    {
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(BuildRequest(ValidRequest("goud")));

        await _storage.Received(4).SaveTicketAsync(Arg.Any<TicketEntity>());
    }

    [Fact]
    public async Task Run_BronsPackage_GeneratesNoTickets()
    {
        await _sut.Run(BuildRequest(ValidRequest("brons")));

        await _storage.DidNotReceive().SaveTicketAsync(Arg.Any<TicketEntity>());
        _ticketPdf.DidNotReceive().GenerateTicketsPdf(
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Run_ValidRequest_GeneratesAndSavesAttestation()
    {
        await _sut.Run(BuildRequest(ValidRequest("zilver")));

        await _attestation.Received(1).GenerateAttestationAsync(
            "Testbedrijf BV", "Teststraat", "12", "8740", "Pittem",
            Arg.Any<string>(), 250m, Arg.Any<DateTime>());
        await _storage.Received(1).SaveSponsorAttestationAsync(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_BronsPackage_CorrectAmount()
    {
        await _sut.Run(BuildRequest(ValidRequest("brons")));

        await _attestation.Received(1).GenerateAttestationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            100m, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Run_WithExtraTicketsAndDrankkaart_CorrectTotalAmount()
    {
        // goud (500) + 1 extra eten&party (50) + 2 drankkaart20 (40) = 590
        var body = ValidRequest("goud") with { ExtraEtenPartyCount = 1, ExtraDrankkaart20Count = 2 };
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(BuildRequest(body));

        await _attestation.Received(1).GenerateAttestationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            590m, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Run_ValidRequest_SendsConfirmationEmailToSponsor()
    {
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(BuildRequest(ValidRequest("zilver")));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            "jan@testbedrijf.be", "Testbedrijf BV", "zilver",
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]>(), Arg.Any<byte[]>());
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
    public async Task Run_ValidRequest_ResponseContainsRequestIdAndDetails()
    {
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        var result = await _sut.Run(BuildRequest(ValidRequest("zilver")));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"ticketsGenerated\":2", json);
        Assert.Contains("\"attestationGenerated\":true", json);
        Assert.Contains("\"testMode\":false", json);
        Assert.Contains("requestId", json);
    }

    // ── Foutafhandeling ─────────────────────────────────────────────────────

    [Fact]
    public async Task Run_AttestationFails_StillSavesAndSendsEmailWithoutAttestation()
    {
        _attestation.GenerateAttestationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<DateTime>())
            .ThrowsAsync(new InvalidOperationException("PDF failed"));

        var result = await _sut.Run(BuildRequest(ValidRequest("brons")));

        Assert.IsType<OkObjectResult>(result);
        await _storage.Received(1).SaveSponsorRequestAsync(Arg.Any<SponsorRequestEntity>());
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            null, null);
    }

    // ── Test mode ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_WithOverrideEmail_SendsToOverrideNotSponsor()
    {
        const string overrideEmail = "test@ocpittem.be";

        await _sut.Run(BuildRequest(ValidRequest(), overrideEmail));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            overrideEmail, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]?>(), Arg.Any<byte[]?>());
        await _email.DidNotReceive().SendSponsorPaymentConfirmationAsync(
            "jan@testbedrijf.be", Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]?>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Run_WithOverrideEmail_ResponseContainsTestModeTrue()
    {
        const string overrideEmail = "test@ocpittem.be";

        var result = await _sut.Run(BuildRequest(ValidRequest(), overrideEmail));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"testMode\":true", json);
        Assert.Contains(overrideEmail, json);
    }
}
