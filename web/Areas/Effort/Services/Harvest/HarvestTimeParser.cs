using System.Globalization;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Utility for parsing time strings from CREST data.
/// Handles various time formats: "8:00 AM", "1:30 PM", "1430", "900", "2400", "0000".
/// </summary>
public static class HarvestTimeParser
{
    /// <summary>
    /// Parse time string into TimeSpan.
    /// Handles formats: "8:00 AM", "1:30 PM", "1430", "900", "2400", "0000".
    /// </summary>
    /// <param name="timeStr">The time string to parse.</param>
    /// <param name="logger">Optional logger for warning/debug messages.</param>
    /// <param name="context">Optional context string for log messages (e.g., offering ID).</param>
    /// <returns>TimeSpan if parsing succeeded, null otherwise.</returns>
    public static TimeSpan? ParseTimeString(string? timeStr, ILogger? logger = null, string? context = null)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
        {
            return null;
        }

        var trimmed = timeStr.Trim();
        var isDigitsOnly = trimmed.All(char.IsDigit);

        // Try parsing as DateTime (handles "8:00 AM", "1:30 PM" formats)
        // Skip for digits-only input to avoid DateTime.TryParse interpreting "1430" as year 1430
        if (!isDigitsOnly && DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, out var dateTime))
        {
            return dateTime.TimeOfDay;
        }

        // Fallback: Try parsing as military time (e.g., "1430", "900", "2400", "0000")
        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length >= 3 && digitsOnly.Length <= 4)
        {
            var paddedTime = digitsOnly.PadLeft(4, '0');
            if (int.TryParse(paddedTime[..2], out var hour) &&
                int.TryParse(paddedTime[2..4], out var minute))
            {
                // Handle "2400" as end of day (24:00 = next day 00:00)
                if (hour == 24 && minute == 0)
                {
                    return new TimeSpan(24, 0, 0);
                }

                // Standard time validation
                if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                {
                    return new TimeSpan(hour, minute, 0);
                }
            }
        }

        logger?.LogWarning(
            "Failed to parse time string '{TimeStr}' {Context}",
            timeStr,
            string.IsNullOrEmpty(context) ? "" : $"for {context}");

        return null;
    }

    /// <summary>
    /// Calculate session duration in minutes from CREST date/time values.
    /// FromDate/ThruDate are DateTime, FromTime/ThruTime are strings (e.g., "1430" for 14:30).
    /// </summary>
    /// <param name="fromDate">Session start date.</param>
    /// <param name="fromTime">Session start time string.</param>
    /// <param name="thruDate">Session end date.</param>
    /// <param name="thruTime">Session end time string.</param>
    /// <param name="logger">Optional logger for warnings.</param>
    /// <param name="sessionId">Optional session ID for log context.</param>
    /// <returns>Duration in minutes, or 0 if times cannot be calculated.</returns>
    public static int CalculateSessionMinutes(
        DateTime? fromDate,
        string? fromTime,
        DateTime? thruDate,
        string? thruTime,
        ILogger? logger = null,
        string? sessionId = null)
    {
        if (fromDate == null || string.IsNullOrEmpty(fromTime) ||
            thruDate == null || string.IsNullOrEmpty(thruTime))
        {
            return 0;
        }

        var startTime = ParseTimeString(fromTime, logger, sessionId);
        var endTime = ParseTimeString(thruTime, logger, sessionId);

        if (startTime == null || endTime == null)
        {
            return 0;
        }

        // Combine date and time components
        var startDateTime = fromDate.Value.Date.Add(startTime.Value);
        var endDateTime = thruDate.Value.Date.Add(endTime.Value);

        // Handle 24:00 (end of day) - treat as next day 00:00
        if (endTime.Value.Hours == 24)
        {
            endDateTime = thruDate.Value.Date.AddDays(1);
        }

        if (endDateTime <= startDateTime)
        {
            logger?.LogWarning(
                "Negative or zero duration calculated: {Start} to {End} {Context}",
                startDateTime,
                endDateTime,
                string.IsNullOrEmpty(sessionId) ? "" : $"for {sessionId}");
            return 0;
        }

        return (int)(endDateTime - startDateTime).TotalMinutes;
    }
}
