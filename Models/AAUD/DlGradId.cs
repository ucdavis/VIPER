using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class DlGradId
{
    public string GradIdsPKey { get; set; } = null!;

    public string GradIdsTermCode { get; set; } = null!;

    public string GradIdsClientid { get; set; } = null!;

    public string? GradIdsMothraid { get; set; }

    public string? GradIdsLoginid { get; set; }

    public string? GradIdsMailid { get; set; }

    public string? GradIdsSpridenId { get; set; }

    public string? GradIdsPidm { get; set; }

    public string? GradIdsEmployeeId { get; set; }

    public int? GradIdsVmacsId { get; set; }

    public string? GradIdsVmcasId { get; set; }

    public string? GradIdsUnexId { get; set; }

    public int? GradIdsMivId { get; set; }

    public string? GradIdsPpsId { get; set; }

    public string? GradIdsIamId { get; set; }
}
