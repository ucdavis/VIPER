using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class PhotoExport
{
    public string IamId { get; set; } = null!;

    public DateTime Dateexported { get; set; }
}
