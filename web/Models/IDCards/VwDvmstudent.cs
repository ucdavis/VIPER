using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwDvmstudent
{
    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;

    public string? PersonMiddleName { get; set; }

    public string? IdsLoginId { get; set; }

    public int? IdsPidm { get; set; }

    public string? IdsMailid { get; set; }

    public string IdsMothraId { get; set; } = null!;

    public string? StudentsClassLevel { get; set; }

    public int? StudentsTermCode { get; set; }
}
