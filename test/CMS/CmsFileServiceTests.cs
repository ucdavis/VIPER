using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;
using File = Viper.Models.VIPER.File;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsFileService CRUD orchestration: friendly-name generation, permission and
/// person deltas, soft delete/restore, and audit calls. Storage and encryption are mocked;
/// the database is EF in-memory.
/// </summary>
public sealed class CmsFileServiceTests : IDisposable
{
    private readonly VIPERContext _context;
    private readonly AAUDContext _aaudContext;
    private readonly ICmsFileStorageService _storage;
    private readonly ICmsFileEncryptionService _encryption;
    private readonly ICmsFileAuditService _audit;
    private readonly IUserHelper _userHelper;
    private readonly TestableCmsFileService _service;
    private readonly List<VIPERContext> _extraContexts = new();
    private readonly List<MemoryStream> _formFileStreams = new();

    public CmsFileServiceTests()
    {
        _context = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase("VIPER_" + Guid.NewGuid()).Options);
        _aaudContext = new AAUDContext(new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase("AAUD_" + Guid.NewGuid()).Options);

        _storage = Substitute.For<ICmsFileStorageService>();
        _storage.RootFolder.Returns(@"C:\FakeRoot");
        _storage.IsValidFolder(Arg.Any<string>()).Returns(true);
        _storage.SaveToTempAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(@"C:\FakeTemp\" + Guid.NewGuid().ToString("N")));
        _storage.MoveIntoPlace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(callInfo => Path.Join(@"C:\FakeRoot", callInfo.ArgAt<string>(1), Path.GetFileName(callInfo.ArgAt<string>(2))));

        _encryption = Substitute.For<ICmsFileEncryptionService>();
        _encryption.GenerateKeyForDb().Returns("encrypted-db-key");

        _audit = Substitute.For<ICmsFileAuditService>();
        _userHelper = Substitute.For<IUserHelper>();

        _service = new TestableCmsFileService(_context, _aaudContext, _storage, _encryption, _audit, _userHelper,
            Substitute.For<ILogger<CmsFileService>>());
    }

    public void Dispose()
    {
        _context.Dispose();
        _aaudContext.Dispose();
        foreach (var ctx in _extraContexts)
        {
            ctx.Dispose();
        }
        foreach (var stream in _formFileStreams)
        {
            stream.Dispose();
        }
    }

    private IFormFile MakeFormFile(string fileName = "report.pdf")
    {
        // Tracked so the backing stream is disposed in Dispose rather than left to the GC.
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _formFileStreams.Add(stream);
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    /// <summary>
    /// Writes a real temp file to stand in for a backup the service returns from BackupManagedFile.
    /// A real path lets the cleanup branch (System.IO.File.Exists/Delete) actually execute rather
    /// than short-circuiting on a fake path that never exists. Callers delete it in a finally.
    /// </summary>
    private static string MakeRealBackupFile()
    {
        string path = Path.Join(Path.GetTempPath(), "Viper-CMS-Test-" + Guid.NewGuid().ToString("N"));
        System.IO.File.WriteAllBytes(path, new byte[] { 9, 9, 9 });
        return path;
    }

    // Shared seed timestamp so update requests can present a matching LastModifiedOn
    // (UpdateFileAsync rejects stale or missing values with 409/400 semantics).
    private readonly DateTime _seededModifiedOn = DateTime.Now.AddDays(-1);

    private async Task<File> SeedFileAsync(Action<File>? customize = null)
    {
        var file = new File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\seeded.pdf",
            Folder = "cats",
            FriendlyName = "cats-seeded.pdf",
            Description = "seeded",
            ModifiedBy = "test",
            ModifiedOn = _seededModifiedOn
        };
        customize?.Invoke(file);
        _context.Files.Add(file);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return file;
    }

    /// <summary>
    /// Seeds a file into a shared named store, then returns a service backed by a second context
    /// on that store whose interceptor throws on save. Lets the rollback path be exercised without
    /// a real database. Reuses the mocked storage/encryption so their calls can be asserted.
    /// </summary>
    private async Task<(CmsFileService Service, Guid FileGuid, string FilePath)> BuildServiceOverFailingSaveAsync(
        Action<File>? customize = null)
    {
        const string filePath = @"C:\FakeRoot\cats\plain.pdf";
        var storeName = "VIPER_" + Guid.NewGuid();
        var file = new File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = filePath,
            Folder = "cats",
            FriendlyName = "cats-plain.pdf",
            Description = "seeded",
            ModifiedBy = "test",
            ModifiedOn = _seededModifiedOn
        };
        customize?.Invoke(file);
        await using (var seedContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName).Options))
        {
            seedContext.Files.Add(file);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var failingContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName)
            .AddInterceptors(new ThrowingSaveInterceptor())
            .Options);
        _extraContexts.Add(failingContext);
        var service = new CmsFileService(failingContext, _aaudContext, _storage, _encryption, _audit, _userHelper,
            Substitute.For<ILogger<CmsFileService>>());
        return (service, file.FileGuid, filePath);
    }

    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("simulated save failure");
        }
    }

    /// <summary>
    /// Throws only when the save being intercepted deletes the given file, so a purge run's other
    /// per-file saves succeed normally. Lets PurgeDeletedFilesAsync's per-file isolation be tested
    /// without a database that can produce a real transient failure.
    /// </summary>
    private sealed class SelectiveThrowingSaveInterceptor : SaveChangesInterceptor
    {
        private readonly Guid _failingFileGuid;

        public SelectiveThrowingSaveInterceptor(Guid failingFileGuid)
        {
            _failingFileGuid = failingFileGuid;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            bool targetsFailingFile = eventData.Context?.ChangeTracker.Entries<File>()
                .Any(e => e.State == EntityState.Deleted && e.Entity.FileGuid == _failingFileGuid) ?? false;
            if (targetsFailingFile)
            {
                throw new InvalidOperationException("simulated transient save failure");
            }
            return ValueTask.FromResult(result);
        }
    }

    #region Create

    [Fact]
    public async Task CreateFile_BuildsFriendlyNameFromFolderAndFileName()
    {
        var request = new CmsFileCreateRequest { Folder = @"cats\photos" };

        var dto = await _service.CreateFileAsync(request, MakeFormFile("pic.jpg"), TestContext.Current.CancellationToken);

        Assert.Equal("cats-photos-pic.jpg", dto.FriendlyName);
        Assert.Equal("pic.jpg", dto.FileName);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(@"cats\photos", saved.Folder);
        Assert.False(saved.Encrypted);
        Assert.Null(saved.Key);
    }

    [Fact]
    public async Task CreateFile_NormalizesForwardSlashFolderToCanonicalForm()
    {
        // IsValidFolder accepts both separator styles, but records store the canonical
        // '\' form so the list filter's "folder\" prefix match finds subfolder rows.
        var request = new CmsFileCreateRequest { Folder = "cats/photos" };

        await _service.CreateFileAsync(request, MakeFormFile("pic.jpg"), TestContext.Current.CancellationToken);

        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(@"cats\photos", saved.Folder);
    }

    [Fact]
    public async Task CreateFile_SavesPermissionsAndPeople_Deduplicated()
    {
        var request = new CmsFileCreateRequest
        {
            Folder = "cats",
            Permissions = new List<string> { "SVMSecure.CATS", "svmsecure.cats", " SVMSecure.Admin " },
            IamIds = new List<string> { "1000123", "1000123", "1000456" }
        };

        var dto = await _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken);

        Assert.Equal(2, dto.Permissions.Count);
        Assert.Contains("SVMSecure.Admin", dto.Permissions);
        Assert.Equal(2, dto.People.Count);
    }

    [Fact]
    public async Task CreateFile_WithEncrypt_GeneratesKeyAndEncryptsTemp()
    {
        var request = new CmsFileCreateRequest { Folder = "cats", Encrypt = true };

        var dto = await _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken);

        Assert.True(dto.Encrypted);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("encrypted-db-key", saved.Key);
        _encryption.Received(1).EncryptFileInPlace(Arg.Any<string>(), "encrypted-db-key");
    }

    [Fact]
    public async Task CreateFile_AuditsAddAndUpload()
    {
        var request = new CmsFileCreateRequest { Folder = "cats" };

        await _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken);

        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.AddFile, Arg.Any<string>());
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.UploadFile, "NewFile");
    }

    [Fact]
    public async Task CreateFile_DuplicateFriendlyName_DeletesMovedFileAndThrows()
    {
        await SeedFileAsync(f => f.FriendlyName = "cats-report.pdf");
        var request = new CmsFileCreateRequest { Folder = "cats" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken));

        _storage.Received(1).DeleteManagedFile(Path.Join(@"C:\FakeRoot", "cats", "report.pdf"));
    }

    [Fact]
    public async Task CreateFile_InvalidFolder_Throws()
    {
        _storage.IsValidFolder("badfolder").Returns(false);
        var request = new CmsFileCreateRequest { Folder = "badfolder" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateFile_DisallowedExtension_Throws()
    {
        var request = new CmsFileCreateRequest { Folder = "cats" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFileAsync(request, MakeFormFile("evil.bat"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateFile_FileNameOverride_StoresUnderOverrideName()
    {
        var request = new CmsFileCreateRequest { Folder = "cats", FileName = "custom.pdf" };

        var dto = await _service.CreateFileAsync(request, MakeFormFile("orig.pdf"), TestContext.Current.CancellationToken);

        // MoveIntoPlace should have been called with the override name, not the upload name.
        _storage.Received(1).MoveIntoPlace(Arg.Any<string>(), "cats", "custom.pdf", Arg.Any<bool>());
        Assert.Equal("custom.pdf", dto.FileName);
        Assert.Equal("cats-custom.pdf", dto.FriendlyName);
    }

    [Fact]
    public async Task CreateFile_OverwriteDiskOnly_ReplacesInPlace()
    {
        const string targetPath = @"C:\FakeRoot\cats\report.pdf";
        _storage.FileNameInUse("cats", "report.pdf").Returns(true);
        _storage.BuildManagedPath("cats", "report.pdf").Returns(targetPath);

        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken);

        _storage.Received(1).ReplaceInPlace(Arg.Any<string>(), targetPath);
        _storage.DidNotReceive().MoveIntoPlace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(targetPath, saved.FilePath);
    }

    [Fact]
    public async Task CreateFile_OverwriteManagedRecord_Throws()
    {
        const string targetPath = @"C:\FakeRoot\cats\report.pdf";
        _storage.FileNameInUse("cats", "report.pdf").Returns(true);
        _storage.BuildManagedPath("cats", "report.pdf").Returns(targetPath);
        _storage.HasFileRecord("cats", "report.pdf", Arg.Any<string?>()).Returns(true);

        // Seed a Files row whose FilePath matches the target path.
        await SeedFileAsync(f =>
        {
            f.FilePath = targetPath;
            f.FriendlyName = "cats-report.pdf";
        });

        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken));

        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateFile_OverwriteWithoutConflict_StoresNormally()
    {
        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await _service.CreateFileAsync(request, MakeFormFile(), TestContext.Current.CancellationToken);

        _storage.Received(1).MoveIntoPlace(Arg.Any<string>(), "cats", "report.pdf", Arg.Any<bool>());
        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateFile_OverwriteOrphan_SaveFails_RestoresOriginalFile()
    {
        const string targetPath = @"C:\FakeRoot\cats\orphan.pdf";
        const string backupPath = @"C:\FakeTemp\backup-orphan";
        _storage.FileNameInUse("cats", "orphan.pdf").Returns(true);
        _storage.BuildManagedPath("cats", "orphan.pdf").Returns(targetPath);
        _storage.ManagedFileExists(targetPath).Returns(true);
        _storage.BackupManagedFile(targetPath).Returns(backupPath);

        var (service, _, _) = await BuildServiceOverFailingSaveAsync();
        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateFileAsync(request, MakeFormFile("orphan.pdf"), TestContext.Current.CancellationToken));

        // The orphaned original (no DB row) was overwritten on disk; the failed save must move the
        // pre-overwrite backup back into place rather than deleting it, so the file isn't lost.
        _storage.Received(1).BackupManagedFile(targetPath);
        _storage.Received(1).ReplaceInPlace(backupPath, targetPath);
        _storage.DidNotReceive().DeleteManagedFile(targetPath);
    }

    [Fact]
    public async Task CreateFile_OverwriteOrphan_SaveFails_RestoreFails_PreservesBackup()
    {
        const string targetPath = @"C:\FakeRoot\cats\orphan.pdf";
        _storage.FileNameInUse("cats", "orphan.pdf").Returns(true);
        _storage.BuildManagedPath("cats", "orphan.pdf").Returns(targetPath);
        _storage.ManagedFileExists(targetPath).Returns(true);

        // A real temp file stands in for the backup so the cleanup branch (File.Exists/Delete)
        // actually runs instead of short-circuiting on a non-existent fake path.
        string backupPath = MakeRealBackupFile();
        try
        {
            _storage.BackupManagedFile(targetPath).Returns(backupPath);
            // The restore attempt itself fails (e.g., the target is locked); the backup is then the
            // only surviving copy of the original bytes and must not be deleted.
            _storage.When(s => s.ReplaceInPlace(backupPath, targetPath))
                .Do(_ => throw new IOException("restore failed"));

            var (service, _, _) = await BuildServiceOverFailingSaveAsync();
            var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateFileAsync(request, MakeFormFile("orphan.pdf"), TestContext.Current.CancellationToken));

            _storage.Received(1).ReplaceInPlace(backupPath, targetPath);
            Assert.True(System.IO.File.Exists(backupPath));
        }
        finally
        {
            if (System.IO.File.Exists(backupPath))
            {
                System.IO.File.Delete(backupPath);
            }
        }
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateFile_AppliesPermissionDeltas()
    {
        var file = await SeedFileAsync(f =>
        {
            f.FileToPermissions.Add(new FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure.Old" });
            f.FileToPermissions.Add(new FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure.Keep" });
        });
        var request = new CmsFileUpdateRequest
        {
            Description = "seeded",
            LastModifiedOn = _seededModifiedOn,
            Permissions = new List<string> { "SVMSecure.Keep", "SVMSecure.New" }
        };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Equal(new List<string> { "SVMSecure.Keep", "SVMSecure.New" }, dto.Permissions);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.EditFile,
            Arg.Is<string>(d => d.Contains("Permission removed: SVMSecure.Old") && d.Contains("Permission added: SVMSecure.New")));
    }

    [Fact]
    public async Task UpdateFile_AppliesPersonDeltas()
    {
        var file = await SeedFileAsync(f =>
            f.FileToPeople.Add(new FileToPerson { FileGuid = f.FileGuid, IamId = "100OLD" }));
        var request = new CmsFileUpdateRequest
        {
            Description = "seeded",
            LastModifiedOn = _seededModifiedOn,
            IamIds = new List<string> { "100NEW" }
        };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Single(dto.People);
        Assert.Equal("100NEW", dto.People[0].IamId);
    }

    [Fact]
    public async Task UpdateFile_EncryptToggleOn_EncryptsExistingFile()
    {
        var file = await SeedFileAsync();
        _storage.ManagedFileExists(file.FilePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = true, LastModifiedOn = _seededModifiedOn };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.True(dto!.Encrypted);
        _encryption.Received(1).EncryptFileInPlace(file.FilePath, "encrypted-db-key");
    }

    [Fact]
    public async Task UpdateFile_EncryptToggleOff_DecryptsExistingFile()
    {
        var file = await SeedFileAsync(f =>
        {
            f.Encrypted = true;
            f.Key = "old-key";
        });
        _storage.ManagedFileExists(file.FilePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = false, LastModifiedOn = _seededModifiedOn };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.False(dto!.Encrypted);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.Key);
        _encryption.Received(1).DecryptFileInPlace(file.FilePath, "old-key");
    }

    [Fact]
    public async Task UpdateFile_WithReplacementUpload_ReplacesInPlaceAndAudits()
    {
        var file = await SeedFileAsync();
        var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

        await _service.UpdateFileAsync(file.FileGuid, request, MakeFormFile("newversion.pdf"), TestContext.Current.CancellationToken);

        _storage.Received(1).ReplaceInPlace(Arg.Any<string>(), file.FilePath);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.UploadFile, "ReplacingFile");
    }

    [Fact]
    public async Task UpdateFile_ReplacementWithDifferentExtension_Throws()
    {
        // The record keeps its name/path on replacement, so a .png swapped for the seeded
        // .pdf would leave the stored .pdf holding .png bytes; the upload must be rejected.
        var file = await SeedFileAsync();
        var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateFileAsync(file.FileGuid, request, MakeFormFile("newversion.png"), TestContext.Current.CancellationToken));
        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateFile_WithReplacementUpload_RestoresSoftDeletedFile()
    {
        var file = await SeedFileAsync(f => f.DeletedOn = DateTime.Now);
        var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

        await _service.UpdateFileAsync(file.FileGuid, request, MakeFormFile("newversion.pdf"), TestContext.Current.CancellationToken);

        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.DeletedOn);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.EditFile,
            Arg.Is<string>(d => d.Contains("Restored file")));
    }

    [Fact]
    public async Task UpdateFile_MetadataOnly_DoesNotRestoreSoftDeletedFile()
    {
        var file = await SeedFileAsync(f => f.DeletedOn = DateTime.Now);
        var request = new CmsFileUpdateRequest { Description = "updated", LastModifiedOn = _seededModifiedOn };

        await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(saved.DeletedOn);
    }

    [Fact]
    public async Task UpdateFile_UnknownGuid_ReturnsNull()
    {
        var dto = await _service.UpdateFileAsync(Guid.NewGuid(), new CmsFileUpdateRequest(), null, TestContext.Current.CancellationToken);

        Assert.Null(dto);
    }

    [Fact]
    public async Task UpdateFile_StaleLastModifiedOn_ThrowsConcurrency_BeforeAnyChange()
    {
        var file = await SeedFileAsync();
        var request = new CmsFileUpdateRequest
        {
            Description = "changed",
            LastModifiedOn = _seededModifiedOn.AddMinutes(-5)
        };

        var ex = await Assert.ThrowsAsync<CmsConcurrencyException>(
            () => _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken));

        // Names who saved and when, and nothing was mutated or audited.
        Assert.Contains("modified by test", ex.Message);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("seeded", saved.Description);
        _audit.DidNotReceive().Audit(Arg.Any<File>(), CmsFileAuditActions.EditFile, Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateFile_MissingLastModifiedOn_ThrowsArgumentException()
    {
        var file = await SeedFileAsync();
        var request = new CmsFileUpdateRequest { Description = "changed" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckName_RecordUnderAnotherStorageRoot_MatchesByFriendlyName()
    {
        // A record created in another environment carries that root in FilePath, but its
        // derived friendly name is stable; the conflict payload must identify the record so
        // the client offers "edit that file" rather than a plain disk overwrite.
        var file = await SeedFileAsync(f =>
        {
            f.FilePath = @"S:\Files\cats\foreign.pdf";
            f.FriendlyName = "cats-foreign.pdf";
        });
        _storage.IsValidFolder("cats").Returns(true);
        _storage.BuildManagedPath("cats", "foreign.pdf").Returns(@"C:\FakeRoot\cats\foreign.pdf");

        var check = await _service.CheckNameAsync("cats", "foreign.pdf", TestContext.Current.CancellationToken);

        Assert.Equal(file.FileGuid, check.ExistingFileGuid);
        Assert.Equal("cats-foreign.pdf", check.ExistingFriendlyName);
    }

    [Fact]
    public async Task CreateFile_OverwriteOfForeignRootRecord_FailsBeforeTouchingDisk()
    {
        await SeedFileAsync(f =>
        {
            f.FilePath = @"S:\Files\cats\foreign.pdf";
            f.FriendlyName = "cats-foreign.pdf";
        });
        _storage.FileNameInUse("cats", "foreign.pdf").Returns(true);
        _storage.BuildManagedPath("cats", "foreign.pdf").Returns(@"C:\FakeRoot\cats\foreign.pdf");
        _storage.HasFileRecord("cats", "foreign.pdf", Arg.Any<string?>()).Returns(true);
        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFileAsync(request, MakeFormFile("foreign.pdf"), TestContext.Current.CancellationToken));

        // The guard must fire before any disk mutation.
        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
        _storage.DidNotReceive().BackupManagedFile(Arg.Any<string>());
    }

    [Fact]
    public async Task CheckName_ExistingRecord_ReturnsItsModifiedOn()
    {
        // The overwrite flow echoes this back as LastModifiedOn, so it must be the record's value.
        var file = await SeedFileAsync();
        _storage.IsValidFolder("cats").Returns(true);
        _storage.BuildManagedPath("cats", "seeded.pdf").Returns(file.FilePath);

        var check = await _service.CheckNameAsync("cats", "seeded.pdf", TestContext.Current.CancellationToken);

        Assert.Equal(file.FileGuid, check.ExistingFileGuid);
        Assert.Equal(_seededModifiedOn, check.ExistingModifiedOn);
    }

    [Fact]
    public async Task UpdateFile_EncryptToggleOn_SaveFails_DecryptsFileBack()
    {
        var (service, fileGuid, filePath) = await BuildServiceOverFailingSaveAsync();
        _storage.ManagedFileExists(filePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = true, LastModifiedOn = _seededModifiedOn };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateFileAsync(fileGuid, request, null, TestContext.Current.CancellationToken));

        // The file was encrypted on disk but the save failed, so it must be decrypted back with
        // the same key or it would be unreadable (no key persisted).
        _encryption.Received(1).DecryptFileInPlace(filePath, "encrypted-db-key");
    }

    [Fact]
    public async Task UpdateFile_EncryptToggleOff_SaveFails_ReEncryptsFile()
    {
        var (service, fileGuid, filePath) = await BuildServiceOverFailingSaveAsync(f =>
        {
            f.Encrypted = true;
            f.Key = "old-key";
        });
        _storage.ManagedFileExists(filePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = false, LastModifiedOn = _seededModifiedOn };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateFileAsync(fileGuid, request, null, TestContext.Current.CancellationToken));

        // The file was decrypted on disk but the save failed, so it must be re-encrypted with the
        // original key to stay consistent with the rolled-back (still-encrypted) database row.
        _encryption.Received(1).EncryptFileInPlace(filePath, "old-key");
    }

    [Fact]
    public async Task UpdateFile_ReplacementUpload_SaveFails_RestoresOriginalFile()
    {
        var (service, fileGuid, filePath) = await BuildServiceOverFailingSaveAsync();
        _storage.ManagedFileExists(filePath).Returns(true);
        const string backupPath = @"C:\FakeTemp\backup-original";
        _storage.BackupManagedFile(filePath).Returns(backupPath);
        var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateFileAsync(fileGuid, request, MakeFormFile("newversion.pdf"), TestContext.Current.CancellationToken));

        // ReplaceInPlace swapped in the new upload, but the save failed, so the pre-replacement
        // backup must be moved back over the managed path to restore the original bytes.
        _storage.Received(1).BackupManagedFile(filePath);
        _storage.Received(1).ReplaceInPlace(backupPath, filePath);
    }

    [Fact]
    public async Task UpdateFile_ReplacementUpload_SaveFails_RestoreFails_PreservesBackup()
    {
        var (service, fileGuid, filePath) = await BuildServiceOverFailingSaveAsync();
        _storage.ManagedFileExists(filePath).Returns(true);

        string backupPath = MakeRealBackupFile();
        try
        {
            _storage.BackupManagedFile(filePath).Returns(backupPath);
            // The restore attempt fails, so the backup holds the only copy of the pre-update bytes
            // (the database rolled back to that older record) and must be preserved, not deleted.
            _storage.When(s => s.ReplaceInPlace(backupPath, filePath))
                .Do(_ => throw new IOException("restore failed"));
            var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UpdateFileAsync(fileGuid, request, MakeFormFile("newversion.pdf"), TestContext.Current.CancellationToken));

            _storage.Received(1).ReplaceInPlace(backupPath, filePath);
            Assert.True(System.IO.File.Exists(backupPath));
        }
        finally
        {
            if (System.IO.File.Exists(backupPath))
            {
                System.IO.File.Delete(backupPath);
            }
        }
    }

    [Fact]
    public async Task UpdateFile_ReplacementUpload_SaveSucceeds_DeletesBackup()
    {
        var file = await SeedFileAsync();
        _storage.ManagedFileExists(file.FilePath).Returns(true);

        string backupPath = MakeRealBackupFile();
        try
        {
            _storage.BackupManagedFile(file.FilePath).Returns(backupPath);
            var request = new CmsFileUpdateRequest { Description = "seeded", LastModifiedOn = _seededModifiedOn };

            await _service.UpdateFileAsync(file.FileGuid, request, MakeFormFile("newversion.pdf"), TestContext.Current.CancellationToken);

            // A committed save makes the pre-replacement backup obsolete; it must be cleaned up.
            Assert.False(System.IO.File.Exists(backupPath));
        }
        finally
        {
            if (System.IO.File.Exists(backupPath))
            {
                System.IO.File.Delete(backupPath);
            }
        }
    }

    #endregion

    #region Delete / Restore

    [Fact]
    public async Task SoftDelete_SetsDeletedOn_AndAudits()
    {
        var file = await SeedFileAsync();

        var result = await _service.SoftDeleteFileAsync(file.FileGuid, TestContext.Current.CancellationToken);

        Assert.True(result);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(saved.DeletedOn);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.DeleteFile, "File Marked for Deletion");
    }

    [Fact]
    public async Task RollbackDeleteFile_NotAttachedElsewhere_SoftDeletesAndAudits()
    {
        var file = await SeedFileAsync();

        var result = await _service.RollbackDeleteFileAsync(file.FileGuid, contentBlockId: 5, TestContext.Current.CancellationToken);

        Assert.True(result);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(saved.DeletedOn);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.DeleteFile, "File Marked for Deletion");
    }

    [Fact]
    public async Task RollbackDeleteFile_AlreadyDeleted_ReturnsFalse_DoesNotReAudit()
    {
        var deletedOn = DateTime.Now.AddMinutes(-5);
        var file = await SeedFileAsync(f => f.DeletedOn = deletedOn);

        var result = await _service.RollbackDeleteFileAsync(file.FileGuid, contentBlockId: 5, TestContext.Current.CancellationToken);

        Assert.False(result);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(deletedOn, saved.DeletedOn);
        _audit.DidNotReceive().Audit(Arg.Any<File>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RollbackDeleteFile_AttachedToAnotherBlock_ReturnsFalse_LeavesFileInPlace()
    {
        var file = await SeedFileAsync();
        _context.ContentBlockToFiles.Add(new ContentBlockToFile { ContentBlockId = 99, FileGuid = file.FileGuid });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.RollbackDeleteFileAsync(file.FileGuid, contentBlockId: 5, TestContext.Current.CancellationToken);

        Assert.False(result);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.DeletedOn);
        _audit.DidNotReceive().Audit(Arg.Any<File>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Restore_ClearsDeletedOn_AndAudits()
    {
        var file = await SeedFileAsync(f => f.DeletedOn = DateTime.Now);

        var result = await _service.RestoreFileAsync(file.FileGuid, TestContext.Current.CancellationToken);

        Assert.True(result);
        var saved = await _context.Files.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.DeletedOn);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.CancelDelete, Arg.Any<string>());
    }

    [Fact]
    public async Task PermanentDelete_RemovesRowAndDiskFile()
    {
        var file = await SeedFileAsync(f =>
            f.FileToPermissions.Add(new FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure" }));
        _storage.ManagedFileExists(file.FilePath).Returns(true);

        var result = await _service.PermanentlyDeleteFileAsync(file.FileGuid, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.FileToPermissions.ToListAsync(TestContext.Current.CancellationToken));
        _storage.Received(1).DeleteManagedFile(file.FilePath);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.DeleteFile, "File Deleted");
    }

    [Fact]
    public async Task PermanentDelete_StillSucceeds_WhenDiskDeleteFails()
    {
        var file = await SeedFileAsync();
        _storage.ManagedFileExists(file.FilePath).Returns(true);
        _storage.When(s => s.DeleteManagedFile(file.FilePath)).Do(_ => throw new IOException("file is locked"));

        var result = await _service.PermanentlyDeleteFileAsync(file.FileGuid, TestContext.Current.CancellationToken);

        // The record is gone by the time the disk delete runs, so a storage failure is logged
        // (orphaned bytes) rather than surfaced as a failure of the whole delete.
        Assert.True(result);
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurgeDeletedFiles_RemovesOnlyFilesPastRetention()
    {
        var stale = await SeedFileAsync(f => f.DeletedOn = DateTime.Now.AddDays(-40));
        var recent = await SeedFileAsync(f => f.DeletedOn = DateTime.Now.AddDays(-5));
        var active = await SeedFileAsync();

        var purged = await _service.PurgeDeletedFilesAsync(30, TestContext.Current.CancellationToken);

        Assert.Equal(1, purged);
        var remaining = await _context.Files.Select(f => f.FileGuid).ToListAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(stale.FileGuid, remaining);
        Assert.Contains(recent.FileGuid, remaining);
        Assert.Contains(active.FileGuid, remaining);
    }

    [Fact]
    public async Task PurgeDeletedFiles_IsolatesPerFileFailure_AndContinuesPurgingOthers()
    {
        // A transient save error (e.g. SqlException) on one file must not abort the whole
        // nightly purge run; the loop should isolate it and keep purging the rest.
        var storeName = "VIPER_" + Guid.NewGuid();
        var failing = new File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\bad.pdf",
            Folder = "cats",
            FriendlyName = "cats-bad.pdf",
            Description = "seeded",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now,
            DeletedOn = DateTime.Now.AddDays(-40)
        };
        var good = new File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = @"C:\FakeRoot\cats\good.pdf",
            Folder = "cats",
            FriendlyName = "cats-good.pdf",
            Description = "seeded",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now,
            DeletedOn = DateTime.Now.AddDays(-40)
        };
        await using (var seedContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName).Options))
        {
            seedContext.Files.AddRange(failing, good);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var partiallyFailingContext = new VIPERContext(new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(storeName)
            .AddInterceptors(new SelectiveThrowingSaveInterceptor(failing.FileGuid))
            .Options);
        var service = new CmsFileService(partiallyFailingContext, _aaudContext, _storage, _encryption, _audit,
            _userHelper, Substitute.For<ILogger<CmsFileService>>());

        var purged = await service.PurgeDeletedFilesAsync(30, TestContext.Current.CancellationToken);

        Assert.Equal(1, purged);
        var remaining = await partiallyFailingContext.Files.Select(f => f.FileGuid)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(failing.FileGuid, remaining);
        Assert.DoesNotContain(good.FileGuid, remaining);
    }

    [Fact]
    public async Task PurgeDeletedFiles_DetachesFromContentBlocks()
    {
        var file = await SeedFileAsync(f => f.DeletedOn = DateTime.Now.AddDays(-40));
        _context.ContentBlockToFiles.Add(new ContentBlockToFile
        {
            ContentBlockId = 1,
            FileGuid = file.FileGuid
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var purged = await _service.PurgeDeletedFilesAsync(30, TestContext.Current.CancellationToken);

        Assert.Equal(1, purged);
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.ContentBlockToFiles.ToListAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region CheckName

    [Fact]
    public async Task CheckName_FreeName_ReturnsNotInUse()
    {
        _storage.FileNameInUse("cats", "report.pdf").Returns(false);
        _storage.BuildManagedPath("cats", "report.pdf").Returns(@"C:\FakeRoot\cats\report.pdf");

        var dto = await _service.CheckNameAsync("cats", "report.pdf", TestContext.Current.CancellationToken);

        Assert.False(dto.InUse);
        Assert.Equal("report.pdf", dto.SuggestedName);
        Assert.Null(dto.ExistingFileGuid);
    }

    [Fact]
    public async Task CheckName_DiskConflictOnly_SuggestsRename()
    {
        _storage.BuildManagedPath("cats", "report.pdf").Returns(@"C:\FakeRoot\cats\report.pdf");
        _storage.FileNameInUse("cats", "report.pdf").Returns(true);
        _storage.GetAvailableFileName("cats", "report.pdf").Returns("report_0.pdf");

        var dto = await _service.CheckNameAsync("cats", "report.pdf", TestContext.Current.CancellationToken);

        Assert.True(dto.InUse);
        Assert.Equal("report_0.pdf", dto.SuggestedName);
        Assert.Null(dto.ExistingFileGuid);
    }

    [Fact]
    public async Task CheckName_ManagedRecordConflict_ReturnsExistingFile()
    {
        const string targetPath = @"C:\FakeRoot\cats\report.pdf";
        _storage.BuildManagedPath("cats", "report.pdf").Returns(targetPath);
        _storage.FileNameInUse("cats", "report.pdf").Returns(false);
        _storage.GetAvailableFileName("cats", "report.pdf").Returns("report_0.pdf");

        var seeded = await SeedFileAsync(f =>
        {
            f.FilePath = targetPath;
            f.FriendlyName = "cats-report.pdf";
            f.DeletedOn = null;
        });

        var dto = await _service.CheckNameAsync("cats", "report.pdf", TestContext.Current.CancellationToken);

        Assert.True(dto.InUse);
        Assert.Equal(seeded.FileGuid, dto.ExistingFileGuid);
        Assert.Equal("cats-report.pdf", dto.ExistingFriendlyName);
        Assert.False(dto.ExistingDeleted);
    }

    [Fact]
    public async Task CheckName_ManagedRecordConflict_SetsExistingDeleted_WhenSoftDeleted()
    {
        const string targetPath = @"C:\FakeRoot\cats\report.pdf";
        _storage.BuildManagedPath("cats", "report.pdf").Returns(targetPath);
        _storage.FileNameInUse("cats", "report.pdf").Returns(false);
        _storage.GetAvailableFileName("cats", "report.pdf").Returns("report_0.pdf");

        await SeedFileAsync(f =>
        {
            f.FilePath = targetPath;
            f.FriendlyName = "cats-report.pdf";
            f.DeletedOn = DateTime.Now;
        });

        var dto = await _service.CheckNameAsync("cats", "report.pdf", TestContext.Current.CancellationToken);

        Assert.True(dto.InUse);
        Assert.True(dto.ExistingDeleted);
    }

    [Fact]
    public async Task CheckName_InvalidFolder_Throws()
    {
        _storage.IsValidFolder("nope").Returns(false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CheckNameAsync("nope", "report.pdf", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckName_DisallowedFileType_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CheckNameAsync("cats", "evil.bat", TestContext.Current.CancellationToken));
    }

    #endregion

    #region List

    [Fact]
    public async Task GetFiles_FiltersByStatus()
    {
        await SeedFileAsync();
        await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-deleted.pdf";
            f.FilePath = @"C:\FakeRoot\cats\deleted.pdf";
            f.DeletedOn = DateTime.Now;
        });

        var (active, activeTotal) = await _service.GetFilesAsync(null, "active", null, null, null, 1, 50, null, false, ct: TestContext.Current.CancellationToken);
        var (deleted, deletedTotal) = await _service.GetFilesAsync(null, "deleted", null, null, null, 1, 50, null, false, ct: TestContext.Current.CancellationToken);
        var (all, allTotal) = await _service.GetFilesAsync(null, "all", null, null, null, 1, 50, null, false, ct: TestContext.Current.CancellationToken);

        Assert.Equal(1, activeTotal);
        Assert.Single(active);
        Assert.Equal(1, deletedTotal);
        Assert.Single(deleted);
        Assert.Equal(2, allTotal);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetFiles_SortsByDeletedOn_OldestFirstForPurgeViews()
    {
        await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-newer.pdf";
            f.FilePath = @"C:\FakeRoot\cats\newer.pdf";
            f.DeletedOn = DateTime.Now.AddDays(-1);
        });
        await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-older.pdf";
            f.FilePath = @"C:\FakeRoot\cats\older.pdf";
            f.DeletedOn = DateTime.Now.AddDays(-20);
        });

        var (ascending, _) = await _service.GetFilesAsync(null, "deleted", null, null, null, 1, 50,
            "deletedOn", false, ct: TestContext.Current.CancellationToken);
        var (descending, _) = await _service.GetFilesAsync(null, "deleted", null, null, null, 1, 50,
            "deletedOn", true, ct: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "cats-older.pdf", "cats-newer.pdf" }, ascending.Select(f => f.FriendlyName));
        Assert.Equal(new[] { "cats-newer.pdf", "cats-older.pdf" }, descending.Select(f => f.FriendlyName));
    }

    [Fact]
    public async Task GetFiles_Deleted_ScopedToOwner_ReturnsOnlyFilesTheyDeleted()
    {
        var mine = await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-mine.pdf";
            f.DeletedOn = DateTime.Now;
            f.ModifiedBy = "me";
        });
        await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-theirs.pdf";
            f.DeletedOn = DateTime.Now;
            f.ModifiedBy = "them";
        });

        var (files, total) = await _service.GetFilesAsync(null, "deleted", null, null, null, 1, 50, null, false,
            "me", TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Equal(mine.FileGuid, Assert.Single(files).FileGuid);
    }

    [Fact]
    public async Task GetFiles_FiltersByFolderIncludingSubfolders()
    {
        await SeedFileAsync();
        await SeedFileAsync(f =>
        {
            f.Folder = @"cats\photos";
            f.FriendlyName = "cats-photos-pic.jpg";
            f.FilePath = @"C:\FakeRoot\cats\photos\pic.jpg";
        });
        await SeedFileAsync(f =>
        {
            f.Folder = "students";
            f.FriendlyName = "students-doc.pdf";
            f.FilePath = @"C:\FakeRoot\students\doc.pdf";
        });

        var (files, total) = await _service.GetFilesAsync("cats", "active", null, null, null, 1, 50, null, false, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, total);
        Assert.All(files, f => Assert.StartsWith("cats", f.Folder));
    }

    [Fact]
    public async Task GetFiles_SearchMatchesFriendlyNameAndDescription()
    {
        await SeedFileAsync(f => f.Description = "the yearly budget");
        await SeedFileAsync(f =>
        {
            f.FriendlyName = "cats-other.pdf";
            f.FilePath = @"C:\FakeRoot\cats\other.pdf";
            f.Description = "unrelated";
        });

        var (files, total) = await _service.GetFilesAsync(null, "active", "budget", null, null, 1, 50, null, false, ct: TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Equal("cats-seeded.pdf", files[0].FriendlyName);
    }

    [Fact]
    public async Task GetFiles_PaginationAppliesSkipTake()
    {
        for (int i = 0; i < 5; i++)
        {
            var index = i;
            await SeedFileAsync(f =>
            {
                f.FriendlyName = $"cats-file{index}.pdf";
                f.FilePath = $@"C:\FakeRoot\cats\file{index}.pdf";
            });
        }

        var (page2, total) = await _service.GetFilesAsync(null, "active", null, null, null, 2, 2, "friendlyName", false, ct: TestContext.Current.CancellationToken);

        Assert.Equal(5, total);
        Assert.Equal(2, page2.Count);
        Assert.Equal("cats-file2.pdf", page2[0].FriendlyName);
    }

    [Fact]
    public async Task GetFolderCounts_RollsUpSubfolders_AndFiltersStatusEncryptedAndPublic()
    {
        await SeedFileAsync(f => f.AllowPublicAccess = true);
        await SeedFileAsync(f =>
        {
            f.Folder = @"cats\photos";
            f.FriendlyName = "cats-photos-pic.jpg";
            f.FilePath = @"C:\FakeRoot\cats\photos\pic.jpg";
        });
        await SeedFileAsync(f =>
        {
            f.Folder = "students";
            f.FriendlyName = "students-doc.pdf";
            f.FilePath = @"C:\FakeRoot\students\doc.pdf";
            f.Encrypted = true;
        });
        await SeedFileAsync(f =>
        {
            f.Folder = "students";
            f.FriendlyName = "students-deleted.pdf";
            f.FilePath = @"C:\FakeRoot\students\deleted.pdf";
            f.DeletedOn = DateTime.Now;
        });

        var active = await _service.GetFolderCountsAsync("active", null, null, ct: TestContext.Current.CancellationToken);
        var unencrypted = await _service.GetFolderCountsAsync("active", false, null, ct: TestContext.Current.CancellationToken);
        var publicOnly = await _service.GetFolderCountsAsync("active", null, true, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, active.Count);
        Assert.Equal(2, active.Single(c => c.Folder == "cats").Count);
        Assert.Equal(1, active.Single(c => c.Folder == "students").Count);
        var onlyCats = Assert.Single(unencrypted);
        Assert.Equal("cats", onlyCats.Folder);
        Assert.Equal(2, onlyCats.Count);
        var publicCats = Assert.Single(publicOnly);
        Assert.Equal("cats", publicCats.Folder);
        Assert.Equal(1, publicCats.Count);
    }

    [Fact]
    public async Task GetFolderCounts_MergesFolderCasings()
    {
        await SeedFileAsync(f => f.Folder = "Accreditation");
        await SeedFileAsync(f =>
        {
            f.Folder = "accreditation";
            f.FriendlyName = "accreditation-two.pdf";
            f.FilePath = @"C:\FakeRoot\accreditation\two.pdf";
        });

        var counts = await _service.GetFolderCountsAsync("active", null, null, ct: TestContext.Current.CancellationToken);

        var entry = Assert.Single(counts);
        Assert.Equal(2, entry.Count);
    }

    #endregion
}
