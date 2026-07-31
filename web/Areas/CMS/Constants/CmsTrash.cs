namespace Viper.Areas.CMS.Constants
{
    /// <summary>
    /// Trash (soft-delete) retention policy. A soft-deleted CMS file stays recoverable for this
    /// many days, then the CmsTrashPurgeScheduledJob permanently deletes it (record + disk),
    /// matching the legacy 30-day "will be permanently deleted on ..." behavior.
    /// </summary>
    public static class CmsTrash
    {
        public const int RetentionDays = 30;

        /// <summary>
        /// Config key gating the trash-purge job. Absent/false means the job never permanently
        /// deletes, so it can ship disabled and stay off across app restarts and IIS recycles
        /// (unlike a dashboard tweak). Kept false until the legacy VIPER 1 30-day purge is retired,
        /// so the two never purge in parallel; flipped to true (via SSM in deployed envs) at cutover.
        /// </summary>
        public const string PurgeEnabledConfigKey = "Cms:TrashPurgeEnabled";
    }
}
