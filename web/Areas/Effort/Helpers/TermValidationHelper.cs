namespace Viper.Areas.Effort.Helpers;

/// <summary>
/// Centralized term eligibility checks using term metadata rather than brittle string parsing.
/// </summary>
public static class TermValidationHelper
{
    /// <summary>
    /// Check if term can be harvested (Created or Harvested status).
    /// </summary>
    public static bool CanHarvest(string? status) => status is "Created" or "Harvested";

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
