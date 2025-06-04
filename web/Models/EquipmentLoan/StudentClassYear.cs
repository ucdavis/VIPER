using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class StudentClassYear
{
    public int StudentClassYearId { get; set; }

    public int PersonId { get; set; }

    public int ClassYear { get; set; }

    public bool Active { get; set; }

    public bool Graduated { get; set; }

    public bool Ross { get; set; }

    public int? LeftTerm { get; set; }

    public int? LeftReason { get; set; }

    public DateTime Added { get; set; }

    public int? AddedBy { get; set; }

    public DateTime? Updated { get; set; }

    public int? UpdatedBy { get; set; }

    public string? Comment { get; set; }

    public virtual Person? AddedByNavigation { get; set; }

    public virtual ClassYearLeftReason? LeftReasonNavigation { get; set; }

    public virtual Person Person { get; set; } = null!;

    public virtual Person? UpdatedByNavigation { get; set; }
}
