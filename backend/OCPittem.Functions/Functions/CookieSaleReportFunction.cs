using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class CookieSaleReportFunction
{
    private readonly ICookieSaleReportService _reportService;
    private readonly ILogger<CookieSaleReportFunction> _logger;

    public CookieSaleReportFunction(
        ICookieSaleReportService reportService,
        ILogger<CookieSaleReportFunction> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [Function("CookieSaleDailyReport")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("CookieSaleDailyReport triggered at {Time} UTC.", DateTime.UtcNow);

        if (timer.IsPastDue)
            _logger.LogWarning("CookieSaleDailyReport timer is running late.");

        await _reportService.SendDailyReportAsync();
    }

    [Function("CookieSaleDailyReportManual")]
    public async Task<IActionResult> RunManual(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "manage/report/cookie-sale/send")] HttpRequest req)
    {
        _logger.LogInformation("Cookie-sale report manually triggered at {Time} UTC.", DateTime.UtcNow);
        await _reportService.SendDailyReportAsync();
        return new OkObjectResult(new { message = "Koekjesverkooprapport verstuurd." });
    }
}
