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

public class AdminMarkSponsorPaidFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ITicketPdfService _ticketPdf = Substitute.For<ITicketPdfService>();
    private readonly ISponsorAttestationService _attestation = Substitute.For<ISponsorAttestationService>();
    private readonly ILogger<AdminMarkSponsorPaidFunction> _logger =
        Substitute.For<ILogger<AdminMarkSponsorPaidFunction>>();
    private readonly AdminMarkSponsorPaidFunction _sut;

    private const string RequestId = "test-request-id-001";

    public AdminMarkSponsorPaidFunctionTests()
    {
        var options = Options.Create(new AppOptions
        {
            ContactEmail = "oc@ocpittem.be",
            TicketHmacSecret = "test-secret-key"
        });
        _sut = new AdminMarkSponsorPaidFunction(_storage, _email, _ticketPdf, _attestation, options, _logger);

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

    private static SponsorRequestEntity PendingSponsor(string package = "zilver") => new()
    {
        PartitionKey = "Sponsor",
        RowKey = RequestId,
        Status = "Pending",
        CompanyName = "TestBV",
        ContactName = "Jan Janssen",
        Email = "jan@testbv.be",
        Package = package,
        EnterpriseNumber = "0403.227.515",
        Street = "Teststraat",
        HouseNumber = "12",
        PostalCode = "8740",
        City = "Pittem",
        IncludedVegetarischCount = 0,
        ExtraEtenPartyCount = 0,
        ExtraVegetarischCount = 0,
        ExtraDrankkaart20Count = 0,
    };

    private static HttpRequest PostRequest(string requestId, string? overrideEmail = null)
    {
        var context = new DefaultHttpContext();
        var qs = overrideEmail != null
            ? $"?requestId={requestId}&overrideEmail={overrideEmail}"
            : $"?requestId={requestId}";
        context.Request.QueryString = new QueryString(qs);
        return context.Request;
    }

    // ── Input validatie ─────────────────────────────────────────────────────

    [Fact]
    public async Task Run_MissingRequestId_ReturnsBadRequest()
    {
        var result = await _sut.Run(new DefaultHttpContext().Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_SponsorNotFound_ReturnsNotFound()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns((SponsorRequestEntity?)null);

        var result = await _sut.Run(PostRequest(RequestId));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Run_SponsorAlreadyPaid_ReturnsBadRequest()
    {
        var sponsor = PendingSponsor();
        sponsor.Status = "Paid";
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);

        var result = await _sut.Run(PostRequest(RequestId));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Run_ValidPendingSponsor_ReturnsOk()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor());
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        var result = await _sut.Run(PostRequest(RequestId));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Run_ValidPendingSponsor_UpdatesStatusToPaid()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor());

        await _sut.Run(PostRequest(RequestId));

        await _storage.Received(1).UpdateSponsorRequestAsync(
            Arg.Is<SponsorRequestEntity>(s => s.Status == "Paid"));
    }

    [Fact]
    public async Task Run_ZilverPackage_GeneratesTwoTickets()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("zilver"));
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(PostRequest(RequestId));

        await _storage.Received(2).SaveTicketAsync(Arg.Any<TicketEntity>());
        _ticketPdf.Received(1).GenerateTicketsPdf(
            Arg.Is<IReadOnlyList<TicketPdfData>>(list => list.Count == 2),
            Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Run_GoudPackage_GeneratesFourTickets()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("goud"));
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(PostRequest(RequestId));

        await _storage.Received(4).SaveTicketAsync(Arg.Any<TicketEntity>());
    }

    [Fact]
    public async Task Run_BronsPackage_GeneratesNoTickets()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("brons"));

        await _sut.Run(PostRequest(RequestId));

        await _storage.DidNotReceive().SaveTicketAsync(Arg.Any<TicketEntity>());
        _ticketPdf.DidNotReceive().GenerateTicketsPdf(
            Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Run_ZilverWithExtraTickets_GeneratesCorrectTicketCount()
    {
        var sponsor = PendingSponsor("zilver");
        sponsor.ExtraEtenPartyCount = 2;
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(PostRequest(RequestId));

        // 2 included + 2 extra = 4
        await _storage.Received(4).SaveTicketAsync(Arg.Any<TicketEntity>());
    }

    [Fact]
    public async Task Run_ValidSponsor_GeneratesAndSavesAttestation()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("zilver"));

        await _sut.Run(PostRequest(RequestId));

        await _attestation.Received(1).GenerateAttestationAsync(
            "TestBV", "Teststraat", "12", "8740", "Pittem", "0403.227.515",
            250m, Arg.Any<DateTime>());
        await _storage.Received(1).SaveSponsorAttestationAsync(RequestId, Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_ValidSponsor_SendsConfirmationEmailToSponsor()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("zilver"));
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        await _sut.Run(PostRequest(RequestId));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            "jan@testbv.be", "TestBV", "zilver",
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task Run_ValidSponsor_SendsContactNotification()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor());

        await _sut.Run(PostRequest(RequestId));

        await _email.Received(1).SendContactNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("manueel")),
            Arg.Any<string>(), "oc@ocpittem.be");
    }

    [Fact]
    public async Task Run_ResponseContainsCorrectDetails()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("zilver"));
        _ticketPdf.GenerateTicketsPdf(Arg.Any<IReadOnlyList<TicketPdfData>>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new byte[] { 9, 8, 7 });

        var result = await _sut.Run(PostRequest(RequestId));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"ticketsGenerated\":2", json);
        Assert.Contains("\"attestationGenerated\":true", json);
        Assert.Contains("\"testMode\":false", json);
    }

    // ── Foutafhandeling ─────────────────────────────────────────────────────

    [Fact]
    public async Task Run_AttestationFails_StillMarksPaidAndSendsEmail()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor("brons"));
        _attestation.GenerateAttestationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<decimal>(), Arg.Any<DateTime>())
            .ThrowsAsync(new InvalidOperationException("PDF generation failed"));

        var result = await _sut.Run(PostRequest(RequestId));

        Assert.IsType<OkObjectResult>(result);
        await _storage.Received(1).UpdateSponsorRequestAsync(
            Arg.Is<SponsorRequestEntity>(s => s.Status == "Paid"));
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
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor());

        await _sut.Run(PostRequest(RequestId, overrideEmail));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            overrideEmail, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]?>(), Arg.Any<byte[]?>());
        await _email.DidNotReceive().SendSponsorPaymentConfirmationAsync(
            "jan@testbv.be", Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]?>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Run_WithOverrideEmail_ResponseContainsTestModeTrue()
    {
        const string overrideEmail = "test@ocpittem.be";
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(PendingSponsor());

        var result = await _sut.Run(PostRequest(RequestId, overrideEmail));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"testMode\":true", json);
        Assert.Contains(overrideEmail, json);
    }
}
