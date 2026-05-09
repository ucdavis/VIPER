using System.Text.RegularExpressions;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Path-safety primitives for CMS file download flows (VPR-138).
    ///
    /// Two surfaces: the static class is used by the legacy
    /// <c>CMS.DownloadZip</c> call site (no DI plumbing); the
    /// <see cref="ICmsFilePathSafety"/> interface is the contract the
    /// PLAN-CMS migration's <c>IFileStorageService</c> / new download
    /// controller depends on. See PLAN-CMS.md §11.7.
    /// </summary>
    public static class CmsFilePathSafety
    {
        private const string DefaultDownloadName = "FileDownload.zip";

        private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static readonly Regex SafeFileNameAllowList = new(@"[^a-zA-Z0-9._\- ]", RegexOptions.Compiled);

        /// <summary>
        /// Returns a filename safe to use in a Content-Disposition response header.
        /// Strips path components, applies an allow-list, rejects reserved Windows
        /// device names, and guarantees a <c>.zip</c> suffix so the name matches
        /// the <c>application/zip</c> MIME type.
        /// </summary>
        public static string SanitizeDownloadName(string? userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return DefaultDownloadName;
            }

            var fileNamePart = StripPathComponents(userInput);
            var filtered = SafeFileNameAllowList.Replace(fileNamePart, string.Empty).Trim();

            // Reject names that collapse to only dots/spaces: ".", ".." etc. would
            // become "..zip" after the suffix step, which is traversal-shaped.
            if (filtered.Trim('.', ' ').Length == 0)
            {
                return DefaultDownloadName;
            }

            var stem = filtered;
            var dotIndex = stem.IndexOf('.');
            if (dotIndex >= 0)
            {
                stem = stem[..dotIndex];
            }
            if (ReservedWindowsNames.Contains(stem))
            {
                return DefaultDownloadName;
            }

            if (!filtered.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                filtered += ".zip";
            }

            return filtered;
        }

        /// <summary>
        /// Builds a per-request temp archive path under <paramref name="tempRoot"/>
        /// using a server-generated GUID, and asserts the resolved path stays
        /// inside that root.
        /// </summary>
        public static string BuildTempArchivePath(string tempRoot)
        {
            if (string.IsNullOrWhiteSpace(tempRoot))
            {
                throw new ArgumentException("Temp root must be provided.", nameof(tempRoot));
            }

            var resolvedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(tempRoot));
            var candidate = Path.Join(resolvedRoot, Guid.NewGuid().ToString("N") + ".zip");
            var resolved = Path.GetFullPath(candidate);

            if (!resolved.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Generated temp path escaped the configured root.");
            }

            return resolved;
        }

        /// <summary>
        /// Dedicated temp directory for ZIP archives. Kept separate from the
        /// CMS content root so transient archives never mix with managed
        /// content, and never get served through content URLs.
        /// </summary>
        public static string GetZipTempFolder()
        {
            return Path.Join(Path.GetTempPath(), "Viper-CMS");
        }

        /// <summary>
        /// Returns a safe ZIP entry name from a stored file's friendly name.
        /// Defense in depth against ZIP-slip when the archive is extracted.
        /// </summary>
        public static string SanitizeZipEntryName(string? friendlyName, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(friendlyName))
            {
                var entry = StripPathComponents(friendlyName);
                if (!string.IsNullOrWhiteSpace(entry) && entry.Trim('.', ' ').Length > 0)
                {
                    return entry;
                }
            }

            return StripPathComponents(fallback);
        }

        // Path.GetFileName only honors the host OS separator, so "\..\evil"
        // leaks through unchanged on Linux runners. Normalize first.
        private static string StripPathComponents(string input)
            => Path.GetFileName(input.Replace('\\', '/'));
    }

    /// <summary>
    /// DI-injectable contract for <see cref="CmsFilePathSafety"/>. New code
    /// (see PLAN-CMS.md §11.7) depends on this interface so the underlying
    /// implementation can evolve without touching call sites.
    /// </summary>
    public interface ICmsFilePathSafety
    {
        string SanitizeDownloadName(string? userInput);
        string BuildTempArchivePath(string tempRoot);
        string GetZipTempFolder();
        string SanitizeZipEntryName(string? friendlyName, string fallback);
    }

    /// <inheritdoc cref="ICmsFilePathSafety"/>
    public sealed class CmsFilePathSafetyService : ICmsFilePathSafety
    {
        public string SanitizeDownloadName(string? userInput) =>
            CmsFilePathSafety.SanitizeDownloadName(userInput);

        public string BuildTempArchivePath(string tempRoot) =>
            CmsFilePathSafety.BuildTempArchivePath(tempRoot);

        public string GetZipTempFolder() =>
            CmsFilePathSafety.GetZipTempFolder();

        public string SanitizeZipEntryName(string? friendlyName, string fallback) =>
            CmsFilePathSafety.SanitizeZipEntryName(friendlyName, fallback);
    }
}
