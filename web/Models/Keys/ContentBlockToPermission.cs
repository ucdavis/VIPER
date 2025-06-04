using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class ContentBlockToPermission
{
    public int ContentBlockPermissionId { get; set; }

    public int ContentBlockId { get; set; }

    public string Permission { get; set; } = null!;
}
