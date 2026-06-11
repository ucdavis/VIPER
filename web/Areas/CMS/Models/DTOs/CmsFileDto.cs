namespace Viper.Areas.CMS.Models.DTOs
{
    public class CmsFileDto
    {
        public Guid FileGuid { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Folder { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public bool Encrypted { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool AllowPublicAccess { get; set; }
        public string? OldUrl { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime? DeletedOn { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<CmsFilePersonDto> People { get; set; } = new();
        public string Url { get; set; } = string.Empty;
        public string FriendlyUrl { get; set; } = string.Empty;
    }

    public class CmsFilePersonDto
    {
        public string IamId { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
