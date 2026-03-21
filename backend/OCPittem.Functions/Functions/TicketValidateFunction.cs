using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class TicketValidateFunction
{
    private readonly AppOptions _appOptions;
    private readonly IStorageService _storage;
    private readonly ILogger<TicketValidateFunction> _logger;

    public TicketValidateFunction(IOptions<AppOptions> appOptions, 
        IStorageService storage, 
        ILogger<TicketValidateFunction> logger)
    {
        _appOptions = appOptions.Value;
        _storage = storage;
        _logger = logger;
    }

    [Function("ValidateTicket")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets/validate")] HttpRequest req)
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

        var secret = Encoding.UTF8.GetBytes(_appOptions.TicketHmacSecret);
        using var hmac = new HMACSHA256(secret);
        var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        var expectedSignature = Convert.ToBase64String(expectedHash)[..16];

        if (!string.Equals(providedSignature, expectedSignature, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid ticket signature for ticket {TicketId}", ticketId);
            return new OkObjectResult(new { valid = false, error = "Ongeldig ticket." });
        }

        var ticket = await _storage.GetTicketByIdAsync(ticketId);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketId} not found in storage", ticketId);
            return new OkObjectResult(new { valid = false, error = "Ongeldig ticket." });
        }

        if (ticket.ScannedAt.HasValue)
        {
            _logger.LogWarning("Ticket {TicketId} already scanned at {ScannedAt}", ticketId, ticket.ScannedAt);
            return new OkObjectResult(new { valid = false, error = $"Ticket al gescand om {ticket.ScannedAt:HH:mm}." });
        }

        await _storage.MarkTicketScannedAsync(ticket);

        _logger.LogInformation("Ticket {TicketId} validated", ticketId);

        return new OkObjectResult(new { valid = true, ticketId, ticketType = ticket.TicketType });
    }
}
