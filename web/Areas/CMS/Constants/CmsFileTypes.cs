namespace Viper.Areas.CMS.Constants
{
    /// <summary>
    /// Allowed CMS file extensions and their MIME types. Mirrors the legacy
    /// ColdFusion Lookups.cfc allow-list; extensions not in this map are rejected on upload.
    /// </summary>
    public static class CmsFileTypes
    {
        public static readonly IReadOnlyDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pdf"] = "application/pdf",
            ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["doc"] = "application/msword",
            ["xls"] = "application/vnd.ms-excel",
            ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ["csv"] = "text/csv",
            ["ppt"] = "application/vnd.ms-powerpoint",
            ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ["pptm"] = "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ["txt"] = "text/plain",
            ["html"] = "application/xhtml+xml",
            ["gif"] = "image/gif",
            ["png"] = "image/png",
            ["jpg"] = "image/jpeg",
            ["jpeg"] = "image/jpeg",
            ["tiff"] = "image/tiff",
            ["mp3"] = "audio/mpeg",
            ["wav"] = "audio/wav",
            ["mp4"] = "video/mp4",
            ["webm"] = "video/webm",
            ["oft"] = "application/vnd.ms-outlook",
            ["eps"] = "application/postscript",
            ["zip"] = "application/zip",
            ["7z"] = "application/x-7z-compressed",
            ["dmg"] = "application/x-apple-diskimage",
            ["exe"] = "application/vnd.microsoft.portable-executable"
        };

        /// <summary>
        /// Extensions the browser would render as active markup (script execution / inline SVG)
        /// rather than download. Legacy .html files must stay downloadable, so instead of removing
        /// them from the allow-list we serve these as an attachment, never inline, to close the
        /// stored-XSS path from an uploaded .html/.svg running in the app origin.
        /// </summary>
        public static readonly IReadOnlySet<string> InlineUnsafeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "html", "htm", "xhtml", "svg"
        };

        public static bool IsAllowedFileName(string fileName)
        {
            return MimeTypes.ContainsKey(GetExtension(fileName));
        }

        /// <summary>True when a file must be served as a download rather than rendered inline.</summary>
        public static bool ShouldForceAttachment(string fileName)
        {
            return InlineUnsafeExtensions.Contains(GetExtension(fileName));
        }

        public static string GetMimeType(string fileName)
        {
            return MimeTypes.TryGetValue(GetExtension(fileName), out var mimeType)
                ? mimeType
                : "application/octet-stream";
        }

        private static string GetExtension(string fileName)
        {
            return Path.GetExtension(fileName).TrimStart('.');
        }
    }
}
