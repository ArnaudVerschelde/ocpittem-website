using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class DailyReportFunction
{
    private readonly IDailyReportService _reportService;
    private readonly ILogger<DailyReportFunction> _logger;

    public DailyReportFunction(IDailyReportService reportService, ILogger<DailyReportFunction> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    // Elke dag om 06:00 UTC (= 08:00 Belgische zomertijd)
    [Function("DailyReport")]
    public async Task Run([TimerTrigger("0 0 6 * * *")] TimerInfo timer)
    {
        _logger.LogInformation("DailyReport triggered at {Time} UTC.", DateTime.UtcNow);

        if (timer.IsPastDue)
            _logger.LogWarning("DailyReport timer is running late.");

        await _reportService.SendDailyReportAsync();
    }
}
