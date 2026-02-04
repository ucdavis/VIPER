namespace Areas.CMS.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a category of tags within a link collection.
    /// </summary>
    [Table("LinkCollectionTagCategory")]
    public class LinkCollectionTagCategory
    {
        [Key]
        public int LinkCollectionTagCategoryId { get; set; }

        [Required]
        public int LinkCollectionId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("LinkCollectionTagCategory")]
        public string LinkCollectionTagCategoryName { get; set; } = string.Empty;

        [Required]
        public int SortOrder { get; set; }

        // Navigation properties
        [ForeignKey("LinkCollectionId")]
        public virtual LinkCollection LinkCollection { get; set; } = null!;
        public virtual ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
    }
}
