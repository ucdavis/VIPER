using System.Data;
using System.Globalization;

namespace Viper.Areas.Reports.Data;

/// <summary>
/// Null-safe reads of stored procedure columns by name. The legacy procedures return many
/// char columns padded with spaces, so text is trimmed on the way in.
/// </summary>
public static class DataRecordExtensions
{
    public static string? GetTrimmedString(this IDataRecord record, string column)
    {
        ArgumentNullException.ThrowIfNull(record);
        int ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal)
            ? null
            : Convert.ToString(record.GetValue(ordinal), CultureInfo.InvariantCulture)?.TrimEnd();
    }

    public static DateOnly? GetDateOnly(this IDataRecord record, string column)
    {
        ArgumentNullException.ThrowIfNull(record);
        int ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(record.GetDateTime(ordinal));
    }

    /// <summary>
    /// A date the procedure returns as text, such as <c>CONVERT(VARCHAR(10), d, 101)</c> for
    /// "MM/dd/yyyy". Blank or unreadable text is null.
    /// </summary>
    public static DateOnly? GetDateOnlyFromText(this IDataRecord record, string column, string format)
    {
        string? text = record.GetTrimmedString(column);
        return DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : null;
    }

    /// <summary>
    /// A whole number from any numeric column type, such as the decimal <c>FLOOR(...)</c> returns.
    /// </summary>
    public static int? GetNullableInt32(this IDataRecord record, string column)
    {
        ArgumentNullException.ThrowIfNull(record);
        int ordinal = record.GetOrdinal(column);
        return record.IsDBNull(ordinal) ? null : Convert.ToInt32(record.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
