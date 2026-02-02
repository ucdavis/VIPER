namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for the My Effort self-service verification page.
/// Contains instructor's effort data and verification status.
/// </summary>
public class MyEffortDto
{
    /// <summary>
    /// The instructor's information.
    /// </summary>
    public PersonDto Instructor { get; set; } = null!;

    /// <summary>
    /// All effort records for this instructor in the term.
    /// </summary>
    public List<InstructorEffortRecordDto> EffortRecords { get; set; } = new();

    /// <summary>
    /// Child courses (cross-listed/sectioned) that don't have separate effort records.
    /// Aggregated from all parent courses.
    /// </summary>
    public List<ChildCourseDto> CrossListedCourses { get; set; } = new();

    /// <summary>
    /// Whether any effort records have zero hours/weeks.
    /// </summary>
    public bool HasZeroEffort { get; set; }

    /// <summary>
    /// IDs of effort records with zero hours/weeks, for frontend highlighting.
    /// </summary>
    public List<int> ZeroEffortRecordIds { get; set; } = new();

    /// <summary>
    /// Whether the instructor can verify (no zero-effort records and has VerifyEffort permission).
    /// </summary>
    public bool CanVerify { get; set; }

    /// <summary>
    /// Whether the instructor can edit their effort (term is open and has EditEffort permission).
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Whether the user has the VerifyEffort permission.
    /// Used to show/hide verification UI elements.
    /// </summary>
    public bool HasVerifyPermission { get; set; }

    /// <summary>
    /// Human-readable term name (e.g., "Fall Quarter 2024").
    /// </summary>
    public string TermName { get; set; } = string.Empty;

    /// <summary>
    /// Last modification date of any effort record.
    /// </summary>
    public DateTime? LastModifiedDate { get; set; }

    /// <summary>
    /// Term code after which clinical effort is tracked in weeks.
    /// </summary>
    public int ClinicalAsWeeksStartTermCode { get; set; }
}

/// <summary>
/// Error codes for verification failures.
/// Allows the frontend to display targeted error messages.
/// </summary>
public static class VerificationErrorCodes
{
    public const string ZeroEffort = "ZERO_EFFORT";
    public const string AlreadyVerified = "ALREADY_VERIFIED";
    public const string TermNotFound = "TERM_NOT_FOUND";
    public const string PersonNotFound = "PERSON_NOT_FOUND";
    public const string NoEffortRecords = "NO_EFFORT_RECORDS";
}

/// <summary>
/// Result of a verification attempt.
/// </summary>
public class VerificationResult
{
    public bool Success { get; set; }

    /// <summary>
    /// Error code from VerificationErrorCodes.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when effort was verified (on success).
    /// </summary>
    public DateTime? VerifiedDate { get; set; }

    /// <summary>
    /// List of courses with zero effort (when ErrorCode = ZERO_EFFORT).
    /// </summary>
    public List<string>? ZeroEffortCourses { get; set; }
}

/// <summary>
/// Result of checking whether an instructor can verify.
/// </summary>
public class CanVerifyResult
{
    public bool CanVerify { get; set; }
    public int ZeroEffortCount { get; set; }
    public List<string> ZeroEffortCourses { get; set; } = new();
    public List<int> ZeroEffortRecordIds { get; set; } = new();
}

/// <summary>
/// History of verification emails sent to an instructor.
/// </summary>
public class EmailHistoryDto
{
    public DateTime SentDate { get; set; }
    public string SentBy { get; set; } = string.Empty;
    public string SentByName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
}

/// <summary>
/// Result of sending a single verification email.
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }

    /// <summary>
    /// Error message if sending failed (e.g., "No email address found", "SMTP error").
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Result of sending bulk verification emails.
/// </summary>
public class BulkEmailResult
{
    public int TotalInstructors { get; set; }
    public int EmailsSent { get; set; }
    public int EmailsFailed { get; set; }
    public List<EmailFailure> Failures { get; set; } = new();
}

/// <summary>
/// Details about a failed email send.
/// </summary>
public class EmailFailure
{
    public int PersonId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Verification settings for the frontend.
/// </summary>
public class VerificationSettingsDto
{
    /// <summary>
    /// Number of days instructors have to respond to verification emails.
    /// </summary>
    public int VerificationReplyDays { get; set; }
}
