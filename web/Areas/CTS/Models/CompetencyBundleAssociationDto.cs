using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    /// <summary>
    /// Data transfer object for competency with its associated bundles
    /// </summary>
    public class CompetencyBundleAssociationDto
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
        public List<BundleInfoDto> Bundles { get; set; } = new List<BundleInfoDto>();

        public CompetencyBundleAssociationDto()
        {
        }

        /// <summary>
        /// Initializes a new instance from a Competency entity
        /// </summary>
        /// <param name="c">The competency entity to map</param>
        public CompetencyBundleAssociationDto(Competency c)
        {
            CompetencyId = c.CompetencyId;
            DomainId = c.DomainId;
            ParentId = c.ParentId;
            Number = c.Number;
            Name = c.Name;
            Description = c.Description;
            CanLinkToStudent = c.CanLinkToStudent;
            DomainName = c.Domain?.Name;
            DomainOrder = c.Domain?.Order;
            
            // Map associated bundles, filtering out any null references
            if (c.BundleCompetencies != null)
            {
                Bundles = c.BundleCompetencies
                    .Where(bc => bc.Bundle != null)
                    .Select(bc => new BundleInfoDto
                    {
                        BundleId = bc.Bundle!.BundleId,
                        Name = bc.Bundle.Name,
                        Clinical = bc.Bundle.Clinical,
                        Assessment = bc.Bundle.Assessment,
                        Milestone = bc.Bundle.Milestone
                    })
                    .GroupBy(b => b.BundleId)
                    .Select(g => g.First())
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Simplified bundle information for display purposes
    /// </summary>
    public class BundleInfoDto
    {
        public int BundleId { get; set; }
        public string Name { get; set; } = null!;
        public bool Clinical { get; set; }
        public bool Assessment { get; set; }
        public bool Milestone { get; set; }
    }
}