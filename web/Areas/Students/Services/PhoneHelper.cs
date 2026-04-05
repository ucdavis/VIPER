using System.Text.RegularExpressions;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Utilities for normalizing, formatting, and validating 7- and 10-digit US phone numbers.
/// Stores raw digits only.
/// </summary>
public static partial class PhoneHelper
{
    [GeneratedRegex(@"[^0-9]")]
    private static partial Regex NonDigitRegex();

    private static string StripToDigits(string input) => NonDigitRegex().Replace(input, "");

    /// <summary>
    /// Strips non-digit characters and returns 7 or 10 raw digits.
    /// Returns null if the input is empty or not a valid length.
    /// </summary>
    public static string? NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = StripToDigits(input);
        return digits.Length is 7 or 10 ? digits : null;
    }

    /// <summary>
    /// Formats a phone string for display.
    /// 7 digits: 123-4567. 10 digits: (123) 456-7890.
    /// Returns the raw input if it cannot be formatted.
    /// </summary>
    public static string? FormatPhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = StripToDigits(input);

        if (digits.Length == 7)
        {
            return $"{digits[..3]}-{digits[3..]}";
        }

        if (digits.Length == 10)
        {
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        }

        return input;
    }

    /// <summary>
    /// Validates whether a phone string contains 7 or 10 digits.
    /// Empty/null is acceptable (fields are optional).
    /// </summary>
    public static bool IsValidPhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var digits = StripToDigits(input);
        return digits.Length is 7 or 10;
    }
}
