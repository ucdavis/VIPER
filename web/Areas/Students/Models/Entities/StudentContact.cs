namespace Viper.Areas.Students.Models.Entities;

public class StudentContact
{
    public int StdContactId { get; set; }
    public int Pidm { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Zip { get; set; }
    public string? HomePhone { get; set; }
    public string? CellPhone { get; set; }
    public bool ContactPermanent { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
    public virtual ICollection<StudentEmergencyContact> EmergencyContacts { get; set; } = new List<StudentEmergencyContact>();
}
