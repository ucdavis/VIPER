namespace Viper.Models.Dictionary;

/// <summary>
/// Entity for dictionary.dbo.dvtSVMUnit table.
/// Contains department codes and their simple names for the School of Veterinary Medicine.
/// </summary>
public class DvtSvmUnit
{
    public string? Code { get; set; }
    public string? SimpleName { get; set; }
    public int? ParentId { get; set; }
}
