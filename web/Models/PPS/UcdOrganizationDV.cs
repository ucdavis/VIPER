using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcdOrganizationDV
{
    public decimal OrgDKey { get; set; }

    public string LocCd { get; set; } = null!;

    public string LocDesc { get; set; } = null!;

    public string DeptCd { get; set; } = null!;

    public string DeptTtl { get; set; } = null!;

    public string DeptShrtTtl { get; set; } = null!;

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

    public string? SubDivL4Cd { get; set; }

    public string? SubDivL4Ttl { get; set; }

    public string? DeptEffStatus { get; set; }

    public DateTime? OrgEffDt { get; set; }
}
