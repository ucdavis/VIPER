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
/// Controller tests for PhoneListModifiedDateController, the endpoint clients poll to decide
/// whether their cached copy of a list is stale. Two properties matter: deleted rows still count
/// (a removal is a change the client has to pick up, and soft-deleted rows are the only record of
/// it), and the date is scoped to the list named in the route.
/// </summary>
public sealed class PhoneListModifiedDateControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly PhoneListModifiedDateController _controller;

    private static readonly DateTime Older = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Local);
    private static readonly DateTime Newer = new(2026, 6, 1, 9, 0, 0, DateTimeKind.Local);

    public PhoneListModifiedDateControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhonesDbContext(options);

        var userHelper = Substitute.For<IUserHelper>();
        userHelper.GetCurrentUser().Returns(new AaudUser
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
        var permissionsService = new PhonePermissionsService(rapsContext, userHelper);
        var phoneListService = new PhoneListService(_context);
        var unitService = new PhoneListUnitService(_context, userHelper, permissionsService);

        _controller = new PhoneListModifiedDateController(phoneListService, unitService);

        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 1,
            Code = "VMDO",
            Name = "Dean's Office",
            MaintainRole = "SVMSecure.PhoneLists.VMDOMaintain",
        });
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 2,
            Code = "OTHER",
            Name = "Some Other Unit",
            MaintainRole = "SVMSecure.PhoneLists.OtherMaintain",
        });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 1, PhoneListId = 1, Name = "Front Office" });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 2, PhoneListId = 2, Name = "Other Office" });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private void AddRow(int unitPersonId, int unitId, DateTime? modifiedDate, bool isActive = true)
    {
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitPersonId = unitPersonId,
            PhoneListUnitId = unitId,
            PersonIam = "person01",
            ListFirst = false,
            IsActive = isActive,
            ModifiedDate = modifiedDate,
        });
        _context.SaveChanges();
    }

    private async Task<DateTime?> GetDate(string code)
    {
        var result = await _controller.GetLastModifiedDate(code, TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return (DateTime?)okResult.Value;
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsTheMostRecentDate()
    {
        AddRow(unitPersonId: 1, unitId: 1, Older);
        AddRow(unitPersonId: 2, unitId: 1, Newer);

        Assert.Equal(Newer, await GetDate("VMDO"));
    }

    [Fact]
    public async Task GetLastModifiedDate_CountsDeletedRows()
    {
        // A removal is the change most likely to matter to a client holding stale rows, and the
        // soft-deleted row is the only record that it happened.
        AddRow(unitPersonId: 1, unitId: 1, Older);
        AddRow(unitPersonId: 2, unitId: 1, Newer, isActive: false);

        Assert.Equal(Newer, await GetDate("VMDO"));
    }

    [Fact]
    public async Task GetLastModifiedDate_IgnoresAnotherListsRows()
    {
        AddRow(unitPersonId: 1, unitId: 1, Older);
        AddRow(unitPersonId: 2, unitId: 2, Newer);

        Assert.Equal(Older, await GetDate("VMDO"));
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsNull_WhenNothingHasBeenModified()
    {
        AddRow(unitPersonId: 1, unitId: 1, modifiedDate: null);

        Assert.Null(await GetDate("VMDO"));
    }

    [Fact]
    public async Task GetLastModifiedDate_ReturnsNotFound_ForAnUnknownCode()
    {
        var result = await _controller.GetLastModifiedDate("NOPE", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
