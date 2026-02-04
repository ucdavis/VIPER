namespace Viper.Models.AAUD;

public partial class VwAdconstituent
{
    public string? LoginId { get; set; }

    public string DisplayLastName { get; set; } = null!;

    public string DisplayFirstName { get; set; } = null!;

    public string? EmailAddress { get; set; }

    public bool Student { get; set; }

    public int Staff { get; set; }

    public int Faculty { get; set; }

    public string? HomeDept { get; set; }

    public string? TeachingDept { get; set; }

    public string? AltDept { get; set; }

    public string? EffortDept { get; set; }

    public string? Title { get; set; }

    public string? TeachingTitle { get; set; }

    public string? EffortTitle { get; set; }
}
