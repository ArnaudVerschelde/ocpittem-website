using System.Security.Cryptography;
using System.Text;

namespace OCPittem.Functions.Services;

internal static class ConfirmationNumberGenerator
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    internal static string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(8);
        var suffix = new StringBuilder(12);
        var buffer = 0;
        var bitsInBuffer = 0;

        foreach (var value in randomBytes)
        {
            buffer = (buffer << 8) | value;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5 && suffix.Length < 12)
            {
                bitsInBuffer -= 5;
                suffix.Append(Alphabet[(buffer >> bitsInBuffer) & 31]);
            }
        }

        return $"KV26-{suffix}";
    }
}
