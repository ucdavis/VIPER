using Viper.Models.VIPER;

namespace Viper.Models
{
    /// <summary>
    /// This is a basic person object, containing id, name and email address
    /// </summary>
    public class PersonSimple
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MailId { get; set; } = null!;

        public string FullName => $"{FirstName} {LastName}";
        public string FullNameLastFirst => $"{LastName}, {FirstName}";
        public string Email => $"{MailId}@ucdavis.edu";

        public PersonSimple() { }
        public PersonSimple(Person p)
        {
            PersonId = p.PersonId;
            FirstName = p.FirstName;
            LastName = p.LastName;
            MailId = p.MailId;
        }
    }
}
