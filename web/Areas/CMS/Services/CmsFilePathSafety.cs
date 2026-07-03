using System.Text.RegularExpressions;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Path-safety primitives for CMS file download flows (VPR-138).
    ///
    /// Two surfaces: the static class is used by the legacy
    /// <c>CMS.DownloadZip</c> call site (no DI plumbing); the
    /// <see cref="ICmsFilePathSafetyService"/> interface is the contract the
    /// PLAN-CMS migration's <c>IFileStorageService</c> / new download
    /// controller depends on. See PLAN-CMS.md §11.7.
    /// </summary>
    public static class CmsFilePathSafety
    {
        /// <summary>
        /// Comparison for path-containment checks: Windows paths are case-insensitive, but on a
        /// case-sensitive filesystem an ignore-case match would treat a differently-cased sibling
        /// (e.g. /srv/Files vs /srv/files) as under the root and weaken the containment guarantee.
        /// </summary>
        public static StringComparison PathComparison =>
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private const string DefaultDownloadName = "FileDownload.zip";

        private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static readonly Regex DisallowedFileNameChars = new(@"[^a-zA-Z0-9._\- ]", RegexOptions.Compiled);

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
            var filtered = DisallowedFileNameChars.Replace(fileNamePart, string.Empty).Trim();

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
        /// True when <paramref name="userInput"/> carried path structure: a path
        /// separator, or ".." as a distinct segment. Used purely to log a
        /// traversal-shaped download name; it never affects what is served (the file
        /// set comes from DB GUIDs and the temp path from a server-generated GUID, so a
        /// hostile name cannot reach the filesystem). A ".." inside a longer name
        /// (e.g. "report..final.zip") is not treated as traversal.
        /// </summary>
        public static bool LooksLikeTraversalAttempt(string? userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return false;
            }

            // A download name should be a single segment: any separator, or a bare ".."
            // segment, is traversal-shaped. ".." inside a name (e.g. "report..final") is not.
            var normalized = userInput.Replace('\\', '/');
            return normalized.Contains('/') || normalized == "..";
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

            // TrimEndingDirectorySeparator leaves a filesystem root (e.g. "C:\" or "/")
            // with its trailing separator, so append one only when it isn't already there
            // to avoid a doubled separator that would break the containment check.
            var rootPrefix = Path.EndsInDirectorySeparator(resolvedRoot)
                ? resolvedRoot
                : resolvedRoot + Path.DirectorySeparatorChar;

            if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
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
    public interface ICmsFilePathSafetyService
    {
        string SanitizeDownloadName(string? userInput);
        bool LooksLikeTraversalAttempt(string? userInput);
        string BuildTempArchivePath(string tempRoot);
        string GetZipTempFolder();
        string SanitizeZipEntryName(string? friendlyName, string fallback);
    }

    /// <inheritdoc cref="ICmsFilePathSafetyService"/>
    public sealed class CmsFilePathSafetyService : ICmsFilePathSafetyService
    {
        public string SanitizeDownloadName(string? userInput) =>
            CmsFilePathSafety.SanitizeDownloadName(userInput);

        public bool LooksLikeTraversalAttempt(string? userInput) =>
            CmsFilePathSafety.LooksLikeTraversalAttempt(userInput);

        public string BuildTempArchivePath(string tempRoot) =>
            CmsFilePathSafety.BuildTempArchivePath(tempRoot);

        public string GetZipTempFolder() =>
            CmsFilePathSafety.GetZipTempFolder();

        public string SanitizeZipEntryName(string? friendlyName, string fallback) =>
            CmsFilePathSafety.SanitizeZipEntryName(friendlyName, fallback);
    }
}
