using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Students.Models;

public class UpdateStudentContactRequest
{
    [Required] public StudentInfoDto StudentInfo { get; set; } = new();
    public required bool ContactPermanent { get; set; }
    [Required] public ContactInfoDto LocalContact { get; set; } = new();
    [Required] public ContactInfoDto EmergencyContact { get; set; } = new();
    [Required] public ContactInfoDto PermanentContact { get; set; } = new();
}
