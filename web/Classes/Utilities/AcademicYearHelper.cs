namespace Viper.Classes.Utilities;

/// <summary>
/// Represents a date range for an academic year.
/// Academic year runs July 1 (year-1) to June 30 (year).
/// Example: AcademicYear 2026 = July 1, 2025 to June 30, 2026.
/// </summary>
public readonly record struct AcademicYearDateRange(DateTime StartDate, DateTime EndDateExclusive)
{
    /// <summary>June 30 for display purposes. For queries, use EndDateExclusive with &lt; comparison.</summary>
    public DateTime EndDateInclusive => EndDateExclusive.AddDays(-1);
}

/// <summary>
/// Provides academic year calculations from dates and term codes.
/// </summary>
public static class AcademicYearHelper
{
    // ── Term-code-based methods ──────────────────────────────────────

    /// <summary>
    /// Converts a 6-digit term code to academic year string (YYYY-YYYY).
    /// Winter (01), Spring Semester (02), Spring Quarter (03) belong to the AY that started the previous calendar year.
    /// Summer (04-08) and Fall (09-10) belong to the AY starting in the current calendar year.
    /// E.g., 202501 → "2024-2025", 202410 → "2024-2025", 202504 → "2025-2026".
    /// </summary>
    public static string GetAcademicYearFromTermCode(int termCode)
    {
        int year = termCode / 100;
        int term = termCode % 100;
        int startYear = (term >= 1 && term <= 3) ? year - 1 : year;
        return $"{startYear}-{startYear + 1}";
    }

    /// <summary>
    /// Filters a collection of term codes to those belonging to the given academic year,
    /// ordered descending (newest first).
    /// </summary>
    public static List<int> GetTermCodesForAcademicYear(IEnumerable<int> allTermCodes, int startYear)
    {
        var expectedYear = $"{startYear}-{startYear + 1}";
        return allTermCodes
            .Where(tc => GetAcademicYearFromTermCode(tc) == expectedYear)
            .OrderByDescending(tc => tc)
            .ToList();
    }

    // ── Date-based methods ───────────────────────────────────────────

    /// <summary>
    /// Gets the start date (July 1) of the academic year containing the given date.
    /// </summary>
    public static DateTime GetAcademicYearStart(DateTime date)
    {
        var year = date.Month < 7 ? date.Year - 1 : date.Year;
        return new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Local);
    }

    /// <summary>
    /// Gets the date range for an academic year integer (e.g., 2026 = July 1, 2025 to June 30, 2026).
    /// Returns exclusive end date (July 1) for correct date comparisons.
    /// </summary>
    public static AcademicYearDateRange GetDateRange(int academicYear)
    {
        var startDate = new DateTime(academicYear - 1, 7, 1, 0, 0, 0, DateTimeKind.Local);
        var endDateExclusive = new DateTime(academicYear, 7, 1, 0, 0, 0, DateTimeKind.Local);
        return new AcademicYearDateRange(startDate, endDateExclusive);
    }

    /// <summary>
    /// Converts a date to its academic year integer.
    /// July+ belongs to the next academic year.
    /// </summary>
    public static int GetAcademicYear(DateTime date)
    {
        return date.Month >= 7 ? date.Year + 1 : date.Year;
    }

    /// <summary>
    /// Converts a date to academic year string (e.g., "2025-2026").
    /// </summary>
    public static string GetAcademicYearString(DateTime date)
    {
        var year = GetAcademicYear(date);
        return $"{year - 1}-{year}";
    }
}
