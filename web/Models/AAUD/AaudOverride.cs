using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class AaudOverride
{
    public int OverrideId { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string MothraId { get; set; } = null!;

    public string EnteredBy { get; set; } = null!;

    public DateTime EnteredOn { get; set; }

    public string? Note { get; set; }

    public string? EmpHomeDept { get; set; }

    public string? EmpAltDeptCode { get; set; }

    public string? EmpPrimaryTitle { get; set; }

    public string? EmpTeachingTitleCode { get; set; }

    public string? EmpTeachingHomeDept { get; set; }

    public decimal? EmpTeachingPercentFulltime { get; set; }

    public string? EmpEffortTitleCode { get; set; }

    public string? EmpEffortHomeDept { get; set; }

    public bool? FlagsStudent { get; set; }

    public bool? FlagsAcademic { get; set; }

    public bool? FlagsStaff { get; set; }

    public bool? FlagsTeachingFaculty { get; set; }

    public bool? FlagsWosemp { get; set; }

    public bool? FlagsSvmPeople { get; set; }

    public bool? FlagsSvmStudent { get; set; }

    public virtual ICollection<AaudOverrideJob> AaudOverrideJobs { get; set; } = new List<AaudOverrideJob>();
}
