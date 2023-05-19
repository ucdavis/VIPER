using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblMemberPermission
{
    public string MemberId { get; set; } = null!;

    public int PermissionId { get; set; }

    public byte Access { get; set; }

    public DateTime? ModTime { get; set; }

    public string? ModBy { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? AddDate { get; set; }

    public virtual TblPermission Permission { get; set; } = null!;
}
