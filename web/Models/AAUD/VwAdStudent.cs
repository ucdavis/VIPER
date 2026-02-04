namespace Viper.Models.AAUD;

public partial class VwAdStudent
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? PersonMiddleName { get; set; }

    public string? Loginid { get; set; }

    public string PersonTermCode { get; set; } = null!;

    public string? LevelCode { get; set; }
}
