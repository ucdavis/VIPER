using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

public class LinkTagDto
{
	public int LinkTagId { get; set; }
	public int LinkId { get; set; }
	public int LinkCollectionTagCategoryId { get; set; }
	public int SortOrder { get; set; }
	public string? Value { get; set; }
	public string? CategoryName { get; set; }
}