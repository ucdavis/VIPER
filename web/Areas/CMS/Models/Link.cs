namespace Areas.CMS.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a link within a link collection.
    /// </summary>
    [Table("Link")]
    public class Link
    {
        [Key]
        public int LinkId { get; set; }

        [Required]
        public int LinkCollectionId { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int SortOrder { get; set; }

        // Navigation properties
        [ForeignKey("LinkCollectionId")]
        public virtual LinkCollection LinkCollection { get; set; } = null!;
        public virtual ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
    }
}
