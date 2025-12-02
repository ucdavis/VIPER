using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

// Create/Update DTOs
public class CreateLinkCollectionDto
{
	[Required]
	[StringLength(100)]
	public string LinkCollection { get; set; } = string.Empty;
}