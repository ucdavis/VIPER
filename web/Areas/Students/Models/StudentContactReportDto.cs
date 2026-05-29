namespace Viper.Areas.Students.Models;

public class StudentContactReportDto
{
    public int PersonId { get; set; }
    public string RowKey { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ClassLevel { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Zip { get; set; }
    public string? HomePhone { get; set; }
    public string? CellPhone { get; set; }
    public ContactInfoDto LocalContact { get; set; } = new();
    public ContactInfoDto EmergencyContact { get; set; } = new();
    public ContactInfoDto PermanentContact { get; set; } = new();
    public bool ContactPermanent { get; set; }
}
