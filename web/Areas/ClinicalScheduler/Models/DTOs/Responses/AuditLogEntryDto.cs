namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// A single schedule-change audit entry, enriched with display names for the
    /// affected person, modifier, rotation, and week. Mirrors the legacy "Schedule
    /// Changes Audit log" row (Area / Person / Action / Rotation / Week / Modified By / Date).
    /// </summary>
    public class AuditLogEntryDto
    {
        public int ScheduleAuditId { get; set; }
        public string Area { get; set; } = string.Empty;
        public string? MothraId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int? RotationId { get; set; }
        public string RotationName { get; set; } = string.Empty;
        public int? WeekId { get; set; }
        public int WeekNum { get; set; }
        public DateTime? WeekStart { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public string ModifiedByName { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
    }
}
