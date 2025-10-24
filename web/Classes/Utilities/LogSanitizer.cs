namespace Viper.Classes.Utilities;

/// <summary>
/// Provides utilities for sanitizing user input before logging to prevent log injection attacks.
/// Designed for user-provided values like identifiers (MothraId) and years (gradYear).
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes an alphanumeric identifier (like MothraId) for safe logging.
    /// Only letters and numbers are preserved. Any other characters are removed
    /// to prevent log injection attacks where malicious users could insert newlines or control characters.
    /// </summary>
    /// <param name="value">The value to sanitize. Can be null.</param>
    /// <returns>A sanitized string containing only alphanumeric characters, or an empty string if sanitization results in no valid characters, or null if input was null.</returns>
    public static string? SanitizeId(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            // Return empty string for whitespace-only input to prevent logging control characters
            return string.Empty;
        }

        // MothraId should only contain alphanumeric characters
        // Remove anything else to prevent log injection
        var sanitized = new string([.. value.Where(char.IsLetterOrDigit)]);

        // If sanitization results in empty string, return it (user input had no valid alphanumeric chars)
        return sanitized;
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

    /// <summary>
    /// Sanitizes and validates a graduation year for safe logging.
    /// Ensures the year is a valid 4-digit year within acceptable range (2009 to current year + 5).
    /// Returns a sanitized representation suitable for logging that indicates invalid years.
    /// </summary>
    /// <param name="year">The graduation year to sanitize.</param>
    /// <returns>The sanitized year as a string, or a safe placeholder if invalid.</returns>
    public static string SanitizeYear(int year)
    {
        const int MinYear = 2009; // Oldest year in the system
        var maxYear = DateTime.UtcNow.Year + 5; // Allow up to 5 years in the future

        // Validate that it's a reasonable 4-digit year
        if (year < MinYear || year > maxYear)
        {
            // Return a safe placeholder that indicates an invalid year without exposing the actual value
            return $"INVALID({year})";
        }

        return year.ToString();
    }

    /// <summary>
    /// Sanitizes and validates a nullable graduation year for safe logging.
    /// </summary>
    /// <param name="year">The nullable graduation year to sanitize.</param>
    /// <returns>The sanitized year as a string, or "null" if the year is null.</returns>
    public static string SanitizeYear(int? year)
    {
        return year.HasValue ? SanitizeYear(year.Value) : "null";
    }
}
