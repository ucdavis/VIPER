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

        /// <summary>
        /// RAPS permissions that authorize delegated editing of this block's content and files.
        /// Manager-only to change (the full-update path); empty means manager-only editing.
        /// </summary>
        public ICollection<string> EditPermissions { get; set; } = new List<string>();

        public List<Guid> FileGuids { get; set; } = new();

        /// <summary>
        /// ModifiedOn value the client loaded; updates with a stale value get 409 Conflict.
        /// Required for updates (null gets 400); ignored on create.
        /// </summary>
        public DateTime? LastModifiedOn { get; set; }
    }
}
