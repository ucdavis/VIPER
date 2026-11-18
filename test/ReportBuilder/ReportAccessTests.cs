using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>The defaults and rules every viewer endpoint shares.</summary>
public sealed class ReportAccessTests
{
    [Fact]
    public void VisibleColumns_InOrderWithLabelsAndFormatsDefaulted()
    {
        var report = new ReportDefinition
        {
            Columns =
            [
                new ReportColumn { Id = 3, SourceColumn = " Dept ", DisplayOrder = 2, Label = "Department", Format = ReportColumnFormat.Text, Sortable = false },
                new ReportColumn { Id = 2, SourceColumn = "Name", DisplayOrder = 1 },
                new ReportColumn { Id = 1, SourceColumn = "Salary", DisplayOrder = 1, Format = ReportColumnFormat.Currency },
                new ReportColumn { Id = 4, SourceColumn = "Hidden", DisplayOrder = 0, Visible = false },
                new ReportColumn { Id = 5, SourceColumn = " ", DisplayOrder = 0 },
            ],
        };

        Assert.Equal(
            [
                new ReportResultColumn { Name = "Salary", Label = "Salary", Format = ReportColumnFormat.Currency, Sortable = true },
                new ReportResultColumn { Name = "Name", Label = "Name", Format = ReportColumnFormat.Text, Sortable = true },
                new ReportResultColumn { Name = "Dept", Label = "Department", Format = ReportColumnFormat.Text, Sortable = false },
            ],
            ReportAccess.VisibleColumns(report));
    }

    [Fact]
    public void UserParameters_LeaveOutCurrentUserAndUnnamedOnes()
    {
        var report = new ReportDefinition
        {
            Parameters =
            [
                new ReportParameter { Name = "B", DisplayOrder = 2, Binding = ReportParameterBinding.UserInput },
                new ReportParameter { Name = "Login", DisplayOrder = 0, Binding = ReportParameterBinding.CurrentUserLoginId },
                new ReportParameter { Name = " ", DisplayOrder = 0 },
                new ReportParameter { Name = "A", DisplayOrder = 1 },
            ],
        };

        Assert.Equal(["A", "B"], ReportAccess.UserParameters(report).Select(p => p.Name));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Paginates_UnlessTurnedOff(bool? paginate, bool expected)
    {
        Assert.Equal(expected, ReportAccess.Paginates(new ReportDefinition { Paginate = paginate }));
    }

    [Theory]
    [InlineData(null, ReportLimits.DefaultPageSize)]
    [InlineData(10, 10)]
    [InlineData(0, 1)]
    [InlineData(5000, ReportLimits.MaxPageSize)]
    public void PageSize_DefaultsAndStaysInRange(int? pageSize, int expected)
    {
        Assert.Equal(expected, ReportAccess.PageSize(new ReportDefinition { PageSize = pageSize }));
    }

    [Fact]
    public void CanView_AdminOrAnyOfTheReportsPermissions()
    {
        var users = new FakeReportUserService();
        var report = new ReportDefinition { Permissions = [new ReportPermission { Permission = "SVMSecure.A" }] };

        Assert.False(ReportAccess.CanView(users, report));

        users.Permissions.Add("SVMSecure.A");
        Assert.True(ReportAccess.CanView(users, report));

        users.Permissions.Clear();
        users.Permissions.Add(ReportBuilderPermissions.Admin);
        Assert.True(ReportAccess.CanView(users, new ReportDefinition()));
    }
}
