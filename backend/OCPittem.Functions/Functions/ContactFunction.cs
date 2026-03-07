using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class ContactFunction
{
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<ContactFunction> _logger;

    public ContactFunction(IEmailService email, IConfiguration config, ILogger<ContactFunction> logger)
    {
        _email = email;
        _config = config;
        _logger = logger;
    }

    [Function("Contact")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req)
    {
        ContactRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<ContactRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig verzoek." });
        }

        if (body == null
            || string.IsNullOrWhiteSpace(body.Name)
            || string.IsNullOrWhiteSpace(body.Email)
            || string.IsNullOrWhiteSpace(body.Subject)
            || string.IsNullOrWhiteSpace(body.Message))
        {
            return new BadRequestObjectResult(new { error = "Vul alle velden in." });
        }

        // Basic length validation
        if (body.Name.Length > 200 || body.Email.Length > 200 || body.Subject.Length > 500 || body.Message.Length > 5000)
        {
            return new BadRequestObjectResult(new { error = "Een of meer velden zijn te lang." });
        }

        var contactEmail = _config["App:ContactEmail"] ?? "oudercomitepittem@gmail.com";

        try
        {
            await _email.SendContactNotificationAsync(body.Name, body.Email, body.Subject, body.Message, contactEmail);
            _logger.LogInformation("Contact form submitted by {Email}", body.Email);
            return new OkObjectResult(new ContactResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", body.Email);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
