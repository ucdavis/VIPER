using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class ExcelGeneratorRequest
{
    public int RequestId { get; set; }

    public string MothraId { get; set; } = null!;

    public string? ArgumentList { get; set; }

    public string Cfc { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string? TheColumnList { get; set; }
}
