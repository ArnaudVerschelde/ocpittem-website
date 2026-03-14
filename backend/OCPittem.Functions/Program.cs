using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OCPittem.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.AddSingleton<IStorageService>(sp =>
        {
            var connectionString = config.GetValue<string>("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
            return new TableStorageService(connectionString, config);
        });

        services.AddSingleton<IStripeService>(sp =>
        {
            var secretKey = config["Stripe:SecretKey"] ?? "";
            var webhookSecret = config["Stripe:WebhookSecret"] ?? "";
            var ticketPriceId = config["Stripe:TicketPriceId"] ?? "";
            var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
            return new StripeService(secretKey, webhookSecret, ticketPriceId, frontendUrl);
        });

        services.AddSingleton<IEmailService>(sp =>
        {
            var apiKey = config["SendGrid:ApiKey"] ?? "";
            var fromEmail = config["SendGrid:FromEmail"] ?? "noreply@ocpittem.be";
            var fromName = config["SendGrid:FromName"] ?? "Oudercomité met PIT!";
            return new SendGridEmailService(apiKey, fromEmail, fromName);
        });

        services.AddSingleton<ITicketPdfService, TicketPdfService>();
    })
    .Build();

host.Run();
