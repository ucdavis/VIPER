namespace Viper.Areas.Effort.Helpers;

/// <summary>
/// Centralized term eligibility checks using term metadata rather than brittle string parsing.
/// </summary>
public static class TermValidationHelper
{
    /// <summary>
    /// Check if term is a Fall term (either semester or quarter).
    /// Supports multiple formats:
    /// - Full strings from TermCodeService: "Fall Semester", "Fall Quarter"
    /// - Single-character codes from database: values may vary
    /// - Null/empty values return false
    /// </summary>
    public static bool IsFallTerm(string? termType) => termType is "Fall Semester" or "Fall Quarter";

    /// <summary>
    /// Check if term is a Fall term using the term code.
    /// Fall term codes end in: 09 (Fall Semester) or 10 (Fall Quarter)
    /// This is more reliable than string matching when database TermType values are inconsistent.
    /// </summary>
    public static bool IsFallTermByCode(int termCode) => (termCode % 100) is 9 or 10;

    /// <summary>
    /// Check if term can be harvested (Created or Harvested status).
    /// </summary>
    public static bool CanHarvest(string? status) => status is "Created" or "Harvested";

    /// <summary>
    /// Check if percent assignments can be rolled over for this term (Fall terms only).
    /// Uses TermType string matching - prefer CanRolloverPercentByCode when term code is available.
    /// </summary>
    public static bool CanRolloverPercent(string? termType) => IsFallTerm(termType);

    /// <summary>
    /// Check if percent assignments can be rolled over for this term.
    /// Requires: Fall term AND status is Created, Harvested, or Opened.
    /// </summary>
    public static bool CanRolloverPercent(string? status, int termCode) =>
        (status is "Created" or "Harvested" or "Opened") && IsFallTermByCode(termCode);

    /// <summary>
    /// Check if percent assignments can be rolled over for this term (Fall terms only, ignores status).
    /// Use CanRolloverPercent(status, termCode) when status is available.
    /// </summary>
    public static bool CanRolloverPercentByCode(int termCode) => IsFallTermByCode(termCode);

    /// <summary>
    /// Check if term is a semester term (not quarter).
    /// Semester term codes end in: 02 (Spring), 04 (Summer), 09 (Fall)
    /// Quarter term codes end in: 01, 03, 10 - clinical data not available
    /// </summary>
    public static bool IsSemesterTerm(int termCode) => (termCode % 100) is 2 or 4 or 9;

    /// <summary>
    /// Check if clinical data can be imported for this term.
    /// Requires: semester term (not quarter) AND status is Created, Harvested, or Opened.
    /// </summary>
    public static bool CanImportClinical(string? status, int termCode) =>
        (status is "Created" or "Harvested" or "Opened") && IsSemesterTerm(termCode);
}
