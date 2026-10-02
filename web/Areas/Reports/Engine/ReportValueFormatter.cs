using System.Globalization;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Formats report values as text for exports that have no cell types (CSV, PDF). Numbers use
/// US English separators and dates use MM/dd/yyyy, matching the legacy reports.
/// </summary>
public static class ReportValueFormatter
{
    public const string DateFormat = "MM/dd/yyyy";

    // Roughly decimal.MaxValue; larger doubles can't convert to decimal.
    private const double DecimalLimit = 7.9e28;

    private static readonly CultureInfo UsEnglish = CultureInfo.GetCultureInfo("en-US");

    public static string Format(object? value, ReportColumnMetadata column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return value switch
        {
            null => string.Empty,
            string text => text,
            DateOnly date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
            IEnumerable<string> values => string.Join(", ", values),
            _ when IsNumeric(column.Format) && TryGetDecimal(value, out decimal number) => FormatNumber(number, column),
            _ => string.Format(CultureInfo.InvariantCulture, "{0}", value),
        };
    }

    public static bool IsNumeric(ReportColumnFormat format)
    {
        return format is ReportColumnFormat.Number or ReportColumnFormat.Currency or ReportColumnFormat.Percent;
    }

    public static bool TryGetDecimal(object? value, out decimal number)
    {
        switch (value)
        {
            case decimal exact:
                number = exact;
                return true;
            case int whole:
                number = whole;
                return true;
            case long wide:
                number = wide;
                return true;
            case double approximate when double.IsFinite(approximate) && Math.Abs(approximate) < DecimalLimit:
                number = (decimal)approximate;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    /// <summary>
    /// The labels of a row's highlights and badges, for formats that can't show color.
    /// </summary>
    public static string FlagText(ReportRowResult row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return string.Join("; ", row.Flags.Select(flag => flag.Label).Distinct(StringComparer.Ordinal));
    }

    private static string FormatNumber(decimal number, ReportColumnMetadata column)
    {
        string decimals = (column.Decimals ?? 0).ToString(CultureInfo.InvariantCulture);
        return column.Format switch
        {
            ReportColumnFormat.Currency => number.ToString("C" + decimals, UsEnglish),
            ReportColumnFormat.Percent => number.ToString("P" + decimals, UsEnglish),
            _ => number.ToString("N" + decimals, UsEnglish),
        };
    }
}
