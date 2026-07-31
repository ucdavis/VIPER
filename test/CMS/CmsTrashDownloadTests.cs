using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.CMS.Constants;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Viper.Services;
using CmsFileRecord = Viper.Models.VIPER.File;
using DataCms = Viper.Areas.CMS.Data.CMS;

namespace Viper.test.CMS;

/// <summary>
/// End-to-end tests for the trash gate on the two file delivery paths, Data.CMS.ProvideFile
/// (/CMS/Files?id=|fn=) and Data.CMS.DownloadZip (/CMS/Files?ids=).
///
/// A soft-deleted file keeps its bytes on disk for the whole retention window, so without this gate
/// a "deleted" file stays downloadable at its old URL - which is what legacy VIPER 1 did
/// (cms/CFC/FileDB.cfc get() never filtered deletedOn). CmsTrashAccessTests covers the rule itself;
/// these prove both handlers apply it, and that it only ever takes access away.
///
/// The trash check runs ahead of the on-disk check in both handlers, so these reach the decision
/// without seeding real bytes - which a test cannot do anyway, since ReplaceRootFolder rewrites
/// every path under the deployment storage root.
///
/// Every outcome here is NotFound, so asserting on the status alone would pass whether or not the
/// gate fired. The assertions read the [CMS-FILE-404] reason the handler logs instead.
/// </summary>
public sealed class CmsTrashDownloadTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<DataCms> _logger;
    private readonly DataCms _cms;
    private readonly Controller _controller;

    public CmsTrashDownloadTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _rapsContext = new RAPSContext(new DbContextOptionsBuilder<RAPSContext>()
            .UseInMemoryDatabase("RAPS_" + Guid.NewGuid()).Options);
        var sanitizer = Substitute.For<IHtmlSanitizerService>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(c => c.ArgAt<string>(0));
        _userHelper = Substitute.For<IUserHelper>();
        _logger = Substitute.For<ILogger<DataCms>>();

        _cms = new DataCms(_context, _rapsContext, sanitizer, _logger) { UserHelper = _userHelper };
        _controller = new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        _rapsContext.Dispose();
    }

    private sealed class TestController : Controller;

    private const string Owner = "adeleter";

    private async Task<CmsFileRecord> SeedFileAsync(DateTime? deletedOn, string modifiedBy = Owner)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var file = new CmsFileRecord
        {
            FileGuid = Guid.NewGuid(),
            // Deliberately never created on disk. Everything asserted here is decided before the
            // existence check, so the handler reaching disk is itself the "allowed through" signal.
            FilePath = @"S:\Files\Misc\trash-gate-" + suffix + ".txt",
            Folder = "Misc",
            FriendlyName = "Misc-trash-gate-" + suffix + ".txt",
            Description = string.Empty,
            AllowPublicAccess = false,
            ModifiedOn = DateTime.Now,
            ModifiedBy = modifiedBy,
            DeletedOn = deletedOn
        };
        _context.Files.Add(file);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return file;
    }

    private AaudUser SignIn(string loginId, params string[] permissions)
    {
        var user = new AaudUser { AaudUserId = 1, LoginId = loginId, MothraId = "m1" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.GetAllPermissions(_rapsContext, user)
            .Returns(permissions.Select(p => new TblPermission { Permission = p }).ToList());
        foreach (var permission in permissions)
        {
            _userHelper.HasPermission(_rapsContext, user, permission).Returns(true);
        }
        return user;
    }

    private void SignOut() => _userHelper.GetCurrentUser().ReturnsNull();

    // The logged [CMS-FILE-404] lines, which carry the reason the request was turned away.
    private List<string> NotFoundLogs() =>
        _logger.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(c => c.GetArguments()[2]?.ToString() ?? string.Empty)
            .Where(m => m.Contains("[CMS-FILE-404]", StringComparison.Ordinal))
            .ToList();

    private void AssertRefusedAsTrashed() =>
        Assert.Contains(NotFoundLogs(), m => m.Contains("reason=deleted", StringComparison.Ordinal));

    // Past the gate: the handler went on to the (absent) bytes rather than refusing it as deleted.
    private void AssertReachedTheFile()
    {
        var logs = NotFoundLogs();
        Assert.DoesNotContain(logs, m => m.Contains("reason=deleted", StringComparison.Ordinal));
        Assert.Contains(logs, m => m.Contains("reason=missing-on-disk", StringComparison.Ordinal));
    }

    #region ProvideFile

    [Fact]
    public async Task ProvideFile_TrashedFile_IsRefusedForAnonymousRequest()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now);
        SignOut();

        var result = _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        Assert.IsType<NotFoundResult>(result);
        AssertRefusedAsTrashed();
    }

    [Fact]
    public async Task ProvideFile_TrashedFile_IsRefusedByFriendlyNameToo()
    {
        // The friendly-name URL is the shape legacy content embeds, so it must gate identically.
        var file = await SeedFileAsync(deletedOn: DateTime.Now);
        SignOut();

        var result = _cms.ProvideFile(_controller, string.Empty, file.FriendlyName, string.Empty);

        Assert.IsType<NotFoundResult>(result);
        AssertRefusedAsTrashed();
    }

    [Fact]
    public async Task ProvideFile_TrashedFile_IsRefusedForAnotherUsersDeletion()
    {
        // A file manager may only reach what they trashed themselves, matching the trash listing.
        var file = await SeedFileAsync(deletedOn: DateTime.Now, modifiedBy: "someone-else");
        SignIn("bmanager", CmsPermissions.AllFiles);

        var result = _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        Assert.IsType<NotFoundResult>(result);
        AssertRefusedAsTrashed();
    }

    [Fact]
    public async Task ProvideFile_TrashedFile_IsRefusedForUserWithoutFilePermissions()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now);
        SignIn(Owner, "SVMSecure");

        var result = _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        Assert.IsType<NotFoundResult>(result);
        AssertRefusedAsTrashed();
    }

    [Fact]
    public async Task ProvideFile_TrashedFile_ReachesTheDeleterWhoHoldsAllFiles()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now);
        SignIn(Owner, CmsPermissions.AllFiles);

        _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        AssertReachedTheFile();
    }

    [Fact]
    public async Task ProvideFile_TrashedFile_ReachesAnAdminForAnotherUsersDeletion()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now, modifiedBy: "someone-else");
        SignIn("cadmin", CmsPermissions.Admin);

        _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        AssertReachedTheFile();
    }

    [Fact]
    public async Task ProvideFile_ActiveFile_IsUnaffectedByTheTrashGate()
    {
        // The gate must only ever take access away: an active file keeps its previous treatment.
        var file = await SeedFileAsync(deletedOn: null);
        SignOut();

        _cms.ProvideFile(_controller, file.FileGuid.ToString(), string.Empty, string.Empty);

        AssertReachedTheFile();
    }

    [Fact]
    public async Task ProvideFile_UnknownFile_IsRefusedAsNoDbMatch()
    {
        await SeedFileAsync(deletedOn: DateTime.Now);
        SignOut();

        var result = _cms.ProvideFile(_controller, Guid.NewGuid().ToString(), string.Empty, string.Empty);

        Assert.IsType<NotFoundResult>(result);
        Assert.Contains(NotFoundLogs(), m => m.Contains("reason=no-db-match", StringComparison.Ordinal));
    }

    #endregion

    #region DownloadZip

    // DownloadZip logs nothing per skipped entry, and a file absent from disk is skipped whether or
    // not the trash gate fired, so the NotFound alone proves nothing here (it holds even with the
    // gate deleted). What does discriminate is whether the gate was consulted at all: only it asks
    // for CmsPermissions.Admin. CheckFilePermission asks for "SVMSecure" instead, and on this path
    // is never reached. So these assert the endpoint's contract plus the consultation, and
    // CmsTrashAccessTests pins what the consultation decides.

    [Fact]
    public async Task DownloadZip_TrashedFile_IsNotFoundForAnonymousRequest()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now);
        SignOut();

        var result = _cms.DownloadZip(_controller, [file.FileGuid.ToString()]);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DownloadZip_TrashedFile_ConsultsTheTrashGateAndIsNotFoundForAnotherUsersDeletion()
    {
        var file = await SeedFileAsync(deletedOn: DateTime.Now, modifiedBy: "someone-else");
        var user = SignIn("bmanager", CmsPermissions.AllFiles);

        var result = _cms.DownloadZip(_controller, [file.FileGuid.ToString()]);

        Assert.IsType<NotFoundResult>(result);
        _userHelper.Received().HasPermission(_rapsContext, user, CmsPermissions.Admin);
    }

    [Fact]
    public async Task DownloadZip_ActiveFile_DoesNotConsultTheTrashGate()
    {
        // The counterpart: an active file never reaches the gate, so the same assertion must not
        // hold - which is what makes the check above a real signal rather than an always-true one.
        var file = await SeedFileAsync(deletedOn: null);
        var user = SignIn("bmanager", CmsPermissions.AllFiles);

        _cms.DownloadZip(_controller, [file.FileGuid.ToString()]);

        _userHelper.DidNotReceive().HasPermission(_rapsContext, user, CmsPermissions.Admin);
    }

    [Fact]
    public void DownloadZip_NoUsableGuids_IsBadRequest()
    {
        var result = _cms.DownloadZip(_controller, [" ", string.Empty]);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
