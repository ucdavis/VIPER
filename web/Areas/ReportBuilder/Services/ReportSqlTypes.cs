using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>Maps SQL Server type names to the kinds of value a report can show or take.</summary>
public static class ReportSqlTypes
{
    private static readonly Dictionary<string, ReportValueKind> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["char"] = ReportValueKind.Text,
        ["varchar"] = ReportValueKind.Text,
        ["nchar"] = ReportValueKind.Text,
        ["nvarchar"] = ReportValueKind.Text,
        ["text"] = ReportValueKind.Text,
        ["ntext"] = ReportValueKind.Text,
        ["sysname"] = ReportValueKind.Text,
        ["xml"] = ReportValueKind.Text,
        ["tinyint"] = ReportValueKind.WholeNumber,
        ["smallint"] = ReportValueKind.WholeNumber,
        ["int"] = ReportValueKind.WholeNumber,
        ["bigint"] = ReportValueKind.WholeNumber,
        ["decimal"] = ReportValueKind.FractionalNumber,
        ["numeric"] = ReportValueKind.FractionalNumber,
        ["money"] = ReportValueKind.FractionalNumber,
        ["smallmoney"] = ReportValueKind.FractionalNumber,
        ["float"] = ReportValueKind.FractionalNumber,
        ["real"] = ReportValueKind.FractionalNumber,
        ["date"] = ReportValueKind.Date,
        ["datetime"] = ReportValueKind.DateTime,
        ["datetime2"] = ReportValueKind.DateTime,
        ["smalldatetime"] = ReportValueKind.DateTime,
        ["datetimeoffset"] = ReportValueKind.DateTime,
        ["time"] = ReportValueKind.Time,
        ["bit"] = ReportValueKind.Boolean,
        ["uniqueidentifier"] = ReportValueKind.UniqueId,
        ["binary"] = ReportValueKind.Binary,
        ["varbinary"] = ReportValueKind.Binary,
        ["image"] = ReportValueKind.Binary,
        ["timestamp"] = ReportValueKind.Binary,
        ["rowversion"] = ReportValueKind.Binary,
    };

    /// <summary>
    /// The kind of a type such as "nvarchar(50)" or "decimal(9,2)"; the length or precision is
    /// ignored. Unknown types (sql_variant, geography, ...) are <see cref="ReportValueKind.Other"/>.
    /// </summary>
    public static ReportValueKind KindOf(string? sqlType)
    {
        if (string.IsNullOrWhiteSpace(sqlType))
        {
            return ReportValueKind.Other;
        }

        int paren = sqlType.IndexOf('(', StringComparison.Ordinal);
        string baseType = (paren < 0 ? sqlType : sqlType[..paren]).Trim();
        return Kinds.TryGetValue(baseType, out ReportValueKind kind) ? kind : ReportValueKind.Other;
    }

    /// <summary>Whether a report can show or take values of this kind.</summary>
    public static bool IsSupported(ReportValueKind kind)
    {
        return kind is not (ReportValueKind.Binary or ReportValueKind.Other);
    }
}
