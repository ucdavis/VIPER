namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for person information in the Effort system.
/// </summary>
public class PersonDto
{
    public int PersonId { get; set; }
    public int TermCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleInitial { get; set; }
    public string FullName => $"{LastName}, {FirstName}";
    public string EffortTitleCode { get; set; } = string.Empty;
    public string EffortDept { get; set; } = string.Empty;
    public double PercentAdmin { get; set; }
    public string? JobGroupId { get; set; }
    public string? Title { get; set; }
    public string? AdminUnit { get; set; }
    public DateTime? EffortVerified { get; set; }
    public string? ReportUnit { get; set; }
    public bool VolunteerWos { get; set; }
    public double? PercentClinical { get; set; }
    public bool IsVerified => EffortVerified.HasValue;

    /// <summary>
    /// Number of effort records for this instructor in the term.
    /// Used to determine if verification emails can be sent.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Whether verification emails can be sent to this instructor.
    /// False if instructor has no effort records or is already verified.
    /// </summary>
    public bool CanSendVerificationEmail => !IsVerified && RecordCount > 0;
}
