namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Read-only entity for accessing users.Person table within PhonesDbContext.
    /// Used for joining to get employee names without cross-context queries.
    /// </summary>
    public class ViperPerson
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public required string IamId { get; set; }
        public required bool CurrentEmployee { get; set; } = false;
        public string MailId { get; set; } = string.Empty;
    }
}
