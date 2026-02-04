namespace Viper.Models.Courses;

public partial class DlBaseinfo
{
    public string BaseinfoPkey { get; set; } = null!;

    public string BaseinfoTermCode { get; set; } = null!;

    public string BaseinfoCrn { get; set; } = null!;

    public string BaseinfoSubjCode { get; set; } = null!;

    public string BaseinfoCrseNumb { get; set; } = null!;

    public string BaseinfoSeqNumb { get; set; } = null!;

    public string BaseinfoTitle { get; set; } = null!;

    public short BaseinfoEnrollment { get; set; }

    public string BaseinfoUnitType { get; set; } = null!;

    public decimal BaseinfoUnitLow { get; set; }

    public decimal BaseinfoUnitHigh { get; set; }

    public string BaseinfoCollCode { get; set; } = null!;

    public string BaseinfoDeptCode { get; set; } = null!;

    public string BaseinfoXlistFlag { get; set; } = null!;

    public string BaseinfoXlistGroup { get; set; } = null!;

    public string? BaseinfoDescTitle { get; set; }
}
