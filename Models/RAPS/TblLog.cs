using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class TblLog
{
    public string? MemberId { get; set; }

    public int? RoleId { get; set; }

    public int? PermissionId { get; set; }

    public DateTime ModTime { get; set; }

    public string ModBy { get; set; } = null!;

    public string Audit { get; set; } = null!;

    public string? Detail { get; set; }

    public string? Comment { get; set; }

    public int? OuGroupId { get; set; }

    public int? RoleTemplateId { get; set; }
}
