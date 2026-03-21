namespace Viper.Areas.Students.Models;

public class StudentContactListItemDto
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ClassLevel { get; set; } = string.Empty;
    public int StudentInfoComplete { get; set; }
    public int StudentInfoTotal { get; set; } = 3;
    public int LocalContactComplete { get; set; }
    public int LocalContactTotal { get; set; } = 6;
    public int EmergencyContactComplete { get; set; }
    public int EmergencyContactTotal { get; set; } = 6;
    public int PermanentContactComplete { get; set; }
    public int PermanentContactTotal { get; set; } = 6;
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
