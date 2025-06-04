using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class VwLegacySessionCompetency
{
    public int SessionCompetencyId { get; set; }

    public int? SessionCompetencyOrder { get; set; }

    public int SessionId { get; set; }

    public string SessionStatus { get; set; } = null!;

    public string? Sessiontype { get; set; }

    public string? SessionTypeDescription { get; set; }

    public string SessionTitle { get; set; } = null!;

    public string? SessionDescription { get; set; }

    public string? MultiRole { get; set; }

    public int? TypeOrder { get; set; }

    public int? PaceOrder { get; set; }

    public string CourseTitle { get; set; } = null!;

    public int CourseId { get; set; }

    public string AcademicYear { get; set; } = null!;

    public int? DvmCompetencyId { get; set; }

    public int? DvmLevelId { get; set; }

    public int? DvmRoleId { get; set; }

    public string? DvmCompetencyName { get; set; }

    public int? DvmCompetencyParentId { get; set; }

    public bool? DvmCompetencyActive { get; set; }

    public string? DvmRoleName { get; set; }

    public string? DvmLevelName { get; set; }

    public int? DvmLevelOrder { get; set; }
}
