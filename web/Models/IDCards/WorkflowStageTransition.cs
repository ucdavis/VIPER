using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class WorkflowStageTransition
{
    public int WorkflowStageTransitionId { get; set; }

    public int FromWorkflowStage { get; set; }

    public int ToWorkflowStage { get; set; }

    public int? ConditionFormFieldId { get; set; }

    public string? Value { get; set; }

    public bool LoadImmediately { get; set; }

    public int Version { get; set; }

    public virtual FormField? ConditionFormField { get; set; }

    public virtual WorkflowStage FromWorkflowStageNavigation { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual WorkflowStage ToWorkflowStageNavigation { get; set; } = null!;
}
