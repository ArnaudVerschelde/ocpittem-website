using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OCPittem.Functions.Services;

public interface ITicketPdfService
{
    byte[] GenerateTicketPdf(string ticketId, string customerName, string eventName, string qrPayload);
}

public class TicketPdfService : ITicketPdfService
{
    public TicketPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateTicketPdf(string ticketId, string customerName, string eventName, string qrPayload)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Oudercomité met Pit").FontSize(20).Bold().FontColor(Colors.Orange.Darken2);
                        col.Item().Text(eventName).FontSize(14).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"Naam: {customerName}").FontSize(14);
                    col.Item().Text($"Ticket ID: {ticketId}").FontSize(10).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(15).AlignCenter().Column(qrCol =>
                    {
                        qrCol.Item().AlignCenter().Text("QR-code").FontSize(10).FontColor(Colors.Grey.Medium);
                        qrCol.Item().AlignCenter()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10)
                            .Text(qrPayload).FontSize(8);
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("ocpittem.be").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.Span(" — ").FontSize(9).FontColor(Colors.Grey.Lighten1);
                    t.Span("Pittem").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }
}
