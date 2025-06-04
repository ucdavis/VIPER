using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class ClassYearLeftReason
{
    public int ClassYearLeftReasonId { get; set; }

    public string Reason { get; set; } = null!;

    public virtual ICollection<StudentClassYear> StudentClassYears { get; set; } = new List<StudentClassYear>();
}
