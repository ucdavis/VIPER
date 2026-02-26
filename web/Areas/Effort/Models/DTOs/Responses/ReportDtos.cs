namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Raw row from effort.sp_effort_general_report stored procedure.
/// </summary>
public class TeachingActivityRow
{
    public int TermCode { get; set; }
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string Course { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public int Enrollment { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string EffortTypeId { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Weeks { get; set; }

    /// <summary>
    /// Resolved effort value matching legacy ISNULL(effort_weeks, effort_hours).
    /// Clinical records store effort in Weeks; other records use Hours.
    /// </summary>
    public decimal EffortValue => Weeks > 0 ? Weeks : Hours;
}

/// <summary>
/// Processed teaching activity report grouped by department and instructor.
/// </summary>
public class TeachingActivityReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public string? FilterPerson { get; set; }
    public string? FilterRole { get; set; }
    public string? FilterTitle { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<TeachingActivityDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping with instructor list and department totals.
/// </summary>
public class TeachingActivityDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<TeachingActivityInstructorGroup> Instructors { get; set; } = [];
    public Dictionary<string, decimal> DepartmentTotals { get; set; } = new();
}

/// <summary>
/// Instructor-level grouping with course list and instructor totals.
/// </summary>
public class TeachingActivityInstructorGroup
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public List<TeachingActivityCourseRow> Courses { get; set; } = [];
    public Dictionary<string, decimal> InstructorTotals { get; set; } = new();
}

/// <summary>
/// Course-level row with effort hours pivoted by effort type.
/// </summary>
public class TeachingActivityCourseRow
{
    public int TermCode { get; set; }
    public int CourseId { get; set; }
    public string Course { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public int Enrollment { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
}

/// <summary>
/// Request body for PDF report generation.
/// </summary>
public record ReportPdfRequest(
    int TermCode = 0,
    string? AcademicYear = null,
    string? Department = null,
    int? PersonId = null,
    string? Role = null,
    string? Title = null);

/// <summary>
/// Request body for clinical effort PDF export.
/// </summary>
public record ClinicalEffortPdfRequest(
    string? AcademicYear = null,
    int ClinicalType = 0);

// ============================================
// Department Summary Report DTOs
// ============================================

/// <summary>
/// Department summary report showing per-instructor effort totals with department averages.
/// </summary>
public class DeptSummaryReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public string? FilterPerson { get; set; }
    public string? FilterRole { get; set; }
    public string? FilterTitle { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<DeptSummaryDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping with instructor rows, totals, and averages.
/// </summary>
public class DeptSummaryDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<DeptSummaryInstructorRow> Instructors { get; set; } = [];
    public Dictionary<string, decimal> DepartmentTotals { get; set; } = new();
    public int FacultyCount { get; set; }
    public int FacultyWithCliCount { get; set; }
    public Dictionary<string, decimal> DepartmentAverages { get; set; } = new();
}

/// <summary>
/// Single instructor row in department summary with effort totals by type.
/// </summary>
public class DeptSummaryInstructorRow
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
}

// ============================================
// School Summary Report DTOs
// ============================================

/// <summary>
/// School-wide summary report aggregating all departments.
/// </summary>
public class SchoolSummaryReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public string? FilterPerson { get; set; }
    public string? FilterRole { get; set; }
    public string? FilterTitle { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<SchoolSummaryDepartmentRow> Departments { get; set; } = [];
    public SchoolSummaryTotalsRow GrandTotals { get; set; } = new();
}

/// <summary>
/// Department-level row in school summary with effort totals and averages.
/// </summary>
public class SchoolSummaryDepartmentRow
{
    public string Department { get; set; } = string.Empty;
    public Dictionary<string, decimal> EffortTotals { get; set; } = new();
    public int FacultyCount { get; set; }
    public int FacultyWithCliCount { get; set; }
    public Dictionary<string, decimal> Averages { get; set; } = new();
}

/// <summary>
/// Grand totals row for school summary report.
/// </summary>
public class SchoolSummaryTotalsRow
{
    public Dictionary<string, decimal> EffortTotals { get; set; } = new();
    public int FacultyCount { get; set; }
    public int FacultyWithCliCount { get; set; }
    public Dictionary<string, decimal> Averages { get; set; } = new();
}

// ============================================
// Merit & Promotion Detail Report DTOs
// ============================================

/// <summary>
/// Merit detail report showing course-level effort data per instructor.
/// </summary>
public class MeritDetailReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public int? FilterPersonId { get; set; }
    public string? FilterRole { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<MeritDetailDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping in merit detail report.
/// </summary>
public class MeritDetailDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<MeritDetailInstructorGroup> Instructors { get; set; } = [];
    public Dictionary<string, decimal> DepartmentTotals { get; set; } = new();
}

/// <summary>
/// Instructor-level grouping in merit detail with course breakdown.
/// </summary>
public class MeritDetailInstructorGroup
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public string? JobGroupDescription { get; set; }
    public List<MeritDetailCourseRow> Courses { get; set; } = [];
    public Dictionary<string, decimal> InstructorTotals { get; set; } = new();
}

/// <summary>
/// Course-level row in merit detail report.
/// </summary>
public class MeritDetailCourseRow
{
    public int TermCode { get; set; }
    public int CourseId { get; set; }
    public string Course { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public int Enrollment { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
}

// ============================================
// Merit & Promotion Average Report DTOs
// ============================================

/// <summary>
/// Merit average report grouped by job group, then department, then instructor.
/// </summary>
public class MeritAverageReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public int? FilterPersonId { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<MeritAverageJobGroup> JobGroups { get; set; } = [];
}

/// <summary>
/// Job group level grouping in merit average report.
/// </summary>
public class MeritAverageJobGroup
{
    public string JobGroupDescription { get; set; } = string.Empty;
    public List<MeritAverageDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping within a job group in merit average report.
/// </summary>
public class MeritAverageDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<MeritAverageInstructorRow> Instructors { get; set; } = [];
    public Dictionary<string, decimal> GroupTotals { get; set; } = new();
    public Dictionary<string, decimal> GroupAverages { get; set; } = new();
    public int FacultyCount { get; set; }
    public int FacultyWithCliCount { get; set; }
}

/// <summary>
/// Instructor-level row in merit average report.
/// </summary>
public class MeritAverageInstructorRow
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public string? JobGroupDescription { get; set; }
    public decimal PercentAdmin { get; set; }
    public List<MeritAverageTermRow> Terms { get; set; } = [];
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
}

/// <summary>
/// Per-term effort row within an instructor in the merit average report.
/// </summary>
public class MeritAverageTermRow
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
}

// ============================================
// Merit & Promotion Summary Report DTOs
// ============================================

/// <summary>
/// Merit summary report grouped by job group description, then department.
/// Shows department totals and averages (CLI averaged over CLI-assigned faculty only).
/// </summary>
public class MeritSummaryReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public List<string> EffortTypes { get; set; } = [];
    public List<MeritSummaryJobGroup> JobGroups { get; set; } = [];
}

/// <summary>
/// Job group level grouping in merit summary report.
/// </summary>
public class MeritSummaryJobGroup
{
    public string JobGroupDescription { get; set; } = string.Empty;
    public List<MeritSummaryDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping in merit summary with totals and averages.
/// </summary>
public class MeritSummaryDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public Dictionary<string, decimal> DepartmentTotals { get; set; } = new();
    public Dictionary<string, decimal> DepartmentAverages { get; set; } = new();
    public int FacultyCount { get; set; }
    public int FacultyWithCliCount { get; set; }
}

// ============================================
// Clinical Effort Report DTOs
// ============================================

/// <summary>
/// Clinical effort report showing instructors with clinical percent assignments
/// and their effort data. Filtered by clinical type (VMTH=1, CAHFS=25).
/// </summary>
public class ClinicalEffortReport
{
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public int ClinicalType { get; set; }
    public string ClinicalTypeName { get; set; } = string.Empty;
    public List<string> EffortTypes { get; set; } = [];
    public List<ClinicalEffortJobGroup> JobGroups { get; set; } = [];
}

/// <summary>
/// Job group level grouping in clinical effort report.
/// </summary>
public class ClinicalEffortJobGroup
{
    public string JobGroupDescription { get; set; } = string.Empty;
    public List<ClinicalEffortInstructorRow> Instructors { get; set; } = [];
}

/// <summary>
/// Instructor row in clinical effort report with percent assignment and CLI ratio.
/// </summary>
public class ClinicalEffortInstructorRow
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal ClinicalPercent { get; set; }
    public Dictionary<string, decimal> EffortByType { get; set; } = new();
    /// <summary>
    /// CLI effort / ClinicalPercent ratio. Null if ClinicalPercent is 0.
    /// </summary>
    public decimal? CliRatio { get; set; }
}

// ============================================
// Scheduled CLI Weeks Report DTOs
// ============================================

/// <summary>
/// Scheduled clinical weeks report from Clinical Scheduler database.
/// Shows weeks scheduled per instructor, term, and service.
/// </summary>
public class ScheduledCliWeeksReport
{
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public List<string> TermNames { get; set; } = [];
    public List<string> Services { get; set; } = [];
    public List<ScheduledCliWeeksInstructorRow> Instructors { get; set; } = [];
}

/// <summary>
/// Instructor row in scheduled CLI weeks report.
/// </summary>
public class ScheduledCliWeeksInstructorRow
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public List<ScheduledCliWeeksTermRow> Terms { get; set; } = [];
    public int TotalWeeks { get; set; }
}

/// <summary>
/// Per-term breakdown of scheduled weeks by service.
/// </summary>
public class ScheduledCliWeeksTermRow
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public Dictionary<string, int> WeeksByService { get; set; } = new();
    public int TermTotal { get; set; }
}

// ============================================
// Zero Effort Report DTOs
// ============================================

/// <summary>
/// Report of instructors with courses assigned but zero effort recorded.
/// </summary>
public class ZeroEffortReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public List<ZeroEffortInstructorRow> Instructors { get; set; } = [];
}

/// <summary>
/// Instructor row in zero effort report.
/// </summary>
public class ZeroEffortInstructorRow
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobGroupId { get; set; } = string.Empty;
    public bool Verified { get; set; }
}
