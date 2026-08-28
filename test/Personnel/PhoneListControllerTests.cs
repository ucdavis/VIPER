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
/// Controller tests for PhoneListController. GetListInfo is what the client renders from before
/// it fetches any rows, so the two capability flags it reports have to match what the write and
/// read endpoints actually enforce: CanMaintain follows the list's own MaintainRole, and
/// CanViewDirectPhone is deliberately broader - a member of the list sees direct numbers without
/// being able to edit it. A flag that overstated either would show the client controls the API
/// then refuses.
/// </summary>
public sealed class PhoneListControllerTests : IDisposable
{
    private readonly PhonesDbContext _context;
    private readonly IUserHelper _userHelper;
    private readonly PhoneListController _controller;

    private const string CallerIam = "caller01";
    private const string VmdoRole = "SVMSecure.PhoneLists.VMDOMaintain";

    public PhoneListControllerTests()
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
        var permissionsService = new PhonePermissionsService(rapsContext, _userHelper);
        var phoneListService = new PhoneListService(_context);
        var unitService = new PhoneListUnitService(_context, _userHelper, permissionsService);

        _controller = new PhoneListController(phoneListService, unitService, permissionsService);

        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 1,
            Code = "VMDO",
            Name = "Dean's Office",
            MaintainRole = VmdoRole,
        });
        _context.PhoneListUnit.Add(new PhoneListUnit { PhoneListUnitId = 1, PhoneListId = 1, Name = "Front Office" });
        _context.PhonePerson.Add(new PhonePerson { PersonIam = CallerIam, Phone = "530-555-1000" });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private void GrantRole(string role)
    {
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), role).Returns(true);
    }

    /// <summary>Puts the caller on the list itself, which is not the same as maintaining it.</summary>
    private void AddCallerToList()
    {
        _context.PhoneListUnitPerson.Add(new PhoneListUnitPerson
        {
            PhoneListUnitPersonId = 1,
            PhoneListUnitId = 1,
            PersonIam = CallerIam,
            ListFirst = false,
            IsActive = true,
        });
        _context.SaveChanges();
    }

    private async Task<PhoneListInfo> GetInfo(string code)
    {
        var result = await _controller.GetListInfo(code, TestContext.Current.CancellationToken);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<PhoneListInfo>(okResult.Value);
    }

    [Fact]
    public async Task GetListInfo_ReturnsTheListIdentity()
    {
        var info = await GetInfo("VMDO");

        Assert.Equal(1, info.PhoneListId);
        Assert.Equal("VMDO", info.Code);
        Assert.Equal("Dean's Office", info.Name);
    }

    [Fact]
    public async Task GetListInfo_ReportsNoCapabilities_ForACallerWithNeitherRoleNorMembership()
    {
        var info = await GetInfo("VMDO");

        Assert.False(info.CanMaintain);
        Assert.False(info.CanViewDirectPhone);
    }

    [Fact]
    public async Task GetListInfo_ReportsBothCapabilities_ForAMaintainer()
    {
        GrantRole(VmdoRole);

        var info = await GetInfo("VMDO");

        Assert.True(info.CanMaintain);
        Assert.True(info.CanViewDirectPhone);
    }

    [Fact]
    public async Task GetListInfo_ReportsDirectPhoneOnly_ForAMemberWhoCannotMaintain()
    {
        // Membership is what grants the direct-number view, so the two flags have to move
        // independently: reporting CanMaintain here would offer edit controls the API refuses.
        AddCallerToList();

        var info = await GetInfo("VMDO");

        Assert.False(info.CanMaintain);
        Assert.True(info.CanViewDirectPhone);
    }

    [Fact]
    public async Task GetListInfo_IgnoresMembershipOfAnotherList()
    {
        _context.PhoneList.Add(new PhoneList
        {
            PhoneListId = 2,
            Code = "OTHER",
            Name = "Some Other Unit",
            MaintainRole = "SVMSecure.PhoneLists.OtherMaintain",
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        AddCallerToList();

        var info = await GetInfo("OTHER");

        Assert.False(info.CanViewDirectPhone);
    }

    [Fact]
    public async Task GetListInfo_ReturnsNotFound_ForAnUnknownCode()
    {
        var result = await _controller.GetListInfo("NOPE", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
