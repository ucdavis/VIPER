using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class AaudOverrideJob
{
    public int OverrideJobsId { get; set; }

    public int OverrideId { get; set; }

    public int JobSeqNum { get; set; }

    public string? JobDepartmentCode { get; set; }

    public decimal? JobPercentFulltime { get; set; }

    public string? JobTitleCode { get; set; }

    public string? JobBargainingUnit { get; set; }

    public string? JobSchoolDivision { get; set; }

    public virtual AaudOverride Override { get; set; } = null!;
}
