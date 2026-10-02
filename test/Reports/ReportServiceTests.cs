using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;
using Viper.Areas.Reports.Services;

namespace Viper.test.Reports;

public sealed class ReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 15, 0, 0, TimeSpan.Zero);

    private readonly TestReport _sample = new();
    private readonly ListLogger<ReportService> _logger = new();

    private ReportService CreateService(params string[] permissions)
    {
        var registry = new ReportRegistry(
        [
            _sample,
            new TestReport(key: "effort.other", title: "Other", area: "Effort", permissions: ["SVMSecure.Effort"]),
        ],
        [new TestPlannedReports()]);
        return new ReportService(
            registry,
            new StubReportUserService(TestReport.ContextWith(permissions)),
            [new ReportCsvExporter()],
            new FixedTimeProvider(Now),
            _logger);
    }

    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    #region Catalog and definitions

    [Fact]
    public void GetCatalog_ListsBuiltAndPlannedReportsTheUserMayRunInTitleOrder()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        IReadOnlyList<ReportCatalogItem> catalog = service.GetCatalog(null);

        Assert.Equal(
            [
                new ReportCatalogItem("test.planned", "Planned report", "Test", "A report not built yet.", Available: false),
                new ReportCatalogItem("test.sample", "Sample report", "Test", "A report used by tests.", Available: true),
            ],
            catalog);
    }

    [Fact]
    public void GetCatalog_OrdersByAreaBeforeTitle()
    {
        ReportService service = CreateService(TestReport.RunPermission, "SVMSecure.Effort", "SVMSecure.Students");

        IReadOnlyList<ReportCatalogItem> catalog = service.GetCatalog(null);

        Assert.Equal(
            ["effort.other", "students.restricted", "test.planned", "test.sample"],
            catalog.Select(item => item.Key));
    }

    [Theory]
    [InlineData(" effort ", 1)]
    [InlineData("Test", 2)]
    [InlineData("Students", 0)]
    [InlineData("", 3)]
    public void GetCatalog_FiltersByArea(string area, int expectedCount)
    {
        ReportService service = CreateService(TestReport.RunPermission, "SVMSecure.Effort");

        Assert.Equal(expectedCount, service.GetCatalog(area).Count);
    }

    [Fact]
    public void GetDefinition_PlannedReport_IsNotFound()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        Assert.Equal(ReportOutcomeStatus.NotFound, service.GetDefinition(TestPlannedReports.Visible.Key).Status);
    }

    [Fact]
    public void GetDefinition_UnknownKey_IsNotFound()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        Assert.Equal(ReportOutcomeStatus.NotFound, service.GetDefinition("test.missing").Status);
    }

    [Fact]
    public void GetDefinition_WithoutPermission_IsForbidden()
    {
        ReportService service = CreateService();

        Assert.Equal(ReportOutcomeStatus.Forbidden, service.GetDefinition("test.sample").Status);
    }

    [Fact]
    public void GetDefinition_WithPermission_ReturnsMetadata()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportDefinitionMetadata> outcome = service.GetDefinition("TEST.SAMPLE");

        Assert.Equal(ReportOutcomeStatus.Ok, outcome.Status);
        Assert.NotNull(outcome.Value);
        Assert.Equal("test.sample", outcome.Value.Key);
    }

    #endregion

    #region Running

    [Fact]
    public async Task RunAsync_UnknownKey_IsNotFound()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportResult> outcome = await service.RunAsync("test.missing", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task RunAsync_PlannedReport_IsNotFound()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportResult> outcome =
            await service.RunAsync(TestPlannedReports.Visible.Key, null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task RunAsync_WithoutPermission_IsForbiddenBeforeReadingParameters()
    {
        ReportService service = CreateService();

        ReportOutcome<ReportResult> outcome =
            await service.RunAsync("test.sample", Json("[\"not an object\"]"), TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Forbidden, outcome.Status);
        Assert.Null(_sample.FetchedWith);
    }

    [Fact]
    public async Task RunAsync_UnreadableParameters_IsInvalid()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportResult> outcome =
            await service.RunAsync("test.sample", Json("""{ "minimumAge": "old" }"""), TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Invalid, outcome.Status);
        Assert.Equal("minimumAge", Assert.Single(outcome.Errors).Parameter);
        Assert.Null(_sample.FetchedWith);
    }

    [Fact]
    public async Task RunAsync_InvalidParameters_IsInvalid()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportResult> outcome =
            await service.RunAsync("test.sample", Json("""{ "facultyType": "X" }"""), TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Invalid, outcome.Status);
        Assert.Equal("facultyType", Assert.Single(outcome.Errors).Parameter);
        Assert.Null(_sample.FetchedWith);
    }

    [Fact]
    public async Task RunAsync_ValidParameters_RunsWithDefaultsAndAudits()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportResult> outcome = await service.RunAsync(
            "test.sample", Json("""{ "search": "a\nb" }"""), TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Ok, outcome.Status);
        Assert.NotNull(outcome.Value);
        Assert.Equal(5, outcome.Value.RowCount);
        Assert.Equal(Now, outcome.Value.GeneratedAt);
        Assert.Equal("S", _sample.FetchedWith?.FacultyType);
        Assert.Equal("tester", _sample.FetchedFor?.LoginId);

        (LogLevel level, string message) = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, level);
        Assert.Contains("test.sample", message, StringComparison.Ordinal);
        Assert.Contains("5 rows", message, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_InformationLoggingOff_SkipsAuditEntry()
    {
        ReportService service = CreateService(TestReport.RunPermission);
        _logger.Enabled = false;

        ReportOutcome<ReportResult> outcome = await service.RunAsync("test.sample", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Ok, outcome.Status);
        Assert.Empty(_logger.Entries);
    }

    #endregion

    #region Exporting

    [Fact]
    public async Task ExportAsync_UnknownKey_IsNotFound()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportFile> outcome =
            await service.ExportAsync("test.missing", "csv", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task ExportAsync_WithoutPermission_IsForbiddenBeforeCheckingFormat()
    {
        ReportService service = CreateService();

        ReportOutcome<ReportFile> outcome =
            await service.ExportAsync("test.sample", "docx", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Forbidden, outcome.Status);
    }

    [Fact]
    public async Task ExportAsync_UnknownFormat_IsInvalidWithoutRunning()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportFile> outcome =
            await service.ExportAsync("test.sample", "docx", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Invalid, outcome.Status);
        Assert.Equal(
            [new ReportValidationError(ReportService.FormatParameter, "Choose an export format: csv.")],
            outcome.Errors);
        Assert.Null(_sample.FetchedWith);
    }

    [Fact]
    public async Task ExportAsync_InvalidParameters_IsInvalid()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportFile> outcome = await service.ExportAsync(
            "test.sample", "csv", Json("""{ "facultyType": "X" }"""), TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Invalid, outcome.Status);
        Assert.Equal("facultyType", Assert.Single(outcome.Errors).Parameter);
    }

    [Fact]
    public async Task ExportAsync_ValidRequest_ReturnsFileAndAudits()
    {
        ReportService service = CreateService(TestReport.RunPermission);

        ReportOutcome<ReportFile> outcome =
            await service.ExportAsync("test.sample", "CSV", null, TestContext.Current.CancellationToken);

        Assert.Equal(ReportOutcomeStatus.Ok, outcome.Status);
        Assert.NotNull(outcome.Value);
        Assert.Equal("Sample report.csv", outcome.Value.FileName);
        Assert.Equal("text/csv", outcome.Value.ContentType);
        Assert.StartsWith("\uFEFFName,Department", Encoding.UTF8.GetString(outcome.Value.Content), StringComparison.Ordinal);
        Assert.Contains("export:csv", Assert.Single(_logger.Entries).Message, StringComparison.Ordinal);
    }

    #endregion
}
