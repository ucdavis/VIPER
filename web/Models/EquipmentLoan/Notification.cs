using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int WorkflowStageTransitionId { get; set; }

    public string SubjectTemplate { get; set; } = null!;

    public string BodyTemplate { get; set; } = null!;

    public string FromEmail { get; set; } = null!;

    public string ToEmail { get; set; } = null!;

    public string? CcEmail { get; set; }

    public string? BccEmail { get; set; }

    public int Version { get; set; }

    public virtual WorkflowStageTransition WorkflowStageTransition { get; set; } = null!;
}
