namespace Viper.Areas.Students.Models;

public class AppAccessStatusDto
{
    public bool AppOpen { get; set; }
    public List<IndividualAccessDto> IndividualGrants { get; set; } = new();
}

public class IndividualAccessDto
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
