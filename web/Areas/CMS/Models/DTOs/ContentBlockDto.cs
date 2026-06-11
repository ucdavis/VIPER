namespace Viper.Areas.CMS.Models.DTOs
{
    public class ContentBlockDto
    {
        public int ContentBlockId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string System { get; set; } = string.Empty;
        public string? Application { get; set; }
        public string? Page { get; set; }
        public string? ViperSectionPath { get; set; }
        public int? BlockOrder { get; set; }
        public string? FriendlyName { get; set; }
        public bool AllowPublicAccess { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime? DeletedOn { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<ContentBlockFileDto> Files { get; set; } = new();
    }

    public class ContentBlockFileDto
    {
        public Guid FileGuid { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class ContentHistoryListItemDto
    {
        public int ContentHistoryId { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class ContentHistoryDto : ContentHistoryListItemDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
