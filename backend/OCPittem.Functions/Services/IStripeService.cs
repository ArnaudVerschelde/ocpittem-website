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
        Stripe.Event ConstructWebhookEvent(string json, string signature);
    }
}
