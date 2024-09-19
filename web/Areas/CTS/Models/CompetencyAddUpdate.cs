namespace Viper.Areas.CTS.Models
{
    public class CompetencyAddUpdate
    {
        public int? CompetencyId { get; set; }
        public int DomainId { get; set; }
        public int? ParentId { get; set; }
        public string Number { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool CanLinkToStudent { get; set; }
    }
}
