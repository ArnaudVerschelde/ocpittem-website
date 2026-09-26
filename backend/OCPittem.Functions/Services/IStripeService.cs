namespace OCPittem.Functions.Services
{
    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
            string orderId, 
            string email, 
            string name,
            int toegangsticketCount, 
            int etenPartyCount,
            int drankkaart10Count, 
            int drankkaart20Count);
        Task<StripeCheckoutResult> CreateSponsorCheckoutSessionAsync(
            string requestId,
            string email,
            string companyName,
            string packageName,
            int extraEtenPartyCount,
            int extraDrankkaart20Count);
        Task<StripeCheckoutResult> CreateCookieSaleCheckoutSessionAsync(
            string orderId,
            string email,
            int coteDorQuantity,
            int lotusQuantity);
        Stripe.Event ConstructWebhookEvent(string json, string signature);
    }
}
