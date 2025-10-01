using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcdDmPsNamesPrefVnamesV
{
    public string Emplid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string MiddleName { get; set; } = null!;

    public string LastNameSrch { get; set; } = null!;

    public string FirstNameSrch { get; set; } = null!;

    public string NameDisplay { get; set; } = null!;

    public DateTime UpdBtDtm { get; set; }

    public DateTime? DwRecInsrtDttm { get; set; }

    public string? DwRecInsrtId { get; set; }
}
