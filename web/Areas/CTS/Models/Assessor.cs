using Viper.Models.VIPER;

namespace Viper.Areas.CTS.Models
{
    public class Assessor
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MailId { get; set; } = null!;

        public string FullName => $"{FirstName} {LastName}";
        public string FullNameLastFirst => $"{LastName}, {FirstName}";

        public Assessor() { }
        public Assessor(Person p)
        {
            PersonId = p.PersonId;
            FirstName = p.FirstName;
            LastName = p.LastName;
            MailId = p.MailId;
        }
    }
}
