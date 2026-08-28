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
        var permissionsService = new PhonePermissionsService(rapsContext, _userHelper);

        _service = new PhoneListUnitService(_context, _userHelper, permissionsService);

        SeedList();
    }

    public void Dispose() => _context.Dispose();

    /// <summary>The seeded list, as the controller would hand it to the service.</summary>
    private PhoneList TestList() =>
        _context.PhoneList.Single(l => l.PhoneListId == 1);

    private void AddUnit(int unitId, string name)
    {
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = unitId, PhoneListId = 1, Name = name });
        _context.SaveChanges();
    }

    /// <summary>Puts a person on a unit, with the phone row the read paths expect them to have.</summary>
    private void AddMember(int unitPersonId, int unitId, string personIam, bool listFirst)
    {
        _context.PhonePerson.Add(new PhonePerson { PersonIam = personIam, Phone = "530-555-0000" });
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitPersonId = unitPersonId,
            PhoneListUnitId = unitId,
            PersonIam = personIam,
            ListFirst = listFirst,
            IsActive = true,
        });
        _context.SaveChanges();
    }

    private async Task<PhoneListUnitPerson?> FindMember(int unitPersonId) =>
        await _context.PhoneListUnitPerson.FindAsync(new object?[] { unitPersonId }, TestContext.Current.CancellationToken);

    private void SeedList()
    {
        _context.PhoneList.Add(new PhoneList { PhoneListId = 1, Code = "VMDO", Name = "Dean's Office", MaintainRole = MaintainRole });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 1, PhoneListId = 1, Name = "Dean's Office" });
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 1,
            IamId = "listed01",
            FirstName = "Listed",
            LastName = "Person",
            FullName = "Listed Person",
            CurrentEmployee = true,
        });
        _context.PhonePerson.Add(new PhonePerson
        {
            PersonIam = "listed01",
            Phone = "530-555-1000",
            DirectPhone = "530-555-2000",
            Office = "Room 100",
        });
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitId = 1,
            PersonIam = "listed01",
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

        var person = Assert.Single(units.Single().PhoneListUnitPersons, p => p.PersonIam == "listed01");
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

    /// <summary>
    /// PhonePerson requires its users.Person row, so every read projection inner joins to it.
    /// A record written against an unknown IAM ID would be invisible to the list and could not
    /// be edited or removed through it, so the write is refused instead.
    /// </summary>
    [Fact]
    public async Task AddUnitPersonData_Throws_AndWritesNothing_WhenTheEmployeeIsNotAViperPerson()
    {
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "ghost01",
            Phone = "530-555-7000",
            DirectPhone = "530-555-7001",
            Office = "Room 700",
            ListFirst = false,
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken));

        Assert.Equal("The selected employee could not be found.", ex.Message);
        Assert.Null(await _context.PhonePerson
            .FirstOrDefaultAsync(p => p.PersonIam == "ghost01", TestContext.Current.CancellationToken));
        Assert.Empty(await _context.PhoneListUnitPerson
            .Where(p => p.PersonIam == "ghost01")
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateUnitPersonData_Throws_WhenTheEmployeeIsNotAViperPerson()
    {
        var unitPerson = await _context.PhoneListUnitPerson
            .SingleAsync(p => p.PersonIam == "listed01" && p.PhoneListUnitId == 1, TestContext.Current.CancellationToken);
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "ghost01",
            Phone = "530-555-7000",
            DirectPhone = "530-555-7001",
            Office = "Room 700",
            ListFirst = false,
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateUnitPersonData(1, unitPerson.PhoneListUnitPersonId, request, TestContext.Current.CancellationToken));

        Assert.Equal("The selected employee could not be found.", ex.Message);
        Assert.Null(await _context.PhonePerson
            .FirstOrDefaultAsync(p => p.PersonIam == "ghost01", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddUnitPersonData_CalledTwiceForSamePerson_UpsertsInsteadOfDuplicating()
    {
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "listed01",
            Phone = "530-555-9000",
            DirectPhone = "530-555-9001",
            Office = "Room 900",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);
        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var associations = await _context.PhoneListUnitPerson
            .Where(p => p.PersonIam == "listed01" && p.PhoneListUnitId == 1)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(associations);

        var phonePerson = await _context.PhonePerson
            .SingleAsync(p => p.PersonIam == "listed01", TestContext.Current.CancellationToken);
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
            IamId = "padded01",
            FirstName = "Padded",
            LastName = "Person",
            FullName = "Padded Person",
            CurrentEmployee = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = " padded01 ",
            Phone = " 530-555-9500 ",
            DirectPhone = " 530-555-9501 ",
            Office = " Room 950 ",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var phonePerson = await _context.PhonePerson
            .SingleAsync(p => p.PersonIam == "padded01", TestContext.Current.CancellationToken);
        Assert.Equal("530-555-9500", phonePerson.Phone);
        Assert.Equal("Room 950", phonePerson.Office);
    }

    [Fact]
    public async Task AddUnitPersonData_CalledTwiceWithAPaddedIam_UpsertsInsteadOfDuplicating()
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 4,
            IamId = "padded02",
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
            EmployeeIam = " padded02 ",
            Phone = " 530-555-9600 ",
            DirectPhone = " 530-555-9601 ",
            Office = " Room 960 ",
            ListFirst = false,
        };

        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);
        await _service.AddUnitPersonData(1, request, TestContext.Current.CancellationToken);

        var association = Assert.Single(await _context.PhoneListUnitPerson
            .Where(p => p.PersonIam == "padded02" && p.PhoneListUnitId == 1)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.True(association.IsActive);
        Assert.Equal("padded02", association.PersonIam);
    }

    [Fact]
    public async Task UpdateUnitPersonData_UpdatesThePhonePerson_AndModifiedMetadata()
    {
        var unitPerson = Assert.Single(_context.PhoneListUnitPerson);
        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "listed01",
            Phone = "  530-555-7000  ",
            DirectPhone = "  530-555-7001  ",
            Office = "  Room 700  ",
            ListFirst = false,
        };

        await _service.UpdateUnitPersonData(
            1, unitPerson.PhoneListUnitPersonId, request, TestContext.Current.CancellationToken);

        var phonePerson = await _context.PhonePerson
            .FindAsync(new object?[] { "listed01" }, TestContext.Current.CancellationToken);
        Assert.NotNull(phonePerson);
        Assert.Equal("530-555-7000", phonePerson.Phone);
        Assert.Equal("530-555-7001", phonePerson.DirectPhone);
        Assert.Equal("Room 700", phonePerson.Office);
        Assert.Equal(CallerIam, unitPerson.ModifiedBy);
        Assert.NotNull(unitPerson.ModifiedDate);
    }

    [Fact]
    public async Task UpdateUnitPersonData_ClearsListFirstOnTheRecordsOwnUnit_NotTheRequestedOne()
    {
        // The request carries a UnitId, but the record already knows which unit it lives in. Only
        // the record's own unit may be cleared: taking the caller's word for it would let a
        // mismatched UnitId unset the first-listed entry of an unrelated unit.
        AddUnit(unitId: 2, name: "Other Office");
        AddMember(unitPersonId: 10, unitId: 1, personIam: "sameunitfirst", listFirst: true);
        AddMember(unitPersonId: 20, unitId: 2, personIam: "otherunitfirst", listFirst: true);
        var target = await _context.PhoneListUnitPerson
            .SingleAsync(p => p.PersonIam == "listed01", TestContext.Current.CancellationToken);

        var request = new PhoneListUnitDataRequest
        {
            UnitId = 2,
            EmployeeIam = "listed01",
            Phone = "530-555-7000",
            ListFirst = true,
        };

        await _service.UpdateUnitPersonData(
            1, target.PhoneListUnitPersonId, request, TestContext.Current.CancellationToken);

        Assert.True(target.ListFirst);
        Assert.False((await FindMember(10))!.ListFirst);
        Assert.True((await FindMember(20))!.ListFirst);
    }

    [Fact]
    public async Task UpdateUnitPersonData_LeavesTheExistingFirstEntry_WhenListFirstIsNotSet()
    {
        AddMember(unitPersonId: 10, unitId: 1, personIam: "sameunitfirst", listFirst: true);
        var target = await _context.PhoneListUnitPerson
            .SingleAsync(p => p.PersonIam == "listed01", TestContext.Current.CancellationToken);

        var request = new PhoneListUnitDataRequest
        {
            UnitId = 1,
            EmployeeIam = "listed01",
            Phone = "530-555-7000",
            ListFirst = false,
        };

        await _service.UpdateUnitPersonData(
            1, target.PhoneListUnitPersonId, request, TestContext.Current.CancellationToken);

        Assert.False(target.ListFirst);
        Assert.True((await FindMember(10))!.ListFirst);
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
            EmployeeIam = "listed01",
            Phone = "530-555-9000",
            ListFirst = false,
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
