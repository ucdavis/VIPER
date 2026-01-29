using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request to send a verification email to a single instructor.
/// </summary>
public class SendVerificationEmailRequest
{
    [Range(1, int.MaxValue)]
    public required int PersonId { get; set; }

    [Range(1, int.MaxValue)]
    public required int TermCode { get; set; }
}

/// <summary>
/// Request to send verification emails to all unverified instructors in a department.
/// </summary>
public class SendBulkEmailRequest
{
    [Required]
    [StringLength(10, MinimumLength = 1)]
    public required string DepartmentCode { get; set; }

    [Range(1, int.MaxValue)]
    public required int TermCode { get; set; }
}
