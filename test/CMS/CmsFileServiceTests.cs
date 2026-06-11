using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        _service = new CmsFileService(_context, _aaudContext, _storage, _encryption, _audit, _userHelper);
    }

    public void Dispose()
    {
        _context.Dispose();
        _aaudContext.Dispose();
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
    public async Task UpdateFile_UnknownGuid_ReturnsNull()
    {
        var dto = await _service.UpdateFileAsync(Guid.NewGuid(), new CmsFileUpdateRequest(), null, TestContext.Current.CancellationToken);

        Assert.Null(dto);
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

    #endregion
}
