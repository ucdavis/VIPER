namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Dashboard statistics for the Effort system.
/// Provides bird's eye view of term progress for staff/admins.
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Current term information.
    /// </summary>
    public TermDto? CurrentTerm { get; set; }

    /// <summary>
    /// Total instructors in the term.
    /// </summary>
    public int TotalInstructors { get; set; }

    /// <summary>
    /// Number of instructors who have verified their effort.
    /// </summary>
    public int VerifiedInstructors { get; set; }

    /// <summary>
    /// Number of instructors pending verification.
    /// </summary>
    public int PendingInstructors => TotalInstructors - VerifiedInstructors;

    /// <summary>
    /// Verification percentage (0-100).
    /// </summary>
    public int VerificationPercent => TotalInstructors > 0
        ? (int)Math.Round((double)VerifiedInstructors / TotalInstructors * 100)
        : 0;

    /// <summary>
    /// Total courses in the term.
    /// </summary>
    public int TotalCourses { get; set; }

    /// <summary>
    /// Courses with at least one instructor assigned.
    /// </summary>
    public int CoursesWithInstructors { get; set; }

    /// <summary>
    /// Courses with no instructors assigned.
    /// </summary>
    public int CoursesWithoutInstructors => TotalCourses - CoursesWithInstructors;

    /// <summary>
    /// Total effort records in the term.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Number of instructors with no effort records.
    /// </summary>
    public int InstructorsWithNoRecords { get; set; }

    /// <summary>
    /// Number of instructors whose effort doesn't total 100%.
    /// </summary>
    public int InstructorsWithEffortMismatch { get; set; }

    /// <summary>
    /// Data hygiene summary for dashboard card.
    /// </summary>
    public DataHygieneSummaryDto HygieneSummary { get; set; } = new();
}

/// <summary>
/// Summary of data hygiene alerts for the dashboard card.
/// </summary>
public class DataHygieneSummaryDto
{
    /// <summary>
    /// Number of active alerts needing review.
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// Number of alerts auto-resolved (issue no longer exists).
    /// </summary>
    public int ResolvedAlerts { get; set; }

    /// <summary>
    /// Number of alerts manually ignored by users.
    /// </summary>
    public int IgnoredAlerts { get; set; }
}
