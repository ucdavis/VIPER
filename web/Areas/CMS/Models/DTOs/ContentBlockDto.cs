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
        public List<string> EditPermissions { get; set; } = new();
        public List<ContentBlockFileDto> Files { get; set; } = new();
    }

    /// <summary>
    /// Shape served by the anonymous display endpoint (content/fn/{friendlyName}). Carries only
    /// what public rendering needs; the full ContentBlockDto would disclose editor login ids,
    /// permission names, and placement metadata to unauthenticated callers.
    /// </summary>
    public class PublicContentBlockDto
    {
        public int ContentBlockId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? FriendlyName { get; set; }
    }

    public class ContentBlockFileDto
    {
        public Guid FileGuid { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Minimal shape for the editor's attach-by-search picker. Carries only the file's guid and
    /// friendly name so delegated editors (who lack AllFiles) never see server paths, permission
    /// lists, or people from the managed file store; the file's own download-time permission checks
    /// still govern who can open it.
    /// </summary>
    public class CmsAttachableFileDto
    {
        public Guid FileGuid { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
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

    /// <summary>
    /// A rendered HTML diff between two versions of a content block. Content holds the merged
    /// markup with htmldiff.net's ins/del (diffins/diffdel/diffmod) markers. The Old/New stamps
    /// describe each side so the viewer can label the comparison direction. HasComparison is false
    /// when there is no other version to compare against (e.g. the original version); Content then
    /// holds the version's own markup with no diff markers.
    /// </summary>
    public class ContentHistoryDiffDto
    {
        public string Content { get; set; } = string.Empty;
        public bool HasComparison { get; set; }
        // False when the two compared versions are identical (the diff carries no ins/del markers),
        // so the viewer can say "identical" instead of showing an unchanged body that looks broken.
        public bool HasChanges { get; set; }
        public DateTime? OldModifiedOn { get; set; }
        public string? OldModifiedBy { get; set; }
        public DateTime? NewModifiedOn { get; set; }
        public string? NewModifiedBy { get; set; }
    }

    /// <summary>
    /// A single cross-block edit-history entry: a superseded content version with the block
    /// it belongs to. Used by the content-block edit-history viewer.
    /// </summary>
    public class ContentHistoryAuditDto
    {
        public int ContentHistoryId { get; set; }
        public int ContentBlockId { get; set; }
        public string? Title { get; set; }
        public string? FriendlyName { get; set; }
        public string? Page { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public bool BlockDeleted { get; set; }
    }
}
