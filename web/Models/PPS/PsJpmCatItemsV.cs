using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsJpmCatItemsV
{
    public string JpmCatType { get; set; } = null!;

    public string JpmCatItemId { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string JpmDescr90 { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;

    public string Country { get; set; } = null!;

    public DateTime? JpmDate1 { get; set; }

    public DateTime? JpmDate2 { get; set; }

    public string JpmEvalMthd { get; set; } = null!;

    public string? JpmText13251 { get; set; }

    public string? JpmText13252 { get; set; }

    public string JpmText2541 { get; set; } = null!;

    public string JpmText2542 { get; set; } = null!;

    public string JpmText2543 { get; set; } = null!;

    public string JpmText2544 { get; set; } = null!;

    public string JpmYn1 { get; set; } = null!;

    public string JpmYn2 { get; set; } = null!;

    public string JpmYn3 { get; set; } = null!;

    public string JpmYn4 { get; set; } = null!;

    public string JpmYn5 { get; set; } = null!;

    public string RatingModel { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string JpmCatItemSrc { get; set; } = null!;

    public string CmCategory { get; set; } = null!;

    public decimal JpmDuration1 { get; set; }

    public string JpmDurationType1 { get; set; } = null!;

    public decimal JpmDuration2 { get; set; }

    public string JpmDurationType2 { get; set; } = null!;

    public string EducationLvl { get; set; } = null!;

    public string NvqLevel { get; set; } = null!;

    public string HpStatsDegLvl { get; set; } = null!;

    public string FpDegreeLvl { get; set; } = null!;

    public string SatisfactionMthd { get; set; } = null!;

    public string EpSubLevel { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
