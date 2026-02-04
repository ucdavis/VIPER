namespace Viper.Models.AAUD;

public partial class VwUConnect
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? Loginid { get; set; }

    public string? EmpHomeDept { get; set; }

    public string? EmpAltDeptCode { get; set; }

    public int? VmdoPeopleUnitId { get; set; }

    public bool FlagsStaff { get; set; }

    public bool FlagsAcademic { get; set; }

    public bool FlagsStudent { get; set; }

    public bool FlagsTeachingFaculty { get; set; }

    public bool FlagsSvmStudent { get; set; }

    public string? StudentsClassLevel { get; set; }

    public string PersonTermCode { get; set; } = null!;

    public int? IdCardNumber { get; set; }
}
