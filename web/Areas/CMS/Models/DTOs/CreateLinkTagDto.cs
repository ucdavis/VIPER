using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

public class CreateLinkTagDto
{
	[Required]
	public int LinkId { get; set; }

	[Required]
	public int LinkCollectionTagCategoryId { get; set; }

	[Required]
	public int SortOrder { get; set; }

	[StringLength(50)]
	public string? Value { get; set; }
}