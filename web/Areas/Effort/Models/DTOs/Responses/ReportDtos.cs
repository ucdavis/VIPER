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
// Year Statistics Report DTOs
// ============================================

/// <summary>
/// Year Statistics report ("Lairmore Report") with 4 sub-reports: SVM, DVM, Resident, Undergrad/Grad.
/// The SP returns raw course-level data; all grouping/filtering/aggregation happens in the service layer.
/// </summary>
public class YearStatisticsReport
{
    public string AcademicYear { get; set; } = string.Empty;
    public List<string> EffortTypes { get; set; } = [];
    public YearStatsSubReport Svm { get; set; } = new();
    public YearStatsSubReport Dvm { get; set; } = new();
    public YearStatsSubReport Resident { get; set; } = new();
    public YearStatsSubReport UndergradGrad { get; set; } = new();
}

/// <summary>
/// A single sub-report within the Year Statistics report.
/// Contains per-instructor details, summary statistics, and optional grouping tables.
/// </summary>
public class YearStatsSubReport
{
    public string Label { get; set; } = string.Empty;
    public List<InstructorEffortDetail> Instructors { get; set; } = [];
    public Dictionary<string, decimal> Sums { get; set; } = new();
    public Dictionary<string, decimal> Averages { get; set; } = new();
    public Dictionary<string, decimal> Medians { get; set; } = new();
    public decimal TeachingHoursSum { get; set; }
    public decimal TeachingHoursAverage { get; set; }
    public decimal TeachingHoursMedian { get; set; }
    public int InstructorCount { get; set; }
    /// <summary>SVM and DVM only: group by department.</summary>
    public List<YearStatsGrouping> ByDepartment { get; set; } = [];
    /// <summary>SVM and DVM only: group by discipline.</summary>
    public List<YearStatsGrouping> ByDiscipline { get; set; } = [];
    /// <summary>SVM and DVM only: group by title/job group.</summary>
    public List<YearStatsGrouping> ByTitle { get; set; } = [];
}

/// <summary>
/// Per-instructor effort detail row in the Year Statistics report.
/// </summary>
public class InstructorEffortDetail
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string JobGroup { get; set; } = string.Empty;
    public Dictionary<string, decimal> Efforts { get; set; } = new();
    public decimal TeachingHours { get; set; }
}

/// <summary>
/// Grouped statistics row in the Year Statistics report (by department, discipline, or title).
/// </summary>
public class YearStatsGrouping
{
    public string GroupName { get; set; } = string.Empty;
    public int InstructorCount { get; set; }
    public Dictionary<string, decimal> Sums { get; set; } = new();
    public Dictionary<string, decimal> Averages { get; set; } = new();
    public Dictionary<string, decimal> Medians { get; set; } = new();
    public decimal TeachingHoursSum { get; set; }
    public decimal TeachingHoursAverage { get; set; }
    public decimal TeachingHoursMedian { get; set; }
}

/// <summary>
/// Request body for year statistics PDF export.
/// </summary>
public record YearStatsPdfRequest(string? AcademicYear = null);

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

// ============================================
// Evaluation Report DTOs
// ============================================

/// <summary>
/// Evaluation summary report grouped by department.
/// Shows weighted average per instructor across all their evaluated courses.
/// </summary>
public class EvalSummaryReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public int? FilterPersonId { get; set; }
    public string? FilterRole { get; set; }
    public List<EvalDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping in evaluation summary.
/// </summary>
public class EvalDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<EvalInstructorSummary> Instructors { get; set; } = [];
    public decimal DepartmentAverage { get; set; }
    public int TotalResponses { get; set; }
}

/// <summary>
/// Instructor-level summary in evaluation report with weighted average.
/// </summary>
public class EvalInstructorSummary
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public decimal WeightedAverage { get; set; }
    public int TotalResponses { get; set; }
    public int TotalEnrolled { get; set; }
}

/// <summary>
/// Evaluation detail report grouped by department and instructor.
/// Shows course-level evaluation data with averages and medians.
/// </summary>
public class EvalDetailReport
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? FilterDepartment { get; set; }
    public int? FilterPersonId { get; set; }
    public string? FilterRole { get; set; }
    public List<EvalDetailDepartmentGroup> Departments { get; set; } = [];
}

/// <summary>
/// Department-level grouping in evaluation detail report.
/// </summary>
public class EvalDetailDepartmentGroup
{
    public string Department { get; set; } = string.Empty;
    public List<EvalDetailInstructor> Instructors { get; set; } = [];
    public decimal DepartmentAverage { get; set; }
}

/// <summary>
/// Instructor-level grouping in evaluation detail with course breakdown.
/// </summary>
public class EvalDetailInstructor
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public List<EvalCourseDetail> Courses { get; set; } = [];
    public decimal InstructorAverage { get; set; }
    public decimal? InstructorMedian { get; set; }
}

/// <summary>
/// Course-level row in evaluation detail report.
/// </summary>
public class EvalCourseDetail
{
    public string Course { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal Average { get; set; }
    public decimal? Median { get; set; }
    public int NumResponses { get; set; }
    public int NumEnrolled { get; set; }
}

// ============================================
// Multi-Year Merit + Evaluation Report DTOs
// ============================================

/// <summary>
/// Combined multi-year report for a single instructor: merit activity + evaluation data.
/// </summary>
public class MultiYearReport
{
    public string MothraId { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public bool UseAcademicYear { get; set; }
    public List<string> ExcludedClinicalTerms { get; set; } = [];
    public List<string> ExcludedDidacticTerms { get; set; } = [];
    public List<string> EffortTypes { get; set; } = [];
    public MultiYearMeritSection MeritSection { get; set; } = new();
    public MultiYearEvalSection EvalSection { get; set; } = new();
}

/// <summary>
/// Merit activity section of the multi-year report grouped by year.
/// </summary>
public class MultiYearMeritSection
{
    public List<MultiYearMeritYear> Years { get; set; } = [];
    public Dictionary<string, decimal> GrandTotals { get; set; } = new();
    public Dictionary<string, decimal> YearlyAverages { get; set; } = new();
    public Dictionary<string, decimal> DepartmentAverages { get; set; } = new();
    /// <summary>Sum of faculty count across years used for department averages.</summary>
    public int DepartmentFacultyCount { get; set; }
}

/// <summary>
/// Single year of merit data with course rows and year totals.
/// </summary>
public class MultiYearMeritYear
{
    public int Year { get; set; }
    public string YearLabel { get; set; } = string.Empty;
    public List<MultiYearCourseRow> Courses { get; set; } = [];
    public Dictionary<string, decimal> YearTotals { get; set; } = new();
}

/// <summary>
/// Course-level row in multi-year merit report with effort by type.
/// </summary>
public class MultiYearCourseRow
{
    public string Course { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public decimal Units { get; set; }
    public int Enrollment { get; set; }
    public string Role { get; set; } = string.Empty;
    public Dictionary<string, decimal> Efforts { get; set; } = new();
}

/// <summary>
/// Evaluation section of the multi-year report grouped by year.
/// </summary>
public class MultiYearEvalSection
{
    public List<MultiYearEvalYear> Years { get; set; } = [];
    public decimal OverallAverage { get; set; }
    public decimal? OverallMedian { get; set; }
    public decimal? DepartmentAverage { get; set; }
}

/// <summary>
/// Single year of evaluation data with course rows and year averages.
/// </summary>
public class MultiYearEvalYear
{
    public int Year { get; set; }
    public string YearLabel { get; set; } = string.Empty;
    public List<MultiYearEvalCourse> Courses { get; set; } = [];
    public decimal YearAverage { get; set; }
    public decimal? YearMedian { get; set; }
}

/// <summary>
/// Course-level row in multi-year evaluation report.
/// </summary>
public class MultiYearEvalCourse
{
    public string Course { get; set; } = string.Empty;
    public string Crn { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public string Role { get; set; } = string.Empty;
    public decimal Average { get; set; }
    public decimal? Median { get; set; }
    public int NumResponses { get; set; }
    public int NumEnrolled { get; set; }
}

/// <summary>
/// Request body for multi-year report PDF export.
/// </summary>
public record MultiYearPdfRequest(
    int PersonId = 0,
    int StartYear = 0,
    int EndYear = 0,
    string? ExcludeClinicalTerms = null,
    string? ExcludeDidacticTerms = null,
    bool UseAcademicYear = false);

/// <summary>
/// Sabbatical/leave exclusion data for a person.
/// </summary>
public class SabbaticalDto
{
    public int PersonId { get; set; }
    public string? ExcludeClinicalTerms { get; set; }
    public string? ExcludeDidacticTerms { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Request body for saving sabbatical/leave exclusion data.
/// </summary>
public record SaveSabbaticalRequest(
    string? ExcludeClinicalTerms = null,
    string? ExcludeDidacticTerms = null);

/// <summary>
/// Min/max calendar years for an instructor's effort data, used for year range dropdowns.
/// </summary>
public class InstructorYearRangeDto
{
    public int MinYear { get; set; }
    public int MaxYear { get; set; }
}
