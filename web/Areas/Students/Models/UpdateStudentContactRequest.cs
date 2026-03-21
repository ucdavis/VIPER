namespace Viper.Areas.Students.Models;

public class UpdateStudentContactRequest
{
    public StudentInfoDto StudentInfo { get; set; } = new();
    public required bool ContactPermanent { get; set; }
    public ContactInfoDto LocalContact { get; set; } = new();
    public ContactInfoDto EmergencyContact { get; set; } = new();
    public ContactInfoDto PermanentContact { get; set; } = new();
}
