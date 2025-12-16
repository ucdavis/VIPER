namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Represents an audit trail entry for display.
/// </summary>
public class EffortAuditRow
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }

    /// <summary>
    /// PersonId of the user who made the change.
    /// </summary>
    public int ChangedBy { get; set; }

    /// <summary>
    /// Display name of the user who made the change.
    /// </summary>
    public string ChangedByName { get; set; } = string.Empty;

    /// <summary>
    /// PersonId of the instructor affected (for person/record/percentage changes).
    /// </summary>
    public int? InstructorPersonId { get; set; }

    /// <summary>
    /// Display name of the instructor affected.
    /// </summary>
    public string? InstructorName { get; set; }

    /// <summary>
    /// Term code associated with the change.
    /// </summary>
    public int? TermCode { get; set; }

    /// <summary>
    /// Human-readable term name (e.g., "Fall Quarter 2024").
    /// </summary>
    public string? TermName { get; set; }

    /// <summary>
    /// Course code for record changes (e.g., "VMD 001-01").
    /// </summary>
    public string? CourseCode { get; set; }

    /// <summary>
    /// Course Reference Number.
    /// </summary>
    public string? Crn { get; set; }

    /// <summary>
    /// Raw JSON string of changes (for backward compatibility).
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Parsed change details as key-value pairs showing old/new values.
    /// </summary>
    public Dictionary<string, ChangeDetail>? ChangesDetail { get; set; }
}

/// <summary>
/// Represents a single field change with before/after values.
/// </summary>
public class ChangeDetail
{
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

/// <summary>
/// Information about a user who has made audit entries.
/// Used for the modifier filter dropdown.
/// </summary>
public class ModifierInfo
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
