using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class OuGroupRole
{
    public int OugroupId { get; set; }

    public int RoleId { get; set; }

    public bool IsGroupRole { get; set; }

    public virtual OuGroup Ougroup { get; set; } = null!;

    public virtual TblRole Role { get; set; } = null!;
}
