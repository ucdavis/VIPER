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

        public CompetencyDto(Competency c) : this(c, includeParent: true)
        {

        }

        // One level only: a cycle in the data would otherwise overflow the stack and kill the process.
        private CompetencyDto(Competency c, bool includeParent)
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
            Parent = includeParent && c?.Parent != null ? new CompetencyDto(c.Parent, includeParent: false) : null;
        }
    }
}
