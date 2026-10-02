using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportContextTests
{
    [Fact]
    public void Constructor_NullPermissionCheck_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ReportContext("user", null!));
    }

    [Fact]
    public void LoginId_ReturnsSuppliedValue()
    {
        var context = new ReportContext("jdoe", _ => false);

        Assert.Equal("jdoe", context.LoginId);
    }

    [Fact]
    public void HasPermission_DelegatesToPermissionCheck()
    {
        ReportContext context = TestReport.ContextWith("SVMSecure.A");

        Assert.True(context.HasPermission("SVMSecure.A"));
        Assert.False(context.HasPermission("SVMSecure.B"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HasPermission_BlankPermission_ReturnsFalseWithoutCheckingUser(string permission)
    {
        int checks = 0;
        var context = new ReportContext("jdoe", _ =>
        {
            checks++;
            return true;
        });

        Assert.False(context.HasPermission(permission));
        Assert.Equal(0, checks);
    }

    [Fact]
    public void HasAnyPermission_OneMatch_ReturnsTrue()
    {
        ReportContext context = TestReport.ContextWith("SVMSecure.B");

        Assert.True(context.HasAnyPermission(["SVMSecure.A", "SVMSecure.B"]));
    }

    [Fact]
    public void HasAnyPermission_NoMatchOrEmptyList_ReturnsFalse()
    {
        ReportContext context = TestReport.ContextWith("SVMSecure.C");

        Assert.False(context.HasAnyPermission(["SVMSecure.A", "SVMSecure.B"]));
        Assert.False(context.HasAnyPermission([]));
    }

    [Fact]
    public void HasAnyPermission_NullList_Throws()
    {
        ReportContext context = TestReport.ContextWith();

        Assert.Throws<ArgumentNullException>(() => context.HasAnyPermission(null!));
    }
}
