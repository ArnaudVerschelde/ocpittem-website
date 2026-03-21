namespace OCPittem.Functions.Services
{
    public interface IEmailService
    {
        Task SendTicketConfirmationAsync(
            string toEmail, 
            string toName,
            int toegangstickets, 
            int etenPartyTickets, 
            int vegetarischCount,
            int drankkaart10, 
            int drankkaart20,
            byte[]? pdfAttachment = null);
        Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail);
        Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName);
    }
}
