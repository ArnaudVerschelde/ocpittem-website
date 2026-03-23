using System.Text.RegularExpressions;

namespace OCPittem.Functions.Validators;

/// <summary>
/// Validates Belgian enterprise numbers (KBO/BCE) using the official modulo-97 check digit algorithm.
/// Accepted input formats: "0123.456.789", "0123456789", "BE0123456789", "BE 0123.456.789".
/// </summary>
public static class BelgianEnterpriseNumberValidator
{
    private static readonly Regex _nonDigits = new(@"[^0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Returns true when <paramref name="value"/> is a valid Belgian enterprise number.
    /// Strips spaces, dots and "BE" prefix before validation.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var digits = _nonDigits.Replace(value, "");

        if (digits.Length != 10) return false;
        if (digits[0] != '0' && digits[0] != '1') return false;

        var prefix = long.Parse(digits[0..8]);
        var check  = int.Parse(digits[8..10]);

        var expected = prefix % 97 == 0 ? 97 : (int)(97 - prefix % 97);

        return check == expected;
    }

    /// <summary>
    /// Normalises a valid enterprise number to the standard "0xxx.xxx.xxx" display format.
    /// Returns the original value unchanged if it cannot be normalised.
    /// </summary>
    public static string Normalize(string value)
    {
        var digits = _nonDigits.Replace(value, "");
        if (digits.Length != 10) return value;
        return $"{digits[0..4]}.{digits[4..7]}.{digits[7..10]}";
    }
}
