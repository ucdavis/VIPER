using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class FormVersion
{
    public int FormVersionId { get; set; }

    public Guid FormId { get; set; }

    public int Version { get; set; }

    public string FormName { get; set; } = null!;

    public string? FriendlyUrl { get; set; }

    public string Description { get; set; } = null!;

    public bool AllowNewSubmissions { get; set; }

    public bool ExistingSubmissionsOpen { get; set; }

    public bool ShowInManageUi { get; set; }

    public virtual Form Form { get; set; } = null!;
}
