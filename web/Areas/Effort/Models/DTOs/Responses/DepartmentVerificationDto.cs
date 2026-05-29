namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Department-level verification summary for the data hygiene view.
/// </summary>
public class DepartmentVerificationDto
{
    /// <summary>
    /// Department code.
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Department name for display.
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Total instructors in this department.
    /// </summary>
    public int TotalInstructors { get; set; }

    /// <summary>
    /// Number of verified instructors.
    /// </summary>
    public int VerifiedInstructors { get; set; }

    /// <summary>
    /// Number of unverified instructors.
    /// </summary>
    public int UnverifiedInstructors => TotalInstructors - VerifiedInstructors;

    /// <summary>
    /// Verification percentage (0-100).
    /// </summary>
    public int VerificationPercent => TotalInstructors > 0
        ? (int)Math.Round((double)VerifiedInstructors / TotalInstructors * 100)
        : 0;

    /// <summary>
    /// Whether this department meets the verification threshold.
    /// </summary>
    public bool MeetsThreshold { get; set; }

    /// <summary>
    /// Status indicator: NeedsFollowup, OnTrack, Complete
    /// </summary>
    public string Status
    {
        get
        {
            if (VerificationPercent == 100) return "Complete";
            if (MeetsThreshold) return "OnTrack";
            return "NeedsFollowup";
        }
    }
}
