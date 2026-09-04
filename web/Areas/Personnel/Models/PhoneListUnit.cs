using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.PhoneListUnit,
    /// a grouping within a phone list.
    /// </summary>
    public class PhoneListUnit
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhoneListUnitId { get; set; }
        public required int PhoneListId { get; set; }
        public required string Name { get; set; }
        public int? SortOrder { get; set; }

        public virtual PhoneList PhoneList { get; set; } = null!;
        public virtual ICollection<PhoneListUnitPerson> PhoneListUnitPersons { get; set; } = [];
    }
}
