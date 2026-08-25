using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhonePersonLookupService: GetPhonePeople implements the same
/// permission-based DirectPhone masking as PhoneListUnitService, but as an independent code
/// path used by the person-picker autocomplete, so a regression there wouldn't be caught by
/// the PhoneListUnitService tests. GetViperCurrentEmployees layers PersonSearchHelper on top
/// of the CurrentEmployee filter.
/// </summary>
public sealed class PhonePersonLookupServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhonePersonLookupService _service;

    private const string MaintainRole = "SVMSecure.PhoneLists.VMDOMaintain";
    private const string CallerIam = "caller01";

    public PhonePersonLookupServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        _userHelper = Substitute.For<IUserHelper>();
        _userHelper.GetCurrentUser().Returns(new AaudUser
        {
            ClientId = "ucd.edu",
            MothraId = CallerIam,
            LastName = "Caller",
            FirstName = "Test",
            DisplayLastName = "Caller",
            DisplayFirstName = "Test",
            DisplayFullName = "Test Caller",
            IamId = CallerIam,
        });

        var rapsContext = Substitute.For<RAPSContext>();
        var permissionsService = new PhonesPermissionsService(rapsContext, _userHelper);
        _service = new PhonePersonLookupService(_context, permissionsService);

        _context.PhoneList.Add(new PhoneList { PhoneListId = 1, Code = "VMDO", Name = "Dean's Office", MaintainRole = MaintainRole });
        _context.PhonePerson.Add(new PhonePerson
        {
            PersonIam = "person01",
            Phone = "530-555-1000",
            DirectPhone = "530-555-2000",
        });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    /// <summary>The seeded list, as the controller would hand it to the service.</summary>
    private PhoneList TestList() =>
        _context.PhoneList.Single(l => l.PhoneListId == 1);

    [Fact]
    public async Task GetPhonePeople_MasksDirectPhone_WhenNoListSupplied()
    {
        var results = await _service.GetPhonePeople(["person01"], ct: TestContext.Current.CancellationToken);

        var person = Assert.Single(results);
        Assert.Equal("", person.DirectPhone);
    }

    [Fact]
    public async Task GetPhonePeople_MasksDirectPhone_WhenCallerLacksMaintainAccessToList()
    {
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), MaintainRole).Returns(false);

        var results = await _service.GetPhonePeople(["person01"], TestList(), ct: TestContext.Current.CancellationToken);

        var person = Assert.Single(results);
        Assert.Equal("", person.DirectPhone);
    }

    [Fact]
    public async Task GetPhonePeople_ShowsDirectPhone_WhenCallerHasMaintainAccessToList()
    {
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), MaintainRole).Returns(true);

        var results = await _service.GetPhonePeople(["person01"], TestList(), ct: TestContext.Current.CancellationToken);

        var person = Assert.Single(results);
        Assert.Equal("530-555-2000", person.DirectPhone);
    }

    [Fact]
    public async Task GetPhonePeople_IgnoresBlankAndWhitespaceIamIds()
    {
        var results = await _service.GetPhonePeople(
            ["person01", "", "   ", null!],
            ct: TestContext.Current.CancellationToken);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetViperCurrentEmployees_ReturnsEmpty_WhenSearchBelowMinimumLength()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 1,
            IamId = "person01",
            FirstName = "Amy",
            LastName = "Smith",
            FullName = "Amy Smith",
            CurrentEmployee = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await _service.GetViperCurrentEmployees("a", TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetViperCurrentEmployees_ExcludesFormerEmployees()
    {
        _context.ViperPerson.AddRange(
            new ViperPerson
            {
                PersonId = 1,
                IamId = "person01",
                FirstName = "Amy",
                LastName = "Smith",
                FullName = "Amy Smith",
                CurrentEmployee = true,
            },
            new ViperPerson
            {
                PersonId = 2,
                IamId = "person02",
                FirstName = "Amy",
                LastName = "Smithson",
                FullName = "Amy Smithson",
                CurrentEmployee = false,
            }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await _service.GetViperCurrentEmployees("Smith", TestContext.Current.CancellationToken);

        var match = Assert.Single(results);
        Assert.Equal("person01", match.IamId);
    }
}
