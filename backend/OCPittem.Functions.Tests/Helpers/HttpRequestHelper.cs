using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace OCPittem.Functions.Tests.Helpers;

public static class HttpRequestHelper
{
    public static HttpRequest CreateJsonRequest(string json)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(json);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        return request;
    }

    public static HttpRequest CreateJsonRequest<T>(T body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CreateJsonRequest(json);
    }

    public static HttpRequest CreateWebhookRequest(string json, string stripeSignature)
    {
        var request = CreateJsonRequest(json);
        request.Headers["Stripe-Signature"] = stripeSignature;
        return request;
    }

    public static HttpRequest CreateGetRequest(Dictionary<string, string>? query = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "GET";

        if (query != null)
        {
            var qs = string.Join("&", query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
            request.QueryString = new QueryString($"?{qs}");
        }

        return request;
    }
}
