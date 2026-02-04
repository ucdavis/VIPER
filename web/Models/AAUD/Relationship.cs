namespace Viper.Models.AAUD;

public partial class Relationship
{
    public int RelId { get; set; }

    public int? RelTypeId { get; set; }

    public string? RelChildMothraId { get; set; }

    public string? RelParentMothraId { get; set; }
}
