using System.Net;
using System.Text;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

internal static class EmailHtmlBuilder
{
    internal static string NormalizeNewLinesToHtml(string input) =>
        WebUtility.HtmlEncode(input)
            .Replace("\r\n", "<br />")
            .Replace("\r", "<br />")
            .Replace("\n", "<br />");

    internal static string BuildTicketCards(IReadOnlyList<TicketPdfData> tickets, bool isSponsor)
    {
        var sb = new StringBuilder();
        foreach (var ticket in tickets)
        {
            var typeLabel = isSponsor
                ? (ticket.IsVegetarisch ? "Eten &amp; Party (Vegetarisch)" : "Eten &amp; Party")
                : ticket.TicketType == nameof(TicketKind.EtenParty)
                    ? $"Eten &amp; Party{(ticket.IsVegetarisch ? " (Vegetarisch)" : "")}"
                    : "Toegangsticket";

            var qrBase64 = QrCodeHelper.GenerateBase64(ticket.QrPayload);
            sb.AppendLine($@"
                <div style=""border:1px solid #13A2A3;border-radius:6px;padding:16px;margin:12px 0;display:flex;align-items:center;gap:20px;"">
                    <div>
                        <p style=""margin:0 0 4px;font-weight:bold;color:#13A2A3;font-size:14px;"">{typeLabel}</p>
                        <img src=""data:image/png;base64,{qrBase64}"" width=""110"" height=""110"" alt=""QR-code"" style=""display:block;""/>
                        <p style=""margin:4px 0 0;font-size:10px;color:#888;"">ID: {ticket.TicketId}</p>
                    </div>
                </div>");
        }
        return sb.ToString();
    }

    internal static string BuildTicketOrderLines(int toegangstickets, int etenPartyTickets, int vegetarischCount, int drankkaart10, int drankkaart20)
    {
        var sb = new StringBuilder();
        if (toegangstickets > 0)
            sb.AppendLine($"<li><strong>{toegangstickets}x Toegangsticket</strong> (vanaf 22u30) &mdash; &euro;{toegangstickets * 8}</li>");
        if (etenPartyTickets > 0)
        {
            var vegStr = vegetarischCount > 0 ? $", waarvan {vegetarischCount} vegetarisch" : "";
            sb.AppendLine($"<li><strong>{etenPartyTickets}x Eten &amp; Party ticket</strong> (vanaf 19u30{vegStr}) &mdash; &euro;{etenPartyTickets * 50}</li>");
        }
        if (drankkaart10 > 0)
            sb.AppendLine($"<li><strong>{drankkaart10}x Drankkaart &euro;10</strong> &mdash; &euro;{drankkaart10 * 10}</li>");
        if (drankkaart20 > 0)
            sb.AppendLine($"<li><strong>{drankkaart20}x Drankkaart &euro;20</strong> &mdash; &euro;{drankkaart20 * 20}</li>");
        return sb.ToString();
    }

    internal static string BuildSponsorOrderLines(string safePackage, int packagePrice, int includedTickets, int includedVegetarisch,
        int extraEtenParty, int extraVegetarisch, int extraDrankkaart20)
    {
        var sb = new StringBuilder();
        var includedVegStr = includedVegetarisch > 0 ? $", waarvan {includedVegetarisch} vegetarisch" : "";
        sb.AppendLine($"<li><strong>Pakket {safePackage}</strong> ({includedTickets} tickets inbegrepen{includedVegStr}) &mdash; &euro;{packagePrice}</li>");
        if (extraEtenParty > 0)
        {
            var vegStr = extraVegetarisch > 0 ? $", waarvan {extraVegetarisch} vegetarisch" : "";
            sb.AppendLine($"<li><strong>{extraEtenParty}x extra Eten &amp; Party ticket</strong>{vegStr} &mdash; &euro;{extraEtenParty * 50}</li>");
        }
        if (extraDrankkaart20 > 0)
            sb.AppendLine($"<li><strong>{extraDrankkaart20}x Drankkaart &euro;20</strong> &mdash; &euro;{extraDrankkaart20 * 20}</li>");
        return sb.ToString();
    }

    internal static string BuildCookieSaleConfirmationHtml(CookieSaleConfirmationData data)
    {
        var safeName = WebUtility.HtmlEncode(data.Name);
        var safeConfirmationNumber = WebUtility.HtmlEncode(data.ConfirmationNumber);
        var safeClassName = WebUtility.HtmlEncode(data.ClassName);
        var lines = new StringBuilder();

        if (data.CoteDorQuantity > 0)
        {
            lines.AppendLine($@"
                <tr>
                    <td style=""padding:8px 10px;"">Côte d'Or pakket</td>
                    <td style=""padding:8px 10px;text-align:right;"">{data.CoteDorQuantity} x &euro;11,00</td>
                    <td style=""padding:8px 10px;text-align:right;"">{FormatEuroCents(data.CoteDorQuantity * 1100)}</td>
                </tr>");
        }

        if (data.LotusQuantity > 0)
        {
            lines.AppendLine($@"
                <tr style=""background:#f8fafc;"">
                    <td style=""padding:8px 10px;"">Lotus pakket</td>
                    <td style=""padding:8px 10px;text-align:right;"">{data.LotusQuantity} x &euro;9,00</td>
                    <td style=""padding:8px 10px;text-align:right;"">{FormatEuroCents(data.LotusQuantity * 900)}</td>
                </tr>");
        }

        return $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
                <h2 style=""color:#13A2A3;margin-bottom:4px;"">Koekjesverkoop &mdash; Oudercomité met Pit</h2>
                <p style=""color:#666;margin-top:0;"">10 december 2026</p>
                <hr style=""border:none;border-top:2px solid #13A2A3;margin-bottom:20px;""/>
                <p>Beste {safeName},</p>
                <p>Bedankt voor je bestelling voor de koekjesverkoop van OC Pittem.</p>
                <p><strong>Bevestigingsnummer:</strong> {safeConfirmationNumber}<br/>
                   <strong>Klas:</strong> {safeClassName}</p>
                <table style=""border-collapse:collapse;width:100%;font-size:14px;margin-top:18px;"">
                    <thead>
                        <tr style=""background:#13A2A3;color:white;"">
                            <th style=""padding:8px 10px;text-align:left;"">Product</th>
                            <th style=""padding:8px 10px;text-align:right;"">Aantal</th>
                            <th style=""padding:8px 10px;text-align:right;"">Subtotaal</th>
                        </tr>
                    </thead>
                    <tbody>{lines}</tbody>
                </table>
                <p><strong>Totaal pakketten:</strong> {data.TotalPackages}</p>
                <p style=""font-size:17px;font-weight:bold;color:#13A2A3;"">Totaal betaald: {FormatEuroCents(data.TotalAmountCents)}</p>
                <p>Je betaling werd succesvol ontvangen. Bedankt voor je steun!</p>
                <p>Met vriendelijke groeten,<br/><em>Oudercomité met Pit</em></p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";
    }

    internal static string FormatEuroCents(int amountCents) =>
        $"&euro;{amountCents / 100},{amountCents % 100:00}";
}
