namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Class for combining ViperPerson and PhonePerson results for name searches.
    /// </summary>
    public class AugmentedViperPerson
    {
        public int PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string IamId { get; set; } = string.Empty;
        public required bool CurrentEmployee { get; set; } = false;
        public string MailId { get; set; } = string.Empty;
        public PhonePerson? PhoneData { get; set; } = null;

        public void AddPhoneData(PhonePerson phonePerson)
        {
            this.PhoneData = phonePerson;
        }
    }
}
