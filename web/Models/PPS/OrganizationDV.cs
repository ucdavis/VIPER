using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class OrganizationDV
{
    public decimal OrgDKey { get; set; }

    public DateTime OrgEffDt { get; set; }

    public DateTime OrgExprDt { get; set; }

    public string LocCd { get; set; } = null!;

    public string LocDesc { get; set; } = null!;

    public string DeptCd { get; set; } = null!;

    public string DeptTtl { get; set; } = null!;

    public string DeptShrtTtl { get; set; } = null!;

    public string DeptAddr1 { get; set; } = null!;

    public string DeptAddr2 { get; set; } = null!;

    public string DeptAddr3 { get; set; } = null!;

    public string DeptMailCd { get; set; } = null!;

    public DateTime DeptActDt { get; set; }

    public DateTime DeptInactDt { get; set; }

    public string OrgCd { get; set; } = null!;

    public string OrgTtl { get; set; } = null!;

    public string DivCd { get; set; } = null!;

    public string DivTtl { get; set; } = null!;

    public string SubDivCd { get; set; } = null!;

    public string SubDivTtl { get; set; } = null!;

    public string OrgSrcDesc { get; set; } = null!;

    public DateTime DdwLastUpdDt { get; set; }
}
