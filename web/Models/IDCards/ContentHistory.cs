using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class ContentHistory
{
    public int ContentHistoryId { get; set; }

    public string ContentBlockContent { get; set; } = null!;

    public int ContentBlockId { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public DateTime? DeletedOn { get; set; }

    public virtual ContentBlock ContentBlock { get; set; } = null!;
}
