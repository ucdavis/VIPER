using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportRegistryTests
{
    [Fact]
    public void All_OrdersByAreaThenTitle()
    {
        var registry = new ReportRegistry(
        [
            new TestReport(key: "personnel.zeta", title: "Zeta", area: "Personnel"),
            new TestReport(key: "effort.beta", title: "beta", area: "effort"),
            new TestReport(key: "personnel.alpha", title: "Alpha", area: "Personnel"),
        ]);

        Assert.Equal(["effort.beta", "personnel.alpha", "personnel.zeta"], registry.All.Select(report => report.Key));
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var report = new TestReport(key: "personnel.alpha");
        var registry = new ReportRegistry([report]);

        Assert.Same(report, registry.Find("Personnel.Alpha"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("personnel.missing")]
    public void Find_BlankOrUnknownKey_ReturnsNull(string? key)
    {
        var registry = new ReportRegistry([new TestReport(key: "personnel.alpha")]);

        Assert.Null(registry.Find(key));
    }

    [Fact]
    public void Constructor_NullDefinitions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ReportRegistry(null!));
    }

    [Fact]
    public void Constructor_DuplicateKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new ReportRegistry(
        [
            new TestReport(key: "personnel.alpha"),
            new TestReport(key: "PERSONNEL.ALPHA".ToLowerInvariant()),
        ]));
    }

    [Fact]
    public void Constructor_InvalidKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new ReportRegistry([new TestReport(key: "Personnel Alpha")]));
    }

    [Theory]
    [InlineData(" ", "Area", "Description")]
    [InlineData("Title", " ", "Description")]
    [InlineData("Title", "Area", " ")]
    public void Constructor_MissingIdentityText_Throws(string title, string area, string description)
    {
        var report = new TestReport(title: title, area: area, description: description);

        Assert.Throws<InvalidOperationException>(() => new ReportRegistry([report]));
    }

    [Fact]
    public void Constructor_NoPermissions_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new ReportRegistry([new TestReport(permissions: [])]));
    }

    [Fact]
    public void Constructor_BlankPermission_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new ReportRegistry([new TestReport(permissions: ["SVMSecure.Test", " "])]));
    }

    [Fact]
    public void Planned_WithoutSources_IsEmpty()
    {
        Assert.Empty(new ReportRegistry([new TestReport()]).Planned);
    }

    [Fact]
    public void Planned_CombinesSourcesInAreaThenTitleOrder()
    {
        var extra = new StubPlannedSource { PlannedReports = [Plan("effort.zeta", "Zeta", "Effort")] };

        var registry = new ReportRegistry([new TestReport()], [new TestPlannedReports(), extra]);

        Assert.Equal(
            ["effort.zeta", "students.restricted", "test.planned"],
            registry.Planned.Select(report => report.Key));
    }

    [Fact]
    public void Find_NeverReturnsAPlannedReport()
    {
        var registry = new ReportRegistry([new TestReport()], [new TestPlannedReports()]);

        Assert.Null(registry.Find(TestPlannedReports.Visible.Key));
    }

    [Fact]
    public void Constructor_PlannedKeyUsedByADefinition_Throws()
    {
        var planned = new StubPlannedSource { PlannedReports = [Plan("test.sample", "Sample", "Test")] };

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => new ReportRegistry([new TestReport()], [planned]));
        Assert.Contains("test.sample", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_DuplicatePlannedKey_Throws()
    {
        var first = new StubPlannedSource { PlannedReports = [Plan("test.planned-twice", "One", "Test")] };
        var second = new StubPlannedSource { PlannedReports = [Plan("test.planned-twice", "Two", "Test")] };

        Assert.Throws<InvalidOperationException>(() => new ReportRegistry([], [first, second]));
    }

    [Theory]
    [InlineData("Test Planned", "Title", "SVMSecure.Test")]
    [InlineData("test.planned", " ", "SVMSecure.Test")]
    [InlineData("test.planned", "Title", " ")]
    public void Constructor_InvalidPlannedReport_Throws(string key, string title, string permission)
    {
        var planned = new StubPlannedSource { PlannedReports = [new PlannedReport(key, title, "Test", "Description", [permission])] };

        Assert.Throws<InvalidOperationException>(() => new ReportRegistry([], [planned]));
    }

    private static PlannedReport Plan(string key, string title, string area)
    {
        return new PlannedReport(key, title, area, "Description", ["SVMSecure.Test"]);
    }

    /// <summary>
    /// Has a parameterless constructor because the registration tests scan this assembly and
    /// build every planned report source they find.
    /// </summary>
    private sealed class StubPlannedSource : IPlannedReportSource
    {
        public IReadOnlyList<PlannedReport> PlannedReports { get; init; } = [];
    }
}
