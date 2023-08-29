using System;
using System.Collections.Generic;
using Viper.Models.RAPS;

namespace Viper.Models.VIPER;

public partial class ContentBlockToPermission
{
    public int ContentBlockPermissionId { get; set; }

    public int ContentBlockId { get; set; }

    public string Permission { get; set; } = null!;

    public virtual ContentBlock ContentBlock { get; set; } = null!;
}
