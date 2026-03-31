namespace Viper.Models.AAUD;

public partial class DlGradEmployee
{
    public string GradEmpPKey { get; set; } = null!;

    public string GradEmpTermCode { get; set; } = null!;

    public string GradEmpClientid { get; set; } = null!;

    public string GradEmpHomeDept { get; set; } = null!;

    public string? GradEmpAltDeptCode { get; set; }

    public string GradEmpSchoolDivision { get; set; } = null!;

    public string GradEmpCbuc { get; set; } = null!;

    public string GradEmpStatus { get; set; } = null!;

    public string? GradEmpPrimaryTitle { get; set; }

    public string? GradEmpTeachingTitleCode { get; set; }

    public string? GradEmpTeachingHomeDept { get; set; }

    public decimal? GradEmpTeachingPercentFulltime { get; set; }

    public string? GradEmpEffortTitleCode { get; set; }

    public string? GradEmpEffortHomeDept { get; set; }
}
