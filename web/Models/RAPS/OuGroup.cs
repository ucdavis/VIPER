namespace Viper.Models.RAPS;

public partial class OuGroup
{
    public int OugroupId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<OuGroupRole> OuGroupRoles { get; set; } = new List<OuGroupRole>();
}
