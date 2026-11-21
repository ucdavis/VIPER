using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.ReportBuilder;

/// <summary>The report runner sees the signed-in user through UserHelper and RAPS.</summary>
public sealed class ReportUserServiceTests : IDisposable
{
    private readonly IUserHelper _userHelper = Substitute.For<IUserHelper>();
    private readonly RAPSContext _rapsContext = new();
    private readonly ReportUserService _service;

    public ReportUserServiceTests()
    {
        _service = new ReportUserService(_rapsContext, _userHelper);
    }

    public void Dispose()
    {
        _rapsContext.Dispose();
    }

    [Fact]
    public void SignedIn_GivesTheirIdentifiersAndPermissions()
    {
        var user = new AaudUser { LoginId = "jdoe", MothraId = "01234567" };
        _userHelper.GetCurrentUser().Returns(user);
        _userHelper.HasPermission(_rapsContext, user, "SVMSecure.Reports.Admin").Returns(true);

        ReportCurrentUser? current = _service.GetCurrentUser();

        Assert.NotNull(current);
        Assert.Equal("jdoe", current.LoginId);
        Assert.Equal("01234567", current.MothraId);
        Assert.True(_service.HasPermission("SVMSecure.Reports.Admin"));
        Assert.False(_service.HasPermission("SVMSecure.Other"));
    }

    [Fact]
    public void NoOneSignedIn_HasNoUserAndNoPermissions()
    {
        _userHelper.GetCurrentUser().ReturnsNull();

        Assert.Null(_service.GetCurrentUser());
        Assert.False(_service.HasPermission("SVMSecure.Reports.Admin"));
        _userHelper.DidNotReceiveWithAnyArgs().HasPermission(default, default, default!);
    }
}
