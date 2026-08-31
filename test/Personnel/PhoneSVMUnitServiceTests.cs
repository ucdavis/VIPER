using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for PhoneSVMUnitService, covering the two places its row handling is asymmetric.
/// DeleteUnitRow: a row is a leader plus the unit-wide admin staff, so deleting it removes the
/// leader and then the staff only once no other row still lists them - in one transaction, since
/// as separate per-record requests the pair could half-apply.
/// AddOrUpdateUnitData: one method serves both POST and PUT, so add and edit are distinguished
/// only by DeanUnitPerson/StaffUnitPerson - unset (-1) means "add another person to this unit"
/// and leaves existing rows alone, while a real id means "replace the person on that row" and
/// must deactivate it even though the incoming DeanIam/StaffIam no longer names its occupant.
/// </summary>
public sealed class PhoneSVMUnitServiceTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMUnitService _service;

    private const string CallerIam = "caller01";

    public PhoneSVMUnitServiceTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new PhonesDbContext(options);

        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(new AaudUser
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

        _service = new PhoneSVMUnitService(_context, userHelper);
    }

    public void Dispose() => _context.Dispose();

    private void SeedUnit()
    {
        _context.SVMUnit.Add(new SVMUnit { UnitId = 1, SectionId = 1, Name = "Dean's Office" });
        _context.PhonePerson.Add(new PhonePerson { PersonIam = "dean01", Phone = "530-555-1000" });
        _context.PhonePerson.Add(new PhonePerson { PersonIam = "staff01", Phone = "530-555-2000" });
        // The service rejects an IAM ID with no users.Person row, so every id these tests
        // write has to exist here, not only the two that start out on the unit.
        SeedViperPerson(1, "dean01", "Dinah", "Deanly");
        SeedViperPerson(2, "staff01", "Sam", "Staffly");
        SeedViperPerson(3, "dean02", "Dana", "Deanly");
        SeedViperPerson(4, "dean03", "Drew", "Deanly");
        SeedViperPerson(5, "staff02", "Sasha", "Staffly");
        _context.SaveChanges();
    }

    private void SeedViperPerson(int personId, string iamId, string firstName, string lastName)
    {
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = personId,
            IamId = iamId,
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}",
            CurrentEmployee = true,
        });
    }


    [Fact]
    public async Task DeleteUnitRow_SoftDeletesTheLeader_AndTheStaffItWasTheLastRowFor()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // One call, not one per underlying record: the caller names the row, the service decides
        // which records that covers.
        await _service.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        Assert.Empty(await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUnitRow_KeepsTheStaff_WhenAnotherLeaderRowStillListsThem()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "dean02",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 3,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        var staffRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 3 }, TestContext.Current.CancellationToken);
        Assert.NotNull(staffRow);
        Assert.True(staffRow.IsActive);

        var survivingLeader = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 2 }, TestContext.Current.CancellationToken);
        Assert.NotNull(survivingLeader);
        Assert.True(survivingLeader.IsActive);
    }

    [Fact]
    public async Task DeleteUnitRow_RemovesTheStaff_WhenNamedForAStaffOnlyRow()
    {
        SeedUnit();
        // A unit with staff but no active leader renders one row keyed by the staff record, so
        // that id is what the delete arrives with.
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        var staffRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(staffRow);
        Assert.False(staffRow.IsActive);
    }

    /// <summary>
    /// A staff-keyed id is only the list's row key while the unit has no active leader. A stale
    /// page can still send one after another maintainer adds a leader, and the staff sweep does
    /// not run in that case - so without the guard the delete soft-deletes nothing and the UI
    /// still reports "Record deleted".
    /// </summary>
    [Fact]
    public async Task DeleteUnitRow_Throws_AndChangesNothing_WhenAStaffRowsUnitHasALeaderAgain()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        // Added after the stale page rendered its staff-keyed row.
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteUnitRow(1, TestContext.Current.CancellationToken));

        Assert.Equal("That record has changed since the page was loaded. Please refresh and try again.", ex.Message);

        // Refused outright rather than half-applied: both rows are still active.
        Assert.Equal(2, await _context.SVMUnitPerson
            .CountAsync(p => p.UnitId == 1 && p.IsActive, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUnitRow_LeavesOtherUnitsAlone()
    {
        SeedUnit();
        _context.SVMUnit.Add(new SVMUnit { UnitId = 2, SectionId = 1, Name = "Another Unit" });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 2,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        var otherUnitStaff = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 2 }, TestContext.Current.CancellationToken);
        Assert.NotNull(otherUnitStaff);
        Assert.True(otherUnitStaff.IsActive);
    }

    [Fact]
    public async Task DeleteUnitRow_Throws_WhenNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteUnitRow(999, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddOrUpdateUnitData_Throws_WhenUnitNotFound()
    {
        // Seeded so the person guard passes and the unit lookup is what actually fails.
        SeedUnit();
        var request = new SVMUnitDataRequest { DeanIam = "dean01", DeanPhone = "530-555-1000" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddOrUpdateUnitData(999, request, TestContext.Current.CancellationToken));

        Assert.Equal("Unit not found", ex.Message);
    }

    /// <summary>
    /// PhonePerson requires its users.Person row, so every read projection inner joins to it.
    /// A record written against an unknown IAM ID would be invisible to the list and could not
    /// be edited or removed through it, so the write is refused instead. The two roles report
    /// separately because the dialog has a picker for each.
    /// </summary>
    [Fact]
    public async Task AddOrUpdateUnitData_Throws_AndWritesNothing_WhenTheDeanIsNotAViperPerson()
    {
        SeedUnit();
        var request = new SVMUnitDataRequest
        {
            DeanIam = "ghost01",
            DeanPhone = "530-555-7000",
            StaffIam = "staff01",
            StaffPhone = "530-555-2000",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken));

        Assert.Equal("The selected dean/director could not be found.", ex.Message);
        Assert.Null(await _context.PhonePerson
            .FirstOrDefaultAsync(p => p.PersonIam == "ghost01", TestContext.Current.CancellationToken));
        Assert.Empty(await _context.SVMUnitPerson.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddOrUpdateUnitData_Throws_WhenTheStaffIsNotAViperPerson()
    {
        SeedUnit();
        var request = new SVMUnitDataRequest
        {
            DeanIam = "dean01",
            DeanPhone = "530-555-1000",
            StaffIam = "ghost02",
            StaffPhone = "530-555-7000",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken));

        Assert.Equal("The selected admin staff member could not be found.", ex.Message);
        Assert.Null(await _context.PhonePerson
            .FirstOrDefaultAsync(p => p.PersonIam == "ghost02", TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Either role may be left blank, so the guard has to skip a blank id rather than treat
    /// it as a person who could not be found.
    /// </summary>
    [Fact]
    public async Task AddOrUpdateUnitData_UpdatesTheUnit_WhenNeitherRoleIsNamed()
    {
        SeedUnit();
        var request = new SVMUnitDataRequest { Fax = "530-555-9999" };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var unit = await _context.SVMUnit.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.Equal("530-555-9999", unit!.Fax);
    }

    [Fact]
    public async Task AddOrUpdateUnitData_DeactivatesPreviousUnitPeople_AndAddsNewActiveRows()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SVMUnitDataRequest
        {
            Fax = " 530-555-9999 ",
            Location = "Room 500",
            DeanIam = "dean01",
            DeanPhone = "530-555-1111",
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var oldRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(oldRow);
        Assert.False(oldRow.IsActive);

        var newRow = await _context.SVMUnitPerson
            .SingleAsync(p => p.IsActive && p.PersonIam == "dean01", TestContext.Current.CancellationToken);
        Assert.Equal("Dean", newRow.PosType);

        var unit = await _context.SVMUnit.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.Equal("530-555-9999", unit!.Fax);
    }

    [Fact]
    public async Task GetSVMUnits_AlwaysBlanksDirectPhone()
    {
        SeedUnit();
        var phonePerson = await _context.PhonePerson
            .SingleAsync(p => p.PersonIam == "dean01", TestContext.Current.CancellationToken);
        phonePerson.DirectPhone = "530-555-4000";
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var units = await _service.GetSVMUnits(TestContext.Current.CancellationToken);

        var person = Assert.Single(Assert.Single(units).UnitPersons);
        Assert.Equal("", person.Person.DirectPhone);
    }

    [Fact]
    public async Task AddOrUpdateUnitData_DeactivatesTheEditedRow_WhenTheDeanIsReplaced()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Editing the row for dean01 and choosing dean02 instead. The outgoing person is
        // identified only by DeanUnitPerson, since DeanIam now names the incoming person.
        var request = new SVMUnitDataRequest
        {
            Fax = "530-555-9999",
            Location = "Room 500",
            DeanIam = "dean02",
            DeanPhone = "530-555-1112",
            DeanUnitPerson = 1,
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var replacedRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(replacedRow);
        Assert.False(replacedRow.IsActive);

        var activeLeader = Assert.Single(await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive && p.PosType != "Staff")
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("dean02", activeLeader.PersonIam);
    }

    [Fact]
    public async Task AddOrUpdateUnitData_DeactivatesTheEditedRow_WhenTheStaffIsReplaced()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SVMUnitDataRequest
        {
            Location = "Room 500",
            DeanIam = "dean01",
            DeanPhone = "530-555-1000",
            DeanUnitPerson = 1,
            StaffIam = "staff02",
            StaffPhone = "530-555-2002",
            StaffUnitPerson = 2,
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var replacedRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 2 }, TestContext.Current.CancellationToken);
        Assert.NotNull(replacedRow);
        Assert.False(replacedRow.IsActive);

        var activeStaff = Assert.Single(await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive && p.PosType == "Staff")
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("staff02", activeStaff.PersonIam);
    }

    [Fact]
    public async Task AddOrUpdateUnitData_LeavesOtherLeaderRowsActive_WhenOneLeaderIsReplaced()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "dean02",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // A unit legitimately has several leader rows; replacing one must not disturb the rest.
        var request = new SVMUnitDataRequest
        {
            Location = "Room 500",
            DeanIam = "dean03",
            DeanPhone = "530-555-1113",
            DeanUnitPerson = 1,
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var untouchedRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 2 }, TestContext.Current.CancellationToken);
        Assert.NotNull(untouchedRow);
        Assert.True(untouchedRow.IsActive);

        var activeLeaders = await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive && p.PosType != "Staff")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, activeLeaders.Count);
        Assert.Contains(activeLeaders, p => p.PersonIam == "dean02");
        Assert.Contains(activeLeaders, p => p.PersonIam == "dean03");
    }

    [Fact]
    public async Task AddOrUpdateUnitData_KeepsExistingLeaders_WhenUnitPersonIdsAreUnset()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // The add path leaves DeanUnitPerson/StaffUnitPerson at their -1 default, which is what
        // separates "add another leader to this unit" from "replace the person on this row".
        var request = new SVMUnitDataRequest
        {
            Location = "Room 500",
            DeanIam = "dean02",
            DeanPhone = "530-555-1112",
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var existingRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(existingRow);
        Assert.True(existingRow.IsActive);

        var activeLeaders = await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive && p.PosType != "Staff")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, activeLeaders.Count);
    }

    [Fact]
    public async Task AddOrUpdateUnitData_RemovesStaff_WhenClearedFromTheEditedRow()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new SVMUnitDataRequest
        {
            Location = "Room 500",
            DeanIam = "dean01",
            DeanPhone = "530-555-1000",
            DeanUnitPerson = 1,
            StaffIam = "",
            StaffUnitPerson = 2,
        };

        await _service.AddOrUpdateUnitData(1, request, TestContext.Current.CancellationToken);

        var clearedRow = await _context.SVMUnitPerson
            .FindAsync(new object?[] { 2 }, TestContext.Current.CancellationToken);
        Assert.NotNull(clearedRow);
        Assert.False(clearedRow.IsActive);

        Assert.Empty(await _context.SVMUnitPerson
            .Where(p => p.UnitId == 1 && p.IsActive && p.PosType == "Staff")
            .ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUnitRow_ReportsAnAlreadyDeletedRow_AsRemoved()
    {
        SeedUnit();
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 1,
            UnitId = 1,
            PersonIam = "dean01",
            PosType = "Dean",
            IsActive = true,
        });
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = 2,
            UnitId = 1,
            PersonIam = "staff01",
            PosType = "Staff",
            IsActive = true,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _service.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        // A maintainer whose page predates someone else's delete. Deleting again must say so
        // rather than silently repeating the cascade over rows that are already gone.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteUnitRow(1, TestContext.Current.CancellationToken));
        Assert.Equal("That record has already been removed.", ex.Message);
    }
}
