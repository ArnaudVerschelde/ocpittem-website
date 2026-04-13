using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _enableSsl;

    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _contactFromEmail;
    private readonly string _contactFromName;
    private readonly string _ticketFromEmail;
    private readonly string _ticketFromName;

    private readonly bool _enabled;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        SmtpOptions smtpOptions,
        MailjetOptions senderOptions,
        bool enabled,
        ILogger<SmtpEmailService> logger)
    {
        _host = smtpOptions.Host;
        _port = smtpOptions.Port;
        _username = smtpOptions.Username;
        _password = smtpOptions.Password;
        _enableSsl = smtpOptions.EnableSsl;

        _fromEmail = senderOptions.FromEmail;
        _fromName = senderOptions.FromName;
        _contactFromEmail = string.IsNullOrWhiteSpace(senderOptions.ContactFromEmail) ? senderOptions.FromEmail : senderOptions.ContactFromEmail;
        _contactFromName = string.IsNullOrWhiteSpace(senderOptions.ContactFromName) ? senderOptions.FromName : senderOptions.ContactFromName;
        _ticketFromEmail = string.IsNullOrWhiteSpace(senderOptions.TicketFromEmail) ? senderOptions.FromEmail : senderOptions.TicketFromEmail;
        _ticketFromName = string.IsNullOrWhiteSpace(senderOptions.TicketFromName) ? senderOptions.FromName : senderOptions.TicketFromName;

        _enabled = enabled;
        _logger = logger;

        if (_enabled)
        {
            if (string.IsNullOrWhiteSpace(_host))
                throw new InvalidOperationException("SMTP host ontbreekt.");
            if (_port <= 0)
                throw new InvalidOperationException("SMTP port is ongeldig.");
            if (string.IsNullOrWhiteSpace(_username))
                throw new InvalidOperationException("SMTP username ontbreekt.");
            if (string.IsNullOrWhiteSpace(_password))
                throw new InvalidOperationException("SMTP password ontbreekt.");
        }
    }

    public async Task SendTicketConfirmationAsync(
        string toEmail, string toName,
        int toegangstickets, int etenPartyTickets, int vegetarischCount, int drankkaart10, int drankkaart20,
        IReadOnlyList<TicketPdfData> tickets, byte[]? pdfAttachment = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send ticket confirmation to {Email}.", toEmail);
            return;
        }

        var orderLines = EmailHtmlBuilder.BuildTicketOrderLines(toegangstickets, etenPartyTickets, vegetarischCount, drankkaart10, drankkaart20);
        var total = (toegangstickets * 8) + (etenPartyTickets * 50) + (drankkaart10 * 10) + (drankkaart20 * 20);
        var ticketCards = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: false);
        var safeName = WebUtility.HtmlEncode(toName);

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
                <p style=""margin-top:20px;color:#555;"">De volledige PDF met al jouw tickets vind je ook in bijlage.<br/>Toon de QR-code aan de ingang.</p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        using var message = CreateHtmlMessage(new MailAddress(_ticketFromEmail, _ticketFromName, Encoding.UTF8),
            "Jouw tickets voor Bal Parental — Oudercomité met Pit", html);
        message.To.Add(new MailAddress(toEmail, toName, Encoding.UTF8));
        if (pdfAttachment != null)
            message.Attachments.Add(CreateAttachment(pdfAttachment, "JouwBalParentalTickets.pdf", MediaTypeNames.Application.Pdf));

        await SendAsync(message, $"ticket confirmation to {toEmail}");
        _logger.LogInformation("Ticket confirmation email sent to {Email}.", toEmail);
    }

    public async Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would forward contact message '{Subject}' from {FromEmail} to {ContactEmail}.", subject, fromEmail, contactEmail);
            return;
        }

        var safeFromName = WebUtility.HtmlEncode(fromName);
        var safeFromEmail = WebUtility.HtmlEncode(fromEmail);
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeMessage = EmailHtmlBuilder.NormalizeNewLinesToHtml(message);

        using var mail = CreateHtmlMessage(new MailAddress(_contactFromEmail, _contactFromName, Encoding.UTF8), subject, $@"
                <h2>Nieuw contactbericht via ocpittem.be</h2>
                <p><strong>Van:</strong> {safeFromName} ({safeFromEmail})</p>
                <p><strong>Onderwerp:</strong> {safeSubject}</p>
                <hr />
                <p>{safeMessage}</p>");
        mail.To.Add(new MailAddress(contactEmail));
        mail.ReplyToList.Add(new MailAddress(fromEmail, fromName, Encoding.UTF8));

        await SendAsync(mail, $"contact forward to {contactEmail}");
        _logger.LogInformation("Contact notification forwarded from {FromEmail} to {ContactEmail}.", fromEmail, contactEmail);
    }

    public async Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send sponsor confirmation to {Email} ({Company}, {Package}).", toEmail, companyName, packageName);
            return;
        }

        var safeCompany = WebUtility.HtmlEncode(companyName);
        var safePackage = WebUtility.HtmlEncode(packageName);

        using var message = CreateHtmlMessage(new MailAddress(_fromEmail, _fromName, Encoding.UTF8),
            "Sponsoraanvraag ontvangen — Oudercomité met Pit",
            $@"<h2>Bedankt voor uw sponsoraanvraag!</h2>
                <p>Beste {safeCompany},</p>
                <p>We hebben uw aanvraag voor het <strong>{safePackage}</strong> pakket goed ontvangen.</p>
                <p>Een lid van het oudercomité zal zo snel mogelijk contact met u opnemen voor de verdere afhandeling.</p>
                <p>Met vriendelijke groeten,</p>
                <p><em>Oudercomité met Pit — Pittem</em></p>");
        message.To.Add(new MailAddress(toEmail));

        await SendAsync(message, $"sponsor confirmation to {toEmail}");
        _logger.LogInformation("Sponsor confirmation email sent to {Email} ({Company}).", toEmail, companyName);
    }

    public async Task SendDailyReportAsync(IReadOnlyList<string> recipients, byte[] excelBytes, DailyReportStats stats, DateTime reportDate)
    {
        var dateLabel = reportDate.ToString("dd/MM/yyyy");

        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send daily report ({Date}) to {Count} recipient(s).", dateLabel, recipients.Count);
            return;
        }

        if (recipients.Count == 0)
        {
            _logger.LogWarning("No recipients configured for daily report.");
            return;
        }

        var html = $@"
            <div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;"">
                <h2 style=""color:#13A2A3;margin-bottom:4px;"">Bal Parental &mdash; Dagelijks overzicht</h2>
                <p style=""color:#666;margin-top:0;"">Rapport van {dateLabel}</p>
                <hr style=""border:none;border-top:2px solid #13A2A3;margin-bottom:20px;""/>
                <h3 style=""margin-bottom:8px;"">🎟️ Bestellingen</h3>
                <table style=""border-collapse:collapse;width:100%;font-size:14px;"">
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Totaal bestellingen</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalOrders}</td></tr>
                    <tr><td style=""padding:6px 12px;"">Betaald</td><td style=""padding:6px 12px;font-weight:bold;color:#16a34a;"">{stats.PaidOrders}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Toegangstickets verkocht</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalToegangstickets}</td></tr>
                    <tr><td style=""padding:6px 12px;"">Eten &amp; Party tickets verkocht</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalEtenPartyTickets}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Waarvan vegetarisch</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalVegetarisch}</td></tr>
                    <tr><td style=""padding:6px 12px;"">Drankkaarten &euro;10</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalDrankkaart10}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Drankkaarten &euro;20</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalDrankkaart20}</td></tr>
                    <tr style=""border-top:2px solid #13A2A3;""><td style=""padding:8px 12px;font-weight:bold;"">Totale omzet (betaald)</td><td style=""padding:8px 12px;font-weight:bold;color:#13A2A3;font-size:16px;"">&euro;{stats.TotalRevenue:F0}</td></tr>
                </table>
                <h3 style=""margin-top:24px;margin-bottom:8px;"">🤝 Sponsorpakketten</h3>
                <table style=""border-collapse:collapse;width:100%;font-size:14px;"">
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Totaal aanvragen</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorRequests}</td></tr>
                    <tr><td style=""padding:6px 12px;"">Betaald</td><td style=""padding:6px 12px;font-weight:bold;color:#16a34a;"">{stats.PaidSponsorOrders}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">🥉 Brons (&euro;100)</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorBrons}</td></tr>
                    <tr><td style=""padding:6px 12px;"">🥈 Zilver (&euro;250)</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorZilver}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">🥇 Goud (&euro;500)</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorGoud}</td></tr>
                    <tr><td style=""padding:6px 12px;"">Extra Eten &amp; Party tickets</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorExtraEtenParty}</td></tr>
                    <tr style=""background:#f0fafa;""><td style=""padding:6px 12px;"">Extra Drankkaarten &euro;20</td><td style=""padding:6px 12px;font-weight:bold;"">{stats.TotalSponsorExtraDrankkaart20}</td></tr>
                    <tr style=""border-top:2px solid #13A2A3;""><td style=""padding:8px 12px;font-weight:bold;"">Totale sponsoromzet (betaald)</td><td style=""padding:8px 12px;font-weight:bold;color:#13A2A3;font-size:16px;"">&euro;{stats.TotalSponsorRevenue:F0}</td></tr>
                </table>
                <p style=""margin-top:24px;color:#555;font-size:13px;"">Het volledige overzicht vind je in de bijlage (Excel-bestand).</p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        using var message = CreateHtmlMessage(new MailAddress(_fromEmail, _fromName, Encoding.UTF8),
            $"Bal Parental — Dagelijks overzicht {dateLabel}", html);
        foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
            message.To.Add(new MailAddress(recipient));
        message.Attachments.Add(CreateAttachment(excelBytes,
            $"BalParental_Bestellingen_{reportDate:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));

        await SendAsync(message, $"daily report to {recipients.Count} recipient(s)");
        _logger.LogInformation("Daily report sent to {Count} recipient(s).", recipients.Count);
    }

    public async Task SendSponsorPaymentConfirmationAsync(
        string toEmail, string companyName, string packageName,
        int extraEtenParty, int extraVegetarisch, int extraDrankkaart20, int includedVegetarisch,
        IReadOnlyList<TicketPdfData> tickets, byte[]? pdfAttachment = null, byte[]? attestationPdf = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send sponsor payment confirmation to {Email}.", toEmail);
            return;
        }

        var safeCompany = WebUtility.HtmlEncode(companyName);
        var normalizedPackage = string.IsNullOrWhiteSpace(packageName) ? "Onbekend"
            : char.ToUpper(packageName[0]) + packageName[1..].ToLower();
        var safePackage = WebUtility.HtmlEncode(normalizedPackage);
        var packagePrice = packageName.ToLowerInvariant() switch { "brons" => 100, "zilver" => 250, "goud" => 500, _ => 0 };
        var includedTickets = packageName.ToLowerInvariant() switch { "zilver" => 2, "goud" => 4, _ => 0 };

        var orderLines = EmailHtmlBuilder.BuildSponsorOrderLines(safePackage, packagePrice, includedTickets,
            includedVegetarisch, extraEtenParty, extraVegetarisch, extraDrankkaart20);
        var total = packagePrice + extraEtenParty * 50 + extraDrankkaart20 * 20;
        var attestationNote = attestationPdf != null ? "<br/>Uw sponsorattest voor de boekhouding vindt u eveneens als bijlage." : "";
        var ticketCards = EmailHtmlBuilder.BuildTicketCards(tickets, isSponsor: true);

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
                <p style=""margin-top:20px;color:#555;"">De volledige PDF met alle tickets vindt u ook in bijlage.<br/>Toon de QR-code aan de ingang.{attestationNote}</p>
                <hr style=""border:none;border-top:1px solid #eee;margin-top:24px;""/>
                <p style=""font-size:11px;color:#aaa;"">Oudercomité met Pit &mdash; Pittem &mdash; ocpittem.be</p>
            </div>";

        using var message = CreateHtmlMessage(new MailAddress(_ticketFromEmail, _ticketFromName, Encoding.UTF8),
            $"Sponsorpakket {safePackage} bevestigd — Bal Parental", html);
        message.To.Add(new MailAddress(toEmail, companyName, Encoding.UTF8));
        if (pdfAttachment != null)
            message.Attachments.Add(CreateAttachment(pdfAttachment, "JouwBalParentalTickets.pdf", MediaTypeNames.Application.Pdf));
        if (attestationPdf != null)
            message.Attachments.Add(CreateAttachment(attestationPdf, "Sponsorattest-BalParental2026.pdf", MediaTypeNames.Application.Pdf));

        await SendAsync(message, $"sponsor payment confirmation to {toEmail}");
        _logger.LogInformation("Sponsor payment confirmation sent to {Email} ({Company}).", toEmail, companyName);
    }

    private async Task SendAsync(MailMessage message, string context)
    {
        try
        {
            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_username, _password)
            };
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed ({Context}).", context);
            throw;
        }
    }

    private static MailMessage CreateHtmlMessage(MailAddress from, string subject, string htmlBody) =>
        new() { From = from, Subject = subject, SubjectEncoding = Encoding.UTF8, Body = htmlBody, BodyEncoding = Encoding.UTF8, IsBodyHtml = true };

    private static Attachment CreateAttachment(byte[] bytes, string fileName, string mediaType) =>
        new(new MemoryStream(bytes), fileName, mediaType);
}
