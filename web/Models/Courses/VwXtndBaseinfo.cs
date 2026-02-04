namespace Viper.Models.Courses;

public partial class VwXtndBaseinfo
{
    public string BaseinfoPkey { get; set; } = null!;

    public string BaseinfoTermCode { get; set; } = null!;

    public string BaseinfoCrn { get; set; } = null!;

    public string BaseinfoSubjCode { get; set; } = null!;

    public string BaseinfoCrseNumb { get; set; } = null!;

    public string BaseinfoSeqNumb { get; set; } = null!;

    public short BaseinfoEnrollment { get; set; }

    public string BaseinfoCollCode { get; set; } = null!;

    public string BaseinfoDeptCode { get; set; } = null!;

    public string PoaPidm { get; set; } = null!;

    public string PoaClientid { get; set; } = null!;

    public string? PoaMailid { get; set; }

    public string? CustodialDeptCode { get; set; }
}
