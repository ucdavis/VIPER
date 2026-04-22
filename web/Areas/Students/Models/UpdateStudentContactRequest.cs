using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Students.Models;

public class UpdateStudentContactRequest
{
    [Required] public StudentInfoDto StudentInfo { get; set; } = null!;
    public required bool ContactPermanent { get; set; }
    [Required] public ContactInfoDto LocalContact { get; set; } = null!;
    [Required] public ContactInfoDto EmergencyContact { get; set; } = null!;
    [Required] public ContactInfoDto PermanentContact { get; set; } = null!;
}
