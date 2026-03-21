namespace Viper.Models.AAUD;

public partial class VwEmployeesForAaud
{
    public string? Emplid { get; set; }

    public string? Ppsid { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string MiddleName { get; set; } = null!;

    public string? Academic { get; set; }

    public string? AcademicSenate { get; set; }

    public string? AcademicFederation { get; set; }

    public string? TeachingFaculty { get; set; }

    public string? Wosemp { get; set; }
}
