using System;
using System.Collections.Generic;

namespace Viper.Models.VIPER;

public partial class SlowPage
{
    public int Id { get; set; }

    public string Path { get; set; } = null!;

    public string Page { get; set; } = null!;

    public int TimeToRender { get; set; }

    public DateTime DateRendered { get; set; }

    public string? LoginId { get; set; }
}
