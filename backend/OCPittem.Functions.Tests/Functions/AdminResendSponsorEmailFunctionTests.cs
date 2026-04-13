using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

public class AdminResendSponsorEmailFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly ILogger<AdminResendSponsorEmailFunction> _logger =
        Substitute.For<ILogger<AdminResendSponsorEmailFunction>>();
    private readonly AdminResendSponsorEmailFunction _sut;

    private const string RequestId = "05fd8383-74a3-43b3-b7f0-bf55b8ad7860";

    public AdminResendSponsorEmailFunctionTests()
    {
        var options = Options.Create(new AppOptions { ContactEmail = "oc@ocpittem.be" });
        _sut = new AdminResendSponsorEmailFunction(_storage, _email, options, _blobServiceClient, _logger);
    }

    private static SponsorRequestEntity PaidSponsor(string pdfUrl = "", string attestUrl = "") => new()
    {
        PartitionKey = "Sponsor",
        RowKey = RequestId,
        Status = "Paid",
        CompanyName = "TestBV",
        ContactName = "Jan Janssen",
        Email = "jan@testbv.be",
        Package = "zilver",
        EnterpriseNumber = "0403.227.515",
        Street = "Teststraat",
        HouseNumber = "12",
        PostalCode = "8740",
        City = "Pittem",
        PdfBlobUrl = pdfUrl,
        AttestationBlobUrl = attestUrl,
    };

    private static HttpRequest PostRequestWithQueryId(string requestId)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?requestId={requestId}");
        return context.Request;
    }

    private static HttpRequest PostRequestWithOverrideEmail(string requestId, string overrideEmail)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?requestId={requestId}&overrideEmail={overrideEmail}");
        return context.Request;
    }

    [Fact]
    public async Task Run_SponsorNotFound_ReturnsNotFound()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns((SponsorRequestEntity?)null);

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Run_SponsorNotPaid_ReturnsBadRequest()
    {
        var sponsor = PaidSponsor();
        sponsor.Status = "Pending";
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_PaidSponsor_WithBlobUrls_SendsBothEmailsAndReturnsOk()
    {
        var pdfBytes = new byte[] { 1, 2, 3 };
        var sponsor = PaidSponsor("https://stocpittem2026.blob.core.windows.net/ticket-pdfs/05fd/tickets.pdf",
                                  "https://stocpittem2026.blob.core.windows.net/document-assets/attest.pdf");
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();
        _blobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        var downloadResult = BlobsModelFactory.BlobDownloadResult(BinaryData.FromBytes(pdfBytes));
        blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(downloadResult, Substitute.For<Response>()));

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        Assert.IsType<OkObjectResult>(result);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            sponsor.Email, sponsor.CompanyName, sponsor.Package,
            sponsor.ExtraEtenPartyCount, sponsor.ExtraVegetarischCount, sponsor.ExtraDrankkaart20Count,
            sponsor.IncludedVegetarischCount,
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            Arg.Any<byte[]>(), Arg.Any<byte[]>());
        await _email.Received(1).SendContactNotificationAsync(
            sponsor.CompanyName, sponsor.Email,
            Arg.Any<string>(), Arg.Any<string>(), "oc@ocpittem.be");
    }

    [Fact]
    public async Task Run_PaidSponsor_EmptyBlobUrls_SendsEmailWithNullAttachments()
    {
        var sponsor = PaidSponsor();
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        Assert.IsType<OkObjectResult>(result);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            null, null);
        _blobServiceClient.DidNotReceive().GetBlobContainerClient(Arg.Any<string>());
    }

    [Fact]
    public async Task Run_PaidSponsor_BlobDownloadFails_StillSendsEmailWithNullAttachments()
    {
        var sponsor = PaidSponsor("https://stocpittem2026.blob.core.windows.net/ticket-pdfs/05fd/tickets.pdf",
                                  "https://stocpittem2026.blob.core.windows.net/document-assets/attest.pdf");
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();
        _blobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("Public access is not permitted."));

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        Assert.IsType<OkObjectResult>(result);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            null, null);
    }

    [Fact]
    public async Task Run_MissingRequestId_ReturnsBadRequest()
    {
        var result = await _sut.Run(new DefaultHttpContext().Request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_PaidSponsor_MapsTicketEntitiesToTicketPdfData()
    {
        var sponsor = PaidSponsor();
        var ticketEntities = new List<TicketEntity>
        {
            new() { RowKey = "t1", QrPayload = "qr1", TicketType = "EtenParty", IsVegetarisch = false },
            new() { RowKey = "t2", QrPayload = "qr2", TicketType = "EtenParty", IsVegetarisch = true },
        };
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(ticketEntities);

        await _sut.Run(PostRequestWithQueryId(RequestId));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Is<IReadOnlyList<TicketPdfData>>(list =>
                list.Count == 2 &&
                list[0].TicketId == "t1" && !list[0].IsVegetarisch &&
                list[1].TicketId == "t2" && list[1].IsVegetarisch),
            null, null);
    }

    [Fact]
    public async Task Run_WithOverrideEmail_SendsConfirmationToOverrideNotSponsor()
    {
        const string overrideEmail = "test@ocpittem.be";
        var sponsor = PaidSponsor();
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        await _sut.Run(PostRequestWithOverrideEmail(RequestId, overrideEmail));

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            overrideEmail, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), null, null);
        await _email.DidNotReceive().SendSponsorPaymentConfirmationAsync(
            sponsor.Email, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(), null, null);
    }

    [Fact]
    public async Task Run_WithOverrideEmail_SendsContactNotificationToOverrideNotContactEmail()
    {
        const string overrideEmail = "test@ocpittem.be";
        var sponsor = PaidSponsor();
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        await _sut.Run(PostRequestWithOverrideEmail(RequestId, overrideEmail));

        await _email.Received(1).SendContactNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(),
            overrideEmail);
    }

    [Fact]
    public async Task Run_WithOverrideEmail_ResponseContainsTestModeTrue()
    {
        const string overrideEmail = "test@ocpittem.be";
        var sponsor = PaidSponsor();
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        var result = await _sut.Run(PostRequestWithOverrideEmail(RequestId, overrideEmail));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"testMode\":true", json);
        Assert.Contains(overrideEmail, json);
    }

    [Fact]
    public async Task Run_WithoutOverrideEmail_ResponseContainsTestModeFalse()
    {
        var sponsor = PaidSponsor();
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());

        var result = await _sut.Run(PostRequestWithQueryId(RequestId));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"testMode\":false", json);
    }
}
