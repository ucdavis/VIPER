using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPersonalPhoneV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string PhoneType { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Extension { get; set; } = null!;

    public string PrefPhoneFlag { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
