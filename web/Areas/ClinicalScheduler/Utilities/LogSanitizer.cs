namespace VIPER.Areas.ClinicalScheduler.Utilities;

/// <summary>
/// Provides utilities for sanitizing user input before logging to prevent log injection attacks.
/// Specifically designed for MothraId and similar identifiers that should only contain alphanumeric characters.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes an alphanumeric identifier (like MothraId) for safe logging.
    /// Only letters and numbers are preserved. Any other characters are removed
    /// to prevent log injection attacks where malicious users could insert newlines or control characters.
    /// </summary>
    /// <param name="value">The value to sanitize. Can be null.</param>
    /// <returns>A sanitized string containing only alphanumeric characters, or null if input was null.</returns>
    public static string? SanitizeId(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // MothraId should only contain alphanumeric characters
        // Remove anything else to prevent log injection
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }

    /// <summary>
    /// Sanitizes multiple alphanumeric identifiers for safe logging.
    /// </summary>
    /// <param name="values">The values to sanitize.</param>
    /// <returns>An array of sanitized strings.</returns>
    public static string?[] SanitizeId(params string?[] values)
    {
        return [.. values.Select(SanitizeId)];
    }
}
