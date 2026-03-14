using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Microsoft.Extensions.Logging;
using System.Net;

namespace OCPittem.Functions.Services;

public class MailjetEmailService : IEmailService
{
    private readonly MailjetClient? _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enabled;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(
        string? apiKey,
        string? apiSecret,
        string fromEmail,
        string fromName,
        bool enabled,
        ILogger<MailjetEmailService> logger)
    {
        _fromEmail = fromEmail;
        _fromName = fromName;
        _enabled = enabled;
        _logger = logger;

        if (_enabled)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Mailjet API key/secret missing while Email__Enabled=true");

            _client = new MailjetClient(apiKey, apiSecret);
        }
    }

    public async Task SendTicketConfirmationAsync(string toEmail, string toName, int quantity, byte[]? pdfAttachment = null)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email disabled. Would send ticket confirmation to {Email} ({Qty} tickets).", toEmail, quantity);
            return;
        }

        var builder = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_fromEmail, _fromName))
            .WithSubject("Jouw tickets voor Bal Parental — Oudercomité met Pit")
            .WithHtmlPart($@"
                <h2>Bedankt voor je bestelling, {WebUtility.HtmlEncode(toName)}!</h2>
                <p>Je hebt <strong>{quantity} ticket(s)</strong> besteld voor het Bal Parental.</p>
                <p>In bijlage vind je jouw ticket(s) als PDF met QR-code.</p>
                <p>Tot dan!</p>
                <p><em>Oudercomité met Pit — Pittem</em></p>
            ")
            .WithTo(new SendContact(toEmail, toName));

        if (pdfAttachment != null)
            builder = builder.WithAttachment(new Attachment("tickets.pdf", "application/pdf", Convert.ToBase64String(pdfAttachment)));

        await Send(builder.Build(), $"ticket confirmation to {toEmail}");
        _logger.LogInformation("Ticket confirmation email sent to {Email} ({Qty} tickets).", toEmail, quantity);
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
            .WithFrom(new SendContact(_fromEmail, _fromName))
            .WithSubject($"[Contact] {safeSubject}")
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
}