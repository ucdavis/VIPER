using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwStudent
{
    public int? TermCode { get; set; }

    public string SpridenId { get; set; } = null!;

    public string? ClassLevel { get; set; }
}
