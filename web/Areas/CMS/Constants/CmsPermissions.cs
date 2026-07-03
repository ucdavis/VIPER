namespace Viper.Areas.CMS.Constants
{
    /// <summary>
    /// RAPS permission strings used by the CMS area.
    /// </summary>
    public static class CmsPermissions
    {
        public const string Base = "SVMSecure.CMS";
        public const string AllFiles = "SVMSecure.CMS.AllFiles";
        public const string ManageContentBlocks = "SVMSecure.CMS.ManageContentBlocks";
        public const string CreateContentBlock = "SVMSecure.CMS.CreateContentBlock";
        public const string ManageNavigation = "SVMSecure.CMS.ManageNavigation";

        // Legacy parity: the ColdFusion CMS gated permanent ("dev only") content-block delete on
        // SVMSecure.CATS.Admin (cms/views/contentBlock.cfm, cms/inc_contentBlock.cfm). VIPER 2 reuses
        // it for permanent delete of blocks and files; legacy file permanent delete was AllFiles-only,
        // so hard delete of files is deliberately narrowed to admins here.
        public const string Admin = "SVMSecure.CATS.Admin";
    }
}
