using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace OCPittem.Functions.Functions;

public class TicketValidateFunction
{
    private readonly ILogger<TicketValidateFunction> _logger;

    public TicketValidateFunction(ILogger<TicketValidateFunction> logger)
    {
        _logger = logger;
    }

    [Function("ValidateTicket")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tickets/validate")] HttpRequest req)
    {
        var code = req.Query["code"].ToString();

        if (string.IsNullOrWhiteSpace(code))
        {
            return new BadRequestObjectResult(new { error = "Geen ticket-code opgegeven." });
        }

        var parts = code.Split(':');
        if (parts.Length != 2)
        {
            return new BadRequestObjectResult(new { valid = false, error = "Ongeldig ticket-formaat." });
        }

        var ticketId = parts[0];
        var providedSignature = parts[1];

        // Verify HMAC
        var secret = Encoding.UTF8.GetBytes("ocpittem-ticket-secret-change-me");
        using var hmac = new HMACSHA256(secret);
        var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        var expectedSignature = Convert.ToBase64String(expectedHash)[..16];

        if (!string.Equals(providedSignature, expectedSignature, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid ticket signature for ticket {TicketId}", ticketId);
            return new OkObjectResult(new { valid = false, error = "Ongeldig ticket." });
        }

        // TODO: Check in Table Storage if ticket exists and is not already scanned
        // TODO: Mark ScannedAt timestamp

        _logger.LogInformation("Ticket {TicketId} validated", ticketId);

        return new OkObjectResult(new { valid = true, ticketId });
    }
}
