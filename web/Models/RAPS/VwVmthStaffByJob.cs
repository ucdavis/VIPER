namespace Viper.Models.RAPS;

public partial class VwVmthStaffByJob
{
    public string? MemberId { get; set; }

    public string Last { get; set; } = null!;

    public string First { get; set; } = null!;

    public string EmpHomeDept { get; set; } = null!;

    public string EmpAltDeptCode { get; set; } = null!;
}
