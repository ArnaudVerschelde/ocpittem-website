namespace OCPittem.Functions.Services
{
    public record TicketPdfData(string TicketId, string QrPayload, string TicketType, bool IsVegetarisch);

    public interface ITicketPdfService
    {
        byte[] GenerateTicketsPdf(IReadOnlyList<TicketPdfData> tickets, string customerName, string eventName);
    }
}
