namespace Viper.Models.VIPER;

public partial class WorkflowStageVersion
{
    public int WorkflowStageVersionId { get; set; }

    public int WorkflowStageId { get; set; }

    public int Version { get; set; }

    public int Order { get; set; }

    public string Message { get; set; } = null!;

    public bool Active { get; set; }

    public virtual WorkflowStage WorkflowStage { get; set; } = null!;
}
