namespace Viper.Models.VIPER;

public partial class QuickLink
{
    public int QuickLinkId { get; set; }

    public string QuickLinkLabel { get; set; } = null!;

    public string? QuickLinkPermission { get; set; }

    public string? QuickLinkUrl { get; set; }

    public string? QuickLinkTab { get; set; }

    public bool? SystemQuicklink { get; set; }

    public string? SystemDescription { get; set; }

    public string? SystemType { get; set; }
}
