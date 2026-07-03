namespace Viper.Areas.CMS.Constants
{
    /// <summary>
    /// Shared CMS file naming rules. Mirrors the legacy ColdFusion Files.cfc:
    /// friendly names are the folder path with separators dashed, plus the file name.
    /// </summary>
    public static class CmsFileNaming
    {
        public static string BuildFriendlyName(string folder, string fileName)
        {
            return folder.Replace('\\', '-').Replace('/', '-') + "-" + fileName;
        }

        /// <summary>
        /// Canonical stored form of a folder path: segments joined with '\' (the legacy
        /// convention). Folder input accepts either separator style, so records must be
        /// written canonically or the list filter's prefix match would miss them.
        /// </summary>
        public static string NormalizeFolderKey(string folder)
        {
            return string.Join('\\', folder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Final path segment of a name, treating both '\' and '/' as separators so path
        /// components are stripped identically on every OS (Path.GetFileName only honors the
        /// host separator, so on Linux it would keep a "sub\file" prefix from a Windows-style
        /// client value).
        /// </summary>
        public static string GetLeafName(string name)
        {
            int lastSeparator = name.LastIndexOfAny(['\\', '/']);
            return lastSeparator >= 0 ? name[(lastSeparator + 1)..] : name;
        }
    }
}
