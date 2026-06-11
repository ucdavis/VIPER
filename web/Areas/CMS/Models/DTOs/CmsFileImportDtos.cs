using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.CMS.Models.DTOs
{
    /// <summary>
    /// Import existing files from the legacy VIPER webroot into the managed file store.
    /// Paths are relative to the configured legacy webroot.
    /// </summary>
    public class CmsFileImportRequest
    {
        [Required]
        public List<string> FilePaths { get; set; } = new();

        [Required]
        public string Folder { get; set; } = string.Empty;

        public List<string> Permissions { get; set; } = new();

        /// <summary>Add the folder's default permission (SVMSecure.{folder}) to each file.</summary>
        public bool? UseDefaultPermission { get; set; }

        public bool? AllowPublicAccess { get; set; }

        public bool? Encrypt { get; set; }
    }

    public class CmsFileImportResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? FileGuid { get; set; }
        public string? FriendlyName { get; set; }
    }

    public class CmsBulkEncryptResult
    {
        public Guid FileGuid { get; set; }
        public string? FriendlyName { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
