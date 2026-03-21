namespace Viper.Models.AAUD;

public partial class VwJobsForAaud
{
    public string? Emplid { get; set; }

    public string? Ppsid { get; set; }

    public DateTime Effdt { get; set; }

    public DateTime? ExpectedEndDate { get; set; }

    public decimal EmplRcd { get; set; }

    public decimal Effseq { get; set; }

    public string Jobcode { get; set; } = null!;

    public string DeptCd { get; set; } = null!;

    public string DeptDesc { get; set; } = null!;

    public string SubDivCd { get; set; } = null!;

    public string UnionCd { get; set; } = null!;

    public decimal AnnualRt { get; set; }

    public decimal Fte { get; set; }

    public string JobStatus { get; set; } = null!;

    public string? Primaryindex { get; set; }

    public int EffDateActive { get; set; }
}
