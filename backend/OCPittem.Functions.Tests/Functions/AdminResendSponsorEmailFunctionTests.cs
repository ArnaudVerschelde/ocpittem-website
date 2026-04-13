using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class AdminResendSponsorEmailFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<AdminResendSponsorEmailFunction> _logger =
        Substitute.For<ILogger<AdminResendSponsorEmailFunction>>();
    private readonly AdminResendSponsorEmailFunction _sut;

    private const string RequestId = "05fd8383-74a3-43b3-b7f0-bf55b8ad7860";

    public AdminResendSponsorEmailFunctionTests()
    {
        var options = Options.Create(new AppOptions { ContactEmail = "oc@ocpittem.be" });
        _sut = new AdminResendSponsorEmailFunction(_storage, _email, options, _httpClientFactory, _logger);
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

    private static HttpRequest EmptyPostRequest => new DefaultHttpContext().Request;

    [Fact]
    public async Task Run_SponsorNotFound_ReturnsNotFound()
    {
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns((SponsorRequestEntity?)null);

        var result = await _sut.Run(EmptyPostRequest, RequestId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Run_SponsorNotPaid_ReturnsBadRequest()
    {
        var sponsor = PaidSponsor();
        sponsor.Status = "Pending";
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);

        var result = await _sut.Run(EmptyPostRequest, RequestId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_PaidSponsor_WithBlobUrls_SendsBothEmailsAndReturnsOk()
    {
        var pdfBytes = new byte[] { 1, 2, 3 };
        var attestBytes = new byte[] { 4, 5, 6 };
        var sponsor = PaidSponsor("https://blob/pdf-sas-url", "https://blob/attest-sas-url");
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());
        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var bytes = req.RequestUri!.ToString().Contains("pdf") ? pdfBytes : attestBytes;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
        });
        _httpClientFactory.CreateClient().Returns(new HttpClient(fakeHandler));

        var result = await _sut.Run(EmptyPostRequest, RequestId);

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

        var result = await _sut.Run(EmptyPostRequest, RequestId);

        Assert.IsType<OkObjectResult>(result);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            null, null);
        _httpClientFactory.DidNotReceive().CreateClient();
    }

    [Fact]
    public async Task Run_PaidSponsor_BlobDownloadFails_StillSendsEmailWithNullAttachments()
    {
        var sponsor = PaidSponsor("https://blob/pdf-sas-url", "https://blob/attest-sas-url");
        _storage.GetSponsorRequestByIdAsync(RequestId).Returns(sponsor);
        _storage.GetTicketsByOrderIdAsync(RequestId).Returns(new List<TicketEntity>());
        var fakeHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
        _httpClientFactory.CreateClient().Returns(new HttpClient(fakeHandler));

        var result = await _sut.Run(EmptyPostRequest, RequestId);

        Assert.IsType<OkObjectResult>(result);
        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IReadOnlyList<TicketPdfData>>(),
            null, null);
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

        await _sut.Run(EmptyPostRequest, RequestId);

        await _email.Received(1).SendSponsorPaymentConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Is<IReadOnlyList<TicketPdfData>>(list =>
                list.Count == 2 &&
                list[0].TicketId == "t1" && !list[0].IsVegetarisch &&
                list[1].TicketId == "t2" && list[1].IsVegetarisch),
            null, null);
    }
}
