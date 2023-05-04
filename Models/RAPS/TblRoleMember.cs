using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblRoleMember
{
    public int RoleId { get; set; }

    public string MemberId { get; set; } = null!;

    public DateTime? EndDate { get; set; }

    public string? ViewName { get; set; }

    public DateTime? AddDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? ModTime { get; set; }

    public string? ModBy { get; set; }

    public virtual TblRole Role { get; set; } = null!;
}
