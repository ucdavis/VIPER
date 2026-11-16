using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>Choices come from a parameter's fixed list, or else from a [report] view the catalog lists.</summary>
public sealed class ReportChoiceServiceTests
{
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportQueryExecutor _executor = new();
    private readonly ReportChoiceService _service;

    public ReportChoiceServiceTests()
    {
        _service = new ReportChoiceService(_catalog, _executor);
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "DeptChoices",
            Kind = ReportSourceKind.View,
            Columns = [],
            Parameters = [],
            PagesInSql = true,
        });
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "usp_Choices",
            Kind = ReportSourceKind.Procedure,
            Columns = [],
            Parameters = [],
            PagesInSql = false,
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ReportParameter Choice(string name, string? list = null, string? source = null,
        ReportParameterType type = ReportParameterType.Choice)
    {
        return new ReportParameter { Name = name, Type = type, ChoicesList = list, ChoicesSource = source };
    }

    [Fact]
    public async Task FixedList_IsUsedWithoutAQuery()
    {
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _service.GetChoicesAsync(
            [Choice("Dept", list: """["VME"]""", source: "DeptChoices")], Ct);

        Assert.Equal([new ReportChoice { Value = "VME", Label = "VME" }], choices["dept"]);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task View_IsReadForValuesAndLabels()
    {
        _executor.Answer = _ => new ReportQueryResult
        {
            Rows =
            [
                FakeReportQueryExecutor.Row(("value", "VME"), ("label", "Medicine")),
                FakeReportQueryExecutor.Row(("Value", 7), ("Label", null)),
                FakeReportQueryExecutor.Row(("Value", null), ("Label", "No value")),
                FakeReportQueryExecutor.Row(("Label", "No value column")),
            ],
            Truncated = false,
        };

        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _service.GetChoicesAsync(
            [Choice("Dept", list: "not json", source: " deptchoices ", type: ReportParameterType.MultiChoice)], Ct);

        Assert.Equal(
            [new ReportChoice { Value = "VME", Label = "Medicine" }, new ReportChoice { Value = "7", Label = "7" }],
            choices["Dept"]);
        (ReportSqlCommand command, int maxRows, TimeSpan timeout) = Assert.Single(_executor.Calls);
        Assert.Equal(ReportSqlBuilder.BuildChoices("DeptChoices").Text, command.Text);
        Assert.Equal(ReportLimits.MaxChoices, maxRows);
        Assert.Equal(ReportLimits.ScreenTimeout, timeout);
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("usp_Choices")]
    public async Task SourceThatIsNotACatalogView_HasNoChoices(string source)
    {
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _service.GetChoicesAsync([Choice("Dept", source: source)], Ct);

        Assert.Empty(choices);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task OtherParameters_AreSkipped()
    {
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _service.GetChoicesAsync(
        [
            new ReportParameter { Name = "Name", Type = ReportParameterType.Text, ChoicesList = """["x"]""" },
            new ReportParameter { Name = " ", Type = ReportParameterType.Choice, ChoicesList = """["x"]""" },
            Choice("Dept"),
        ], Ct);

        Assert.Empty(choices);
    }

    [Fact]
    public async Task LongLists_AreCapped()
    {
        string list = "[" + string.Join(",", Enumerable.Range(0, ReportLimits.MaxChoices + 5).Select(i => $"\"{i}\"")) + "]";

        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices = await _service.GetChoicesAsync([Choice("N", list: list)], Ct);

        Assert.Equal(ReportLimits.MaxChoices, choices["N"].Count);
    }

    [Fact]
    public async Task Catalog_IsReadOnceForSeveralViews()
    {
        await _service.GetChoicesAsync([Choice("A", source: "DeptChoices"), Choice("B", source: "DeptChoices")], Ct);

        Assert.Equal(2, _executor.Calls.Count);
    }
}
