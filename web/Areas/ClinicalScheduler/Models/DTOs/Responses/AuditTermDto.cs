namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// A selectable term (semester) option for the audit trail "Term" filter, scoped to a grad year.
    /// </summary>
    public class AuditTermDto
    {
        public int TermCode { get; set; }
        public string Term { get; set; } = string.Empty;
    }
}
