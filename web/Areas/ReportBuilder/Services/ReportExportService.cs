using Viper.Areas.ReportBuilder.Exporters;
using Viper.Areas.ReportBuilder.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>Runs a report for a download and writes it as an Excel workbook or a PDF.</summary>
public interface IReportExportService
{
    /// <summary>
    /// The download in <paramref name="format"/> ("xlsx" or "pdf"). The run checks the view
    /// permission and the report's download flag before anything else.
    /// </summary>
    Task<ReportExportOutcome> ExportAsync(string slug, string format, ReportRunRequest request, CancellationToken ct);
}

public sealed class ReportExportService : IReportExportService
{
    internal const string ExcelFormat = "xlsx";
    internal const string PdfFormat = "pdf";

    private readonly IReportRunService _runs;
    private readonly TimeProvider _clock;

    public ReportExportService(IReportRunService runs, TimeProvider? clock = null)
    {
        _runs = runs;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ReportExportOutcome> ExportAsync(string slug, string format, ReportRunRequest request, CancellationToken ct)
    {
        ReportRunMode? mode = null;
        if (string.Equals(format, ExcelFormat, StringComparison.OrdinalIgnoreCase))
        {
            mode = ReportRunMode.ExportExcel;
        }
        else if (string.Equals(format, PdfFormat, StringComparison.OrdinalIgnoreCase))
        {
            mode = ReportRunMode.ExportPdf;
        }

        if (mode is null)
        {
            return new ReportExportOutcome { Status = ReportRunStatus.Invalid, Errors = ["Choose a download format: xlsx or pdf."] };
        }

        ReportRunOutcome run = await _runs.RunAsync(slug, request, mode.Value, ct);
        if (run.Result is not { } result || run.Details is not { } details)
        {
            return new ReportExportOutcome { Status = run.Status, Errors = run.Errors };
        }

        if (result.Rows.Count == 0)
        {
            return new ReportExportOutcome { Status = ReportRunStatus.Ok };
        }

        var document = new ReportExportDocument
        {
            Title = details.Title,
            GeneratedAt = _clock.GetLocalNow(),
            Filters = details.Filters,
            SensitiveNotice = details.SensitiveNotice,
            Landscape = details.PdfLandscape,
            Columns = result.Columns,
            Rows = result.Rows,
            Truncated = result.Truncated,
        };
        bool excel = mode == ReportRunMode.ExportExcel;
        string extension = excel ? ReportExcelWriter.Extension : ReportPdfWriter.Extension;
        return new ReportExportOutcome
        {
            Status = ReportRunStatus.Ok,
            File = new ReportFile
            {
                Content = excel ? ReportExcelWriter.Write(document) : ReportPdfWriter.Write(document),
                ContentType = excel ? ReportExcelWriter.ContentType : ReportPdfWriter.ContentType,
                FileName = ExcelHelper.BuildExportFilename(new ExportFilenameOptions { ReportName = details.Title, Extension = extension }),
            },
        };
    }
}
