using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlPerson
{
    public string PersonPKey { get; set; } = null!;

    public string PersonTermCode { get; set; } = null!;

    public string PersonClientid { get; set; } = null!;

    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;

    public string? PersonMiddleName { get; set; }

    public string? PersonDisplayLastName { get; set; }

    public string? PersonDisplayFirstName { get; set; }

    public string? PersonDisplayMiddleName { get; set; }

    public string? PersonDisplayFullName { get; set; }
}
