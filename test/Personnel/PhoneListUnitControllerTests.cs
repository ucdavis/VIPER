using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Controllers;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// Controller tests for PhoneListUnitController. Beyond the InvalidOperationException-to-400
/// mapping, these cover the authorization model: write access is the role named by the target
/// list's own MaintainRole column, so holding one list's role must grant nothing on another,
/// and a record id from one list must not be reachable through another list's route.
/// </summary>
public sealed class PhoneListUnitControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhoneListUnitController _controller;

    private const string CallerIam = "caller01";
    private const string VmdoRole = "SVMSecure.PhoneLists.VMDOMaintain";
    private const string OtherRole = "SVMSecure.PhoneLists.OtherMaintain";

    public PhoneListUnitControllerTests()
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

        var rapsContext = Substitute.For<RAPSContext>();
        var permissionsService = new PhonePermissionsService(rapsContext, _userHelper);
        var phoneListService = new PhoneListService(_context);
        var unitService = new PhoneListUnitService(_context, _userHelper, permissionsService);

        _controller = new PhoneListUnitController(phoneListService, unitService, permissionsService, Substitute.For<ILogger<PhoneListUnitController>>());

        // Two lists, each with its own unit and its own maintain role.
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 1,
            Code = "VMDO",
            Name = "Dean's Office",
            MaintainRole = VmdoRole,
        });
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 2,
            Code = "OTHER",
            Name = "Some Other Unit",
            MaintainRole = OtherRole,
        });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 1, PhoneListId = 1, Name = "Front Office" });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 2, PhoneListId = 2, Name = "Other Office" });
        _context.PhonePerson.Add(new PhonePerson { PersonIam = "person01", Phone = "530-555-1000" });
        // The service rejects an IAM ID with no users.Person row, so writes need one seeded.
        _context.ViperPerson.Add(new ViperPerson
        {
            PersonId = 1,
            IamId = "person01",
            FirstName = "Test",
            LastName = "Person",
            FullName = "Test Person",
            CurrentEmployee = true,
        });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    /// <summary>Grants the caller exactly one maintain role.</summary>
    private void GrantRole(string role)
    {
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), role).Returns(true);
    }

    private void AddUnitPersonRow(int unitPersonId, int unitId)
    {
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitPersonId = unitPersonId,
            PhoneListUnitId = unitId,
            PersonIam = "person01",
            ListFirst = false,
            IsActive = true,
        });
        _context.SaveChanges();
    }

    private static PhoneListUnitDataRequest Request(int unitId) => new()
    {
        UnitId = unitId,
        EmployeeIam = "person01",
        Phone = "530-555-1000",
        DirectPhone = "530-555-2000",
        Office = "Room 100",
        ListFirst = false,
    };

    [Fact]
    public async Task GetUnits_ReturnsOk_WithUnitsForTheNamedList()
    {
        var result = await _controller.GetUnits("VMDO", TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var units = Assert.IsAssignableFrom<List<PhoneListUnitDto>>(okResult.Value);
        Assert.Equal("Front Office", Assert.Single(units).Name);
    }

    [Fact]
    public async Task GetUnits_SerializesWithoutTheEntityNavigationProperties()
    {
        // The endpoint used to return the EF entity, so the payload carried navigation properties
        // the query never populated - phoneList and phoneListUnit, always null. They are gone from
        // the DTO, and asserting on the serialized JSON is what actually pins the wire contract:
        // the DTO type alone would not catch a nav property being reintroduced on it.
        //
        // The row matters: phoneListUnit and isActive live on PhoneListUnitPerson, so without one
        // in the payload those two assertions pass against an empty array without ever seeing the
        // DTO they are about.
        AddUnitPersonRow(1, 1);

        var result = await _controller.GetUnits("VMDO", TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        // camelCase to match what ASP.NET Core actually puts on the wire, so the property names
        // asserted on below are the ones the TypeScript types are written against.
        var json = System.Text.Json.JsonSerializer.Serialize(
            okResult.Value,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });

        Assert.DoesNotContain("phoneList\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneListUnit\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isActive", json, StringComparison.OrdinalIgnoreCase);
        // The data the client does read still arrives. person01 is the one that proves a nested
        // PhoneListUnitPersonDto actually serialized, rather than an empty array wearing the right
        // property name - which is what would put the two assertions above back to sleep.
        Assert.Contains("phoneListUnitPersons", json, StringComparison.Ordinal);
        Assert.Contains("person01", json, StringComparison.Ordinal);
        Assert.Contains("Front Office", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnits_ReturnsNotFound_ForAnUnknownCode()
    {
        var result = await _controller.GetUnits("NOPE", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddUnitPersonData_ReturnsOk_ForAMaintainerOfThatList()
    {
        GrantRole(VmdoRole);

        var result = await _controller.AddUnitPersonData("VMDO", Request(1), TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddUnitPersonData_IsForbidden_WithoutTheRoleForThatList()
    {
        var result = await _controller.AddUnitPersonData("VMDO", Request(1), TestContext.Current.CancellationToken);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AddUnitPersonData_IsForbidden_ForAMaintainerOfADifferentList()
    {
        // Holding VMDOMaintain must not confer write access to the OTHER list, which is what a
        // hard-coded permission attribute on the endpoint would have allowed.
        GrantRole(VmdoRole);

        var result = await _controller.AddUnitPersonData("OTHER", Request(2), TestContext.Current.CancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await _context.PhoneListUnitPerson.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddUnitPersonData_ReturnsBadRequest_WhenTheUnitBelongsToAnotherList()
    {
        // Unit 2 is on the OTHER list; routing through VMDO must not reach it.
        GrantRole(VmdoRole);

        var result = await _controller.AddUnitPersonData("VMDO", Request(2), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await _context.PhoneListUnitPerson.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateUnitPersonData_ReturnsBadRequest_WhenNotFound()
    {
        GrantRole(VmdoRole);

        var result = await _controller.UpdateUnitPersonData("VMDO", 999, Request(1), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUnitPersonData_ReturnsBadRequest_WhenTheRecordBelongsToAnotherList()
    {
        AddUnitPersonRow(unitPersonId: 5, unitId: 2);
        GrantRole(VmdoRole);

        var result = await _controller.UpdateUnitPersonData("VMDO", 5, Request(1), TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        var untouched = await _context.PhoneListUnitPerson
            .FindAsync(new object?[] { 5 }, TestContext.Current.CancellationToken);
        Assert.NotNull(untouched);
        Assert.True(untouched.IsActive);
    }

    [Fact]
    public async Task DeleteUnitPersonData_ReturnsBadRequest_WhenNotFound()
    {
        GrantRole(VmdoRole);

        var result = await _controller.DeleteUnitPersonData("VMDO", 999, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUnitPersonData_ReturnsOk_AndSoftDeletes_WhenFound()
    {
        AddUnitPersonRow(unitPersonId: 1, unitId: 1);
        GrantRole(VmdoRole);

        var result = await _controller.DeleteUnitPersonData("VMDO", 1, TestContext.Current.CancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var stillExists = await _context.PhoneListUnitPerson
            .FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
        Assert.NotNull(stillExists);
        Assert.False(stillExists.IsActive);
    }

    [Fact]
    public async Task DeleteUnitPersonData_LeavesTheRecordAlone_WhenItBelongsToAnotherList()
    {
        AddUnitPersonRow(unitPersonId: 5, unitId: 2);
        GrantRole(VmdoRole);

        var result = await _controller.DeleteUnitPersonData("VMDO", 5, TestContext.Current.CancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        var untouched = await _context.PhoneListUnitPerson
            .FindAsync(new object?[] { 5 }, TestContext.Current.CancellationToken);
        Assert.NotNull(untouched);
        Assert.True(untouched.IsActive);
    }
}
