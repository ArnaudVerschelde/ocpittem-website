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
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(string apiKey, string fromEmail, string fromName)
    {
        _client = new SendGridClient(apiKey);
        _fromEmail = fromEmail;
        _fromName = fromName;
    }

    public async Task SendTicketConfirmationAsync(string toEmail, string toName, int quantity, byte[]? pdfAttachment = null)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = $"Jouw tickets voor Bal Parental — Oudercomité met PIT!",
            HtmlContent = $@"
                <h2>Bedankt voor je bestelling, {toName}!</h2>
                <p>Je hebt <strong>{quantity} ticket(s)</strong> besteld voor het Bal Parental.</p>
                <p>In bijlage vind je jouw ticket(s) als PDF met QR-code.</p>
                <p>Tot dan!</p>
                <p><em>Oudercomité met PIT! — Pittem</em></p>
            ",
        };
        msg.AddTo(new EmailAddress(toEmail, toName));

        if (pdfAttachment != null)
        {
            msg.AddAttachment("tickets.pdf", Convert.ToBase64String(pdfAttachment), "application/pdf");
        }

        await _client.SendEmailAsync(msg);
    }

    public async Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = $"[Contact] {subject}",
            HtmlContent = $@"
                <h2>Nieuw contactbericht via ocpittem.be</h2>
                <p><strong>Van:</strong> {fromName} ({fromEmail})</p>
                <p><strong>Onderwerp:</strong> {subject}</p>
                <hr />
                <p>{message.Replace("\n", "<br />")}</p>
            ",
            ReplyTo = new EmailAddress(fromEmail, fromName),
        };
        msg.AddTo(new EmailAddress(contactEmail));

        await _client.SendEmailAsync(msg);
    }

    public async Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = $"Sponsoraanvraag ontvangen — Oudercomité met PIT!",
            HtmlContent = $@"
                <h2>Bedankt voor uw sponsoraanvraag!</h2>
                <p>Beste {companyName},</p>
                <p>We hebben uw aanvraag voor het <strong>{packageName}</strong> pakket goed ontvangen.</p>
                <p>Een lid van het oudercomité zal zo snel mogelijk contact met u opnemen voor de verdere afhandeling.</p>
                <p>Met vriendelijke groeten,</p>
                <p><em>Oudercomité met PIT! — Pittem</em></p>
            ",
        };
        msg.AddTo(new EmailAddress(toEmail));

        await _client.SendEmailAsync(msg);
    }
}
