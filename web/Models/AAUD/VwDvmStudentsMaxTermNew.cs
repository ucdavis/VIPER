using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwDvmStudentsMaxTermNew
{
    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;

    public string? PersonMiddleName { get; set; }

    public string? IdsLoginId { get; set; }

    public string? IdsPidm { get; set; }

    public string? IdsMailid { get; set; }

    public string IdsMothraId { get; set; } = null!;

    public string? StudentsClassLevel { get; set; }

    public string StudentsTermCode { get; set; } = null!;
}
