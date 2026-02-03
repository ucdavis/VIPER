using System.Collections.Frozen;

namespace Viper.Areas.Effort.Constants;

/// <summary>
/// Shared constants for the Effort system.
/// </summary>
public static class EffortConstants
{
    /// <summary>
    /// Academic departments that can have effort records.
    /// </summary>
    public static readonly string[] AcademicDepartments = ["APC", "PHR", "PMI", "VMB", "VME", "VSR"];

    /// <summary>
    /// Guest account MothraIDs for department-level guest accounts.
    /// PersonIds are looked up from users.Person at runtime.
    /// </summary>
    public static readonly string[] GuestAccountIds = ["APCGUEST", "PHRGUEST", "PMIGUEST", "VMBGUEST", "VMEGUEST", "VSRGUEST"];

    /// <summary>
    /// Department overrides by MothraID. These instructors are assigned to a specific
    /// department regardless of their job data in AAUD.
    /// Key: MothraID, Value: Department code
    /// </summary>
    public static readonly FrozenDictionary<string, string> DepartmentOverrides = new Dictionary<string, string>
    {
        ["02493928"] = "VSR"
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets the department override for a given MothraID, or null if no override exists.
    /// </summary>
    public static string? GetDepartmentOverride(string? mothraId)
    {
        if (string.IsNullOrEmpty(mothraId))
        {
            return null;
        }
        return DepartmentOverrides.TryGetValue(mothraId, out var dept) ? dept : null;
    }

    #region Term-Gated Business Rules

    /// <summary>
    /// Term code from which CLI effort types use weeks instead of hours.
    /// </summary>
    public const int ClinicalAsWeeksStartTermCode = 201604;

    #endregion

    #region Harvest Constants

    /// <summary>
    /// Role ID for Director (Instructor of Record).
    /// </summary>
    public const int DirectorRoleId = 1;

    /// <summary>
    /// Role ID for Instructor (non-primary instructor role).
    /// </summary>
    public const int InstructorRoleId = 2;

    /// <summary>
    /// First name used for guest instructor placeholders.
    /// Guest instructors are department placeholders for unattributed teaching.
    /// </summary>
    public const string GuestInstructorFirstName = "GUEST";

    /// <summary>
    /// Role ID for Clinical Instructor (legacy alias for InstructorRoleId).
    /// </summary>
    public const int ClinicalInstructorRoleId = InstructorRoleId;

    /// <summary>
    /// Effort type code for Clinical rotations.
    /// </summary>
    public const string ClinicalEffortType = "CLI";

    /// <summary>
    /// Effort type code for Dissertation/Research courses.
    /// </summary>
    public const string ResearchEffortType = "DIS";

    /// <summary>
    /// Effort type code for Variable/Other courses.
    /// </summary>
    public const string VariableEffortType = "VAR";

    /// <summary>
    /// CREST role code that identifies the Director (IOR) for a course.
    /// </summary>
    public const string CrestDirectorRoleCode = "Dir";

    /// <summary>
    /// Default custodial department for clinical courses.
    /// </summary>
    public const string ClinicalCustodialDept = "DVM";

    /// <summary>
    /// Default units for clinical courses.
    /// </summary>
    public const int DefaultClinicalUnits = 15;

    /// <summary>
    /// Default enrollment for clinical courses.
    /// </summary>
    public const int DefaultClinicalEnrollment = 149;

    #endregion

    #region Harvest Source and Status Strings

    /// <summary>
    /// Status value indicating a term has been harvested.
    /// </summary>
    public const string TermStatusHarvested = "Harvested";

    /// <summary>
    /// Source identifier for CREST-imported data.
    /// </summary>
    public const string SourceCrest = "CREST";

    /// <summary>
    /// Source identifier for non-CREST imported data.
    /// </summary>
    public const string SourceNonCrest = "NonCREST";

    /// <summary>
    /// Source identifier for courses that exist in CREST (shown for transparency, not imported).
    /// </summary>
    public const string SourceInCrest = "In CREST";

    /// <summary>
    /// Source identifier for clinical scheduler data.
    /// </summary>
    public const string SourceClinical = "Clinical";

    /// <summary>
    /// Source identifier for guest accounts.
    /// </summary>
    public const string SourceGuest = "Guest";

    /// <summary>
    /// Source identifier for existing data (shown in removed items list).
    /// </summary>
    public const string SourceExisting = "Existing";

    /// <summary>
    /// CREST session type for debrief sessions (excluded from effort calculation).
    /// </summary>
    public const string SessionTypeDebrief = "DEBRIEF";

    /// <summary>
    /// Phase name for clinical import.
    /// </summary>
    public const string PhaseClinical = "Clinical";

    #endregion
}
