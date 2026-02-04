namespace Viper.Areas.CTS.Models
{
    public class CompetencyAddUpdate
    {
        public int? CompetencyId { get; set; }
        public required int DomainId { get; set; }
        public int? ParentId { get; set; }
        public required string Number { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required bool CanLinkToStudent { get; set; }
    }
}
