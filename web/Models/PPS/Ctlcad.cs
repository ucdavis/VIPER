using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Ctlcad
{
    public decimal? CadYear { get; set; }

    public decimal? CadMonth { get; set; }

    public decimal? CadDay { get; set; }

    public string? CadDayType { get; set; }

    public string? CadB1PayEnd { get; set; }

    public string? CadB1PayDay { get; set; }

    public string? CadB2PayEnd { get; set; }

    public string? CadB2PayDay { get; set; }

    public string? CadSmPayEnd { get; set; }

    public string? CadSmPayDay { get; set; }

    public string? CadMoPayEnd { get; set; }

    public string? CadMoPayDay { get; set; }

    public string? CadMaPayEnd { get; set; }

    public string? CadMaPayDay { get; set; }

    public string? CadLastAction { get; set; }

    public DateTime? CadLastActionDt { get; set; }
}
