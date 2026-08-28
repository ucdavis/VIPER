namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents data from phones.Person.
    /// Ties a person to phone number and office data.
    /// </summary>
    public class PhonePerson
    {
        public required string PersonIam { get; set; }
        public string? Phone { get; set; }
        public string? DirectPhone { get; set; }
        public string? Office { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public virtual ICollection<SVMUnitPerson> UnitPersons { get; set; } = [];
        public virtual ICollection<PhoneListUnitPerson> PhoneListUnitPersons { get; set; } = [];
        public virtual ViperPerson? ViperPerson { get; set; }
        public virtual ViperPerson? ViperModPerson { get; set; }
    }
}
