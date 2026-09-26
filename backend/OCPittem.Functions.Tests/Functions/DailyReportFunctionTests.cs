using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Functions;

public class DailyReportFunctionTests
{
    private readonly IDailyReportService _balReportService = Substitute.For<IDailyReportService>();
    private readonly ICookieSaleReportService _cookieSaleReportService =
        Substitute.For<ICookieSaleReportService>();
    private readonly DailyReportFunction _balFunction;
    private readonly CookieSaleReportFunction _cookieFunction;

    public DailyReportFunctionTests()
    {
        _balFunction = new DailyReportFunction(
            _balReportService,
            Substitute.For<ILogger<DailyReportFunction>>());
        _cookieFunction = new CookieSaleReportFunction(
            _cookieSaleReportService,
            Substitute.For<ILogger<CookieSaleReportFunction>>());
    }

    [Fact]
    public async Task BalRun_OnSchedule_CallsOnlyBalReportService()
    {
        var timer = new TimerInfo { IsPastDue = false };

        await _balFunction.Run(timer);

        await _balReportService.Received(1).SendDailyReportAsync();
        await _cookieSaleReportService.DidNotReceive().SendDailyReportAsync();
    }

    [Fact]
    public async Task BalRun_PastDue_StillCallsOnlyBalReportService()
    {
        var timer = new TimerInfo { IsPastDue = true };

        await _balFunction.Run(timer);

        await _balReportService.Received(1).SendDailyReportAsync();
        await _cookieSaleReportService.DidNotReceive().SendDailyReportAsync();
    }

    [Fact]
    public async Task BalRunManual_CallsOnlyBalReportService()
    {
        await _balFunction.RunManual(Substitute.For<HttpRequest>());

        await _balReportService.Received(1).SendDailyReportAsync();
        await _cookieSaleReportService.DidNotReceive().SendDailyReportAsync();
    }

    [Fact]
    public async Task CookieRun_OnSchedule_CallsOnlyCookieReportService()
    {
        var timer = new TimerInfo { IsPastDue = false };

        await _cookieFunction.Run(timer);

        await _cookieSaleReportService.Received(1).SendDailyReportAsync();
        await _balReportService.DidNotReceive().SendDailyReportAsync();
    }

    [Fact]
    public async Task CookieRun_PastDue_StillCallsOnlyCookieReportService()
    {
        var timer = new TimerInfo { IsPastDue = true };

        await _cookieFunction.Run(timer);

        await _cookieSaleReportService.Received(1).SendDailyReportAsync();
        await _balReportService.DidNotReceive().SendDailyReportAsync();
    }

    [Fact]
    public async Task CookieRunManual_CallsOnlyCookieReportService()
    {
        await _cookieFunction.RunManual(Substitute.For<HttpRequest>());

        await _cookieSaleReportService.Received(1).SendDailyReportAsync();
        await _balReportService.DidNotReceive().SendDailyReportAsync();
    }
}
