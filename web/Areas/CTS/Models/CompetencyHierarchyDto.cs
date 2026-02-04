namespace Viper.Areas.CTS.Models
{
    public class CompetencyHierarchyDto
    {
        public int CompetencyId { get; set; }
        public int DomainId { get; set; }
        public int? ParentId { get; set; }
        public string Number { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool CanLinkToStudent { get; set; }
        public string? DomainName { get; set; }
        public int? DomainOrder { get; set; }
        public IEnumerable<CompetencyHierarchyDto> Children { get; set; } = new List<CompetencyHierarchyDto>();
        public string Type
        {
            get
            {
                return Number.Split(".", StringSplitOptions.RemoveEmptyEntries).Length switch
                {
                    2 => "Competency",
                    3 => "Illustrative Sub-Competency",
                    4 => "Detail",
                    _ => "Unknown",
                };
            }
        }
    }
}
