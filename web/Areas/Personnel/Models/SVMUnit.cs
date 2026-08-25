namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.SVMUnit,
    /// a department, unit, or dean's office.
    /// </summary>
    public class SVMUnit
    {
        public required int UnitId { get; set; }
        public required int SectionId { get; set; }
        public string? Name { get; set; }
        public string? Abbrv { get; set; }
        public int? SortOrder { get; set; }
        public string? Fax { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual SVMSection Section { get; set; } = null!;
        public virtual ICollection<SVMUnitPerson> UnitPersons { get; set; } = [];
        public virtual ViperPerson? ViperModPerson { get; set; }
    }
}
