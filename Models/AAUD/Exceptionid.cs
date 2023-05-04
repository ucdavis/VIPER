using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class Exceptionid
{
    public string IdsPKey { get; set; } = null!;

    public string IdsTermCode { get; set; } = null!;

    public string IdsClientid { get; set; } = null!;

    public string? IdsMothraid { get; set; }

    public string? IdsLoginid { get; set; }

    public string? IdsMailid { get; set; }

    public string? IdsSpridenId { get; set; }

    public string? IdsPidm { get; set; }

    public string? IdsEmployeeId { get; set; }

    public string? IdsVmacsId { get; set; }

    public string? IdsPpsId { get; set; }

    public string? IdsIamId { get; set; }
}
