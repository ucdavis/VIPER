using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcpathItemNote
{
    public int NoteId { get; set; }

    public int ItemId { get; set; }

    public string Note { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string LoginId { get; set; } = null!;
}
