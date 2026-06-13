using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        // Default: available name echoes the input (no rename needed).
        _storage.GetAvailableFileName(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.ArgAt<string>(1));
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
            Substitute.For<IUserHelper>(), configuration, Substitute.For<ILogger<CmsFileImportService>>());
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

    #region Preview

    [Fact]
    public async Task Preview_ValidFile_ReportsNamesWithoutMoving()
    {
        var source = CreateWebrootFile(@"cats\docs\manual.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "/cats/docs/manual.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.True(result.CanImport, result.Message);
        Assert.Equal("manual.pdf", result.FileName);
        Assert.Equal("cats-manual.pdf", result.FriendlyName);
        Assert.Equal("/cats/docs/manual.pdf", result.OldUrl);
        Assert.Null(result.RenamedFrom);
        // Source must not be moved or deleted.
        Assert.True(File.Exists(source));
        // No DB rows must be created.
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Preview_MissingFile_ReportsNotFound()
    {
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/docs/missing.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.CanImport);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_TraversalPath_ReportsOutsideWebroot()
    {
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "../outside.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.CanImport);
        Assert.Contains("outside", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_DuplicateLines_FlagsSecondAsDuplicate()
    {
        CreateWebrootFile(@"cats\docs\manual.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string>
            {
                "cats/docs/manual.pdf",
                "cats/docs/manual.pdf"
            },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].CanImport);
        Assert.False(results[1].CanImport);
        Assert.Contains("Duplicate", results[1].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_ExistingFriendlyName_Blocks()
    {
        CreateWebrootFile(@"cats\docs\manual.pdf");

        // Seed a Files row whose FriendlyName matches what the preview would compute.
        _context.Files.Add(new Viper.Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\manual.pdf",
            Folder = "cats",
            FriendlyName = "cats-manual.pdf",
            Description = "existing",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/docs/manual.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.CanImport);
        Assert.Contains("cats-manual.pdf", result.Message);
    }

    [Fact]
    public async Task Preview_InvalidPathCharacters_ReportsInvalidPath()
    {
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/bad\0name.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.CanImport);
        Assert.Contains("not valid", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_DisallowedFileType_ReportsNotAllowed()
    {
        CreateWebrootFile(@"cats\evil.bat", "nope");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/evil.bat" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.CanImport);
        Assert.Contains("not allowed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_DiskConflict_ReportsAutoRename()
    {
        CreateWebrootFile(@"cats\docs\manual.pdf");
        _storage.GetAvailableFileName("cats", "manual.pdf").Returns("manual_0.pdf");
        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string> { "cats/docs/manual.pdf" },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.True(result.CanImport, result.Message);
        Assert.Equal("manual_0.pdf", result.FileName);
        Assert.Equal("manual.pdf", result.RenamedFrom);
        Assert.Equal("cats-manual_0.pdf", result.FriendlyName);
    }

    [Fact]
    public async Task Preview_TwoLinesSameFinalName_NonBlockingMessageOnSecond()
    {
        CreateWebrootFile(@"cats\docs\manual.pdf");
        // A second file in a different sub-path that produces the same final name after rename.
        Directory.CreateDirectory(Path.Join(_webroot, "cats", "other"));
        CreateWebrootFile(@"cats\other\manual.pdf");

        // Stub storage to always return the same available name so both resolve to "manual.pdf".
        _storage.GetAvailableFileName("cats", "manual.pdf").Returns("manual.pdf");

        var request = new CmsFileImportRequest
        {
            FilePaths = new List<string>
            {
                "cats/docs/manual.pdf",
                "cats/other/manual.pdf"
            },
            Folder = "cats"
        };

        var results = await _service.PreviewImportAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        // First entry: CanImport true, no message.
        Assert.True(results[0].CanImport);
        Assert.Null(results[0].Message);
        // Second entry: CanImport true (non-blocking), but has a message about renaming.
        Assert.True(results[1].CanImport);
        Assert.NotNull(results[1].Message);
        Assert.Contains("renamed", results[1].Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

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

    [Fact]
    public async Task BulkEncrypt_NonDatabaseSaveFailure_DecryptsFileBack()
    {
        // Named in-memory stores are shared, so the seeding context and the failing
        // context (whose interceptor throws a non-DB exception on save) see the same data.
        var storeName = "VIPER_" + Guid.NewGuid();
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
        await using (var seedContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName).Options))
        {
            seedContext.Files.Add(plain);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var failingContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName)
            .AddInterceptors(new ThrowingSaveInterceptor())
            .Options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CMS:LegacyWebrootPath"] = _webroot })
            .Build();
        var service = new CmsFileImportService(failingContext, _storage, _encryption, _audit,
            Substitute.For<IUserHelper>(), configuration, Substitute.For<ILogger<CmsFileImportService>>());
        _storage.ManagedFileExists(plain.FilePath).Returns(true);

        var results = await service.BulkEncryptAsync(new List<Guid> { plain.FileGuid }, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.False(result.Success);
        Assert.Contains("simulated save failure", result.Message);
        _encryption.Received(1).DecryptFileInPlace(plain.FilePath, "encrypted-db-key");
    }

    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("simulated save failure");
        }
    }
}
