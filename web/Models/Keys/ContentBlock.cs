using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class ContentBlock
{
    public int ContentBlockId { get; set; }

    public string ContentBlock1 { get; set; } = null!;

    public string? Title { get; set; }

    public string System { get; set; } = null!;

    public string? Application { get; set; }

    public string? Page { get; set; }

    public string? ViperSectionPath { get; set; }

    public int? BlockOrder { get; set; }

    public string? FriendlyName { get; set; }

    public bool AllowPublicAccess { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime? DeletedOn { get; set; }

    public virtual ICollection<ContentBlockToFile> ContentBlockToFiles { get; set; } = new List<ContentBlockToFile>();

    public virtual ICollection<ContentHistory> ContentHistories { get; set; } = new List<ContentHistory>();
}
