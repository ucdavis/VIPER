namespace Viper.Areas.Students.Models;

/// <summary>
/// One career selection dropdown option, served to the form and the admin page alike. The three
/// option tables name their id and label columns differently; this flattens them to one shape.
/// </summary>
public class CareerSelectionOptionDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsOther { get; set; }
    /// <summary>
    /// How many career selections reference the option. Anything above zero blocks deletion,
    /// since the selections hold a foreign key to it. Only populated for admins; zero otherwise.
    /// </summary>
    public int UsageCount { get; set; }
}
