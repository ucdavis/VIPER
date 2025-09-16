using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

public class LinkDto
{
	public int LinkId { get; set; }
	public int LinkCollectionId { get; set; }
	public string Url { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int SortOrder { get; set; }
	public List<LinkTagDto> LinkTags { get; set; } = new();
}
