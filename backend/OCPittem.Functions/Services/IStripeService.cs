namespace OCPittem.Functions.Services
{
    public interface IStripeService
    {
        Task<StripeCheckoutResult> CreateCheckoutSessionAsync(string orderId, string email, string name, int quantity);
        Stripe.Event ConstructWebhookEvent(string json, string signature);
    }
}
