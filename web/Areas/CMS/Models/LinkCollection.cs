
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Areas.CMS.Models
{
    /// <summary>
    /// Represents a collection of links.
    /// </summary>
    [Table("LinkCollection")]
    public class LinkCollection
    {
        [Key]
        public int LinkCollectionId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("LinkCollection")]
        public string LinkCollectionName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Link> Links { get; set; } = new List<Link>();
        public virtual ICollection<LinkCollectionTagCategory> LinkCollectionTagCategories { get; set; } = new List<LinkCollectionTagCategory>();
    }
}
