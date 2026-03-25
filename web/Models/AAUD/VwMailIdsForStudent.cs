namespace Viper.Models.AAUD;

public partial class VwMailIdsForStudent
{
    public string? IdsMailid { get; set; }

    public string PersonLastName { get; set; } = null!;

    public string? PersonDisplayFirstName { get; set; }

    public string PersonFirstName { get; set; } = null!;

    public string? StudentsClassLevel { get; set; }

    public string StudentsTermCode { get; set; } = null!;

    public string StudentsMajorCode1 { get; set; } = null!;

    public string? StudentsMajorCode2 { get; set; }

    public string StudentsDegreeCode1 { get; set; } = null!;

    public string? StudentsDegreeCode2 { get; set; }
}
