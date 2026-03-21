using OCPittem.Functions.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OCPittem.Functions.Services;

public class TicketPdfService : ITicketPdfService
{
    private const string BrandColor = "#13A2A3";

    public TicketPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateTicketsPdf(IReadOnlyList<TicketPdfData> tickets, string customerName, string eventName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Oudercomité met Pit").FontSize(22).Bold().FontColor(BrandColor);
                            col.Item().Text(eventName).FontSize(13).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    header.Item().PaddingTop(6).Height(2).Background(BrandColor);
                });

                page.Content().PaddingTop(20).Column(content =>
                {
                    content.Item().Text($"Beste {customerName},").FontSize(14).Bold();
                    content.Item().PaddingTop(4).Text(
                        $"Bedankt voor jouw bestelling voor {eventName}! Hierbij vind je jouw ticket(s).")
                        .FontSize(12);
                    content.Item().PaddingTop(2).Text("Tot op zaterdag 20 juni!")
                        .FontSize(13).Bold().FontColor(BrandColor);

                    foreach (var (ticket, index) in tickets.Select((t, i) => (t, i + 1)))
                    {
                        var typeLabel = ticket.TicketType == nameof(TicketKind.EtenParty)
                            ? $"Eten & Party{(ticket.IsVegetarisch ? " (Vegetarisch)" : "")}"
                            : "Toegangsticket";

                        var qrBytes = QrCodeHelper.GeneratePng(ticket.QrPayload);

                        content.Item().PaddingTop(16)
                            .Border(1).BorderColor(BrandColor)
                            .Padding(14)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Ticket {index}").FontSize(9).FontColor(Colors.Grey.Medium);
                                    col.Item().PaddingTop(2).Text(typeLabel).FontSize(15).Bold().FontColor(BrandColor);
                                    col.Item().PaddingTop(8).Text($"Naam: {customerName}").FontSize(11);
                                    col.Item().PaddingTop(4).Text($"ID: {ticket.TicketId}").FontSize(8).FontColor(Colors.Grey.Medium);
                                });

                                row.ConstantItem(110).AlignMiddle().AlignRight().Image(qrBytes);
                            });
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().Height(1).Background(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(8).AlignCenter().Text(t =>
                    {
                        t.Span("ocpittem.be").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span(" \u2014 Zaterdag 20 juni \u2014 Bal Parental").FontSize(9).FontColor(Colors.Grey.Lighten1);
                    });
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }
}

