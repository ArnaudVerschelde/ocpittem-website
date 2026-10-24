using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Functions;

namespace OCPittem.Functions.Tests.Functions;

public class HealthFunctionTests
{
    [Fact]
    public async Task Run_Returns200WithStatusOkJson()
    {
        var services = new ServiceCollection()
            .Configure<WorkerOptions>(o => o.Serializer = new Azure.Core.Serialization.JsonObjectSerializer())
            .BuildServiceProvider();
        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(services);
        var sut = new HealthFunction(Substitute.For<ILogger<HealthFunction>>());

        var response = await sut.Run(new FakeRequest(context));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var json = await new StreamReader(response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Equal("""{"status":"ok"}""", json);
    }

    private sealed class FakeRequest(FunctionContext context) : HttpRequestData(context)
    {
        public override Stream Body { get; } = new MemoryStream();
        public override HttpHeadersCollection Headers { get; } = new();
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];
        public override Uri Url { get; } = new("http://localhost/api/health");
        public override IEnumerable<System.Security.Claims.ClaimsIdentity> Identities { get; } = [];
        public override string Method => "GET";
        public override HttpResponseData CreateResponse() => new FakeResponse(FunctionContext);
    }

    private sealed class FakeResponse(FunctionContext context) : HttpResponseData(context)
    {
        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = new();
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies { get; } = Substitute.For<HttpCookies>();
    }
}
