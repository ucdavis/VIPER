using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class CourseRole
{
    public int CourseRoleId { get; set; }

    public int CourseId { get; set; }

    public int RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;
}
