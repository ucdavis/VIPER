namespace Viper.Areas.CMS.Models
{
    public class CMSBlockAddEdit
    {
        public required int ContentBlockId { get; set; }

        public string Content { get; set; } = null!;

        public string? Title { get; set; }

        public string System { get; set; } = null!;

        public string? Application { get; set; }

        public string? Page { get; set; }

        public string? ViperSectionPath { get; set; }

        public int? BlockOrder { get; set; }

        public string? FriendlyName { get; set; }

        public required bool AllowPublicAccess { get; set; }

        public ICollection<string> Permissions { get; set; } = new List<string>();
    }
}
