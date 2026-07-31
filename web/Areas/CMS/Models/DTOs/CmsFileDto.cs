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

        /// <summary>When a trashed file will be permanently purged (DeletedOn + retention); null if not deleted.</summary>
        public DateTime? PurgeOn { get; set; }

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

    public class CmsFolderCountDto
    {
        public string Folder { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>Result of a pre-upload name availability check for a folder.</summary>
    public class CmsFileNameCheckDto
    {
        public bool InUse { get; set; }

        /// <summary>The name an auto-rename would store the file under (the name itself when free).</summary>
        public string SuggestedName { get; set; } = string.Empty;

        /// <summary>Set when the conflicting name belongs to an existing file record.</summary>
        public Guid? ExistingFileGuid { get; set; }

        public string? ExistingFriendlyName { get; set; }

        public bool ExistingDeleted { get; set; }

        /// <summary>
        /// ModifiedOn of the conflicting record, echoed back as LastModifiedOn when the user
        /// chooses to overwrite it, so the overwrite hits the same 409 stale-edit guard as a
        /// metadata edit if the record changed after this check.
        /// </summary>
        public DateTime? ExistingModifiedOn { get; set; }
    }
}
