using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Services;
using Viper.Areas.Scheduler.Models;
using Viper.Areas.Scheduler.Services;

namespace Viper.Areas.CMS.Jobs
{
    /// <summary>
    /// Daily purge of the CMS file trash: permanently deletes files that were soft-deleted more than
    /// <see cref="CmsTrash.RetentionDays"/> days ago (record + disk), restoring the legacy 30-day
    /// retention routine that VIPER 1 ran but the migration hadn't yet reimplemented.
    ///
    /// Gated by <see cref="CmsTrash.PurgeEnabledConfigKey"/> (default false): the job stays
    /// registered and fires on cron, but no-ops until an environment opts in via config. That lets
    /// it ship disabled and be enabled only once the legacy VIPER 1 purge is retired, without a
    /// dashboard change that the next app restart would silently undo.
    /// </summary>
    [ScheduledJob(id: "cms:trash-purge", cron: "0 3 * * *")]
    public sealed class CmsTrashPurgeScheduledJob : IScheduledJob
    {
        private readonly ICmsFileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CmsTrashPurgeScheduledJob> _logger;

        public CmsTrashPurgeScheduledJob(ICmsFileService fileService, IConfiguration configuration,
            ILogger<CmsTrashPurgeScheduledJob> logger)
        {
            _fileService = fileService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RunAsync(ScheduledJobContext context, CancellationToken ct)
        {
            if (!_configuration.GetValue<bool>(CmsTrash.PurgeEnabledConfigKey))
            {
                context.WriteLine(
                    $"CMS trash purge is disabled ({CmsTrash.PurgeEnabledConfigKey} is not true); skipping.");
                _logger.LogInformation(
                    "CMS trash purge skipped: {ConfigKey} is not enabled", CmsTrash.PurgeEnabledConfigKey);
                return;
            }

            context.WriteLine($"CMS trash purge starting (retention={CmsTrash.RetentionDays}d, modBy={context.ModBy})");
            var purged = await _fileService.PurgeDeletedFilesAsync(CmsTrash.RetentionDays, ct);
            context.WriteLine($"Purged {purged} trashed file(s) older than {CmsTrash.RetentionDays} days.");
            _logger.LogInformation(
                "CMS trash purge removed {Count} file(s) older than {Days} days", purged, CmsTrash.RetentionDays);
        }
    }
}
