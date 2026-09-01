using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.SVMUnitPerson,
    /// connecting people in leadership and admin roles to a 
    /// given unit.
    /// </summary>
    public class SVMUnitPerson
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UnitPersonId { get; set; }
        public required int UnitId { get; set; }
        public required string PersonIam { get; set; }
        public string? Office { get; set; }
        public string? PosType { get; set; }
        public string? Interim { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }

        public virtual SVMUnit Unit { get; set; } = null!;
        public virtual PhonePerson Person { get; set; } = null!;
        public virtual ViperPerson? ViperModPerson { get; set; }
    }
}
