namespace Viper.Models.AAUD;

public partial class DlGradPerson
{
    public string GradPersonPKey { get; set; } = null!;

    public string GradPersonTermCode { get; set; } = null!;

    public string GradPersonClientid { get; set; } = null!;

    public string GradPersonLastName { get; set; } = null!;

    public string GradPersonFirstName { get; set; } = null!;

    public string? GradPersonMiddleName { get; set; }

    public string? GradPersonDisplayLastName { get; set; }

    public string? GradPersonDisplayFirstName { get; set; }

    public string? GradPersonDisplayMiddleName { get; set; }

    public string? GradPersonDisplayFullName { get; set; }
}
