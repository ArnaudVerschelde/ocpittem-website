using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class EmailHtmlBuilderTests
{
    // ── NormalizeNewLinesToHtml ─────────────────────────────────────────────

    [Fact]
    public void NormalizeNewLinesToHtml_LfNewline_ReplacedWithBrTag()
    {
        var result = EmailHtmlBuilder.NormalizeNewLinesToHtml("line1\nline2");

        Assert.Contains("<br />", result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void NormalizeNewLinesToHtml_CrLfNewline_ReplacedWithBrTag()
    {
        var result = EmailHtmlBuilder.NormalizeNewLinesToHtml("line1\r\nline2");

        Assert.Contains("<br />", result);
        Assert.DoesNotContain("\r\n", result);
    }

    [Fact]
    public void NormalizeNewLinesToHtml_CrNewline_ReplacedWithBrTag()
    {
        var result = EmailHtmlBuilder.NormalizeNewLinesToHtml("line1\rline2");

        Assert.Contains("<br />", result);
        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public void NormalizeNewLinesToHtml_HtmlSpecialChars_AreEncoded()
    {
        var result = EmailHtmlBuilder.NormalizeNewLinesToHtml("<script>alert('xss')</script>");

        Assert.Contains("&lt;script&gt;", result);
        Assert.DoesNotContain("<script>", result);
    }

    [Fact]
    public void NormalizeNewLinesToHtml_PlainText_ReturnedAsIs()
    {
        var result = EmailHtmlBuilder.NormalizeNewLinesToHtml("Gewone tekst zonder newlines.");

        Assert.Equal("Gewone tekst zonder newlines.", result);
    }

    // ── BuildTicketOrderLines ───────────────────────────────────────────────

    [Fact]
    public void BuildTicketOrderLines_NoItems_ReturnsEmpty()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(0, 0, 0, 0, 0);

        Assert.Equal(string.Empty, result.Trim());
    }

    [Fact]
    public void BuildTicketOrderLines_ToegangsticketOnly_ContainsCorrectPriceAndLabel()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(3, 0, 0, 0, 0);

        Assert.Contains("3x Toegangsticket", result);
        Assert.Contains("&euro;24", result); // 3 * 8
        Assert.Contains("22u30", result);
    }

    [Fact]
    public void BuildTicketOrderLines_EtenPartyWithVegetarisch_ContainsVegetarischText()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(0, 2, 1, 0, 0);

        Assert.Contains("2x Eten", result);
        Assert.Contains("waarvan 1 vegetarisch", result);
        Assert.Contains("&euro;100", result); // 2 * 50
    }

    [Fact]
    public void BuildTicketOrderLines_EtenPartyWithoutVegetarisch_DoesNotContainVegetarischText()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(0, 2, 0, 0, 0);

        Assert.Contains("2x Eten", result);
        Assert.DoesNotContain("vegetarisch", result);
    }

    [Fact]
    public void BuildTicketOrderLines_Drankkaart10_ContainsCorrectPrice()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(0, 0, 0, 2, 0);

        Assert.Contains("2x Drankkaart", result);
        Assert.Contains("&euro;10", result);
        Assert.Contains("&euro;20", result); // 2 * 10
    }

    [Fact]
    public void BuildTicketOrderLines_Drankkaart20_ContainsCorrectPrice()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(0, 0, 0, 0, 3);

        Assert.Contains("3x Drankkaart", result);
        Assert.Contains("&euro;20", result);
        Assert.Contains("&euro;60", result); // 3 * 20
    }

    [Fact]
    public void BuildTicketOrderLines_AllItems_ContainsAllLines()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(1, 1, 0, 1, 1);

        Assert.Contains("Toegangsticket", result);
        Assert.Contains("Eten", result);
        Assert.Contains("Drankkaart &euro;10", result);
        Assert.Contains("Drankkaart &euro;20", result);
    }

    [Fact]
    public void BuildTicketOrderLines_ZeroCount_DoesNotIncludeThatLine()
    {
        var result = EmailHtmlBuilder.BuildTicketOrderLines(1, 0, 0, 0, 0);

        Assert.Contains("Toegangsticket", result);
        Assert.DoesNotContain("Eten", result);
        Assert.DoesNotContain("Drankkaart", result);
    }

    // ── BuildSponsorOrderLines ──────────────────────────────────────────────

    [Fact]
    public void BuildSponsorOrderLines_BasicPackage_ContainsPackageNameAndPrice()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Goud", 500, 4, 0, 0, 0, 0);

        Assert.Contains("Pakket Goud", result);
        Assert.Contains("4 tickets inbegrepen", result);
        Assert.Contains("&euro;500", result);
    }

    [Fact]
    public void BuildSponsorOrderLines_WithIncludedVegetarisch_ContainsVegetarischText()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Zilver", 250, 2, 1, 0, 0, 0);

        Assert.Contains("waarvan 1 vegetarisch", result);
    }

    [Fact]
    public void BuildSponsorOrderLines_WithoutIncludedVegetarisch_DoesNotContainVegetarischText()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Zilver", 250, 2, 0, 0, 0, 0);

        Assert.DoesNotContain("vegetarisch", result);
    }

    [Fact]
    public void BuildSponsorOrderLines_WithExtraEtenParty_ContainsExtraLine()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Goud", 500, 4, 0, 2, 0, 0);

        Assert.Contains("2x extra Eten", result);
        Assert.Contains("&euro;100", result); // 2 * 50
    }

    [Fact]
    public void BuildSponsorOrderLines_WithExtraEtenPartyAndVegetarisch_ContainsVegetarischText()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Goud", 500, 4, 0, 2, 1, 0);

        Assert.Contains("waarvan 1 vegetarisch", result);
    }

    [Fact]
    public void BuildSponsorOrderLines_WithExtraDrankkaart20_ContainsExtraLine()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Brons", 100, 0, 0, 0, 0, 3);

        Assert.Contains("3x Drankkaart &euro;20", result);
        Assert.Contains("&euro;60", result); // 3 * 20
    }

    [Fact]
    public void BuildSponsorOrderLines_WithoutExtras_DoesNotContainExtraLines()
    {
        var result = EmailHtmlBuilder.BuildSponsorOrderLines("Brons", 100, 0, 0, 0, 0, 0);

        Assert.DoesNotContain("extra", result);
        Assert.DoesNotContain("Drankkaart", result);
    }

    // ── BuildTicketCards ────────────────────────────────────────────────────

    [Fact]
    public void BuildTicketCards_EmptyList_ReturnsEmpty()
    {
        var result = EmailHtmlBuilder.BuildTicketCards([], isSponsor: false);

        Assert.Equal(string.Empty, result.Trim());
    }

    [Fact]
    public void BuildTicketCards_ToegangsticketNotSponsor_ContainsToegangsticketLabel()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.Toegang), false)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);

        Assert.Contains("Toegangsticket", result);
        Assert.DoesNotContain("Eten", result);
    }

    [Fact]
    public void BuildTicketCards_EtenPartyNotSponsor_ContainsEtenPartyLabel()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.EtenParty), false)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);

        Assert.Contains("Eten &amp; Party", result);
        Assert.DoesNotContain("Vegetarisch", result);
    }

    [Fact]
    public void BuildTicketCards_EtenPartyVegetarischNotSponsor_ContainsVegetarischLabel()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.EtenParty), true)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);

        Assert.Contains("Eten &amp; Party", result);
        Assert.Contains("Vegetarisch", result);
    }

    [Fact]
    public void BuildTicketCards_SponsorNonVegetarisch_ContainsEtenPartyLabel()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.EtenParty), false)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: true);

        Assert.Contains("Eten &amp; Party", result);
        Assert.DoesNotContain("Vegetarisch", result);
    }

    [Fact]
    public void BuildTicketCards_SponsorVegetarisch_ContainsVegetarischLabel()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.EtenParty), true)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: true);

        Assert.Contains("Eten &amp; Party", result);
        Assert.Contains("Vegetarisch", result);
    }

    [Fact]
    public void BuildTicketCards_ContainsTicketId()
    {
        var tickets = new List<TicketPdfData>
        {
            new("mijn-ticket-id-123", "mijn-ticket-id-123:payload", nameof(TicketKind.Toegang), false)
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);

        Assert.Contains("mijn-ticket-id-123", result);
    }

    [Fact]
    public void BuildTicketCards_MultipleTickets_ContainsAllIds()
    {
        var tickets = new List<TicketPdfData>
        {
            new("t-1", "t-1:payload", nameof(TicketKind.Toegang), false),
            new("t-2", "t-2:payload", nameof(TicketKind.EtenParty), true),
        };

        var result = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);

        Assert.Contains("t-1", result);
        Assert.Contains("t-2", result);
    }
}
