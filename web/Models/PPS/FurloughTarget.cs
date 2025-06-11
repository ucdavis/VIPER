using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class FurloughTarget
{
    public int FurloughId { get; set; }

    public string FurloughChart { get; set; } = null!;

    public string FurloughAcct { get; set; } = null!;

    public string FurloughSubAcct { get; set; } = null!;

    public string? FurloughObjConsol { get; set; }

    public string FurloughObject { get; set; } = null!;

    public string FurloughSubObj { get; set; } = null!;

    public string FurloughProject { get; set; } = null!;

    public string FurloughOpFund { get; set; } = null!;

    public string FurloughEmployeeId { get; set; } = null!;

    public string FurloughDeptCode { get; set; } = null!;

    public string FurloughDeptName { get; set; } = null!;

    public string FurloughEmpName { get; set; } = null!;

    public string FurloughTitleCode { get; set; } = null!;

    public string FurloughDos { get; set; } = null!;

    public decimal FurloughCumulativeSalary { get; set; }

    public decimal? FurloughCurrentSalary { get; set; }

    public string? FurloughBeginDate { get; set; }

    public string? FurloughEndDate { get; set; }

    public decimal? FurloughCommittedFySalary { get; set; }

    public decimal? FurloughCommittedNfySalary { get; set; }

    public decimal? FurloughProjectedFySalary { get; set; }

    public decimal? FurloughProjectedSalary { get; set; }
}
