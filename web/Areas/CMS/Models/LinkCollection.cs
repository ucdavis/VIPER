
namespace Areas.CMS.Models
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;
	

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
		public virtual ICollection<LinkCollectionTagCategory> TagCategories { get; set; } = new List<LinkCollectionTagCategory>();
	}
}