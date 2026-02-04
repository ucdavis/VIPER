namespace Areas.CMS.Models.DTOs;

public class UpdateLinkCollectionTagCategoryOrderDto
{
    public required int LinkCollectionTagCategoryId { get; set; }
    public required int SortOrder { get; set; }
}
