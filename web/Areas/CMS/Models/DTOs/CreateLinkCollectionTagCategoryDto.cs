using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;


public class CreateLinkCollectionTagCategoryDto
{
	[Required]
	public int LinkCollectionId { get; set; }

	[Required]
	[StringLength(100)]
	public string LinkCollectionTagCategory { get; set; } = string.Empty;

	[Required]
	public int SortOrder { get; set; }
}
