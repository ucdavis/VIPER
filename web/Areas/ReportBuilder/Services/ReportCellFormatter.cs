using System.Globalization;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>
/// Turns a report value into text for formats without cell types (PDF, filter headings), and
/// tells the Excel export which values are numbers and dates. US English separators; dates as
/// MM/dd/yyyy like the rest of VIPER. Percent columns hold fractions (0.25 shows as 25%).
/// </summary>
public static class ReportCellFormatter
{
    public const string DateFormat = "MM/dd/yyyy";
    public const string DateTimeFormat = "MM/dd/yyyy h:mm tt";
    private const string TimeFormat = "h:mm tt";
    private const string Yes = "Yes";
    private const string No = "No";

    private static readonly CultureInfo UsEnglish = CultureInfo.GetCultureInfo("en-US");
    private static readonly HashSet<string> YesWords = new(StringComparer.OrdinalIgnoreCase) { "Y", "Yes", "True", "1" };
    private static readonly HashSet<string> NoWords = new(StringComparer.OrdinalIgnoreCase) { "N", "No", "False", "0" };

    public static string Text(object? value, ReportColumnFormat format)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (format == ReportColumnFormat.YesNo)
        {
            return YesNo(value);
        }

        if (TryGetDateTime(value, out DateTime dateTime))
        {
            return dateTime.ToString(ShowsTime(format, dateTime) ? DateTimeFormat : DateFormat, CultureInfo.InvariantCulture);
        }

        if (TryGetNumber(value, out decimal number))
        {
            return FormatNumber(number, format);
        }

        return value switch
        {
            bool flag => flag ? Yes : No,
            TimeSpan time => DateTime.MinValue.Add(time).ToString(TimeFormat, CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    /// <summary>Whether the format lines its values up on the right.</summary>
    public static bool IsNumeric(ReportColumnFormat format)
    {
        return format is ReportColumnFormat.Number or ReportColumnFormat.Currency or ReportColumnFormat.Percent;
    }

    /// <summary>Whether a date shows its time: always for DateTime columns, and for text columns when it has one.</summary>
    public static bool ShowsTime(ReportColumnFormat format, DateTime value)
    {
        return format == ReportColumnFormat.DateTime || (format != ReportColumnFormat.Date && value.TimeOfDay != TimeSpan.Zero);
    }

    public static bool TryGetNumber(object? value, out decimal number)
    {
        switch (value)
        {
            case decimal exact:
                number = exact;
                return true;
            case int or long or short or byte:
                number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            case double or float:
                double approximate = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                bool fits = double.IsFinite(approximate) && Math.Abs(approximate) < (double)decimal.MaxValue;
                number = fits ? (decimal)approximate : 0;
                return fits;
            default:
                number = 0;
                return false;
        }
    }

    public static bool TryGetDateTime(object? value, out DateTime dateTime)
    {
        switch (value)
        {
            case DateTime exact:
                dateTime = exact;
                return true;
            case DateTimeOffset offset:
                dateTime = offset.DateTime;
                return true;
            case DateOnly date:
                dateTime = date.ToDateTime(TimeOnly.MinValue);
                return true;
            default:
                dateTime = default;
                return false;
        }
    }

    private static string FormatNumber(decimal number, ReportColumnFormat format)
    {
        return format switch
        {
            ReportColumnFormat.Currency => number.ToString("C2", UsEnglish),
            ReportColumnFormat.Percent => number.ToString("#,##0.##%", UsEnglish),
            _ => number.ToString("#,##0.##", UsEnglish),
        };
    }

    private static string YesNo(object value)
    {
        if (value is bool flag)
        {
            return flag ? Yes : No;
        }

        if (TryGetNumber(value, out decimal number))
        {
            return number != 0 ? Yes : No;
        }

        string text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (YesWords.Contains(text))
        {
            return Yes;
        }

        return NoWords.Contains(text) ? No : text;
    }
}
