namespace Areas.CMS.Models.DTOs;

public class LinkCollectionTagCategoryDto
{
    public int LinkCollectionTagCategoryId { get; set; }
    public int LinkCollectionId { get; set; }
    public string LinkCollectionTagCategory { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
