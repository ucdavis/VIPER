using Viper.Areas.RAPS.Services;
using Viper.Areas.Scheduler.Models;
using Viper.Areas.Scheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.RAPS.Jobs;

/// <summary>
/// First consumer of the scheduled-job abstraction: nightly role membership
/// refresh. Wraps <see cref="RoleViews.UpdateRoles"/> and threads the run's
/// <see cref="ScheduledJobContext.ModBy"/> through so the audit log clearly
/// distinguishes scheduler-driven changes (<c>"__sched"</c>) from
/// admin-driven manual runs (a real LoginId).
/// </summary>
[ScheduledJob(id: "raps:role-refresh", cron: "0 0 * * *", TimeZoneId = "Pacific Standard Time")]
public sealed class RapsRoleRefreshScheduledJob : IScheduledJob
{
    private readonly RAPSContext _rapsContext;
    private readonly ILogger<RapsRoleRefreshScheduledJob> _logger;

    public RapsRoleRefreshScheduledJob(
        RAPSContext rapsContext,
        ILogger<RapsRoleRefreshScheduledJob> logger)
    {
        _rapsContext = rapsContext;
        _logger = logger;
    }

    public async Task RunAsync(ScheduledJobContext context, CancellationToken ct)
    {
        context.WriteLine($"RAPS role refresh starting (modBy={context.ModBy})");
        var roleViews = new RoleViews(_rapsContext);
        var messages = await roleViews.UpdateRoles(modBy: context.ModBy, debugOnly: false, ct: ct);
        foreach (var message in messages)
        {
            context.WriteLine(message);
        }
        context.WriteLine($"Done. {messages.Count} change message(s).");
        _logger.LogInformation(
            "RAPS role refresh (modBy={ModBy}) wrote {ChangeCount} change message(s)",
            LogSanitizer.SanitizeString(context.ModBy),
            messages.Count);
    }
}
