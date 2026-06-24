namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// A distinct person referenced by an audited schedule change, used to populate the
    /// audit trail's person picklists — both "Modified By" (the user who made the change)
    /// and "Person" (the affected student/clinician).
    /// </summary>
    public class AuditModifierDto
    {
        public string MothraId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
