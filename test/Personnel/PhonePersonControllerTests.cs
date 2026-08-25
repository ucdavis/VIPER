using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Controller tests for PhonePersonController, the person picker behind the phone-record dialogs.
/// The controller does the merge itself rather than delegating it: two independent queries (people
/// from users.Person, phone rows from phones.Person) are joined in memory, so the cases worth
/// pinning are the ones the join can get wrong - a person with no phone row, a phone row with no
/// matching person - plus the direct-number masking, which depends on a list code supplied by the
/// caller and so must fail closed when that code is absent or bogus.
/// </summary>
public sealed class PhonePersonControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhonePersonController _controller;

    private const string VmdoRole = "SVMSecure.PhoneLists.VMDOMaintain";

    public PhonePersonControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        _userHelper = Substitute.For<IUserHelper>();
        _userHelper.GetCurrentUser().Returns(new AaudUser
        {
            ClientId = "ucd.edu",
            MothraId = "caller01",
            LastName = "Caller",
            FirstName = "Test",
            DisplayLastName = "Caller",
            DisplayFirstName = "Test",
            DisplayFullName = "Test Caller",
            IamId = "caller01",
        });

        var rapsContext = Substitute.For<RAPSContext>();
        var permissionsService = new PhonePermissionsService(rapsContext, _userHelper);
        var phoneListService = new PhoneListService(_context);
        var lookupService = new PhonePersonLookupService(_context, permissionsService);

        _controller = new PhonePersonController(phoneListService, lookupService);

        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 1,
            Code = "VMDO",
            Name = "Dean's Office",
            MaintainRole = VmdoRole,
        });
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 1,
            IamId = "person01",
            FirstName = "Ada",
            LastName = "Smithers",
            FullName = "Ada Smithers",
            CurrentEmployee = true,
            MailId = "asmithers",
        });
        _context.PhonePerson.Add(new PhonePerson
        {
            PersonIam = "person01",
            Phone = "530-555-1000",
            DirectPhone = "530-555-2000",
        });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private void GrantRole(string role)
    {
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), role).Returns(true);
    }

    private async Task<List<AugmentedViperPerson>> Search(string search, string? listCode = null)
    {
        var result = await _controller.GetCurrentEmployees(search, listCode, TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<List<AugmentedViperPerson>>(okResult.Value);
    }

    [Fact]
    public async Task GetCurrentEmployees_MergesPhoneDataOntoTheMatchedPerson()
    {
        var results = await Search("Smithers");

        var person = Assert.Single(results);
        Assert.Equal("person01", person.IamId);
        Assert.Equal("Ada Smithers", person.FullName);
        Assert.NotNull(person.PhoneData);
        Assert.Equal("530-555-1000", person.PhoneData.Phone);
    }

    [Fact]
    public async Task GetCurrentEmployees_MasksDirectPhone_WhenNoListIsNamed()
    {
        var results = await Search("Smithers");

        Assert.Equal("", Assert.Single(results).PhoneData?.DirectPhone);
    }

    [Fact]
    public async Task GetCurrentEmployees_MasksDirectPhone_ForANonMaintainer()
    {
        var results = await Search("Smithers", "VMDO");

        Assert.Equal("", Assert.Single(results).PhoneData?.DirectPhone);
    }

    [Fact]
    public async Task GetCurrentEmployees_ReturnsDirectPhone_ForAMaintainerOfTheNamedList()
    {
        GrantRole(VmdoRole);

        var results = await Search("Smithers", "VMDO");

        Assert.Equal("530-555-2000", Assert.Single(results).PhoneData?.DirectPhone);
    }

    [Fact]
    public async Task GetCurrentEmployees_MasksDirectPhone_ForAnUnknownListCode()
    {
        // An unresolvable code drops to "no list", not "no permission check": holding the role
        // must not be enough on its own, since the code is caller-supplied.
        GrantRole(VmdoRole);

        var results = await Search("Smithers", "NOPE");

        Assert.Equal("", Assert.Single(results).PhoneData?.DirectPhone);
    }

    [Fact]
    public async Task GetCurrentEmployees_ReturnsThePerson_WhenTheyHaveNoPhoneRow()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 2,
            IamId = "person02",
            FirstName = "Bo",
            LastName = "Smithfield",
            FullName = "Bo Smithfield",
            CurrentEmployee = true,
            MailId = "bsmithfield",
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await Search("Smithfield");

        var person = Assert.Single(results);
        Assert.Equal("person02", person.IamId);
        Assert.Null(person.PhoneData);
    }

    [Fact]
    public async Task GetCurrentEmployees_IgnoresPhoneRowsWithNoMatchingPerson()
    {
        // phones.Person outlives users.Person entries, so an orphaned phone row must not
        // materialize as a pickable person.
        _context.PhonePerson.Add(new PhonePerson { PersonIam = "ghost01", Phone = "530-555-9999" });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await Search("Smithers");

        Assert.Equal("person01", Assert.Single(results).IamId);
    }

    [Fact]
    public async Task GetCurrentEmployees_ExcludesFormerEmployees()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 3,
            IamId = "person03",
            FirstName = "Cy",
            LastName = "Smithson",
            FullName = "Cy Smithson",
            CurrentEmployee = false,
            MailId = "csmithson",
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await Search("Smithson"));
    }

    [Fact]
    public async Task GetCurrentEmployees_ReturnsEmpty_ForASearchTermBelowTheMinimumLength()
    {
        Assert.Empty(await Search("S"));
    }
}
