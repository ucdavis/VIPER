using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class WorkflowStage
{
    public int WorkflowStageId { get; set; }

    public Guid FormId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsFinal { get; set; }

    public string ViewPermission { get; set; } = null!;

    public string EditPermission { get; set; } = null!;

    public bool PubliclyAccessible { get; set; }

    public virtual Form Form { get; set; } = null!;

    public virtual ICollection<FormSubmissionSnapshot> FormSubmissionSnapshots { get; set; } = new List<FormSubmissionSnapshot>();

    public virtual ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();

    public virtual ICollection<WorkflowStageTransition> WorkflowStageTransitionFromWorkflowStageNavigations { get; set; } = new List<WorkflowStageTransition>();

    public virtual ICollection<WorkflowStageTransition> WorkflowStageTransitionToWorkflowStageNavigations { get; set; } = new List<WorkflowStageTransition>();

    public virtual ICollection<WorkflowStageVersion> WorkflowStageVersions { get; set; } = new List<WorkflowStageVersion>();

    public virtual ICollection<Page> Pages { get; set; } = new List<Page>();
}
