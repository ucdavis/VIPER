using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class CompetencyDto
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
        public CompetencyDto? Parent { get; set; }

        public CompetencyDto()
        {

        }

        public CompetencyDto(Competency c)
        {
            CompetencyId = c.CompetencyId;
            DomainId = c.DomainId;
            ParentId = c.ParentId;
            Number = c.Number;
            Name = c.Name;
            Description = c.Description;
            CanLinkToStudent = c.CanLinkToStudent;
            DomainName = c?.Domain?.Name;
            DomainOrder = c?.Domain?.Order;
            Parent = c?.Parent != null ? new CompetencyDto(c.Parent) : null;
        }
    }
}
