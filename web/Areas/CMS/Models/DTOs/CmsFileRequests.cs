using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.CMS.Models.DTOs
{
    /// <summary>
    /// Form fields accompanying a new file upload (multipart/form-data).
    /// </summary>
    public class CmsFileCreateRequest
    {
        [Required]
        public string Folder { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool? AllowPublicAccess { get; set; }

        [MaxLength(256)]
        public string? OldUrl { get; set; }

        public List<string> Permissions { get; set; } = new();

        public List<string> IamIds { get; set; } = new();

        public bool? Encrypt { get; set; }

        /// <summary>Optional stored-name override; defaults to the uploaded file's name.</summary>
        [MaxLength(256)]
        public string? FileName { get; set; }

        /// <summary>
        /// When true, overwrite a same-named disk file that has no file record. A conflict with
        /// a managed record still fails (replace it by editing that record instead).
        /// </summary>
        public bool? Overwrite { get; set; }
    }

    /// <summary>
    /// Form fields for editing file metadata; the file itself is an optional replacement upload.
    /// </summary>
    public class CmsFileUpdateRequest
    {
        /// <summary>
        /// ModifiedOn value the client loaded; updates with a stale value get 409 Conflict
        /// and a missing value gets 400 (mirrors CMSBlockAddEdit.LastModifiedOn).
        /// </summary>
        public DateTime? LastModifiedOn { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool? AllowPublicAccess { get; set; }

        [MaxLength(256)]
        public string? OldUrl { get; set; }

        public List<string> Permissions { get; set; } = new();

        public List<string> IamIds { get; set; } = new();

        public bool? Encrypt { get; set; }
    }
}
