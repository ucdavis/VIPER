using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Students.Models;

/// <summary>
/// Career selection information for a student, matching the client's expected format.
/// </summary>
public class StudentCareerInfoDto
{
    public CareerDropdownOption? Direction { get; set; }
    [MaxLength(200, ErrorMessage = "Career Direction must be 200 characters or fewer.")]
    public string? DirectionOther { get; set; }
    public CareerDropdownOption? PrimaryFocus { get; set; }
    [MaxLength(200, ErrorMessage = "Primary Species must be 200 characters or fewer.")]
    public string? PrimaryFocusOther { get; set; }
    public CareerDropdownOption? SecondaryFocus { get; set; }
    [MaxLength(200, ErrorMessage = "Secondary Species must be 200 characters or fewer.")]
    public string? SecondaryFocusOther { get; set; }
    public int? MentorId { get; set; }
    public string? MentorName { get; set; }
    /// <summary>
    /// Display only, like <see cref="MentorName"/>: the mentor picker keys its options on the IAM
    /// id, so a saved mentor needs one to render. A save reads the mentor from
    /// <see cref="MentorId"/> and ignores whatever the client sends here.
    /// </summary>
    public string? MentorIamId { get; set; }
    public CareerDropdownOption? PostGrad { get; set; }
    [MaxLength(5000, ErrorMessage = "Short Term Statement must be 5000 characters or fewer.")]
    public string? ShortTermPlans { get; set; }
    [MaxLength(5000, ErrorMessage = "Long Term Statement must be 5000 characters or fewer.")]
    public string? LongTermPlans { get; set; }
}
