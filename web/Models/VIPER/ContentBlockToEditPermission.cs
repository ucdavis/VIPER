namespace Viper.Models.VIPER;

public class ContentBlockToEditPermission
{
    public int ContentBlockEditPermissionId { get; set; }

    public int ContentBlockId { get; set; }

    public string Permission { get; set; } = null!;

    public virtual ContentBlock ContentBlock { get; set; } = null!;
}
