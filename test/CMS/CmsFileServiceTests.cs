using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;
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
    private readonly CmsFileService _service;
    private readonly List<VIPERContext> _extraContexts = new();

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
            .Returns(callInfo => Task.FromResult(@"C:\FakeTemp\" + Guid.NewGuid().ToString("N")));
        _storage.MoveIntoPlace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(callInfo => Path.Join(@"C:\FakeRoot", callInfo.ArgAt<string>(1), Path.GetFileName(callInfo.ArgAt<string>(2))));

        _encryption = Substitute.For<ICmsFileEncryptionService>();
        _encryption.GenerateKeyForDb().Returns("encrypted-db-key");

        _audit = Substitute.For<ICmsFileAuditService>();
        _userHelper = Substitute.For<IUserHelper>();

        _service = new CmsFileService(_context, _aaudContext, _storage, _encryption, _audit, _userHelper,
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
    }

    private static IFormFile MakeFormFile(string fileName = "report.pdf")
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

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
            ModifiedOn = DateTime.Now.AddDays(-1)
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
            ModifiedOn = DateTime.Now.AddDays(-1)
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
            () => _service.CreateFileAsync(request, MakeFormFile("report.pdf"), TestContext.Current.CancellationToken));

        _storage.Received(1).DeleteManagedFile(@"C:\FakeRoot\cats\report.pdf");
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

        await _service.CreateFileAsync(request, MakeFormFile("report.pdf"), TestContext.Current.CancellationToken);

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

        // Seed a Files row whose FilePath matches the target path.
        await SeedFileAsync(f =>
        {
            f.FilePath = targetPath;
            f.FriendlyName = "cats-report.pdf";
        });

        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFileAsync(request, MakeFormFile("report.pdf"), TestContext.Current.CancellationToken));

        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CreateFile_OverwriteWithoutConflict_StoresNormally()
    {
        var request = new CmsFileCreateRequest { Folder = "cats", Overwrite = true };

        await _service.CreateFileAsync(request, MakeFormFile("report.pdf"), TestContext.Current.CancellationToken);

        _storage.Received(1).MoveIntoPlace(Arg.Any<string>(), "cats", "report.pdf", Arg.Any<bool>());
        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateFile_AppliesPermissionDeltas()
    {
        var file = await SeedFileAsync(f =>
        {
            f.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure.Old" });
            f.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure.Keep" });
        });
        var request = new CmsFileUpdateRequest
        {
            Description = "seeded",
            Permissions = new List<string> { "SVMSecure.Keep", "SVMSecure.New" }
        };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Equal(new List<string> { "SVMSecure.Keep", "SVMSecure.New" }, dto!.Permissions);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.EditFile,
            Arg.Is<string>(d => d.Contains("Permission removed: SVMSecure.Old") && d.Contains("Permission added: SVMSecure.New")));
    }

    [Fact]
    public async Task UpdateFile_AppliesPersonDeltas()
    {
        var file = await SeedFileAsync(f =>
            f.FileToPeople.Add(new Viper.Models.VIPER.FileToPerson { FileGuid = f.FileGuid, IamId = "100OLD" }));
        var request = new CmsFileUpdateRequest
        {
            Description = "seeded",
            IamIds = new List<string> { "100NEW" }
        };

        var dto = await _service.UpdateFileAsync(file.FileGuid, request, null, TestContext.Current.CancellationToken);

        Assert.NotNull(dto);
        Assert.Single(dto!.People);
        Assert.Equal("100NEW", dto.People[0].IamId);
    }

    [Fact]
    public async Task UpdateFile_EncryptToggleOn_EncryptsExistingFile()
    {
        var file = await SeedFileAsync();
        _storage.ManagedFileExists(file.FilePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = true };

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
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = false };

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
        var request = new CmsFileUpdateRequest { Description = "seeded" };

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
        var request = new CmsFileUpdateRequest { Description = "seeded" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateFileAsync(file.FileGuid, request, MakeFormFile("newversion.png"), TestContext.Current.CancellationToken));
        _storage.DidNotReceive().ReplaceInPlace(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateFile_WithReplacementUpload_RestoresSoftDeletedFile()
    {
        var file = await SeedFileAsync(f => f.DeletedOn = DateTime.Now);
        var request = new CmsFileUpdateRequest { Description = "seeded" };

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
        var request = new CmsFileUpdateRequest { Description = "updated" };

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
    public async Task UpdateFile_EncryptToggleOn_SaveFails_DecryptsFileBack()
    {
        var (service, fileGuid, filePath) = await BuildServiceOverFailingSaveAsync();
        _storage.ManagedFileExists(filePath).Returns(true);
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = true };

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
        var request = new CmsFileUpdateRequest { Description = "seeded", Encrypt = false };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateFileAsync(fileGuid, request, null, TestContext.Current.CancellationToken));

        // The file was decrypted on disk but the save failed, so it must be re-encrypted with the
        // original key to stay consistent with the rolled-back (still-encrypted) database row.
        _encryption.Received(1).EncryptFileInPlace(filePath, "old-key");
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
            f.FileToPermissions.Add(new Viper.Models.VIPER.FileToPermission { FileGuid = f.FileGuid, Permission = "SVMSecure" }));
        _storage.ManagedFileExists(file.FilePath).Returns(true);

        var result = await _service.PermanentlyDeleteFileAsync(file.FileGuid, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Empty(await _context.Files.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await _context.FileToPermissions.ToListAsync(TestContext.Current.CancellationToken));
        _storage.Received(1).DeleteManagedFile(file.FilePath);
        _audit.Received(1).Audit(Arg.Any<File>(), CmsFileAuditActions.DeleteFile, "File Deleted");
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

        var (active, activeTotal) = await _service.GetFilesAsync(null, "active", null, null, null, 1, 50, null, false, TestContext.Current.CancellationToken);
        var (deleted, deletedTotal) = await _service.GetFilesAsync(null, "deleted", null, null, null, 1, 50, null, false, TestContext.Current.CancellationToken);
        var (all, allTotal) = await _service.GetFilesAsync(null, "all", null, null, null, 1, 50, null, false, TestContext.Current.CancellationToken);

        Assert.Equal(1, activeTotal);
        Assert.Single(active);
        Assert.Equal(1, deletedTotal);
        Assert.Single(deleted);
        Assert.Equal(2, allTotal);
        Assert.Equal(2, all.Count);
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

        var (files, total) = await _service.GetFilesAsync("cats", "active", null, null, null, 1, 50, null, false, TestContext.Current.CancellationToken);

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

        var (files, total) = await _service.GetFilesAsync(null, "active", "budget", null, null, 1, 50, null, false, TestContext.Current.CancellationToken);

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

        var (page2, total) = await _service.GetFilesAsync(null, "active", null, null, null, 2, 2, "friendlyName", false, TestContext.Current.CancellationToken);

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

        var active = await _service.GetFolderCountsAsync("active", null, null, TestContext.Current.CancellationToken);
        var unencrypted = await _service.GetFolderCountsAsync("active", false, null, TestContext.Current.CancellationToken);
        var publicOnly = await _service.GetFolderCountsAsync("active", null, true, TestContext.Current.CancellationToken);

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

        var counts = await _service.GetFolderCountsAsync("active", null, null, TestContext.Current.CancellationToken);

        var entry = Assert.Single(counts);
        Assert.Equal(2, entry.Count);
    }

    #endregion
}
