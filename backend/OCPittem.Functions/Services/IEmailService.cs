using OCPittem.Functions.Models;

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
            IReadOnlyList<TicketPdfData> tickets,
            byte[]? pdfAttachment = null);
        Task SendContactNotificationAsync(string fromName, string fromEmail, string subject, string message, string contactEmail);
        Task SendSponsorConfirmationAsync(string toEmail, string companyName, string packageName);
        Task SendSponsorPaymentConfirmationAsync(
            string toEmail,
            string companyName,
            string packageName,
            int extraEtenParty,
            int extraVegetarisch,
            int extraDrankkaart20,
            int includedVegetarisch,
            IReadOnlyList<TicketPdfData> tickets,
            byte[]? pdfAttachment = null);
        Task SendDailyReportAsync(IReadOnlyList<string> recipients, byte[] excelBytes, DailyReportStats stats, DateTime reportDate);
    }
}
