using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwJobCodeAndGroup
{
    public string Jobcode { get; set; } = null!;

    public string JobcodeDesc { get; set; } = null!;

    public string JobcodeShortDesc { get; set; } = null!;

    public string FlsaStatus { get; set; } = null!;

    public string JobcodeUnionCode { get; set; } = null!;

    public string Jobgroup { get; set; } = null!;

    public string JobgroupDesc { get; set; } = null!;

    public string ClassIndc { get; set; } = null!;
}
