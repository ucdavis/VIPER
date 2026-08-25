using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhoneListUnitService, focused on the direct-phone visibility rule:
/// a caller may only see DirectPhone for a list if they hold the list's maintain
/// permission, OR they are themselves an active member of that list.
/// </summary>
public sealed class PhoneListUnitServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhoneListUnitService _service;

    private const string MaintainRole = "SVMSecure.PhoneLists.VMDOMaintain";
    private const string CallerIam = "caller01";

    public PhoneListUnitServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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

        // rapsContext is unused when IUserHelper.HasPermission is mocked directly,
        // so a bare substitute (no seeded roles) is sufficient here.
        var rapsContext = Substitute.For<RAPSContext>();
        var permissionsService = new PhonesPermissionsService(rapsContext, _userHelper);

        _service = new PhoneListUnitService(_context, _userHelper, permissionsService);

        SeedList();
    }

    public void Dispose() => _context.Dispose();

    /// <summary>The seeded list, as the controller would hand it to the service.</summary>
    private PhoneList TestList() =>
        _context.PhoneList.Single(l => l.PhoneListId == 1);

    private void SeedList()
    {
        _context.PhoneList.Add(new PhoneList { PhoneListId = 1, Code = "VMDO", Name = "Dean's Office", MaintainRole = MaintainRole });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 1, PhoneListId = 1, Name = "Dean's Office" });
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 1,
            IamId = "listedperson",
            FirstName = "Listed",
            LastName = "Person",
            FullName = "Listed Person",
            CurrentEmployee = true,
        });
        _context.PhonePerson.Add(new PhonePerson
        {
            PersonIam = "listedperson",
            Phone = "530-555-1000",
            DirectPhone = "530-555-2000",
            Office = "Room 100",
        });
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitId = 1,
            PersonIam = "listedperson",
            ListFirst = false,
            IsActive = true,
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPhoneListUnits_MasksDirectPhone_WhenCallerHasNoAccess()
    {
        // Caller has neither the maintain permission nor a membership row on this list.
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), MaintainRole).Returns(false);

        var units = await _service.GetPhoneListUnits(TestList(), TestContext.Current.CancellationToken);

        var person = Assert.Single(Assert.Single(units).PhoneListUnitPersons);
        Assert.Equal("530-555-1000", person.Person.Phone);
        Assert.Equal("", person.Person.DirectPhone);
    }

    [Fact]
    public async Task GetPhoneListUnits_ShowsDirectPhone_WhenCallerIsListMember()
    {
        // No maintain permission, but the caller is themselves an active member of the list -
        // membership alone should be enough to unlock direct numbers for that list.
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), MaintainRole).Returns(false);
        _context.PhonePerson.Add(new PhonePerson { PersonIam = CallerIam, Phone = "", DirectPhone = "", Office = "" });
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitId = 1,
            PersonIam = CallerIam,
            ListFirst = false,
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var units = await _service.GetPhoneListUnits(TestList(), TestContext.Current.CancellationToken);

        var person = Assert.Single(units.Single().PhoneListUnitPersons, p => p.PersonIam == "listedperson");
        Assert.Equal("530-555-2000", person.Person.DirectPhone);
    }

    [Fact]
    public async Task AddUnitPersonData_UnsetsPreviousListFirst_WhenNewPersonIsMarkedFirst()
    {
        var existingFirstPerson = Assert.Single(_context.PhoneListUnitPerson);
        existingFirstPerson.ListFirst = true;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 2,
            IamId = "newperson",
            FirstName = "New",
            LastName = "Person",
            FullName = "New Person",
            CurrentEmployee = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "newperson",
            Phone = "530-555-5000",
            DirectPhone = "530-555-6000",
            Office = "Room 300",
            ListFirst = true,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        Assert.False(existingFirstPerson.ListFirst);
        var newPerson = await _context.PhoneListUnitPerson
            .SingleAsync(p => p.PersonIam == "newperson", TestContext.Current.CancellationToken);
        Assert.True(newPerson.ListFirst);
    }

    [Fact]
    public async Task AddUnitPersonData_CalledTwiceForSamePerson_UpsertsInsteadOfDuplicating()
    {
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "listedperson",
            Phone = "530-555-9000",
            DirectPhone = "530-555-9001",
            Office = "Room 900",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);
        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var associations = await _context.PhoneListUnitPerson
            .Where(p => p.PersonIam == "listedperson" && p.PhoneListUnitId == 1)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(associations);

        var phonePerson = await _context.PhonePerson
            .SingleAsync(p => p.PersonIam == "listedperson", TestContext.Current.CancellationToken);
        Assert.Equal("530-555-9000", phonePerson.Phone);
        Assert.Equal("530-555-9001", phonePerson.DirectPhone);
        Assert.Equal("Room 900", phonePerson.Office);
    }

    [Fact]
    public async Task AddUnitPersonData_TrimsWhitespace_ForANewPerson()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 3,
            IamId = "paddedperson",
            FirstName = "Padded",
            LastName = "Person",
            FullName = "Padded Person",
            CurrentEmployee = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = " paddedperson ",
            Phone = " 530-555-9500 ",
            DirectPhone = " 530-555-9501 ",
            Office = " Room 950 ",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var phonePerson = await _context.PhonePerson
            .SingleAsync(p => p.PersonIam == "paddedperson", TestContext.Current.CancellationToken);
        Assert.Equal("530-555-9500", phonePerson.Phone);
        Assert.Equal("Room 950", phonePerson.Office);
    }

    [Fact]
    public async Task AddUnitPersonData_CalledTwiceWithAPaddedIam_UpsertsInsteadOfDuplicating()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 4,
            IamId = "paddedtwice",
            FirstName = "Padded",
            LastName = "Twice",
            FullName = "Padded Twice",
            CurrentEmployee = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // The existing-row lookup has to trim the same way the insert does. Matching a padded
        // request against the trimmed PersonIam already stored finds nothing, so every resubmit
        // would add another association row for the same person and unit.
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = " paddedtwice ",
            Phone = " 530-555-9600 ",
            DirectPhone = " 530-555-9601 ",
            Office = " Room 960 ",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);
        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var association = Assert.Single(await _context.PhoneListUnitPerson
            .Where(p => p.PersonIam == "paddedtwice" && p.PhoneListUnitId == 1)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.True(association.IsActive);
        Assert.Equal("paddedtwice", association.PersonIam);
    }

    [Fact]
    public async Task DeleteUnitPersonData_SoftDeletes_KeepsRowButMarksInactive()
    {
        var unitPerson = Assert.Single(_context.PhoneListUnitPerson);

        await _service.DeleteUnitPersonData(1, unitPerson.PhoneListUnitPersonId, TestContext.Current.CancellationToken);

        var stillExists = await _context.PhoneListUnitPerson
            .FindAsync(new object?[] { unitPerson.PhoneListUnitPersonId }, TestContext.Current.CancellationToken);
        Assert.NotNull(stillExists);
        Assert.False(stillExists.IsActive);
        Assert.Equal(CallerIam, stillExists.ModifiedBy);
    }

    [Fact]
    public async Task DeleteUnitPersonData_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteUnitPersonData(1, 9999, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EditingAnAlreadyDeletedRecord_ReportsItAsRemoved_RatherThanResurrectingIt()
    {
        var unitPerson = Assert.Single(_context.PhoneListUnitPerson);
        await _service.DeleteUnitPersonData(1, unitPerson.PhoneListUnitPersonId, TestContext.Current.CancellationToken);
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "listedperson",
            Phone = "530-555-9000",
        };

        // A maintainer whose page predates someone else's delete. Saving must not bring the row
        // back, and the message becomes an error banner, so it is worded for that reader.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateUnitPersonData(
                1, unitPerson.PhoneListUnitPersonId, request, TestContext.Current.CancellationToken));
        Assert.Equal("That record has already been removed.", ex.Message);

        var stillDeleted = await _context.PhoneListUnitPerson
            .FindAsync(new object?[] { unitPerson.PhoneListUnitPersonId }, TestContext.Current.CancellationToken);
        Assert.NotNull(stillDeleted);
        Assert.False(stillDeleted.IsActive);
    }
}
