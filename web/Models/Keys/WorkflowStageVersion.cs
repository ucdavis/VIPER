using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class WorkflowStageVersion
{
    public int WorkflowStageVersionId { get; set; }

    public int WorkflowStageId { get; set; }

    public int Version { get; set; }

    public int Order { get; set; }

    public string Message { get; set; } = null!;

    public bool Active { get; set; }

    public int? ClientFormUpload { get; set; }

    public int? PatientFormUpload { get; set; }

    public virtual WorkflowStage WorkflowStage { get; set; } = null!;
}
