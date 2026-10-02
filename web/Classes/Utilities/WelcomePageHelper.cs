using System.Collections.Frozen;
using System.Globalization;

namespace Viper.Classes.Utilities;

/// <summary>
/// Pure helpers for the unauthenticated Welcome (landing) page.
/// </summary>
public static class WelcomePageHelper
{
    private static readonly FrozenDictionary<string, string> AreaLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["RAPS"] = "RAPS",
            ["Effort"] = "Effort Reporting",
            ["ClinicalScheduler"] = "Clinical Scheduler",
            ["CTS"] = "Competency Tracking System",
            ["Directory"] = "Directory",
            ["CMS"] = "CMS",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolve a human-readable destination label for the welcome page's deep-link indicator.
    /// Returns null for missing, root-only, or non-local URLs. The check mirrors Url.IsLocalUrl semantics
    /// so the helper is safe to call directly without prior validation.
    /// </summary>
    public static string? ResolveDestinationLabel(string? returnUrl)
    {
        if (!IsLocalUrl(returnUrl))
        {
            return null;
        }

        // After IsLocalUrl, returnUrl is non-null and non-empty.
        var path = returnUrl!;

        int queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        // Strip a leading ~ (tilde-rooted app path) as well as /, so ~/Area resolves like /Area.
        path = path.TrimStart('~', '/');
        if (path.Length == 0)
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        if (AreaLabels.TryGetValue(segments[0], out var label))
        {
            return label;
        }

        return ToTitleCase(segments[^1]);
    }

    /// <summary>
    /// Defensive local-URL check matching Url.IsLocalUrl semantics. Rejects null/empty, absolute URLs
    /// with a scheme, scheme-relative URLs (//host), and anything not beginning with a single '/'.
    /// </summary>
    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        if (url[0] == '/')
        {
            // Reject scheme-relative URLs like //evil.com/x and //\evil.com
            if (url.Length == 1)
            {
                return true;
            }
            return url[1] != '/' && url[1] != '\\';
        }

        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            // Reject ~// and ~/\ for parity with Url.IsLocalUrl semantics
            if (url.Length == 2)
            {
                return true;
            }
            return url[2] != '/' && url[2] != '\\';
        }

        return false;
    }

    private static string ToTitleCase(string value)
    {
        return char.ToUpper(value[0], CultureInfo.InvariantCulture) + value[1..];
    }
}
