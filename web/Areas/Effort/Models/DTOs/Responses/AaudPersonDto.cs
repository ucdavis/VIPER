namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for AAUD person search results when adding instructors.
/// Contains basic person info plus employment details from AAUD.
/// </summary>
public class AaudPersonDto
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleInitial { get; set; }
    public string FullName => string.IsNullOrEmpty(MiddleInitial)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleInitial}.";
    public string? EffortDept { get; set; }
    public string? TitleCode { get; set; }
    public string? JobGroupId { get; set; }
}
