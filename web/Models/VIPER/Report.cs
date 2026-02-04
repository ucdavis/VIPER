namespace Viper.Models.VIPER;

public partial class Report
{
    public int ReportId { get; set; }

    public Guid FormId { get; set; }

    public string Name { get; set; } = null!;

    public bool Excel { get; set; }

    public DateTime LastUpdatedOn { get; set; }

    public string LastUpdatedBy { get; set; } = null!;

    public string? FormColumns { get; set; }

    public virtual Form Form { get; set; } = null!;

    public virtual ICollection<ReportField> ReportFields { get; set; } = new List<ReportField>();
}
