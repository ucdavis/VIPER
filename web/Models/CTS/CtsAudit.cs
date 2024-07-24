using Viper.Models.VIPER;

namespace Viper.Models.CTS
{
    public class CtsAudit
    {
        public int CtsAuditId { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime TimeStamp {  get; set; }
        public string Area { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string? Detail { get; set; }
        public int? EncounterId { get; set; }

        public Person Modifier { get; set; } = null!;
        public Encounter? Encounter { get; set; }
        
    }
}
