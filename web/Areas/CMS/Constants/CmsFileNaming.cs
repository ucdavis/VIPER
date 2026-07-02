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
    }
}
