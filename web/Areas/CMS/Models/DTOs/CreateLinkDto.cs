using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Models.DTOs;

public class CreateLinkDto
{
    [Required]
    public required int LinkCollectionId { get; set; }

    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public required int SortOrder { get; set; }
}
