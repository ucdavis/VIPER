using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

public class LinkCollectionDto
{
	public int LinkCollectionId { get; set; }
	public string LinkCollection { get; set; } = string.Empty;
	public List<LinkCollectionTagCategoryDto> TagCategories { get; set; } = new();
}

