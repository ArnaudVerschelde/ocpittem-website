using System.Net;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OCPittem.Functions.Services;

public interface IEmailService
{
    Task SendTicketConfirmationAsync(string toEmail, string toName, int quantity, byte[]? pdfAttachment = null);
    Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail);
    Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName);
}

public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient? _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enabled;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        string? apiKey,
        string fromEmail,
        string fromName,
        bool enabled,
        ILogger<SendGridEmailService> logger)
    {
        _fromEmail = fromEmail;
        _fromName = fromName;
        _enabled = enabled;
        _logger = logger;

        if (_enabled)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid API key missing while Email__Enabled=true");

            _client = new SendGridClient(apiKey);
        }
    }

    public async Task SendTicketConfirmationAsync(string toEmail, string toName, int quantity, byte[]? pdfAttachment = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation(
                "Email disabled. Would send ticket confirmation to {Email} ({Qty} tickets).",
                toEmail, quantity);
            return;
        }

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = "Jouw tickets voor Bal Parental — Oudercomité met Pit",
            HtmlContent = $@"
                <h2>Bedankt voor je bestelling, {toName}!</h2>
                <p>Je hebt <strong>{quantity} ticket(s)</strong> besteld voor het Bal Parental.</p>
                <p>In bijlage vind je jouw ticket(s) als PDF met QR-code.</p>
                <p>Tot dan!</p>
                <p><em>Oudercomité met Pit — Pittem</em></p>
            ",
        };

        msg.AddTo(new EmailAddress(toEmail, toName));

        if (pdfAttachment != null)
            msg.AddAttachment("tickets.pdf", Convert.ToBase64String(pdfAttachment), "application/pdf");

        var response = await _client!.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Ticket confirmation email sent to {Email} ({Qty} tickets).", toEmail, quantity);
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed for ticket confirmation to {Email}. Status: {Status}, Body: {Body}",
                toEmail, (int)response.StatusCode, body);
        }
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

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = $"[Contact] {safeSubject}",
            HtmlContent = $@"
                <h2>Nieuw contactbericht via ocpittem.be</h2>
                <p><strong>Van:</strong> {safeFromName} ({safeFromEmail})</p>
                <p><strong>Onderwerp:</strong> {safeSubject}</p>
                <hr />
                <p>{safeMessage}</p>
            ",
            ReplyTo = new EmailAddress(fromEmail, fromName),
        };

        msg.AddTo(new EmailAddress(contactEmail));

        var response = await _client!.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Contact notification forwarded from {FromEmail} to {ContactEmail}.", fromEmail, contactEmail);
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed for contact notification from {FromEmail}. Status: {Status}, Body: {Body}",
                fromEmail, (int)response.StatusCode, body);
        }
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

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = "Sponsoraanvraag ontvangen — Oudercomité met Pit",
            HtmlContent = $@"
                <h2>Bedankt voor uw sponsoraanvraag!</h2>
                <p>Beste {companyName},</p>
                <p>We hebben uw aanvraag voor het <strong>{packageName}</strong> pakket goed ontvangen.</p>
                <p>Een lid van het oudercomité zal zo snel mogelijk contact met u opnemen voor de verdere afhandeling.</p>
                <p>Met vriendelijke groeten,</p>
                <p><em>Oudercomité met Pit — Pittem</em></p>
            ",
        };

        msg.AddTo(new EmailAddress(toEmail));

        var response = await _client!.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Sponsor confirmation email sent to {Email} ({Company}).", toEmail, companyName);
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed for sponsor confirmation to {Email}. Status: {Status}, Body: {Body}",
                toEmail, (int)response.StatusCode, body);
        }
    }
}