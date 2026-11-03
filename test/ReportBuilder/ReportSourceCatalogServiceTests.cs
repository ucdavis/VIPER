using NSubstitute;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// The catalog lists the [reports] views and procedures and describes one at a time, only by
/// names it listed. The metadata reader is a fake, so these tests cover the rules, not the SQL.
/// </summary>
public sealed class ReportSourceCatalogServiceTests
{
    private readonly IReportSourceMetadataReader _reader = Substitute.For<IReportSourceMetadataReader>();
    private readonly ReportSourceCatalogService _catalog;

    public ReportSourceCatalogServiceTests()
    {
        _catalog = new ReportSourceCatalogService(_reader);
        _reader.GetObjectsAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceObjectRow { Name = "FacultyRoster", Type = "V", Description = " Current faculty " },
            new ReportSourceObjectRow { Name = "usp_Leave", Type = "P", Description = " " },
            new ReportSourceObjectRow { Name = "usp_Paged", Type = "P" },
        ]);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetSources_ListsViewsAndProceduresWithTrimmedDescriptions()
    {
        IReadOnlyList<ReportSourceSummary> sources = await _catalog.GetSourcesAsync(Ct);

        Assert.Equal(
            [
                new ReportSourceSummary { Name = "FacultyRoster", Kind = ReportSourceKind.View, Description = "Current faculty" },
                new ReportSourceSummary { Name = "usp_Leave", Kind = ReportSourceKind.Procedure },
                new ReportSourceSummary { Name = "usp_Paged", Kind = ReportSourceKind.Procedure },
            ],
            sources);
    }

    [Fact]
    public async Task GetSource_UnknownName_IsNullAndNeverReachesTheCatalog()
    {
        Assert.Null(await _catalog.GetSourceAsync("Persons]; DROP TABLE x;--", Ct));

        await _reader.DidNotReceive().GetViewColumnsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _reader.DidNotReceive().GetProcedureParametersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSource_View_DescribesItsColumnsAndPagesInSql()
    {
        _reader.GetViewColumnsAsync("FacultyRoster", Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceColumnRow { Name = "Name", SqlType = "nvarchar", IsNullable = false },
            new ReportSourceColumnRow { Name = "Photo", SqlType = "varbinary", IsNullable = true },
        ]);

        // Matched ignoring case, as SQL Server matches names.
        ReportSourceDetail? view = await _catalog.GetSourceAsync("facultyroster", Ct);

        Assert.NotNull(view);
        Assert.Equal("FacultyRoster", view.Name);
        Assert.Equal(ReportSourceKind.View, view.Kind);
        Assert.True(view.PagesInSql);
        Assert.Empty(view.Parameters);
        Assert.Null(view.ColumnsError);
        Assert.Equal(
            [
                new ReportSourceColumn { Name = "Name", SqlType = "nvarchar", Kind = ReportValueKind.Text, IsNullable = false, IsSupported = true },
                new ReportSourceColumn { Name = "Photo", SqlType = "varbinary", Kind = ReportValueKind.Binary, IsNullable = true, IsSupported = false },
            ],
            view.Columns);
    }

    [Fact]
    public async Task GetSource_Procedure_DescribesParametersAndResultColumns()
    {
        _reader.GetProcedureParametersAsync("usp_Leave", Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceParameterRow { Name = "@StartDate", SqlType = "date" },
            new ReportSourceParameterRow { Name = "@Count", SqlType = "int", IsOutput = true },
        ]);
        _reader.DescribeProcedureResultAsync("usp_Leave", Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceColumnRow { Name = "Name", SqlType = "nvarchar(100)" },
            new ReportSourceColumnRow { Name = null, SqlType = "int" },
        ]);

        ReportSourceDetail? procedure = await _catalog.GetSourceAsync("usp_Leave", Ct);

        Assert.NotNull(procedure);
        Assert.False(procedure.PagesInSql);
        Assert.Equal(
            [
                new ReportSourceParameter { Name = "StartDate", SqlType = "date", Kind = ReportValueKind.Date, IsSupported = true },
                new ReportSourceParameter { Name = "Count", SqlType = "int", Kind = ReportValueKind.WholeNumber, IsSupported = false },
            ],
            procedure.Parameters);

        // An unnamed result column can't be chosen, and an unknown nullability reads as nullable.
        ReportSourceColumn column = Assert.Single(procedure.Columns);
        Assert.Equal("Name", column.Name);
        Assert.True(column.IsNullable);
    }

    [Fact]
    public async Task GetSource_ProcedureWithSkipAndTake_PagesInSqlAndHidesThem()
    {
        _reader.GetProcedureParametersAsync("usp_Paged", Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceParameterRow { Name = "@Dept", SqlType = "varchar" },
            new ReportSourceParameterRow { Name = "@skip", SqlType = "int" },
            new ReportSourceParameterRow { Name = "@Take", SqlType = "int" },
        ]);
        _reader.DescribeProcedureResultAsync("usp_Paged", Arg.Any<CancellationToken>()).Returns([]);

        ReportSourceDetail? procedure = await _catalog.GetSourceAsync("usp_Paged", Ct);

        Assert.NotNull(procedure);
        Assert.True(procedure.PagesInSql);
        Assert.Equal(["Dept"], procedure.Parameters.Select(p => p.Name));
    }

    [Theory]
    [InlineData("varchar", "int")]
    [InlineData("int", "nvarchar")]
    public async Task GetSource_SkipOrTakeThatIsNotAnInteger_DoesNotPageInSql(string skipType, string takeType)
    {
        _reader.GetProcedureParametersAsync("usp_Paged", Arg.Any<CancellationToken>()).Returns(
        [
            new ReportSourceParameterRow { Name = "@Skip", SqlType = skipType },
            new ReportSourceParameterRow { Name = "@Take", SqlType = takeType },
        ]);
        _reader.DescribeProcedureResultAsync("usp_Paged", Arg.Any<CancellationToken>()).Returns([]);

        ReportSourceDetail? procedure = await _catalog.GetSourceAsync("usp_Paged", Ct);

        Assert.NotNull(procedure);
        Assert.False(procedure.PagesInSql);
        Assert.Equal(2, procedure.Parameters.Count);
    }

    [Fact]
    public async Task GetSource_ProcedureSqlServerCannotDescribe_ReportsWhy()
    {
        _reader.GetProcedureParametersAsync("usp_Leave", Arg.Any<CancellationToken>()).Returns([]);
        _reader.DescribeProcedureResultAsync("usp_Leave", Arg.Any<CancellationToken>()).Returns(
            [new ReportSourceColumnRow { ErrorMessage = "Invalid object name '#people'." }]);

        ReportSourceDetail? procedure = await _catalog.GetSourceAsync("usp_Leave", Ct);

        Assert.NotNull(procedure);
        Assert.Empty(procedure.Columns);
        Assert.Equal("Invalid object name '#people'.", procedure.ColumnsError);
    }
}
