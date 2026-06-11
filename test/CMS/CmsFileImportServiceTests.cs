using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsFileImportService: importing from the legacy webroot (source containment,
/// move semantics, oldURL tracking, default permission) and bulk encryption.
/// </summary>
public sealed class CmsFileImportServiceTests : IDisposable
{
    private readonly string _webroot;
    private readonly VIPERContext _context;
    private readonly ICmsFileStorageService _storage;
    private readonly ICmsFileEncryptionService _encryption;
    private readonly ICmsFileAuditService _audit;
    private readonly CmsFileImportService _service;

    public CmsFileImportServiceTests()
    {
        _webroot = Path.Join(Path.GetTempPath(), "ViperCmsImportTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Join(_webroot, "cats", "docs"));

        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);

        _storage = Substitute.For<ICmsFileStorageService>();
        _storage.IsValidFolder(Arg.Any<string>()).Returns(true);
        _storage.MoveIntoPlace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                // emulate the real move so source-removal logic can be tested
                File.Delete(callInfo.ArgAt<string>(0));
                return Path.Join(@"C:\FakeRoot", callInfo.ArgAt<string>(1), Path.GetFileName(callInfo.ArgAt<string>(2)));
            });

        _encryption = Substitute.For<ICmsFileEncryptionService>();
        _encryption.GenerateKeyForDb().Returns("encrypted-db-key");
        _audit = Substitute.For<ICmsFileAuditService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CMS:LegacyWebrootPath"] = _webroot })
            .Build();

        _service = new CmsFileImportService(_context, _storage, _encryption, _audit,
            Substitute.For<IUserHelper>(), configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_webroot))
        {
            Directory.Delete(_webroot, recursive: true);
        }
    }

    private string CreateWebrootFile(string relativePath, string contents = "legacy file")
    {
        var path = Path.Join(_webroot, relativePath);
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public async Task Import_MovesFileAndRecordsOldUrl()
    {
        var source = CreateWebrootFile(@"cats\docs\manual.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "/cats/docs/manual.pdf" },
            Folder = "cats"
        };

        var results = await _service.ImportFilesAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.True(result.Success, result.Message);
        Assert.Equal("cats-manual.pdf", result.FriendlyName);
        Assert.False(File.Exists(source));
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("/cats/docs/manual.pdf", saved.OldUrl);
        Assert.Equal("Automatically imported from Viper", saved.Description);
        _audit.Received(1).Audit(Arg.Any<Viper.Models.VIPER.File>(), CmsFileAuditActions.ImportFile, Arg.Any<string>());
    }

    [Fact]
    public async Task Import_DefaultPermission_AddsSvmSecureFolder()
    {
        CreateWebrootFile(@"cats\docs\manual.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/docs/manual.pdf" },
            Folder = @"cats\docs",
            UseDefaultPermission = true,
            Permissions = new List<string> { "SVMSecure.Extra" }
        };

        await _service.ImportFilesAsync(request, TestContext.Current.CancellationToken);

        var saved = await _context.Files.Include(f => f.FileToPermissions).SingleAsync(TestContext.Current.CancellationToken);
        var permissions = saved.FileToPermissions.Select(p => p.Permission).ToList();
        Assert.Equal(2, permissions.Count);
        Assert.Contains("SVMSecure.Extra", permissions);
        Assert.Contains("SVMSecure.cats", permissions);
    }

    [Fact]
    public async Task Import_TraversalOutsideWebroot_Fails()
    {
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { @"..\..\Windows\system.ini" },
            Folder = "cats"
        };

        var results = await _service.ImportFilesAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.Contains("outside", result.Message);
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Import_MissingAndDisallowedFiles_ReportPerFileErrors()
    {
        CreateWebrootFile(@"cats\evil.bat", "nope");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/missing.pdf", "cats/evil.bat" },
            Folder = "cats"
        };

        var results = await _service.ImportFilesAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        Assert.Contains("not found", results[0].Message);
        Assert.Contains("not allowed", results[1].Message);
    }

    [Fact]
    public async Task Import_WithEncrypt_EncryptsBeforeMove()
    {
        CreateWebrootFile(@"cats\docs\secret.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/docs/secret.pdf" },
            Folder = "cats",
            Encrypt = true
        };

        var results = await _service.ImportFilesAsync(request, TestContext.Current.CancellationToken);

        Assert.True(results[0].Success);
        _encryption.Received(1).EncryptFileInPlace(Arg.Any<string>(), "encrypted-db-key");
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(saved.Encrypted);
        Assert.Equal("encrypted-db-key", saved.Key);
    }

    [Fact]
    public async Task BulkEncrypt_EncryptsUnencryptedAndSkipsOthers()
    {
        var plain = new Viper.Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\plain.pdf",
            Folder = "cats",
            FriendlyName = "cats-plain.pdf",
            Description = "",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now
        };
        var already = new Viper.Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\already.pdf",
            Folder = "cats",
            FriendlyName = "cats-already.pdf",
            Description = "",
            Encrypted = true,
            Key = "existing-key",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now
        };
        _context.Files.AddRange(plain, already);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _storage.ManagedFileExists(plain.FilePath).Returns(true);

        var results = await _service.BulkEncryptAsync(
            new List<Guid> { plain.FileGuid, already.FileGuid, Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        Assert.Contains("Already encrypted", results[1].Message);
        Assert.False(results[2].Success);
        _encryption.Received(1).EncryptFileInPlace(plain.FilePath, "encrypted-db-key");
        var saved = await _context.Files.SingleAsync(f => f.FileGuid == plain.FileGuid, TestContext.Current.CancellationToken);
        Assert.True(saved.Encrypted);
    }
}
