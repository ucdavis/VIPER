using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating only the enrollment of an R-course.
/// Used by users with ManageRCourseEnrollment permission.
/// </summary>
public class UpdateEnrollmentRequest
{
    [Range(0, int.MaxValue)]
    public int Enrollment { get; set; }
}
