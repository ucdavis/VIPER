namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Preview of harvest data before committing.
/// </summary>
public class HarvestPreviewDto
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;

    // Phase 1: CREST
    public List<HarvestPersonPreview> CrestInstructors { get; set; } = [];
    public List<HarvestCoursePreview> CrestCourses { get; set; } = [];
    public List<HarvestRecordPreview> CrestEffort { get; set; } = [];

    // Phase 2: Non-CREST
    public List<HarvestCoursePreview> NonCrestCourses { get; set; } = [];
    public List<HarvestPersonPreview> NonCrestInstructors { get; set; } = [];
    public List<HarvestRecordPreview> NonCrestEffort { get; set; } = [];

    // Phase 3: Clinical Scheduler
    public List<HarvestPersonPreview> ClinicalInstructors { get; set; } = [];
    public List<HarvestCoursePreview> ClinicalCourses { get; set; } = [];
    public List<HarvestRecordPreview> ClinicalEffort { get; set; } = [];

    // Phase 4: Guest Accounts
    public List<HarvestPersonPreview> GuestAccounts { get; set; } = [];

    // Items that exist but won't be re-imported (will be deleted)
    public List<HarvestPersonPreview> RemovedInstructors { get; set; } = [];
    public List<HarvestCoursePreview> RemovedCourses { get; set; } = [];

    // Summary
    public HarvestSummary Summary { get; set; } = new();

    // Problems
    public List<HarvestWarning> Warnings { get; set; } = [];
    public List<HarvestError> Errors { get; set; } = [];
}

/// <summary>
/// Preview of an instructor/person to be imported.
/// </summary>
public class HarvestPersonPreview
{
    /// <summary>
    /// MothraID or guest account ID (e.g., APCGUEST).
    /// </summary>
    public string MothraId { get; set; } = string.Empty;

    /// <summary>
    /// PersonId from users.Person table (0 for guest accounts).
    /// </summary>
    public int PersonId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Title code for database storage (max 6 chars).
    /// </summary>
    public string TitleCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title description for display.
    /// </summary>
    public string TitleDescription { get; set; } = string.Empty;

    /// <summary>
    /// Source of this data: CREST, NonCREST, Clinical, or Guest.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// True if this instructor does not exist in Effort_People for this term and will be created.
    /// False if the instructor already exists and will be updated/replaced.
    /// </summary>
    public bool IsNew { get; set; }
}

/// <summary>
/// Preview of a course to be imported.
/// </summary>
public class HarvestCoursePreview
{
    public string Crn { get; set; } = string.Empty;
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public string SeqNumb { get; set; } = string.Empty;
    public int Enrollment { get; set; }
    public decimal Units { get; set; }
    public string CustDept { get; set; } = string.Empty;

    /// <summary>
    /// Source of this data: CREST, NonCREST, or Clinical.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// True if this course does not exist in Effort_Courses for this term and will be created.
    /// False if the course already exists and will be updated/replaced.
    /// </summary>
    public bool IsNew { get; set; }
}

/// <summary>
/// Preview of an effort record to be imported.
/// </summary>
public class HarvestRecordPreview
{
    public string MothraId { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string EffortType { get; set; } = string.Empty;
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Source of this data: CREST or Clinical.
    /// </summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Summary counts for harvest preview.
/// </summary>
public class HarvestSummary
{
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEffortRecords { get; set; }
    public int GuestAccounts { get; set; }
}

/// <summary>
/// Warning message during harvest preview/execution.
/// </summary>
public class HarvestWarning
{
    public string Phase { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Error that prevents harvest from completing.
/// </summary>
public class HarvestError
{
    public string Phase { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
