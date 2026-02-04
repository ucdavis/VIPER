using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;


public class CreateLinkCollectionTagCategoryDto
{
    [Required]
    public required int LinkCollectionId { get; set; }

    [Required]
    [StringLength(100)]
    public string LinkCollectionTagCategory { get; set; } = string.Empty;

    [Required]
    public required int SortOrder { get; set; }
}
