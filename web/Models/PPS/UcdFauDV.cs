using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcdFauDV
{
    public string? Chart { get; set; }

    public string? Account { get; set; }

    public string? AccountNm { get; set; }

    public string? FundCode { get; set; }

    public string? SubFundGroup { get; set; }

    public DateTime DwRecInsrtTs { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public string? OrgCd { get; set; }

    public string? OrganizationCode { get; set; }

    public string? OrgNm { get; set; }
}
