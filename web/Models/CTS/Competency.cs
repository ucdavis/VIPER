namespace Viper.Models.CTS;

public partial class Competency
{
    public int CompetencyId { get; set; }

    public int DomainId { get; set; }

    public int? ParentId { get; set; }

    public string Number { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool CanLinkToStudent { get; set; }

    public int Order { get; set; }

    public virtual Domain Domain { get; set; } = null!;

    public virtual ICollection<Competency> Children { get; set; } = new List<Competency>();

    public virtual ICollection<BundleCompetency> BundleCompetencies { get; set; } = new List<BundleCompetency>();

    public virtual Competency? Parent { get; set; }
}
