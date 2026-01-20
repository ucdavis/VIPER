namespace Viper.Models.Dictionary;

/// <summary>
/// Entity for dictionary.dbo.dvtTitle table.
/// Contains title codes and their descriptions for faculty/staff positions.
/// </summary>
public class DvtTitle
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Abbreviation { get; set; }
    public string? JobGroupId { get; set; }
    public string? JobGroupName { get; set; }
}
