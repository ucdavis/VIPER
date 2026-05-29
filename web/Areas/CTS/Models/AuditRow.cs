using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class AuditRow
    {
        public string Area { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? Detail { get; set; }
        public DateTime Timestamp { get; set; }
        public int? ModifiedById { get; set; }
        public string? ModifiedByName { get; set; }
        public int? ModifiedPersonId { get; set; }
        public string? ModifiedPersonName { get; set; }

        public AuditRow() { }
        public AuditRow(CtsAudit dbAudit)
        {
            Area = dbAudit.Area;
            Action = dbAudit.Action;
            Detail = dbAudit.Detail;
            Timestamp = dbAudit.TimeStamp;
            ModifiedById = dbAudit.ModifiedBy;
            ModifiedByName = dbAudit.Modifier.LastName + ", " + dbAudit.Modifier.FirstName;
            if (dbAudit?.Encounter?.Student != null)
            {
                ModifiedPersonId = dbAudit.Encounter?.StudentUserId;
                ModifiedPersonName = dbAudit.Encounter?.Student?.LastName + ", " + dbAudit.Encounter?.Student?.FirstName;
            }

        }
    }
}
