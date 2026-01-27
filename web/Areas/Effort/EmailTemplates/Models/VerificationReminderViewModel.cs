using Viper.EmailTemplates.Models;

namespace Viper.Areas.Effort.EmailTemplates.Models;

/// <summary>
/// View model for the verification reminder email template.
/// </summary>
public class VerificationReminderViewModel : EmailViewModelBase
{
    /// <summary>
    /// Human-readable term name (e.g., "Fall 2024").
    /// </summary>
    public string TermDescription { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the term/semester.
    /// </summary>
    public DateTime? TermStartDate { get; set; }

    /// <summary>
    /// End date of the term/semester.
    /// </summary>
    public DateTime? TermEndDate { get; set; }

    /// <summary>
    /// Date by which the instructor should reply.
    /// </summary>
    public string ReplyByDate { get; set; } = string.Empty;

    /// <summary>
    /// Whether the instructor has no effort records for this term.
    /// </summary>
    public bool HasNoRecords { get; set; }

    /// <summary>
    /// URL to verify effort in the system.
    /// </summary>
    public string VerificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether any effort records have zero effort.
    /// </summary>
    public bool HasZeroEffort { get; set; }

    /// <summary>
    /// Course groups with their effort records.
    /// </summary>
    public List<EffortCourseGroup> Courses { get; set; } = new();

    /// <summary>
    /// Cross-listed and sectioned child courses.
    /// </summary>
    public List<ChildCourseDisplay> ChildCourses { get; set; } = new();
}

/// <summary>
/// A course with its effort records grouped together.
/// </summary>
public class EffortCourseGroup
{
    /// <summary>
    /// Course code (e.g., "VET 101-001").
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of units for the course.
    /// </summary>
    public decimal Units { get; set; }

    /// <summary>
    /// Student enrollment count.
    /// </summary>
    public int Enrollment { get; set; }

    /// <summary>
    /// Instructor's role in the course.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Individual effort line items for this course.
    /// </summary>
    public List<EffortLineItem> EffortItems { get; set; } = new();
}

/// <summary>
/// An individual effort entry (e.g., "LEC = 10 Hours").
/// </summary>
public class EffortLineItem
{
    /// <summary>
    /// Effort type code (e.g., "LEC", "CLI").
    /// </summary>
    public string EffortType { get; set; } = string.Empty;

    /// <summary>
    /// Effort value (hours or weeks).
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Unit label ("Hours", "Hour", "Weeks", "Week").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Whether this record has zero effort.
    /// </summary>
    public bool IsZero => Value == 0;
}

/// <summary>
/// A child course for cross-listing/sectioning display.
/// </summary>
public class ChildCourseDisplay
{
    /// <summary>
    /// Course code (e.g., "VET 102-001").
    /// </summary>
    public string CourseCode { get; set; } = string.Empty;

    /// <summary>
    /// Relationship type ("CrossList" or "Section").
    /// </summary>
    public string RelationshipType { get; set; } = string.Empty;
}
