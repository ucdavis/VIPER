namespace Viper.Classes
{
    /// <summary>
    /// Works out which left nav item corresponds to the page being rendered.
    /// Page links (relative URLs) get the primary highlight; instance links (tilde-prefixed
    /// URLs that point to the same page in a different RAPS instance) get a secondary
    /// highlight. When only an instance link matches, it is promoted to the primary highlight.
    /// </summary>
    public static class LeftNavHighlight
    {
        /// <summary>
        /// Find the menu items to highlight for the current request.
        /// </summary>
        /// <param name="items">Menu items, in render order.</param>
        /// <param name="requestPath">The request path, e.g. "/raps/Viper/Rolelist".</param>
        /// <param name="resolveAppPath">Resolves a "~/" URL to an app path, i.e. IUrlHelper.Content.</param>
        /// <returns>Indexes into <paramref name="items"/>, or -1 when nothing matches.</returns>
        public static (int ActiveIndex, int SecondaryActiveIndex) FindActive(
            IReadOnlyList<NavMenuItem> items,
            string requestPath,
            Func<string, string?> resolveAppPath)
        {
            string currentPath = requestPath.TrimEnd('/');
            // Relative menu URLs resolve against the current page's directory,
            // e.g. "/raps/Viper/" from "/raps/Viper/Rolelist".
            string basePath = currentPath[..(currentPath.LastIndexOf('/') + 1)];
            int activePageIndex = -1;
            int activeInstanceIndex = -1;
            for (int i = 0; i < items.Count; i++)
            {
                NavMenuItem item = items[i];
                if (string.IsNullOrEmpty(item.MenuItemURL))
                {
                    continue;
                }

                // A child page has no nav entry of its own, so it highlights its parent item.
                bool matched = IsCurrentPage(item.MenuItemURL, currentPath, basePath, resolveAppPath)
                    || item.ChildPageURLs.Exists(childUrl => IsCurrentPage(childUrl, currentPath, basePath, resolveAppPath));
                if (!matched)
                {
                    continue;
                }

                bool isTilde = item.MenuItemURL.StartsWith('~');
                if (isTilde && activeInstanceIndex < 0)
                {
                    activeInstanceIndex = i;
                }
                else if (!isTilde && activePageIndex < 0)
                {
                    activePageIndex = i;
                }
            }

            return activePageIndex >= 0
                ? (activePageIndex, activeInstanceIndex)
                : (activeInstanceIndex, -1);
        }

        private static bool IsCurrentPage(string url, string currentPath, string basePath, Func<string, string?> resolveAppPath)
        {
            // Headers carry "" as their URL, and NavMenuItem coerces the CMS's nullable
            // Url column to "" on the way in, so empty is the only non-link value here.
            if (url.Length == 0)
            {
                return false;
            }

            string rawUrl = url.Split('?')[0].TrimEnd('/');
            if (rawUrl.Length == 0)
            {
                return false;
            }

            // Resolve the on-site forms first. The absolute-URL test below must not run
            // before these: on Unix, Uri parses a leading-slash path as an absolute
            // file:// URI, so testing it first classifies every root-relative menu URL
            // as off-site and highlights nothing.
            string resolvedPath;
            if (rawUrl.StartsWith('~'))
            {
                string? appPath = resolveAppPath(rawUrl);
                if (string.IsNullOrEmpty(appPath))
                {
                    return false;
                }
                resolvedPath = appPath.TrimEnd('/');
            }
            else if (rawUrl.StartsWith('/'))
            {
                resolvedPath = rawUrl;
            }
            else if (Uri.TryCreate(rawUrl, UriKind.Absolute, out _))
            {
                // Carries a scheme (http://, https://, mailto:), so it points off this site.
                return false;
            }
            else
            {
                resolvedPath = basePath + rawUrl;
            }

            return string.Equals(currentPath, resolvedPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
