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
