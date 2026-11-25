using System.Text;
using QuestPDF.Infrastructure;
using Viper.Areas.ReportBuilder.Exporters;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>A download runs the report in its own mode and writes the rows to the chosen format.</summary>
public sealed class ReportExportServiceTests
{
    private const string Slug = "8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e";
    private readonly FakeReportRunService _runs = new();
    private readonly ReportExportService _service;

    public ReportExportServiceTests()
    {
        // Program.cs sets this at startup; unit tests don't run Program.cs.
        QuestPDF.Settings.License = LicenseType.Community;
        _service = new ReportExportService(_runs);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ReportRunOutcome Ok(params IReadOnlyDictionary<string, object?>[] rows)
    {
        return new ReportRunOutcome
        {
            Status = ReportRunStatus.Ok,
            Result = new ReportRunResult
            {
                Columns = [new ReportResultColumn { Name = "Name", Label = "Name", Format = ReportColumnFormat.Text, Sortable = true }],
                Rows = rows,
                Page = 1,
                PerPage = rows.Length,
                TotalRows = rows.Length,
                Truncated = false,
            },
            Details = new ReportRunDetails { Title = "Faculty: roster", PdfLandscape = true, Filters = [] },
        };
    }

    [Theory]
    [InlineData("xlsx", ReportRunMode.ExportExcel, ReportExcelWriter.ContentType, "Faculty roster.xlsx", "PK")]
    [InlineData("PDF", ReportRunMode.ExportPdf, ReportPdfWriter.ContentType, "Faculty roster.pdf", "%PDF-")]
    public async Task Export_RunsInItsModeAndWritesTheFile(string format, ReportRunMode mode, string contentType, string fileName, string signature)
    {
        _runs.Outcome = Ok(FakeReportQueryExecutor.Row(("Name", "Ada")));
        var request = new ReportRunRequest { Search = "Ada" };

        ReportExportOutcome outcome = await _service.ExportAsync(Slug, format, request, Ct);

        Assert.Equal(ReportRunStatus.Ok, outcome.Status);
        ReportFile file = Assert.IsType<ReportFile>(outcome.File);
        Assert.Equal(contentType, file.ContentType);
        Assert.Equal(fileName, file.FileName);
        Assert.Equal(signature, Encoding.ASCII.GetString(file.Content, 0, signature.Length));
        Assert.Equal([(Slug, request, mode)], _runs.Calls);
    }

    [Fact]
    public async Task Export_NoRows_HasNoFile()
    {
        _runs.Outcome = Ok();

        ReportExportOutcome outcome = await _service.ExportAsync(Slug, "xlsx", new ReportRunRequest(), Ct);

        Assert.Equal(ReportRunStatus.Ok, outcome.Status);
        Assert.Null(outcome.File);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("")]
    public async Task Export_UnknownFormat_IsInvalidWithoutRunning(string format)
    {
        ReportExportOutcome outcome = await _service.ExportAsync(Slug, format, new ReportRunRequest(), Ct);

        Assert.Equal(ReportRunStatus.Invalid, outcome.Status);
        Assert.Equal(["Choose a download format: xlsx or pdf."], outcome.Errors);
        Assert.Empty(_runs.Calls);
    }

    [Theory]
    [InlineData(ReportRunStatus.Forbidden)]
    [InlineData(ReportRunStatus.NotFound)]
    [InlineData(ReportRunStatus.Invalid)]
    [InlineData(ReportRunStatus.Unavailable)]
    public async Task Export_RunThatFails_PassesTheFailureOn(ReportRunStatus status)
    {
        _runs.Outcome = new ReportRunOutcome { Status = status, Errors = ["Why"] };

        ReportExportOutcome outcome = await _service.ExportAsync(Slug, "pdf", new ReportRunRequest(), Ct);

        Assert.Equal(status, outcome.Status);
        Assert.Equal(["Why"], outcome.Errors);
        Assert.Null(outcome.File);
    }

    private sealed class FakeReportRunService : IReportRunService
    {
        public ReportRunOutcome Outcome { get; set; } = new() { Status = ReportRunStatus.NotFound };

        public List<(string Slug, ReportRunRequest Request, ReportRunMode Mode)> Calls { get; } = [];

        public Task<ReportRunOutcome> RunAsync(string slug, ReportRunRequest request, ReportRunMode mode, CancellationToken ct)
        {
            Calls.Add((slug, request, mode));
            return Task.FromResult(Outcome);
        }

        public Task<ReportRunOutcome> PreviewAsync(ReportDefinition report, ReportRunRequest request, CancellationToken ct)
        {
            throw new NotSupportedException("Downloads never preview.");
        }
    }
}
