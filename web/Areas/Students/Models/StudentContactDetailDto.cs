namespace Viper.Areas.Students.Models;

public class StudentContactDetailDto
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ClassLevel { get; set; } = string.Empty;
    public StudentInfoDto StudentInfo { get; set; } = new();
    public bool ContactPermanent { get; set; }
    public ContactInfoDto LocalContact { get; set; } = new();
    public ContactInfoDto EmergencyContact { get; set; } = new();
    public ContactInfoDto PermanentContact { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
}
