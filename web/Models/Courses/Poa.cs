namespace Viper.Models.Courses;

public partial class Poa
{
    public string PoaPkey { get; set; } = null!;

    public string PoaTermCode { get; set; } = null!;

    public string PoaCrn { get; set; } = null!;

    public string PoaPidm { get; set; } = null!;

    public int PoaRole { get; set; }
}
