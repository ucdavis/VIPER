using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.PhoneList,
    /// a (typically unit/department-level) grouping for phone numbers.
    /// </summary>
    public class PhoneList
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhoneListId { get; set; }

        /**
         * Stable lookup key used in routes and API paths (e.g. "VMDO").
         * Allows changing Name without breaking links.
         */
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string MaintainRole { get; set; }

        public virtual ICollection<PhoneListUnit> PhoneListUnits { get; set; } = [];
    }
}
