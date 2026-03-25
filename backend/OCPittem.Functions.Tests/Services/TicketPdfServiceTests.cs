using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class TicketPdfServiceTests
{
    private readonly TicketPdfService _sut = new();

    [Fact]
    public void GenerateTicketsPdf_EmptyList_ReturnsNonEmptyBytes()
    {
        var result = _sut.GenerateTicketsPdf([], "Jan Janssen", "Bal Parental 2026");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GenerateTicketsPdf_SingleToegangsticket_ReturnsNonEmptyBytes()
    {
        var tickets = new List<TicketPdfData>
        {
            new("ticket-1", "ticket-1:abc123def456", nameof(TicketKind.Toegang), false)
        };

        var result = _sut.GenerateTicketsPdf(tickets, "Jan Janssen", "Bal Parental 2026");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GenerateTicketsPdf_EtenPartyTicketVegetarisch_ReturnsNonEmptyBytes()
    {
        var tickets = new List<TicketPdfData>
        {
            new("ticket-2", "ticket-2:xyz789abc123", nameof(TicketKind.EtenParty), true)
        };

        var result = _sut.GenerateTicketsPdf(tickets, "Marie Pieters", "Bal Parental 2026");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GenerateTicketsPdf_MultipleTicketsMixed_ReturnsNonEmptyBytes()
    {
        var tickets = new List<TicketPdfData>
        {
            new("ticket-1", "ticket-1:abc123", nameof(TicketKind.Toegang), false),
            new("ticket-2", "ticket-2:def456", nameof(TicketKind.Toegang), false),
            new("ticket-3", "ticket-3:ghi789", nameof(TicketKind.EtenParty), false),
            new("ticket-4", "ticket-4:jkl012", nameof(TicketKind.EtenParty), true),
        };

        var result = _sut.GenerateTicketsPdf(tickets, "Familie Janssen", "Bal Parental 2026");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GenerateTicketsPdf_ReturnsPdfMagicBytes()
    {
        var tickets = new List<TicketPdfData>
        {
            new("ticket-1", "ticket-1:abc123", nameof(TicketKind.Toegang), false)
        };

        var result = _sut.GenerateTicketsPdf(tickets, "Jan Janssen", "Bal Parental 2026");

        // PDF files start with "%PDF"
        Assert.Equal((byte)'%', result[0]);
        Assert.Equal((byte)'P', result[1]);
        Assert.Equal((byte)'D', result[2]);
        Assert.Equal((byte)'F', result[3]);
    }
}
