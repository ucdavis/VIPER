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
    }
}
