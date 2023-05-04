using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class OuGroupRole
{
    public int Ougroupid { get; set; }

    public int Roleid { get; set; }

    public bool IsGroupRole { get; set; }

    public virtual OuGroup Ougroup { get; set; } = null!;

    public virtual TblRole Role { get; set; } = null!;
}
