using System.Text.RegularExpressions;

namespace Viper.Classes.Utilities;

public record ExportFilenameOptions
{
    public required string ReportName { get; init; }
    public string? AcademicYear { get; init; }
    public string? Department { get; init; }
    public string? TermName { get; init; }
    public string? ClinicalType { get; init; }
    public string? InstructorName { get; init; }
    public int? StartYear { get; init; }
    public int? EndYear { get; init; }
    public string Extension { get; init; } = ".xlsx";
}

public static partial class ExcelHelper
{
    /// <summary>
    /// Prevent CSV/Excel formula injection (OWASP CWE-1236).
    /// Prefixes strings starting with =, +, -, or @ with a leading apostrophe.
    /// </summary>
    public static string SanitizeStringCell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        return value[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
    }

    /// <summary>
    /// Build a sanitized export filename from report metadata.
    /// Joins non-null tokens with hyphens and removes invalid filename characters.
    /// </summary>
    public static string BuildExportFilename(ExportFilenameOptions options)
    {
        var parts = new List<string> { options.ReportName };

        // AcademicYear and TermName are mutually exclusive time identifiers;
        // prefer AcademicYear when both are set to avoid duplicate segments.
        if (!string.IsNullOrWhiteSpace(options.AcademicYear)) parts.Add(options.AcademicYear);
        else if (!string.IsNullOrWhiteSpace(options.TermName)) parts.Add(options.TermName);
        if (!string.IsNullOrWhiteSpace(options.Department)) parts.Add(options.Department);
        if (!string.IsNullOrWhiteSpace(options.ClinicalType)) parts.Add(options.ClinicalType);
        if (!string.IsNullOrWhiteSpace(options.InstructorName)) parts.Add(options.InstructorName);
        if (options.StartYear.HasValue && options.EndYear.HasValue)
            parts.Add($"{options.StartYear}-{options.EndYear}");

        var joined = string.Join("-", parts);

        // Remove invalid filename characters
        var sanitized = InvalidFilenameCharsRegex().Replace(joined, "");

        // Collapse whitespace
        sanitized = WhitespaceRegex().Replace(sanitized, " ").Trim();

        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "Report";

        return sanitized + options.Extension;
    }

    /// <summary>
    /// Sanitize a string for use as an Excel worksheet name.
    /// Strips characters forbidden by Excel (<c>\ / ? * [ ] :</c>),
    /// collapses whitespace, and truncates to 31 characters.
    /// </summary>
    public static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sheet";

        var sanitized = InvalidSheetNameCharsRegex().Replace(name, "");
        sanitized = WhitespaceRegex().Replace(sanitized, " ").Trim();

        if (sanitized.Length > 31)
        {
            sanitized = sanitized[..31].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "Sheet" : sanitized;
    }

    [GeneratedRegex(@"[<>:""/\\|?*]")]
    private static partial Regex InvalidFilenameCharsRegex();

    [GeneratedRegex(@"[\\/?*\[\]:]")]
    private static partial Regex InvalidSheetNameCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
