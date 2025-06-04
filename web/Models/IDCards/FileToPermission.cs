using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class FileToPermission
{
    public int FileToPermissionId { get; set; }

    public Guid FileGuid { get; set; }

    public string Permission { get; set; } = null!;

    public virtual File File { get; set; } = null!;
}
