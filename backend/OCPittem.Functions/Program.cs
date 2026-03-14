using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions;
using OCPittem.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        services.Configure<StripeOptions>(config.GetSection("Stripe"));
        services.Configure<MailjetOptions>(config.GetSection("Mailjet"));
        services.Configure<EmailOptions>(config.GetSection("Email"));
        services.Configure<AppOptions>(config.GetSection("App"));
        services.Configure<StorageOptions>(config.GetSection("Storage"));

        services.AddSingleton<IStorageService>(sp =>
        {
            var connectionString = config["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true";
            var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return new TableStorageService(connectionString, opts);
        });

        services.AddSingleton<IStripeService>(sp =>
        {
            var stripe = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
            var app = sp.GetRequiredService<IOptions<AppOptions>>().Value;
            return new StripeService(stripe.SecretKey, stripe.WebhookSecret, stripe.TicketPriceId, app.FrontendUrl);
        });

        services.AddSingleton<IEmailService>(sp =>
        {
            var mailjet = sp.GetRequiredService<IOptions<MailjetOptions>>().Value;
            var email = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<MailjetEmailService>>();
            return new MailjetEmailService(mailjet.ApiKey, mailjet.ApiSecret, mailjet.FromEmail, mailjet.FromName, email.Enabled, logger);
        });

        services.AddSingleton<ITicketPdfService, TicketPdfService>();
    })
    .Build();

host.Run();
