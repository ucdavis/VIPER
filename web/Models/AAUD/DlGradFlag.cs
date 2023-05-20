using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlGradFlag
{
    public string GradFlagsPKey { get; set; } = null!;

    public string GradFlagsTermCode { get; set; } = null!;

    public string GradFlagsClientid { get; set; } = null!;

    public bool GradFlagsStudent { get; set; }

    public bool GradFlagsAcademic { get; set; }

    public bool GradFlagsStaff { get; set; }

    public bool GradFlagsTeachingFaculty { get; set; }

    public bool GradFlagsWosemp { get; set; }

    public bool GradFlagsConfidential { get; set; }

    public bool GradFlagsSvmPeople { get; set; }

    public bool GradFlagsSvmStudent { get; set; }

    public bool? GradFlagsRossStudent { get; set; }
}
