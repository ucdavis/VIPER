using Viper.Areas.Students.Services;

namespace Viper.Areas.Students.Models;

public class StudentContactListItemDto
{
    public int PersonId { get; set; }
    public string RowKey { get; set; } = string.Empty;
    public bool HasDetailRoute { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ClassLevel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CellPhone { get; set; }
    public int StudentInfoComplete { get; set; }
    public int StudentInfoTotal { get; set; } = EmergencyContactService.StudentInfoFieldCount;
    public List<string> StudentInfoMissing { get; set; } = new();
    public int LocalContactComplete { get; set; }
    public int LocalContactTotal { get; set; } = EmergencyContactService.ContactFieldCount;
    public List<string> LocalContactMissing { get; set; } = new();
    public int EmergencyContactComplete { get; set; }
    public int EmergencyContactTotal { get; set; } = EmergencyContactService.ContactFieldCount;
    public List<string> EmergencyContactMissing { get; set; } = new();
    public int PermanentContactComplete { get; set; }
    public int PermanentContactTotal { get; set; } = EmergencyContactService.ContactFieldCount;
    public List<string> PermanentContactMissing { get; set; } = new();
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Overall completeness status for quick UI display:
    /// "complete" = all sections fully filled, "partial" = some data entered, "empty" = no data.
    /// </summary>
    public string CompletenessStatus
    {
        get
        {
            var totalComplete = StudentInfoComplete + LocalContactComplete
                + EmergencyContactComplete + PermanentContactComplete;
            var totalFields = StudentInfoTotal + LocalContactTotal
                + EmergencyContactTotal + PermanentContactTotal;

            if (totalComplete >= totalFields)
            {
                return "complete";
            }
            return totalComplete > 0 ? "partial" : "empty";
        }
    }
}
