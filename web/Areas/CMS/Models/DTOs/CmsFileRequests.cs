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

        /// <summary>When true, auto-rename on name conflict (name_0..name_999) instead of failing.</summary>
        public bool? MakeUnique { get; set; }
    }

    /// <summary>
    /// Form fields for editing file metadata; the file itself is an optional replacement upload.
    /// </summary>
    public class CmsFileUpdateRequest
    {
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
