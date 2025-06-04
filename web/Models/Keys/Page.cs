using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Page
{
    public int PageId { get; set; }

    public Guid FormId { get; set; }

    public string? Title { get; set; }

    public int Order { get; set; }

    public int Version { get; set; }

    public string? ReadOnlyWorkflowstages { get; set; }

    public virtual Form Form { get; set; } = null!;

    public virtual ICollection<PageToFormField> PageToFormFields { get; set; } = new List<PageToFormField>();

    public virtual ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
}
