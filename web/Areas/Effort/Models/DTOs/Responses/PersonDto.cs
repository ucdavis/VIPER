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
    public string FullName => string.IsNullOrEmpty(MiddleInitial)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleInitial}.";
    public string EffortTitleCode { get; set; } = string.Empty;
    public string EffortDept { get; set; } = string.Empty;
    public double PercentAdmin { get; set; }
    public string? Title { get; set; }
    public string? AdminUnit { get; set; }
    public DateTime? EffortVerified { get; set; }
    public string? ReportUnit { get; set; }
    public double? PercentClinical { get; set; }
    public bool IsVerified => EffortVerified.HasValue;
}
