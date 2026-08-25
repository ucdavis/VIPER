namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Represents an entry from phones.SVMSection,
    /// a grouping for the SVM Phone List.
    /// </summary>
    public class SVMSection
    {
        public required int SectionId { get; set; }
        public string? Name { get; set; }
        public bool? IncludeAbbrv { get; set; }
        public string? UnitName { get; set; }
        public string? DirectorTitle { get; set; }
        public int? SortOrder { get; set; }

        public virtual ICollection<SVMUnit> Units { get; set; } = [];
    }
}
