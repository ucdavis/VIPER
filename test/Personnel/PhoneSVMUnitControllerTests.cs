using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Controller tests for PhoneSVMUnitController: the read shape the SVM page renders from, and the
/// InvalidOperationException-to-400 mapping on the three maintain endpoints. Unlike the per-list
/// controllers, write access here is a fixed role on the endpoint rather than a per-row lookup, so
/// what the controller itself decides is narrower - which makes the failure mapping the thing
/// worth pinning, since a 500 here would surface to a maintainer as an unexplained error banner
/// instead of the message the service wrote for them.
/// </summary>
public sealed class PhoneSVMUnitControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneSVMUnitController _controller;

    private const string CallerIam = "caller01";

    public PhoneSVMUnitControllerTests()
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

        _controller = new PhoneSVMUnitController(new PhoneSVMUnitService(_context, userHelper), Substitute.For<ILogger<PhoneSVMUnitController>>());

        _context.SVMUnit.Add(new SVMUnit { UnitId = 1, SectionId = 1, Name = "Dean's Office" });
        // The read projection joins phones.Person to users.Person on a required relationship, so
        // a phone row without its person is invisible to GetUnits.
        AddPerson(personId: 1, "dean01", "Dinah", "Deanly", "530-555-1000");
        AddPerson(personId: 2, "staff01", "Sam", "Staffly", "530-555-2000");
        _context.SaveChanges();
    }

    private void AddPerson(int personId, string iamId, string firstName, string lastName, string phone)
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
        _context.PhonePerson.Add(new PhonePerson { PersonIam = iamId, Phone = phone });
    }

    public void Dispose() => _context.Dispose();

    private void AddUnitPerson(int unitPersonId, string personIam, string posType, bool isActive = true)
    {
        _context.SVMUnitPerson.Add(new SVMUnitPerson
        {
            UnitPersonId = unitPersonId,
            UnitId = 1,
            PersonIam = personIam,
            PosType = posType,
            IsActive = isActive,
        });
        _context.SaveChanges();
    }

    private static SVMUnitDataRequest Request() => new()
    {
        Fax = "530-555-3000",
        Location = "Room 100",
        DeanIam = "dean01",
        DeanPhone = "530-555-1000",
    };

    private async Task<SVMUnitPerson?> FindUnitPerson(int unitPersonId) =>
        await _context.SVMUnitPerson.FindAsync(new object?[] { unitPersonId }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetUnits_ReturnsOk_WithActivePeopleOnly()
    {
        AddUnitPerson(unitPersonId: 1, "dean01", "Dean");
        AddUnitPerson(unitPersonId: 2, "staff01", "Staff", isActive: false);

        var result = await _controller.GetUnits(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var units = Assert.IsAssignableFrom<List<SVMUnitDto>>(okResult.Value);
        var unit = Assert.Single(units);
        Assert.Equal("dean01", Assert.Single(unit.UnitPersons).PersonIam);
    }

    [Fact]
    public async Task GetUnits_ReturnsOk_WithAnEmptyListWhenThereAreNoUnits()
    {
        // An empty SVM list is a legitimate state, not a 404: the page still renders its sections.
        _context.SVMUnit.RemoveRange(_context.SVMUnit);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetUnits(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<List<SVMUnitDto>>(okResult.Value));
    }

    [Fact]
    public async Task AddUnitData_ReturnsBadRequest_ForAnUnknownUnit()
    {
        var result = await _controller.AddUnitData(999, Request(), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await _context.SVMUnitPerson.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddUnitData_ReturnsOk_AndAddsTheLeader()
    {
        var result = await _controller.AddUnitData(1, Request(), TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var added = Assert.Single(await _context.SVMUnitPerson
            .Where(p => p.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("dean01", added.PersonIam);
        Assert.Equal("Dean", added.PosType);
    }

    [Fact]
    public async Task UpdateUnitData_ReturnsBadRequest_ForAnUnknownUnit()
    {
        var result = await _controller.UpdateUnitData(999, Request(), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUnitData_ReturnsOk_AndReplacesTheNamedRow()
    {
        AddUnitPerson(unitPersonId: 1, "dean01", "Dean");
        var request = Request();
        request.DeanUnitPerson = 1;

        var result = await _controller.UpdateUnitData(1, request, TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var replaced = await FindUnitPerson(1);
        Assert.NotNull(replaced);
        Assert.False(replaced.IsActive);
        Assert.Equal("Room 100", Assert.Single(await _context.SVMUnitPerson
            .Where(p => p.IsActive)
            .ToListAsync(TestContext.Current.CancellationToken)).Office);
    }

    [Fact]
    public async Task DeleteUnitRow_ReturnsBadRequest_WhenNotFound()
    {
        var result = await _controller.DeleteUnitRow(999, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUnitRow_ReturnsBadRequest_WhenTheRowWasAlreadyRemoved()
    {
        // The realistic way to miss through the UI: two maintainers on the same list.
        AddUnitPerson(unitPersonId: 1, "dean01", "Dean", isActive: false);

        var result = await _controller.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("That record has already been removed.", badRequest.Value);
    }

    [Fact]
    public async Task DeleteUnitRow_ReturnsOk_AndSoftDeletes_WhenFound()
    {
        AddUnitPerson(unitPersonId: 1, "dean01", "Dean");

        var result = await _controller.DeleteUnitRow(1, TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var stillExists = await FindUnitPerson(1);
        Assert.NotNull(stillExists);
        Assert.False(stillExists.IsActive);
    }
}
