using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Jobs;
using Viper.Areas.CMS.Services;
using Viper.Areas.Scheduler.Models;

namespace Viper.test.CMS;

/// <summary>
/// The trash-purge job permanently deletes files, so its config gate is safety-critical: it must
/// stay off unless an environment explicitly opts in via <see cref="CmsTrash.PurgeEnabledConfigKey"/>.
/// </summary>
public sealed class CmsTrashPurgeScheduledJobTests
{
    private static CmsTrashPurgeScheduledJob MakeJob(ICmsFileService fileService, bool? enabled)
    {
        var settings = new Dictionary<string, string?>();
        if (enabled != null)
        {
            settings[CmsTrash.PurgeEnabledConfigKey] = enabled.Value ? "true" : "false";
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new CmsTrashPurgeScheduledJob(
            fileService, configuration, Substitute.For<ILogger<CmsTrashPurgeScheduledJob>>());
    }

    private static ScheduledJobContext Context() => new(ScheduledJobContext.SchedulerActor);

    [Fact]
    public async Task RunAsync_DoesNotPurge_WhenFlagMissing()
    {
        var fileService = Substitute.For<ICmsFileService>();
        var job = MakeJob(fileService, enabled: null);

        await job.RunAsync(Context(), CancellationToken.None);

        await fileService.DidNotReceiveWithAnyArgs().PurgeDeletedFilesAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_DoesNotPurge_WhenDisabled()
    {
        var fileService = Substitute.For<ICmsFileService>();
        var job = MakeJob(fileService, enabled: false);

        await job.RunAsync(Context(), CancellationToken.None);

        await fileService.DidNotReceiveWithAnyArgs().PurgeDeletedFilesAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_Purges_WhenEnabled()
    {
        var fileService = Substitute.For<ICmsFileService>();
        fileService.PurgeDeletedFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(3);
        var job = MakeJob(fileService, enabled: true);

        await job.RunAsync(Context(), CancellationToken.None);

        await fileService.Received(1).PurgeDeletedFilesAsync(CmsTrash.RetentionDays, Arg.Any<CancellationToken>());
    }
}
