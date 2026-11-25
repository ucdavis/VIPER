using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// The SQL a report runs: names are bracket-quoted, and every value, filter or search text
/// alike, is a parameter, so nothing a user types reaches the SQL text.
/// </summary>
public sealed class ReportSqlBuilderTests
{
    private static BoundReportParameter Filter(string name, object value, ReportFilterOperator? op = null,
        ReportParameterType type = ReportParameterType.Text)
    {
        return new BoundReportParameter { Name = name, Type = type, Operator = op, Value = value, FromUser = true };
    }

    private static ReportViewQuery View(params BoundReportParameter[] filters)
    {
        return new ReportViewQuery { Source = "FacultyRoster", Columns = ["Name", "Dept"], Filters = filters };
    }

    [Fact]
    public void BuildView_Unpaged_ReadsOneRowPastTheCap()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View());

        Assert.Equal("SELECT TOP (@__top) [Name], [Dept] FROM [report].[FacultyRoster] ORDER BY (SELECT NULL)", command.Text);
        Assert.Null(command.CountText);
        Assert.Equal(ReportLimits.MaxRows + 1, command.Parameters["@__top"]);
    }

    [Fact]
    public void BuildView_Paged_UsesOffsetFetchAndCountsTheSameRows()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View(Filter("Dept", "VME")) with
        {
            SortColumn = "Name",
            SortDescending = true,
            Skip = 50,
            Take = 25,
        });

        Assert.Equal(
            "SELECT [Name], [Dept] FROM [report].[FacultyRoster] WHERE [Dept] = @p0 ORDER BY [Name] DESC OFFSET @__skip ROWS FETCH NEXT @__take ROWS ONLY",
            command.Text);
        Assert.Equal("SELECT COUNT(*) FROM [report].[FacultyRoster] WHERE [Dept] = @p0", command.CountText);
        Assert.Equal(50, command.Parameters["@__skip"]);
        Assert.Equal(25, command.Parameters["@__take"]);
        Assert.Equal("VME", command.Parameters["@p0"]);
    }

    [Fact]
    public void BuildView_SortsAscendingByDefault()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View() with { SortColumn = "Dept" });

        Assert.Contains("ORDER BY [Dept] ASC", command.Text);
    }

    [Fact]
    public void BuildView_Operators_EachBecomeTheirComparison()
    {
        var date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View(
            Filter("Name", "o'brien", ReportFilterOperator.Contains),
            Filter("Hired", date, ReportFilterOperator.From, ReportParameterType.Date),
            Filter("Hired", date, ReportFilterOperator.To, ReportParameterType.Date),
            Filter("Salary", 100m, ReportFilterOperator.To, ReportParameterType.Number),
            Filter("Dept", new List<string> { "VME", "PMI" }, ReportFilterOperator.In, ReportParameterType.MultiChoice),
            Filter("Title", "Professor", ReportFilterOperator.In),
            Filter("Active", true)));

        Assert.Equal(
            "SELECT TOP (@__top) [Name], [Dept] FROM [report].[FacultyRoster] WHERE "
            + "[Name] LIKE @p0 ESCAPE '\\' AND [Hired] >= @p1 AND [Hired] < @p2 AND [Salary] <= @p3 "
            + "AND [Dept] IN (@p4_0, @p4_1) AND [Title] IN (@p5_0) AND [Active] = @p6 ORDER BY (SELECT NULL)",
            command.Text);
        Assert.Equal("%o'brien%", command.Parameters["@p0"]);
        Assert.Equal(date, command.Parameters["@p1"]);
        Assert.Equal(date.AddDays(1), command.Parameters["@p2"]);
        Assert.Equal(100m, command.Parameters["@p3"]);
        Assert.Equal("VME", command.Parameters["@p4_0"]);
        Assert.Equal("PMI", command.Parameters["@p4_1"]);
        Assert.Equal("Professor", command.Parameters["@p5_0"]);
        Assert.True(Assert.IsType<bool>(command.Parameters["@p6"]));
    }

    [Fact]
    public void BuildView_Search_LooksInEachSearchColumnWithOneParameter()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View(Filter("Dept", "VME")) with
        {
            SearchColumns = ["Name", "Dept"],
            Search = "  50%_off  ",
        });

        Assert.Contains(
            "WHERE [Dept] = @p0 AND ([Name] LIKE @__search ESCAPE '\\' OR [Dept] LIKE @__search ESCAPE '\\')",
            command.Text);
        Assert.Equal("%50\\%\\_off%", command.Parameters["@__search"]);
    }

    [Fact]
    public void BuildView_SearchWithoutTextColumns_AddsNoCondition()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(View() with { Search = "x" });

        Assert.DoesNotContain("WHERE", command.Text);
        Assert.False(command.Parameters.ContainsKey("@__search"));
    }

    [Fact]
    public void BuildView_NamesWithBrackets_AreEscapedNotTrusted()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildView(new ReportViewQuery
        {
            Source = "Roster]; DROP TABLE x;--",
            Columns = ["Na]me"],
        });

        Assert.Equal("SELECT TOP (@__top) [Na]]me] FROM [report].[Roster]]; DROP TABLE x;--] ORDER BY (SELECT NULL)", command.Text);
    }

    [Fact]
    public void BuildView_NoColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() => ReportSqlBuilder.BuildView(new ReportViewQuery { Source = "V", Columns = [] }));
    }

    [Fact]
    public void BuildProcedure_PassesEachArgumentAsAParameter()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildProcedure("usp_Leave",
        [
            Filter("LoginId", "jdoe"),
            Filter("@Depts", new List<string> { "VME", "PMI" }, type: ReportParameterType.MultiChoice),
        ]);

        Assert.Equal("EXEC [report].[usp_Leave] @LoginId = @p0, @Depts = @p1", command.Text);
        Assert.Equal("jdoe", command.Parameters["@p0"]);
        Assert.Equal("VME,PMI", command.Parameters["@p1"]);
    }

    [Fact]
    public void BuildProcedure_NoArguments_IsAPlainExec()
    {
        Assert.Equal("EXEC [report].[usp_All]", ReportSqlBuilder.BuildProcedure("usp_All", []).Text);
    }

    [Fact]
    public void BuildProcedure_WithPaging_PassesSkipAndTake()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildProcedure("usp_Paged", [], skip: 25, take: 25);

        Assert.Equal("EXEC [report].[usp_Paged] @Skip = @__skip, @Take = @__take", command.Text);
        Assert.Equal(25, command.Parameters["@__skip"]);
        Assert.Equal(25, command.Parameters["@__take"]);
    }

    [Theory]
    [InlineData("Login Id")]
    [InlineData("x;DROP")]
    [InlineData("1abc")]
    [InlineData("")]
    public void BuildProcedure_ParameterNameThatIsNotAnIdentifier_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => ReportSqlBuilder.BuildProcedure("usp_Leave", [Filter(name, "x")]));
    }

    [Fact]
    public void BuildChoices_ReadsValueAndLabelUpToTheChoiceLimit()
    {
        ReportSqlCommand command = ReportSqlBuilder.BuildChoices("DeptChoices");

        Assert.Equal("SELECT TOP (@__top) [Value], [Label] FROM [report].[DeptChoices] ORDER BY [Label]", command.Text);
        Assert.Equal(ReportLimits.MaxChoices, command.Parameters["@__top"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Quote_BlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => ReportSqlBuilder.Quote(name));
    }
}
