namespace Viper.Models.VIPER;

public partial class FormSubmissionSnapshot
{
    public Guid FormsubmissionSnapshotId { get; set; }

    public Guid FormSubmissionId { get; set; }

    public Guid FormId { get; set; }

    public string SubmissionTitle { get; set; } = null!;

    public string InitiatedBy { get; set; } = null!;

    public DateTime InitiatedOn { get; set; }

    public string LastUpdatedBy { get; set; } = null!;

    public DateTime LastUpdatedOn { get; set; }

    public int WorkflowStageId { get; set; }

    public string SubmissionData { get; set; } = null!;

    public string WorkflowStageData { get; set; } = null!;

    public DateTime SnapshotTimestamp { get; set; }

    public int Version { get; set; }

    public virtual Form Form { get; set; } = null!;

    public virtual WorkflowStage WorkflowStage { get; set; } = null!;
}
