namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a person in the evalHarvest database.
/// Maps to dbo.eh_People table.
/// </summary>
public class EhPerson
{
    public string MailId { get; set; } = string.Empty;
    public int TermCode { get; set; }
    public string MothraId { get; set; } = string.Empty;
    public string LoginId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TeachingDept { get; set; } = string.Empty;
}
