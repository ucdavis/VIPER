namespace Areas.CMS.Models
{
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;

	/// <summary>
	/// Represents a tag associated with a link within a specific tag category.
	/// </summary>}
	[Table("LinkTag")]
	public class LinkTag
	{
		[Key]
		public int LinkTagId { get; set; }

		[Required]
		public int LinkId { get; set; }

		[Required]
		public int LinkCollectionTagCategoryId { get; set; }

		[Required]
		public int SortOrder { get; set; }

		[StringLength(50)]
		public string? Value { get; set; }

		// Navigation properties
		[ForeignKey("LinkId")]
		public virtual Link Link { get; set; } = null!;

		[ForeignKey("LinkCollectionTagCategoryId")]
		public virtual LinkCollectionTagCategory LinkCollectionTagCategory { get; set; } = null!;
	}
}