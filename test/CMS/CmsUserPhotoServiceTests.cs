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
/// Tests for CmsUserPhotoService: AAUD id resolution, alternate ProfilePhotos lookup with
/// traversal protection, and delegation to the Students photo pipeline.
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

    private void SeedUser(string loginId = "jdoe", string mailId = "jdoe", string iamId = "1000999")
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
            MothraId = "m1",
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
        var photo = await _service.GetUserPhotoAsync("jdoe", null, null, preferAltPhoto: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo);
        await _photoService.Received(1).GetStudentPhotoAsync("jdoe");
    }

    [Fact]
    public async Task GetUserPhoto_ByLoginId_ResolvesMailIdThroughAaud()
    {
        SeedUser(loginId: "jdoe", mailId: "janedoe");

        await _service.GetUserPhotoAsync(null, "jdoe", null, preferAltPhoto: false,
            TestContext.Current.CancellationToken);

        await _photoService.Received(1).GetStudentPhotoAsync("janedoe");
    }

    [Fact]
    public async Task GetUserPhoto_UnknownLoginId_FallsBackToDefaultPipeline()
    {
        var photo = await _service.GetUserPhotoAsync(null, "nosuchuser", null, preferAltPhoto: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo);
        await _photoService.Received(1).GetStudentPhotoAsync(string.Empty);
    }

    [Fact]
    public async Task GetUserPhoto_PreferAltPhoto_ServesProfilePhoto()
    {
        SeedUser(iamId: "1000999");
        await File.WriteAllBytesAsync(Path.Join(_profilePhotoRoot, "1000999.jpg"), AltPhotoBytes,
            TestContext.Current.CancellationToken);

        var photo = await _service.GetUserPhotoAsync(null, "jdoe", null, preferAltPhoto: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(AltPhotoBytes, photo);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetUserPhoto_ByMailIdPreferAltPhoto_ResolvesIamIdAndServesProfilePhoto()
    {
        SeedUser(mailId: "jdoe", iamId: "1000999");
        await File.WriteAllBytesAsync(Path.Join(_profilePhotoRoot, "1000999.jpg"), AltPhotoBytes,
            TestContext.Current.CancellationToken);

        var photo = await _service.GetUserPhotoAsync("jdoe", null, null, preferAltPhoto: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(AltPhotoBytes, photo);
        await _photoService.DidNotReceive().GetStudentPhotoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetUserPhoto_PreferAltPhoto_MissingAltFallsBackToIdCard()
    {
        SeedUser(mailId: "janedoe", iamId: "1000999");

        var photo = await _service.GetUserPhotoAsync(null, "jdoe", null, preferAltPhoto: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo);
        await _photoService.Received(1).GetStudentPhotoAsync("janedoe");
    }

    [Theory]
    [InlineData("../1000999")]
    [InlineData("..\\secrets")]
    [InlineData("a/b")]
    public async Task GetUserPhoto_TraversalShapedIamId_IgnoresAltPhoto(string iamId)
    {
        var photo = await _service.GetUserPhotoAsync(null, null, iamId, preferAltPhoto: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(IdCardPhotoBytes, photo);
    }
}
