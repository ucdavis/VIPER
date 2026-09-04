using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents data from phones.SVMFrequentNumber.
    /// Provides a spot for additional important phone
    /// numbers not tied to a specific person.
    /// </summary>
    public class SVMFrequentNumber
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NumberId { get; set; }
        public required string Label { get; set; }
        public required string Phone { get; set; }
        public int? SortOrder { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public virtual ViperPerson? ViperModPerson { get; set; }
    }
}
