using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Functions;

public class DailyReportFunctionTests
{
    private readonly IDailyReportService _reportService = Substitute.For<IDailyReportService>();
    private readonly ILogger<DailyReportFunction> _logger = Substitute.For<ILogger<DailyReportFunction>>();
    private readonly DailyReportFunction _sut;

    public DailyReportFunctionTests()
    {
        _sut = new DailyReportFunction(_reportService, _logger);
    }

    [Fact]
    public async Task Run_OnSchedule_CallsReportService()
    {
        var timer = new TimerInfo { IsPastDue = false };

        await _sut.Run(timer);

        await _reportService.Received(1).SendDailyReportAsync();
    }

    [Fact]
    public async Task Run_PastDue_StillCallsReportService()
    {
        var timer = new TimerInfo { IsPastDue = true };

        await _sut.Run(timer);

        await _reportService.Received(1).SendDailyReportAsync();
    }
}
