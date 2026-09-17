using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.CMS.Services;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.CMS;

/// <summary>
/// Tests for CmsUserPhotoService: AAUD id resolution (MailId, LoginId, IamId, MothraId),
/// alternate ProfilePhotos lookup with traversal protection, delegation to the Students photo
/// pipeline, and the Last-Modified timestamp used for conditional-caching (FIX 4).
/// GetUserPhotoAsync (the primary/id-card photo) always returns a result, falling back to the
/// default "no picture" image, which makes it the right choice for most places a photo is
/// displayed. GetAlternatePhotoAsync has no such fallback: a missing/rejected alt photo returns
/// null rather than the primary id-card photo, so a caller can tell "no alt photo" apart from a
/// successful lookup instead of the two being indistinguishable.
/// </summary>
public sealed class CmsUserPhotoServiceTests : IDisposable
{
    private readonly string _profilePhotoRoot;
    private readonly AAUDContext _aaudContext;
    private readonly IPhotoService _photoService;
    private readonly CmsUserPhotoService _service;

    private static readonly byte[] IdCardPhotoBytes = { 1, 1, 1 };
    private static readonly byte[] AltPhotoBytes = { 2, 2, 2 };

    public CmsUserPhotoServiceTests()
    {
        _profilePhotoRoot = Path.Join(Path.GetTempPath(), "ViperCmsPhotoTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_profilePhotoRoot);

        _aaudContext = new AAUDContext(new DbContextOptionsBuilder<AAUDContext>()
            .UseInMemoryDatabase("AAUD_" + Guid.NewGuid()).Options);

        _photoService = Substitute.For<IPhotoService>();
        _photoService.GetStudentPhotoAsync(Arg.Any<string>()).Returns(Task.FromResult(IdCardPhotoBytes));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CMS:ProfilePhotoPath"] = _profilePhotoRoot })
            .Build();

        _service = new CmsUserPhotoService(_aaudContext, _photoService, configuration,
            Substitute.For<ILogger<CmsUserPhotoService>>());
    }

    public void Dispose()
    {
        _aaudContext.Dispose();
        if (Directory.Exists(_profilePhotoRoot))
        {
            Directory.Delete(_profilePhotoRoot, recursive: true);
        }
    }

    private void SeedUser(string loginId = "jdoe", string mailId = "jdoe", string iamId = "1000999",
        string mothraId = "m1")
    {
        _aaudContext.AaudUsers.Add(new AaudUser
        {
            AaudUserId = 1,
            ClientId = "test-client",
            LoginId = loginId,
            MailId = mailId,
            IamId = iamId,
            Current = 1,
            LastName = "Doe",
            FirstName = "Jane",
            DisplayLastName = "Doe",
            DisplayFirstName = "Jane",
            DisplayFullName = "Doe, Jane",
            MothraId = mothraId,
            SpridenId = null,
            Pidm = null,
            EmployeeId = null,
            VmacsId = null,
            VmcasId = null,
            UnexId = null
        });
        _aaudContext.SaveChanges();
    }

    [Fact]
    public async Task GetUserPhoto_ByMailId_DelegatesToStudentPipeline()
    {
        var photo = await _service.GetUserPhotoAsync("jdoe", null, null, null,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo.Bytes);
        await _photoService.Received(1).GetStudentPhotoAsync("jdoe");
    }

    [Fact]
    public async Task GetUserPhoto_ByLoginId_ResolvesMailIdThroughAaud()
    {
        SeedUser(loginId: "jdoe", mailId: "janedoe");

        await _service.GetUserPhotoAsync(null, "jdoe", null, null,
            TestContext.Current.CancellationToken);

        await _photoService.Received(1).GetStudentPhotoAsync("janedoe");
    }

    [Fact]
    public async Task GetUserPhoto_ByMothraId_ResolvesMailIdThroughAaud()
    {
        SeedUser(mothraId: "m-9001", mailId: "janedoe");

        await _service.GetUserPhotoAsync(null, null, null, "m-9001",
            TestContext.Current.CancellationToken);

        await _photoService.Received(1).GetStudentPhotoAsync("janedoe");
    }

    [Fact]
    public async Task GetUserPhoto_UnknownMothraId_FallsBackToDefaultPipeline()
    {
        var photo = await _service.GetUserPhotoAsync(null, null, null, "no-such-mothra",
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo.Bytes);
        await _photoService.Received(1).GetStudentPhotoAsync(string.Empty);
    }

    [Fact]
    public async Task GetUserPhoto_UnknownLoginId_FallsBackToDefaultPipeline()
    {
        var photo = await _service.GetUserPhotoAsync(null, "nosuchuser", null, null,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo.Bytes);
        await _photoService.Received(1).GetStudentPhotoAsync(string.Empty);
    }

    [Fact]
    public async Task GetUserPhoto_IdCardPhoto_LastModifiedIsStableAcrossCalls()
    {
        // The delegated Students pipeline exposes no per-file timestamp, so id-card/nopic
        // responses use a stable per-process proxy; repeated calls must agree.
        var first = await _service.GetUserPhotoAsync("jdoe", null, null, null,
            TestContext.Current.CancellationToken);
        var second = await _service.GetUserPhotoAsync("jdoe", null, null, null,
            TestContext.Current.CancellationToken);

        Assert.Equal(first.LastModified, second.LastModified);
        Assert.Equal(0, first.LastModified.Millisecond);
    }

    [Fact]
    public async Task GetAlternatePhoto_ServesProfilePhoto()
    {
        SeedUser(iamId: "1000999");
        await File.WriteAllBytesAsync(Path.Join(_profilePhotoRoot, "1000999.jpg"), AltPhotoBytes,
            TestContext.Current.CancellationToken);

        var photo = await _service.GetAlternatePhotoAsync(null, "jdoe", null, null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(photo);
        Assert.Equal(AltPhotoBytes, photo.Bytes);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAlternatePhoto_ByMailId_ResolvesIamIdAndServesProfilePhoto()
    {
        SeedUser(mailId: "jdoe", iamId: "1000999");
        await File.WriteAllBytesAsync(Path.Join(_profilePhotoRoot, "1000999.jpg"), AltPhotoBytes,
            TestContext.Current.CancellationToken);

        var photo = await _service.GetAlternatePhotoAsync("jdoe", null, null, null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(photo);
        Assert.Equal(AltPhotoBytes, photo.Bytes);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAlternatePhoto_Missing_ReturnsNull()
    {
        SeedUser(mailId: "janedoe", iamId: "1000999");

        var photo = await _service.GetAlternatePhotoAsync(null, "jdoe", null, null,
            TestContext.Current.CancellationToken);

        // Unlike the primary id-card photo, the alternate photo has no placeholder fallback -
        // a missing alt photo means "don't render this image", not "show the primary instead".
        Assert.Null(photo);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Theory]
    [InlineData("../1000999")]
    [InlineData("..\\secrets")]
    [InlineData("a/b")]
    public async Task GetAlternatePhoto_TraversalShapedIamId_ReturnsNull(string iamId)
    {
        var photo = await _service.GetAlternatePhotoAsync(null, null, iamId, null,
            TestContext.Current.CancellationToken);

        Assert.Null(photo);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAlternatePhoto_LastModifiedReflectsFileWriteTime_TruncatedToWholeSeconds()
    {
        SeedUser(iamId: "1000999");
        var photoPath = Path.Join(_profilePhotoRoot, "1000999.jpg");
        await File.WriteAllBytesAsync(photoPath, AltPhotoBytes, TestContext.Current.CancellationToken);
        var fileWriteTime = File.GetLastWriteTimeUtc(photoPath);

        var photo = await _service.GetAlternatePhotoAsync(null, "jdoe", null, null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(photo);
        Assert.Equal(0, photo.LastModified.Millisecond);
        Assert.True(Math.Abs((photo.LastModified.UtcDateTime - fileWriteTime).TotalSeconds) < 1.5);
    }
}
