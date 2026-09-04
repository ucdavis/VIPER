using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.PhoneListUnitPerson,
    /// connecting people to a given unit for a phone list.
    /// </summary>
    public class PhoneListUnitPerson
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhoneListUnitPersonId { get; set; }
        public required int PhoneListUnitId { get; set; }
        public required string PersonIam { get; set; }
        public required bool ListFirst { get; set; }
        public required bool IsActive { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual PhoneListUnit PhoneListUnit { get; set; } = null!;
        public virtual PhonePerson Person { get; set; } = null!;
        public virtual ViperPerson? ViperModPerson { get; set; }
    }
}
