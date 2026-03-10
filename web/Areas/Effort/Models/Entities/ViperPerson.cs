namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Read-only entity for accessing users.Person table within EffortDbContext.
/// Used for joining to get sender names without cross-context queries.
/// </summary>
public class ViperPerson
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? MailId { get; set; }
    public string MothraId { get; set; } = string.Empty;
}
