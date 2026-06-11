using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsFileStorageService: folder validation (user input must never escape the
/// storage root), name-collision handling, and managed-file containment checks.
/// </summary>
public sealed class CmsFileStorageServiceTests : IDisposable
{
    private readonly string _root;
    private readonly VIPERContext _context;
    private readonly CmsFileStorageService _service;

    public CmsFileStorageServiceTests()
    {
        _root = Path.Join(Path.GetTempPath(), "ViperCmsStorageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Join(_root, "cats"));
        Directory.CreateDirectory(Path.Join(_root, "students"));

        var options = new DbContextOptionsBuilder<VIPERContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new VIPERContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CMS:FileStorageRoot"] = _root })
            .Build();
        _service = new CmsFileStorageService(_context, configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static string CreateTempFile(string contents = "test contents")
    {
        var path = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, contents);
        return path;
    }

    #region GetTopLevelFolders / IsValidFolder

    [Fact]
    public void GetTopLevelFolders_ReturnsDirectoriesUnderRoot()
    {
        var folders = _service.GetTopLevelFolders();

        Assert.Equal(new[] { "cats", "students" }, folders);
    }

    [Theory]
    [InlineData("cats")]
    [InlineData("students")]
    [InlineData(@"cats\photos")]
    [InlineData("cats/photos/2026")]
    public void IsValidFolder_AcceptsExistingTopLevelAndSubfolders(string folder)
    {
        Assert.True(_service.IsValidFolder(folder));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("doesnotexist")]
    [InlineData(@"..\cats")]
    [InlineData(@"cats\..\..\evil")]
    [InlineData(@"cats\.")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"\\server\share")]
    [InlineData("cats\\sub<dir>")]
    public void IsValidFolder_RejectsUnknownTraversalAndInvalid(string folder)
    {
        Assert.False(_service.IsValidFolder(folder));
    }

    #endregion

    #region MoveIntoPlace

    [Fact]
    public void MoveIntoPlace_MovesTempFileIntoFolder()
    {
        var temp = CreateTempFile();

        var finalPath = _service.MoveIntoPlace(temp, "cats", "report.pdf", makeUnique: false);

        Assert.Equal(Path.Join(_root, "cats", "report.pdf"), finalPath);
        Assert.True(File.Exists(finalPath));
        Assert.False(File.Exists(temp));
    }

    [Fact]
    public void MoveIntoPlace_CreatesSubfolders()
    {
        var temp = CreateTempFile();

        var finalPath = _service.MoveIntoPlace(temp, @"cats\photos", "pic.jpg", makeUnique: false);

        Assert.Equal(Path.Join(_root, "cats", "photos", "pic.jpg"), finalPath);
        Assert.True(File.Exists(finalPath));
    }

    [Fact]
    public void MoveIntoPlace_Conflict_WithoutMakeUnique_Throws()
    {
        File.WriteAllText(Path.Join(_root, "cats", "report.pdf"), "existing");
        var temp = CreateTempFile();

        Assert.Throws<InvalidOperationException>(() => _service.MoveIntoPlace(temp, "cats", "report.pdf", makeUnique: false));
    }

    [Fact]
    public void MoveIntoPlace_Conflict_WithMakeUnique_AppendsCounter()
    {
        File.WriteAllText(Path.Join(_root, "cats", "report.pdf"), "existing");
        File.WriteAllText(Path.Join(_root, "cats", "report_0.pdf"), "existing");
        var temp = CreateTempFile();

        var finalPath = _service.MoveIntoPlace(temp, "cats", "report.pdf", makeUnique: true);

        Assert.Equal(Path.Join(_root, "cats", "report_1.pdf"), finalPath);
    }

    [Fact]
    public void MoveIntoPlace_StripsPathComponentsFromFileName()
    {
        var temp = CreateTempFile();

        var finalPath = _service.MoveIntoPlace(temp, "cats", @"..\..\evil.pdf", makeUnique: false);

        Assert.Equal(Path.Join(_root, "cats", "evil.pdf"), finalPath);
    }

    [Theory]
    [InlineData("malware.bat")]
    [InlineData("script.ps1")]
    [InlineData("noextension")]
    public void MoveIntoPlace_DisallowedExtension_Throws(string fileName)
    {
        var temp = CreateTempFile();

        try
        {
            Assert.Throws<ArgumentException>(() => _service.MoveIntoPlace(temp, "cats", fileName, makeUnique: false));
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void MoveIntoPlace_InvalidFolder_Throws()
    {
        var temp = CreateTempFile();

        try
        {
            Assert.Throws<ArgumentException>(() => _service.MoveIntoPlace(temp, @"..\evil", "report.pdf", makeUnique: false));
        }
        finally
        {
            File.Delete(temp);
        }
    }

    #endregion

    #region FileNameInUse

    [Fact]
    public void FileNameInUse_DetectsFileOnDisk()
    {
        File.WriteAllText(Path.Join(_root, "cats", "ondisk.pdf"), "x");

        Assert.True(_service.FileNameInUse("cats", "ondisk.pdf"));
        Assert.False(_service.FileNameInUse("cats", "notthere.pdf"));
    }

    [Fact]
    public void FileNameInUse_DetectsDatabaseRecordWithoutDiskFile()
    {
        _context.Files.Add(new Viper.Models.VIPER.File
        {
            FileGuid = Guid.NewGuid(),
            FilePath = Path.Join(_root, "cats", "dbonly.pdf"),
            Folder = "cats",
            FriendlyName = "cats-dbonly.pdf",
            Description = "",
            ModifiedBy = "test",
            ModifiedOn = DateTime.Now
        });
        _context.SaveChanges();

        Assert.True(_service.FileNameInUse("cats", "dbonly.pdf"));
    }

    #endregion

    #region SaveToTempAsync / ReplaceInPlace / DeleteManagedFile

    [Fact]
    public async Task SaveToTempAsync_WritesUploadOutsideStorageRoot()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFile = new FormFile(stream, 0, stream.Length, "file", "upload.pdf");

        var tempPath = await _service.SaveToTempAsync(formFile, TestContext.Current.CancellationToken);

        try
        {
            Assert.True(File.Exists(tempPath));
            Assert.False(tempPath.StartsWith(_root, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ReplaceInPlace_OverwritesManagedFile()
    {
        var managed = Path.Join(_root, "cats", "replace.pdf");
        File.WriteAllText(managed, "old");
        var temp = CreateTempFile("new");

        _service.ReplaceInPlace(temp, managed);

        Assert.Equal("new", File.ReadAllText(managed));
        Assert.False(File.Exists(temp));
    }

    [Fact]
    public void ReplaceInPlace_TargetOutsideRoot_Throws()
    {
        var outside = CreateTempFile("victim");
        var temp = CreateTempFile("new");

        try
        {
            Assert.Throws<ArgumentException>(() => _service.ReplaceInPlace(temp, outside));
        }
        finally
        {
            File.Delete(outside);
            File.Delete(temp);
        }
    }

    [Fact]
    public void DeleteManagedFile_DeletesUnderRoot()
    {
        var managed = Path.Join(_root, "cats", "todelete.pdf");
        File.WriteAllText(managed, "x");

        _service.DeleteManagedFile(managed);

        Assert.False(File.Exists(managed));
    }

    [Fact]
    public void DeleteManagedFile_OutsideRoot_Throws()
    {
        var outside = CreateTempFile();

        try
        {
            Assert.Throws<ArgumentException>(() => _service.DeleteManagedFile(outside));
            Assert.True(File.Exists(outside));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    #endregion
}
