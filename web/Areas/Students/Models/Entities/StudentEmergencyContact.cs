namespace Viper.Areas.Students.Models.Entities;

public class StudentEmergencyContact
{
    public int EmContactId { get; set; }
    public int StdContactId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Relationship { get; set; }
    public string? WorkPhone { get; set; }
    public string? HomePhone { get; set; }
    public string? CellPhone { get; set; }
    public string? Email { get; set; }
    public virtual StudentContact StudentContact { get; set; } = null!;
}
