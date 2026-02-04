namespace Viper.Models.AAUD;

public partial class DlJob
{
    public string JobPKey { get; set; } = null!;

    public int JobSeqNum { get; set; }

    public string JobTermCode { get; set; } = null!;

    public string JobClientid { get; set; } = null!;

    public string JobDepartmentCode { get; set; } = null!;

    public decimal JobPercentFulltime { get; set; }

    public string JobTitleCode { get; set; } = null!;

    public string JobBargainingUnit { get; set; } = null!;

    public string? JobSchoolDivision { get; set; }
}
