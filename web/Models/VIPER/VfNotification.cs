namespace Viper.Models.VIPER;

public partial class VfNotification
{
    /// <summary>
    /// ID
    /// </summary>
    public int NotifyId { get; set; }

    /// <summary>
    /// form ID
    /// </summary>
    public int? FormId { get; set; }

    /// <summary>
    /// short description, ie application approved/declined
    /// </summary>
    public string? NotifyDesc { get; set; }

    public string? Body { get; set; }

    public string? SendTo { get; set; }

    public string? Stage { get; set; }

    public int? StageId { get; set; }

    public string? Subject { get; set; }
}
