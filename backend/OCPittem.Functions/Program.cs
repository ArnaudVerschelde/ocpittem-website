using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
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
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));
        services.Configure<AppOptions>(config.GetSection("App"));
        services.Configure<StorageOptions>(config.GetSection("Storage"));
        services.Configure<SponsorAttestationOptions>(config.GetSection("SponsorAttestation"));

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
            return new StripeService(stripe, app.FrontendUrl);
        });

        services.AddSingleton<IEmailService>(sp =>
        {
            var email = sp.GetRequiredService<IOptions<EmailOptions>>().Value;

            if (string.Equals(email.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                var smtp = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;
                var sender = sp.GetRequiredService<IOptions<MailjetOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<SmtpEmailService>>();
                return new SmtpEmailService(smtp, sender, email.Enabled, logger);
            }
            else
            {
                var mailjet = sp.GetRequiredService<IOptions<MailjetOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<MailjetEmailService>>();
                return new MailjetEmailService(mailjet, email.Enabled, logger);
            }
        });

        services.AddSingleton<ITicketPdfService, TicketPdfService>();
        services.AddHttpClient();
        services.AddSingleton<ISponsorLogoPackageService, SponsorLogoPackageService>();
        services.AddSingleton<ISponsorAttestationService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SponsorAttestationOptions>>();
            var logger = sp.GetRequiredService<ILogger<SponsorAttestationService>>();
            var connectionString = config.GetConnectionString("StorageAccount") ?? config["AzureWebJobsStorage"];
            var blobServiceClient = !string.IsNullOrWhiteSpace(connectionString)
                ? new BlobServiceClient(connectionString)
                : new BlobServiceClient(
                    new Uri(opts.Value.BlobServiceUri
                        ?? throw new InvalidOperationException("Geen storage connection string of BlobServiceUri gevonden voor SponsorAttestationService.")),
                    new DefaultAzureCredential());
            return new SponsorAttestationService(blobServiceClient, opts, logger);
        });
        services.AddSingleton<IDailyReportService, DailyReportService>();
    })
    .Build();

host.Run();
