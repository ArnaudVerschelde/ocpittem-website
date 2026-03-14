namespace OCPittem.Functions.Services
{
    public interface ITicketPdfService
    {
        byte[] GenerateTicketPdf(string ticketId, string customerName, string eventName, string qrPayload);
    }
}
