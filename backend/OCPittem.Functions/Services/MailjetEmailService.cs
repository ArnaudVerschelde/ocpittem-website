using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Models;
using System.Net;

namespace OCPittem.Functions.Services;

public class MailjetEmailService : IEmailService
{
    private readonly MailjetClient? _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _contactFromEmail;
    private readonly string _contactFromName;
    private readonly string _ticketFromEmail;
    private readonly string _ticketFromName;
    private readonly bool _enabled;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(
        MailjetOptions options,
        bool enabled,
        ILogger<MailjetEmailService> logger)
    {
        _fromEmail = options.FromEmail;
        _fromName = options.FromName;
        _contactFromEmail = string.IsNullOrEmpty(options.ContactFromEmail) ? options.FromEmail : options.ContactFromEmail;
        _contactFromName = string.IsNullOrEmpty(options.ContactFromName) ? options.FromName : options.ContactFromName;
        _ticketFromEmail = string.IsNullOrEmpty(options.TicketFromEmail) ? options.FromEmail : options.TicketFromEmail;
        _ticketFromName = string.IsNullOrEmpty(options.TicketFromName) ? options.FromName : options.TicketFromName;
        _enabled = enabled;
        _logger = logger;

        if (_enabled)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.ApiSecret))
                throw new InvalidOperationException("Mailjet API key/secret missing while Email__Enabled=true");

            _client = new MailjetClient(options.ApiKey, options.ApiSecret);
        }
    }

    public async Task SendTicketConfirmationAsync(
        string toEmail,
        string toName,
        int toegangstickets,
        int etenPartyTickets,
        int vegetarischCount,
        int drankkaart10,
        int drankkaart20,
        IReadOnlyList<TicketPdfData> tickets,
        byte[]? pdfAttachment = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send ticket confirmation to {Email}.", toEmail);
            return;
        }

        var safeName = WebUtility.HtmlEncode(toName);

        var orderLines = new System.Text.StringBuilder();
        if (toegangstickets > 0)
            orderLines.AppendLine($"<li><strong>{toegangstickets}x Toegangsticket</strong> (vanaf 22u30) &mdash; &euro;{toegangstickets * 8}</li>");
        if (etenPartyTickets > 0)
        {
            var vegStr = vegetarischCount > 0 ? $", waarvan {vegetarischCount} vegetarisch" : "";
            orderLines.AppendLine($"<li><strong>{etenPartyTickets}x Eten &amp; Party ticket</strong> (vanaf 19u00{vegStr}) &mdash; &euro;{etenPartyTickets * 50}</li>");
        }
        if (drankkaart10 > 0)
            orderLines.AppendLine($"<li><strong>{drankkaart10}x Drankkaart &euro;10</strong> &mdash; &euro;{drankkaart10 * 10}</li>");
        if (drankkaart20 > 0)
            orderLines.AppendLine($"<li><strong>{drankkaart20}x Drankkaart &euro;20</strong> &mdash; &euro;{drankkaart20 * 20}</li>");

        var total = (toegangstickets * 8) + (etenPartyTickets * 50) + (drankkaart10 * 10) + (drankkaart20 * 20);

        var ticketCards = new System.Text.StringBuilder();
        foreach (var ticket in tickets)
        {
            var typeLabel = ticket.TicketType == nameof(TicketKind.EtenParty)
                ? $"Eten &amp; Party{(ticket.IsVegetarisch ? " (Vegetarisch)" : "")}"
                : "Toegangsticket";
            var qrBase64 = QrCodeHelper.GenerateBase64(ticket.QrPayload);

            ticketCards.AppendLine($@"
                <div style=""border:1px solid #13A2A3;border-radius:6px;padding:16px;margin:12px 0;display:flex;align-items:center;gap:20px;"">
                    <div>
                        <p style=""margin:0 0 4px;font-weight:bold;color:#13A2A3;font-size:14px;"">{typeLabel}</p>
                        <img src=""data:image/png;base64,{qrBase64}"" width=""110"" height=""110"" alt=""QR-code"" style=""display:block;""/>
                        <p style=""margin:4px 0 0;font-size:10px;color:#888;"">ID: {ticket.TicketId}</p>
                    </div>
                </div>");
        }

        var html = $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
                <h2 style=""color:#13A2A3;margin-bottom:4px;"">Bal Parental &mdash; Oudercomité met Pit</h2>
                <hr style=""border:none;border-top:2px solid #13A2A3;margin-bottom:20px;""/>
                <p>Beste {safeName},</p>
                <p>Bedankt voor jouw bestelling voor Bal Parental! Hierbij vind je jouw ticket(s).</p>
                <p style=""font-size:16px;font-weight:bold;color:#13A2A3;"">Tot op zaterdag 20 juni! 🎉</p>
                <h3 style=""margin-top:24px;"">Jouw bestelling:</h3>
                <ul style=""padding-left:20px;"">{orderLines}</ul>
                <p><strong>Totaal: &euro;{total}</strong></p>
                <h3 style=""margin-top:24px;"">Jouw ticket(s) met QR-code:</h3>
                {ticketCards}
                <p style=""margin-top:20px;color:#555;"">
                    De volledige PDF met al jouw tickets vind je ook in bijlage.<br/>
                    Toon de QR-code aan de ingang.
                </p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        var builder = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_ticketFromEmail, _ticketFromName))
            .WithSubject("Jouw tickets voor Bal Parental — Oudercomité met Pit")
            .WithHtmlPart(html)
            .WithTo(new SendContact(toEmail, toName));

        if (pdfAttachment != null)
            builder = builder.WithAttachment(
                new Attachment("JouwBalParentalTickets.pdf", "application/pdf", Convert.ToBase64String(pdfAttachment)));

        await Send(builder.Build(), $"ticket confirmation to {toEmail}");
        _logger.LogInformation("Ticket confirmation email sent to {Email}.", toEmail);
    }

    public async Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail)
    {
        if (!_enabled)
        {
            _logger.LogInformation(
                "Email disabled. Would forward contact message '{Subject}' from {FromEmail} to {ContactEmail}.",
                subject, fromEmail, contactEmail);
            return;
        }

        var safeFromName = WebUtility.HtmlEncode(fromName);
        var safeFromEmail = WebUtility.HtmlEncode(fromEmail);
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeMessage = WebUtility.HtmlEncode(message)
            .Replace("\r\n", "<br />")
            .Replace("\r", "<br />")
            .Replace("\n", "<br />");

        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_contactFromEmail, _contactFromName))
            .WithHtmlPart($@"
                <h2>Nieuw contactbericht via ocpittem.be</h2>
                <p><strong>Van:</strong> {safeFromName} ({safeFromEmail})</p>
                <p><strong>Onderwerp:</strong> {safeSubject}</p>
                <hr />
                <p>{safeMessage}</p>
            ")
            .WithReplyTo(new SendContact(fromEmail, fromName))
            .WithTo(new SendContact(contactEmail))
            .Build();

        await Send(email, $"contact forward to {contactEmail}");
        _logger.LogInformation("Contact notification forwarded from {FromEmail} to {ContactEmail}.", fromEmail, contactEmail);
    }

    public async Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName)
    {
        if (!_enabled)
        {
            _logger.LogInformation(
                "Email disabled. Would send sponsor confirmation to {Email} ({Company}, {Package}).",
                toEmail, companyName, packageName);
            return;
        }

        var safeCompany = WebUtility.HtmlEncode(companyName);
        var safePackage = WebUtility.HtmlEncode(packageName);

        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_fromEmail, _fromName))
            .WithSubject("Sponsoraanvraag ontvangen — Oudercomité met Pit")
            .WithHtmlPart($@"
                <h2>Bedankt voor uw sponsoraanvraag!</h2>
                <p>Beste {safeCompany},</p>
                <p>We hebben uw aanvraag voor het <strong>{safePackage}</strong> pakket goed ontvangen.</p>
                <p>Een lid van het oudercomité zal zo snel mogelijk contact met u opnemen voor de verdere afhandeling.</p>
                <p>Met vriendelijke groeten,</p>
                <p><em>Oudercomité met Pit — Pittem</em></p>
            ")
            .WithTo(new SendContact(toEmail))
            .Build();

        await Send(email, $"sponsor confirmation to {toEmail}");
        _logger.LogInformation("Sponsor confirmation email sent to {Email} ({Company}).", toEmail, companyName);
    }

    public async Task SendDailyReportAsync(IReadOnlyList<string> recipients, byte[] excelBytes, DailyReportStats stats, DateTime reportDate)
    {
        var dateLabel = reportDate.ToString("dd/MM/yyyy");

        if (!_enabled)
        {
            _logger.LogInformation(
                "Email disabled. Would send daily report ({Date}) to {Count} recipient(s).",
                dateLabel, recipients.Count);
            return;
        }

        var html = $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
                <h2 style=""color:#13A2A3;margin-bottom:4px;"">Bal Parental &mdash; Dagelijks overzicht</h2>
                <p style=""color:#666;margin-top:0;"">Rapport van {dateLabel}</p>
                <hr style=""border:none;border-top:2px solid #13A2A3;margin-bottom:20px;""/>

                <h3 style=""margin-bottom:8px;"">🎟️ Bestellingen</h3>
                <table style=""border-collapse:collapse;width:100%;font-size:14px;"">
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Totaal bestellingen</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalOrders}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">Betaald</td>
                        <td style=""padding:6px 12px;font-weight:bold;color:#16a34a;"">{stats.PaidOrders}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Toegangstickets verkocht</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalToegangstickets}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">Eten &amp; Party tickets verkocht</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalEtenPartyTickets}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Waarvan vegetarisch</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalVegetarisch}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">Drankkaarten &euro;10</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalDrankkaart10}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Drankkaarten &euro;20</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalDrankkaart20}</td>
                    </tr>
                    <tr style=""border-top:2px solid #13A2A3;"">
                        <td style=""padding:8px 12px;font-weight:bold;"">Totale omzet (betaald)</td>
                        <td style=""padding:8px 12px;font-weight:bold;color:#13A2A3;font-size:16px;"">&euro;{stats.TotalRevenue:F0}</td>
                    </tr>
                </table>

                <h3 style=""margin-top:24px;margin-bottom:8px;"">🤝 Sponsorpakketten</h3>
                <table style=""border-collapse:collapse;width:100%;font-size:14px;"">
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Totaal aanvragen</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorRequests}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">Betaald</td>
                        <td style=""padding:6px 12px;font-weight:bold;color:#16a34a;"">{stats.PaidSponsorOrders}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">🥉 Brons (&euro;100)</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorBrons}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">🥈 Zilver (&euro;250)</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorZilver}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">🥇 Goud (&euro;500)</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorGoud}</td>
                    </tr>
                    <tr>
                        <td style=""padding:6px 12px;"">Extra Eten &amp; Party tickets</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorExtraEtenParty}</td>
                    </tr>
                    <tr style=""background:#f0fafa;"">
                        <td style=""padding:6px 12px;"">Extra Drankkaarten &euro;20</td>
                        <td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorExtraDrankkaart20}</td>
                    </tr>
                    <tr style=""border-top:2px solid #13A2A3;"">
                        <td style=""padding:8px 12px;font-weight:bold;"">Totale sponsoromzet (betaald)</td>
                        <td style=""padding:8px 12px;font-weight:bold;color:#13A2A3;font-size:16px;"">&euro;{stats.TotalSponsorRevenue:F0}</td>
                    </tr>
                </table>

                <p style=""margin-top:24px;color:#555;font-size:13px;"">
                    Het volledige overzicht vind je in de bijlage (Excel-bestand).
                </p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        var fileName = $"BalParental_Bestellingen_{reportDate:yyyyMMdd}.xlsx";
        var attachmentBase64 = Convert.ToBase64String(excelBytes);

        var toContacts = recipients.Select(r => new SendContact(r)).ToList();

        var builder = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_fromEmail, _fromName))
            .WithSubject($"Bal Parental — Dagelijks overzicht {dateLabel}")
            .WithHtmlPart(html)
            .WithAttachment(new Attachment(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", attachmentBase64));

        foreach (var contact in toContacts)
            builder = builder.WithTo(contact);

        await Send(builder.Build(), $"daily report to {recipients.Count} recipient(s)");
        _logger.LogInformation("Daily report sent to {Count} recipient(s).", recipients.Count);
    }

    private async Task Send(TransactionalEmail email, string context)
    {
        TransactionalEmailResponse resp = await _client!.SendTransactionalEmailAsync(email);

        if (resp.Messages == null || resp.Messages.Length == 0)
            throw new InvalidOperationException("Mailjet: empty response");

        var first = resp.Messages[0];
        if (!string.Equals(first.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var err = first.Errors != null && first.Errors.Count > 0
                ? $"{first.Errors[0].ErrorCode}: {first.Errors[0].ErrorMessage}"
                : "unknown error";
            _logger.LogError("Mailjet failed sending ({Context}). Status={Status}. Error={Error}", context, first.Status, err);
            throw new InvalidOperationException($"Mailjet send failed: {err}");
        }
    }

    public async Task SendSponsorPaymentConfirmationAsync(
        string toEmail,
        string companyName,
        string packageName,
        int extraEtenParty,
        int extraVegetarisch,
        int extraDrankkaart20,
        int includedVegetarisch,
        IReadOnlyList<TicketPdfData> tickets,
        byte[]? pdfAttachment = null,
        byte[]? attestationPdf = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send sponsor payment confirmation to {Email}.", toEmail);
            return;
        }

        var safeCompany = WebUtility.HtmlEncode(companyName);
        var safePackage = WebUtility.HtmlEncode(char.ToUpper(packageName[0]) + packageName[1..].ToLower());

        var packagePrice = packageName.ToLower() switch
        {
            "brons" => 100, "zilver" => 250, "goud" => 500, _ => 0
        };
        var includedTickets = packageName.ToLower() switch
        {
            "zilver" => 2, "goud" => 4, _ => 0
        };

        var orderLines = new System.Text.StringBuilder();
        var includedVegStr = includedVegetarisch > 0 ? $", waarvan {includedVegetarisch} vegetarisch" : "";
        orderLines.AppendLine($"<li><strong>Pakket {safePackage}</strong> ({includedTickets} tickets inbegrepen{includedVegStr}) &mdash; &euro;{packagePrice}</li>");
        if (extraEtenParty > 0)
        {
            var vegStr = extraVegetarisch > 0 ? $", waarvan {extraVegetarisch} vegetarisch" : "";
            orderLines.AppendLine($"<li><strong>{extraEtenParty}x extra Eten &amp; Party ticket</strong>{vegStr} &mdash; &euro;{extraEtenParty * 50}</li>");
        }
        if (extraDrankkaart20 > 0)
            orderLines.AppendLine($"<li><strong>{extraDrankkaart20}x Drankkaart &euro;20</strong> &mdash; &euro;{extraDrankkaart20 * 20}</li>");

        var total = packagePrice + extraEtenParty * 50 + extraDrankkaart20 * 20;
        var attestationNote = attestationPdf != null
            ? "<br/>Uw sponsorattest voor de boekhouding vindt u eveneens als bijlage."
            : "";

        var ticketCards = new System.Text.StringBuilder();
        foreach (var ticket in tickets)
        {
            var typeLabel = ticket.IsVegetarisch ? "Eten &amp; Party (Vegetarisch)" : "Eten &amp; Party";
            var qrBase64 = QrCodeHelper.GenerateBase64(ticket.QrPayload);
            ticketCards.AppendLine($@"
                <div style=""border:1px solid #13A2A3;border-radius:6px;padding:16px;margin:12px 0;display:flex;align-items:center;gap:20px;"">
                    <div>
                        <p style=""margin:0 0 4px;font-weight:bold;color:#13A2A3;font-size:14px;"">{typeLabel}</p>
                        <img src=""data:image/png;base64,{qrBase64}"" width=""110"" height=""110"" alt=""QR-code"" style=""display:block;""/>
                        <p style=""margin:4px 0 0;font-size:10px;color:#888;"">ID: {ticket.TicketId}</p>
                    </div>
                </div>");
        }

        var html = $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
                <h2 style=""color:#13A2A3;margin-bottom:4px;"">Bal Parental &mdash; Oudercomité met Pit</h2>
                <hr style=""border:none;border-top:2px solid #13A2A3;margin-bottom:20px;""/>
                <p>Beste {safeCompany},</p>
                <p>Hartelijk bedankt voor uw steun aan het Bal Parental als <strong>{safePackage}</strong>-sponsor! Uw betaling is ontvangen.</p>
                <p style=""font-size:16px;font-weight:bold;color:#13A2A3;"">Tot op zaterdag 20 juni! 🎉</p>
                <h3 style=""margin-top:24px;"">Uw bestelling:</h3>
                <ul style=""padding-left:20px;"">{orderLines}</ul>
                <p><strong>Totaal: &euro;{total}</strong></p>
                <h3 style=""margin-top:24px;"">Uw ticket(s) met QR-code:</h3>
                {ticketCards}
                <p style=""margin-top:20px;color:#555;"">
                    De volledige PDF met alle tickets vindt u ook in bijlage.<br/>
                    Toon de QR-code aan de ingang.{attestationNote}
                </p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        var builder = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_ticketFromEmail, _ticketFromName))
            .WithSubject($"Sponsorpakket {safePackage} bevestigd — Bal Parental")
            .WithHtmlPart(html)
            .WithTo(new SendContact(toEmail, companyName));

        if (pdfAttachment != null)
            builder = builder.WithAttachment(
                new Attachment("JouwBalParentalTickets.pdf", "application/pdf", Convert.ToBase64String(pdfAttachment)));

        if (attestationPdf != null)
            builder = builder.WithAttachment(
                new Attachment("Sponsorattest-BalParental2026.pdf", "application/pdf", Convert.ToBase64String(attestationPdf)));

        await Send(builder.Build(), $"sponsor payment confirmation to {toEmail}");
        _logger.LogInformation("Sponsor payment confirmation sent to {Email} ({Company}).", toEmail, companyName);
    }
}
