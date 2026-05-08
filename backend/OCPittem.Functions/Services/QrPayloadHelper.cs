using System.Security.Cryptography;
using System.Text;

namespace OCPittem.Functions.Services;

internal static class QrPayloadHelper
{
    internal static string Generate(string ticketId, string hmacSecret)
    {
        var secret = Encoding.UTF8.GetBytes(hmacSecret);
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        var signature = Convert.ToBase64String(hash)[..16];
        return $"{ticketId}:{signature}";
    }
}
