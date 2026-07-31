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

        /// <summary>
        /// Whether a user may still reach a trashed file at all (its listing, and its bytes through
        /// the download handler). A soft-deleted file keeps its bytes on disk until the purge job
        /// runs, so this is the only thing standing between "deleted" and a still-live download URL.
        /// <para>
        /// Deliberately the same rule as CMSFilesController.OwnerRestriction, which scopes the trash
        /// listing: admins see the whole trash, other file managers only what they deleted themselves
        /// (SoftDeleteFileAsync stamps ModifiedBy with the deleter). Keeping the download gate and the
        /// listing gate identical means a file is downloadable exactly when the user can see it in
        /// the trash UI, so neither can quietly become more permissive than the other.
        /// </para>
        /// </summary>
        /// <param name="isAdmin">Holds <see cref="CmsPermissions.Admin"/>.</param>
        /// <param name="hasAllFiles">Holds <see cref="CmsPermissions.AllFiles"/>.</param>
        /// <param name="deletedBy">The file's ModifiedBy, which soft delete sets to the deleter.</param>
        /// <param name="loginId">The requesting user's login id; null for an anonymous request.</param>
        public static bool CanAccessTrashed(bool isAdmin, bool hasAllFiles, string? deletedBy, string? loginId)
        {
            if (isAdmin)
            {
                return true;
            }

            // Fail closed on a missing login or owner rather than letting two blanks match.
            if (!hasAllFiles || string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(deletedBy))
            {
                return false;
            }

            return string.Equals(deletedBy, loginId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
