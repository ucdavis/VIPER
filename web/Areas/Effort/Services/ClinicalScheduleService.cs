using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for querying clinical schedule data from the Clinical Scheduler database.
/// Produces the "Scheduled CLI Weeks" report showing how many weeks each instructor
/// is scheduled for clinical rotations, grouped by service and term.
/// </summary>
public class ClinicalScheduleService : BaseReportService, IClinicalScheduleService
{
    private readonly ClinicalSchedulerContext _clinicalContext;
    private readonly AAUDContext _aaudContext;
    private readonly ILogger<ClinicalScheduleService> _logger;

    public ClinicalScheduleService(
        EffortDbContext context,
        ClinicalSchedulerContext clinicalContext,
        AAUDContext aaudContext,
        ITermService termService,
        ILogger<ClinicalScheduleService> logger)
        : base(context, termService)
    {
        _clinicalContext = clinicalContext;
        _aaudContext = aaudContext;
        _logger = logger;
    }

    public async Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportAsync(
        int termCode,
        CancellationToken ct = default)
    {
        var termName = _termService.GetTermName(termCode);
        var rawRows = await QueryClinicalScheduleAsync([termCode], ct);

        var report = BuildReport(rawRows, _termService);
        report.TermName = termName;
        return report;
    }

    public async Task<ScheduledCliWeeksReport> GetScheduledCliWeeksReportByYearAsync(
        string academicYear,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);

        if (termCodes.Count == 0)
        {
            return new ScheduledCliWeeksReport
            {
                TermName = academicYear,
                AcademicYear = academicYear
            };
        }

        var rawRows = await QueryClinicalScheduleAsync(termCodes, ct);

        var report = BuildReport(rawRows, _termService);
        report.TermName = academicYear;
        report.AcademicYear = academicYear;
        return report;
    }

    /// <summary>
    /// Query the Clinical Scheduler database for instructor schedule data.
    /// Joins InstructorSchedule → Rotation → Service and InstructorSchedule → Week
    /// to get service names and term codes, then joins to vPerson for instructor names.
    /// Groups by MothraId, term, and service to count scheduled weeks.
    /// </summary>
    private async Task<List<CliWeeksRawRow>> QueryClinicalScheduleAsync(
        List<int> termCodes,
        CancellationToken ct)
    {
        // Get week IDs for the requested terms
        var weekIds = await _clinicalContext.Weeks
            .AsNoTracking()
            .Where(w => termCodes.Contains(w.TermCode))
            .Select(w => new { w.WeekId, w.TermCode })
            .ToListAsync(ct);

        if (weekIds.Count == 0)
        {
            return [];
        }

        var weekIdList = weekIds.Select(w => w.WeekId).ToList();
        var weekTermLookup = weekIds.ToDictionary(w => w.WeekId, w => w.TermCode);

        // Query instructor schedules with rotation → service navigation
        var scheduleData = await _clinicalContext.InstructorSchedules
            .AsNoTracking()
            .Include(isc => isc.Rotation)
                .ThenInclude(r => r.Service)
            .Where(isc => EF.Parameter(weekIdList).Contains(isc.WeekId))
            .Select(isc => new
            {
                isc.MothraId,
                isc.WeekId,
                ServiceName = isc.Rotation.Service.ServiceName ?? ""
            })
            .ToListAsync(ct);

        if (scheduleData.Count == 0)
        {
            return [];
        }

        // Get distinct MothraIds and filter to instructors with eligible job codes
        var distinctMothraIds = scheduleData
            .Select(s => s.MothraId)
            .Distinct()
            .ToList();

        // Filter to instructors whose job code has IncludeClinSchedule (matches legacy — no IsActive filter)
        var clinicalJobCodes = await _context.JobCodes
            .AsNoTracking()
            .Where(jc => jc.IncludeClinSchedule)
            .Select(jc => jc.Code)
            .ToListAsync(ct);

        if (clinicalJobCodes.Count == 0)
        {
            return [];
        }

        // Use pps.dbo.vw_personJobPosition (legacy view) instead of aaud.dbo.vw_jobsForAAUD
        // — the PPS view contains different/additional job codes for the same employees
        var eligibleEmplIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand { Connection = connection };
        var paramNames = new List<string>(clinicalJobCodes.Count);
        for (var i = 0; i < clinicalJobCodes.Count; i++)
        {
            var paramName = $"@jc{i}";
            paramNames.Add(paramName);
            command.Parameters.AddWithValue(paramName, clinicalJobCodes[i]);
        }
        command.CommandText = $"SELECT DISTINCT emplid FROM pps.dbo.vw_personJobPosition WHERE jobcode IN ({string.Join(", ", paramNames)})";

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!await reader.IsDBNullAsync(0, ct))
            {
                eligibleEmplIds.Add(reader.GetString(0));
            }
        }

        var eligibleMothraIds = (await _aaudContext.AaudUsers
            .AsNoTracking()
            .Where(u => u.EmployeeId != null && EF.Parameter(eligibleEmplIds.ToList()).Contains(u.EmployeeId))
            .Select(u => u.MothraId)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        distinctMothraIds = distinctMothraIds
            .Where(m => eligibleMothraIds.Contains(m))
            .ToList();

        scheduleData = scheduleData
            .Where(s => eligibleMothraIds.Contains(s.MothraId))
            .ToList();

        if (scheduleData.Count == 0)
        {
            return [];
        }

        var personLookup = await _clinicalContext.Persons
            .AsNoTracking()
            .Where(p => EF.Parameter(distinctMothraIds).Contains(p.IdsMothraId))
            .ToDictionaryAsync(
                p => p.IdsMothraId,
                p => p.PersonDisplayLastName.TrimEnd() + ", " + p.PersonDisplayFirstName.TrimEnd(),
                ct);

        // Group by MothraId + TermCode + Service and count distinct weeks
        var grouped = scheduleData
            .Where(s => !string.IsNullOrEmpty(s.MothraId))
            .GroupBy(s => new
            {
                s.MothraId,
                TermCode = weekTermLookup.GetValueOrDefault(s.WeekId, 0),
                s.ServiceName
            })
            .Select(g => new CliWeeksRawRow
            {
                MothraId = g.Key.MothraId,
                Instructor = personLookup.GetValueOrDefault(g.Key.MothraId, g.Key.MothraId),
                TermCode = g.Key.TermCode,
                Service = (g.Key.ServiceName ?? "").TrimEnd(),
                WeekCount = g.Select(s => s.WeekId).Distinct().Count()
            })
            .ToList();

        _logger.LogDebug(
            "Clinical schedule query returned {RowCount} grouped rows for {TermCount} term(s)",
            grouped.Count, termCodes.Count);

        return grouped;
    }

    /// <summary>
    /// Build the ScheduledCliWeeksReport DTO from raw grouped rows.
    /// </summary>
    private static ScheduledCliWeeksReport BuildReport(
        List<CliWeeksRawRow> rows,
        ITermService termService)
    {
        if (rows.Count == 0)
        {
            return new ScheduledCliWeeksReport();
        }

        // Collect distinct services and term names for column headers
        var services = rows
            .Select(r => r.Service)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        var termCodes = rows
            .Select(r => r.TermCode)
            .Distinct()
            .OrderBy(tc => tc)
            .ToList();

        var termNames = termCodes
            .Select(tc => termService.GetTermName(tc))
            .ToList();

        // Group by instructor, then by term within each instructor
        var instructors = rows
            .GroupBy(r => r.MothraId, StringComparer.OrdinalIgnoreCase)
            .Select(instrGroup =>
            {
                var first = instrGroup.First();

                var terms = instrGroup
                    .GroupBy(r => r.TermCode)
                    .OrderBy(g => g.Key)
                    .Select(termGroup =>
                    {
                        var weeksByService = new Dictionary<string, int>();
                        foreach (var row in termGroup.Where(r => !string.IsNullOrWhiteSpace(r.Service)))
                        {
                            weeksByService[row.Service] =
                                weeksByService.GetValueOrDefault(row.Service) + row.WeekCount;
                        }

                        return new ScheduledCliWeeksTermRow
                        {
                            TermCode = termGroup.Key,
                            TermName = termService.GetTermName(termGroup.Key),
                            WeeksByService = weeksByService,
                            TermTotal = weeksByService.Values.Sum()
                        };
                    })
                    .ToList();

                return new ScheduledCliWeeksInstructorRow
                {
                    MothraId = first.MothraId,
                    Instructor = first.Instructor,
                    Terms = terms,
                    TotalWeeks = terms.Sum(t => t.TermTotal)
                };
            })
            .OrderBy(i => i.Instructor, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ScheduledCliWeeksReport
        {
            TermNames = termNames,
            Services = services,
            Instructors = instructors
        };
    }

    public Task<byte[]> GenerateReportPdfAsync(ScheduledCliWeeksReport report)
    {
        Settings.License = LicenseType.Community;

        var showTotal = report.TermNames.Count > 1;
        var termCount = report.TermNames.Count;
        var fontSize = ReportPdfSettings.FontSize;
        var headerFontSize = ReportPdfSettings.HeaderFontSize;
        var cellPadV = ReportPdfSettings.CellPadV;
        // Use landscape for multi-term, portrait for single term
        var useLandscape = termCount > 1;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(useLandscape ? PageSizes.Letter.Landscape() : PageSizes.Letter);
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.25f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(fontSize));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                    });
                    col.Item().BorderBottom(1.5f).BorderColor(Colors.Black)
                        .PaddingVertical(6).Row(row =>
                        {
                            row.RelativeItem(3).Text("Merit & Promotion Report - Scheduled Clinical Weeks").SemiBold().FontSize(12);
                            var termLabel = report.AcademicYear != null
                                ? $"Academic Year {report.TermName}"
                                : report.TermName;
                            row.RelativeItem(1).AlignRight().Text(termLabel).SemiBold().FontSize(12);
                        });
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // Proportional widths for instructor and per-term data
                        columns.RelativeColumn(3);
                        for (var i = 0; i < termCount; i++)
                        {
                            columns.RelativeColumn(3);
                        }
                        if (showTotal)
                        {
                            columns.ConstantColumn(55);
                        }
                    });

                    var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                    table.Header(header =>
                    {
                        header.Cell().PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
                        foreach (var termName in report.TermNames)
                        {
                            header.Cell().PaddingVertical(cellPadV).Text(termName).Style(hdrStyle);
                        }
                        if (showTotal)
                        {
                            header.Cell().PaddingVertical(cellPadV).AlignCenter().Text("AY Total").Style(hdrStyle);
                        }
                    });

                    foreach (var instructor in report.Instructors)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(cellPadV).Text(instructor.Instructor);

                        foreach (var termName in report.TermNames)
                        {
                            var term = instructor.Terms.FirstOrDefault(t => t.TermName == termName);
                            if (term != null)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(cellPadV).Column(col =>
                                    {
                                        foreach (var (service, weeks) in term.WeeksByService.Where(kv => kv.Value > 0))
                                        {
                                            col.Item().Text($"{service} - {weeks}");
                                        }
                                    });
                            }
                            else
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(cellPadV).Text("");
                            }
                        }

                        if (showTotal)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(cellPadV).AlignCenter().Text(instructor.TotalWeeks.ToString());
                        }
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    public MemoryStream GenerateReportExcel(ScheduledCliWeeksReport report)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Scheduled CLI Weeks");
        var showTotal = report.TermNames.Count > 1;

        // Header rows matching PDF (term is already in the column headers)
        int row = AddExcelHeader(ws, "Merit & Promotion Report - Scheduled Clinical Weeks", null);
        row++; // blank separator

        // Column headers
        int col = 1;
        ws.Cell(row, col++).Value = "Instructor";
        foreach (var termName in report.TermNames)
        {
            ws.Cell(row, col++).Value = termName;
        }
        if (showTotal)
        {
            ws.Cell(row, col).Value = "AY Total";
        }
        ws.Range($"{row}:{row}").Style.Font.Bold = true;
        ws.SheetView.FreezeRows(row);
        row++;
        foreach (var instructor in report.Instructors)
        {
            col = 1;
            ws.Cell(row, col++).Value = ExcelHelper.SanitizeStringCell(instructor.Instructor);

            foreach (var termName in report.TermNames)
            {
                var term = instructor.Terms.FirstOrDefault(t => t.TermName == termName);
                if (term != null)
                {
                    var services = term.WeeksByService
                        .Where(kv => kv.Value > 0)
                        .Select(kv => $"{kv.Key} - {kv.Value}");
                    var cell = ws.Cell(row, col);
                    cell.Value = string.Join("\n", services);
                    cell.Style.Alignment.WrapText = true;
                }
                col++;
            }

            if (showTotal)
            {
                ws.Cell(row, col).Value = instructor.TotalWeeks;
            }
            row++;
        }

        ws.Columns().AdjustToContents();

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        wb.Dispose();
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Internal row from the clinical schedule query, pre-grouped by
    /// instructor + term + service.
    /// </summary>
    private sealed class CliWeeksRawRow
    {
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public int TermCode { get; set; }
        public string Service { get; set; } = string.Empty;
        public int WeekCount { get; set; }
    }
}
