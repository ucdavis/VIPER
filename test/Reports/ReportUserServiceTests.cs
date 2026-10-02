using NSubstitute;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Reports;

public sealed class ReportUserServiceTests
{
    private readonly RAPSContext _rapsContext = Substitute.For<RAPSContext>();
    private readonly IUserHelper _userHelper = Substitute.For<IUserHelper>();

    [Fact]
    public void GetCurrentContext_NoUser_HasNoPermissions()
    {
        _userHelper.GetCurrentUser().Returns((AaudUser?)null);
        var service = new ReportUserService(_rapsContext, _userHelper);

        ReportContext context = service.GetCurrentContext();

        Assert.Null(context.LoginId);
        Assert.False(context.HasPermission("SVMSecure"));
        _userHelper.DidNotReceive().HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), Arg.Any<string>());
    }

    [Fact]
    public void GetCurrentContext_SignedInUser_ChecksPermissionsThroughRaps()
    {
        var user = new AaudUser { MothraId = "01234567", LoginId = "jdoe" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, "SVMSecure.Personnel.Sabbatic").Returns(true);
        var service = new ReportUserService(_rapsContext, _userHelper);

        ReportContext context = service.GetCurrentContext();

        Assert.Equal("jdoe", context.LoginId);
        Assert.True(context.HasPermission("SVMSecure.Personnel.Sabbatic"));
        Assert.False(context.HasPermission("SVMSecure.Personnel.FacultySalary"));
    }
}
