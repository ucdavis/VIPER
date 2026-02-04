namespace Areas.CMS.Models.DTOs;

public class UpdateLinkOrderDto
{
    public required int LinkId { get; set; }
    public required int SortOrder { get; set; }
}
