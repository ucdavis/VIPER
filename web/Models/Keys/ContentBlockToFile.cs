using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class ContentBlockToFile
{
    public int ContentBlockFileId { get; set; }

    public int ContentBlockId { get; set; }

    public Guid FileGuid { get; set; }

    public virtual ContentBlock ContentBlock { get; set; } = null!;

    public virtual File File { get; set; } = null!;
}
