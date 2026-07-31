namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Helpers shared by the CMS write services (files, content blocks, left navs) so the
    /// optimistic-concurrency contract and permission-list normalization cannot drift between
    /// copies. Each service wraps these with its own entity-typed one-liner.
    /// </summary>
    internal static class CmsServiceHelpers
    {
        /// <summary>
        /// A missing stamp is a 400 (the client must send it) and a stale one is a 409 (someone
        /// saved after the editor loaded). Compared to the second: serialized timestamps lose
        /// sub-second precision round-tripping through the client.
        /// </summary>
        public static void AssertNotStale(string noun, DateTime modifiedOn, string? modifiedBy, DateTime? lastModifiedOn)
        {
            if (lastModifiedOn == null)
            {
                throw new ArgumentException("LastModifiedOn is required so concurrent edits can be detected.");
            }
            if (Math.Abs((modifiedOn - lastModifiedOn.Value).TotalSeconds) >= 1)
            {
                throw new CmsConcurrencyException(
                    $"This {noun} was modified by {modifiedBy} on {modifiedOn:g}. Reload to get the latest version.");
            }
        }

        /// <summary>
        /// Trim, drop blanks, and de-duplicate case-insensitively. A client can post
        /// "permissions": null explicitly; treat it as empty rather than 500.
        /// </summary>
        public static List<string> CleanList(IEnumerable<string>? values)
        {
            return (values ?? [])
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
