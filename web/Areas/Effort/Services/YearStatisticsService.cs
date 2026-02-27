using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public class YearStatisticsService : BaseReportService, IYearStatisticsService
{
    private readonly ILogger<YearStatisticsService> _logger;

    public YearStatisticsService(
        EffortDbContext context,
        ILogger<YearStatisticsService> logger)
        : base(context)
    {
        _logger = logger;
    }

    // ============================================
    // Year Statistics Report
    // ============================================

    public async Task<YearStatisticsReport> GetYearStatisticsReportAsync(
        string academicYear,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        if (termCodes.Count == 0)
        {
            return new YearStatisticsReport { AcademicYear = academicYear };
        }

        // SP only accepts a single term code, so call for each term and merge
        var allRows = new List<YearStatsRawRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteYearStatisticsSpAsync(tc, ct);
            allRows.AddRange(rows);
        }

        if (allRows.Count == 0)
        {
            return new YearStatisticsReport { AcademicYear = academicYear };
        }

        // Remove excluded instructors
        allRows.RemoveAll(r => YearStatsConstants.ExcludedInstructors.Contains(r.MothraId));

        // Discover effort types from the data
        var effortTypes = DiscoverEffortTypes(allRows);
        var orderedTypes = GetOrderedEffortTypes(effortTypes);

        // Build all 4 sub-reports
        var report = new YearStatisticsReport
        {
            AcademicYear = academicYear,
            EffortTypes = orderedTypes,
            Svm = BuildSubReport("SVM - All Instructors", allRows, orderedTypes, clinicalMothraIds, includeGroupings: true),
            Dvm = BuildSubReport("DVM/VET Programs", FilterDvm(allRows), orderedTypes, clinicalMothraIds, includeGroupings: true),
            Resident = BuildSubReport("Resident Programs", FilterResident(allRows), orderedTypes, clinicalMothraIds, includeGroupings: false),
            UndergradGrad = BuildSubReport("Undergrad/Grad Programs", FilterUndergradGrad(allRows), orderedTypes, clinicalMothraIds, includeGroupings: false)
        };

        _logger.LogDebug(
            "Year statistics report for {AcademicYear}: SVM={SvmCount}, DVM={DvmCount}, Res={ResCount}, UG={UgCount} instructors",
            academicYear, report.Svm.InstructorCount, report.Dvm.InstructorCount,
            report.Resident.InstructorCount, report.UndergradGrad.InstructorCount);

        return report;
    }

    // ── Sub-Report Filtering ────────────────────────────────────────

    private static List<YearStatsRawRow> FilterDvm(List<YearStatsRawRow> rows)
    {
        return rows.Where(r =>
            r.Course.StartsWith("DVM", StringComparison.OrdinalIgnoreCase)
            || r.Course.StartsWith("VET", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static List<YearStatsRawRow> FilterResident(List<YearStatsRawRow> rows)
    {
        return rows.Where(r =>
        {
            // Course number ends with 'R' — extract the number portion (e.g., "VME 299R-001")
            var courseNum = ExtractCourseNumber(r.Course);
            return courseNum.Length > 0
                && courseNum[^1] == 'R';
        }).ToList();
    }

    private static List<YearStatsRawRow> FilterUndergradGrad(List<YearStatsRawRow> rows)
    {
        var dvmRows = new HashSet<YearStatsRawRow>(FilterDvm(rows));
        var resRows = new HashSet<YearStatsRawRow>(FilterResident(rows));

        return rows.Where(r => !dvmRows.Contains(r) && !resRows.Contains(r))
            .ToList();
    }

    /// <summary>
    /// Extract the course number portion from a formatted course string like "VME 299R-001".
    /// Returns the middle segment (e.g., "299R").
    /// </summary>
    private static string ExtractCourseNumber(string course)
    {
        var parts = course.Trim().Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1].Trim() : string.Empty;
    }

    // ── Sub-Report Building ─────────────────────────────────────────

    private static YearStatsSubReport BuildSubReport(
        string label,
        List<YearStatsRawRow> rows,
        List<string> effortTypes,
        HashSet<string> clinicalMothraIds,
        bool includeGroupings)
    {
        if (rows.Count == 0)
        {
            return new YearStatsSubReport { Label = label };
        }

        // Aggregate per instructor
        var instructors = AggregateInstructors(rows, effortTypes);

        // Sort by JobGroup then Instructor name
        instructors = instructors
            .OrderBy(i => i.JobGroup)
            .ThenBy(i => i.Instructor)
            .ToList();

        var instructorCount = instructors.Count;
        var cliAssignedCount = instructors.Count(i => clinicalMothraIds.Contains(i.MothraId));

        // Calculate summary statistics
        var sums = CalculateSums(instructors, effortTypes);
        var averages = CalculateSubReportAverages(sums, effortTypes, instructorCount, cliAssignedCount);
        var medians = CalculateMedians(instructors, effortTypes);
        var teachingHoursSum = instructors.Sum(i => i.TeachingHours);
        var teachingHoursAvg = instructorCount > 0 ? Math.Round(teachingHoursSum / instructorCount, 1) : 0;
        var teachingHoursMedian = CalculateMedianValue(instructors.Select(i => i.TeachingHours).ToList());

        var subReport = new YearStatsSubReport
        {
            Label = label,
            Instructors = instructors,
            Sums = sums,
            Averages = averages,
            Medians = medians,
            TeachingHoursSum = teachingHoursSum,
            TeachingHoursAverage = teachingHoursAvg,
            TeachingHoursMedian = teachingHoursMedian,
            InstructorCount = instructorCount
        };

        if (includeGroupings)
        {
            subReport.ByDepartment = BuildGroupings(instructors, effortTypes, clinicalMothraIds, i => i.Department);
            subReport.ByDiscipline = BuildGroupings(instructors, effortTypes, clinicalMothraIds, i => i.Discipline);
            subReport.ByTitle = BuildGroupings(instructors, effortTypes, clinicalMothraIds, i => i.JobGroup);
        }

        return subReport;
    }

    /// <summary>
    /// Aggregate raw course-level rows into per-instructor totals.
    /// </summary>
    private static List<InstructorEffortDetail> AggregateInstructors(
        List<YearStatsRawRow> rows, List<string> effortTypes)
    {
        return rows
            .GroupBy(r => r.MothraId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var efforts = new Dictionary<string, decimal>();

                foreach (var et in effortTypes)
                {
                    var total = g.Sum(r => r.EffortByType.GetValueOrDefault(et));
                    if (total != 0)
                    {
                        efforts[et] = total;
                    }
                }

                // Teaching Hours = sum of all effort EXCEPT CLI and VAR
                var teachingHours = efforts
                    .Where(kvp => !YearStatsConstants.NonTeachingEffortTypes.Contains(kvp.Key))
                    .Sum(kvp => kvp.Value);

                // Apply overrides
                var department = YearStatsConstants.ResolveDepartment(first.MothraId, first.Department);
                var discipline = YearStatsConstants.ResolveDiscipline(first.MothraId, first.AcademicDepartment);
                var jobGroup = YearStatsConstants.ResolveTitle(first.MothraId, first.JobGroupDescription);

                return new InstructorEffortDetail
                {
                    MothraId = first.MothraId,
                    Instructor = first.Instructor,
                    Department = department,
                    Discipline = discipline,
                    JobGroup = jobGroup,
                    Efforts = efforts,
                    TeachingHours = teachingHours
                };
            })
            .ToList();
    }

    // ── Statistics Helpers ───────────────────────────────────────────

    private static Dictionary<string, decimal> CalculateSums(
        List<InstructorEffortDetail> instructors, List<string> effortTypes)
    {
        var sums = new Dictionary<string, decimal>();
        foreach (var et in effortTypes)
        {
            var total = instructors.Sum(i => i.Efforts.GetValueOrDefault(et));
            if (total != 0)
            {
                sums[et] = total;
            }
        }
        return sums;
    }

    private static Dictionary<string, decimal> CalculateSubReportAverages(
        Dictionary<string, decimal> sums, List<string> effortTypes,
        int instructorCount, int cliAssignedCount)
    {
        var averages = new Dictionary<string, decimal>();
        foreach (var et in effortTypes)
        {
            var total = sums.GetValueOrDefault(et);
            if (total == 0) continue;

            // CLI uses CLI-assigned count per AD-6
            var divisor = string.Equals(et, "CLI", StringComparison.OrdinalIgnoreCase)
                ? cliAssignedCount
                : instructorCount;

            if (divisor > 0)
            {
                averages[et] = Math.Round(total / divisor, 1);
            }
        }
        return averages;
    }

    private static Dictionary<string, decimal> CalculateMedians(
        List<InstructorEffortDetail> instructors, List<string> effortTypes)
    {
        var medians = new Dictionary<string, decimal>();
        foreach (var et in effortTypes)
        {
            var values = instructors
                .Select(i => i.Efforts.GetValueOrDefault(et))
                .ToList();

            var median = CalculateMedianValue(values);
            if (median != 0)
            {
                medians[et] = median;
            }
        }
        return medians;
    }

    /// <summary>
    /// Standard statistical median: for odd count, middle value;
    /// for even count, average of two middle values.
    /// </summary>
    private static decimal CalculateMedianValue(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            return Math.Round((sorted[mid - 1] + sorted[mid]) / 2m, 1);
        }

        return Math.Round(sorted[mid], 1);
    }

    // ── Grouping Tables ─────────────────────────────────────────────

    private static List<YearStatsGrouping> BuildGroupings(
        List<InstructorEffortDetail> instructors,
        List<string> effortTypes,
        HashSet<string> clinicalMothraIds,
        Func<InstructorEffortDetail, string> groupSelector)
    {
        return instructors
            .GroupBy(i => groupSelector(i), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var groupInstructors = g.ToList();
                var count = groupInstructors.Count;
                var cliCount = groupInstructors.Count(i => clinicalMothraIds.Contains(i.MothraId));

                var sums = CalculateSums(groupInstructors, effortTypes);
                var averages = CalculateSubReportAverages(sums, effortTypes, count, cliCount);
                var medians = CalculateMedians(groupInstructors, effortTypes);
                var thSum = groupInstructors.Sum(i => i.TeachingHours);
                var thAvg = count > 0 ? Math.Round(thSum / count, 1) : 0;
                var thMedian = CalculateMedianValue(groupInstructors.Select(i => i.TeachingHours).ToList());

                return new YearStatsGrouping
                {
                    GroupName = g.Key,
                    InstructorCount = count,
                    Sums = sums,
                    Averages = averages,
                    Medians = medians,
                    TeachingHoursSum = thSum,
                    TeachingHoursAverage = thAvg,
                    TeachingHoursMedian = thMedian
                };
            })
            .ToList();
    }

    // ── Effort Type Discovery ───────────────────────────────────────

    private static List<string> DiscoverEffortTypes(List<YearStatsRawRow> rows)
    {
        return rows
            .SelectMany(r => r.EffortByType.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();
    }

    // ── SP Execution ────────────────────────────────────────────────

    /// <summary>
    /// Raw row from sp_merit_summary_report (the "Lairmore Report" SP).
    /// </summary>
    private sealed class YearStatsRawRow
    {
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AcademicDepartment { get; set; } = string.Empty;
        public string JobGroupDescription { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string Course { get; set; } = string.Empty;
        public string Units { get; set; } = string.Empty;
        public int Enrollment { get; set; }
        public string Role { get; set; } = string.Empty;
        public Dictionary<string, decimal> EffortByType { get; set; } = new();
    }

    /// <summary>
    /// All effort type columns in the SP result set, in order.
    /// These are fixed pivot columns defined in sp_merit_summary_report.
    /// </summary>
    private static readonly string[] SpEffortColumns =
    [
        "CLI", "DIS", "EXM", "LAB", "LEC", "LED", "SEM", "VAR",
        "AUT", "FWK", "INT", "LAD", "PRJ", "TUT", "FAS", "PBL",
        "JLC", "ACT", "CBL", "PRS", "TBL", "PRB", "T-D", "WVL",
        "CON", "DAL", "DSL", "IND", "LIS", "LLA", "TMP", "WED", "WRK"
    ];

    private async Task<List<YearStatsRawRow>> ExecuteYearStatisticsSpAsync(
        int termCode, CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<YearStatsRawRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_summary_report]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        // No @Department param — always get full school for year stats

        await using var reader = await command.ExecuteReaderAsync(ct);

        // Fixed columns: MothraId(0), Instructor(1), Department(2), AcademicDepartment(3),
        // JobGroupDescription(4), CourseId(5), Course(6), Units(7), Enrollment(8), Role(9),
        // then 33 effort type columns starting at index 10
        while (await reader.ReadAsync(ct))
        {
            var row = new YearStatsRawRow
            {
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                Department = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                AcademicDepartment = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                JobGroupDescription = await reader.IsDBNullAsync(4, ct) ? "" : reader.GetString(4),
                CourseId = reader.GetInt32(5),
                Course = reader.GetString(6),
                Units = await reader.IsDBNullAsync(7, ct) ? "" : reader.GetString(7),
                Enrollment = await reader.IsDBNullAsync(8, ct) ? 0 : reader.GetInt32(8),
                Role = await reader.IsDBNullAsync(9, ct) ? "" : reader.GetString(9),
                EffortByType = new Dictionary<string, decimal>()
            };

            // Read all 33 effort type columns
            for (int i = 0; i < SpEffortColumns.Length; i++)
            {
                var colIndex = 10 + i;
                if (!await reader.IsDBNullAsync(colIndex, ct))
                {
                    var value = reader.GetInt32(colIndex);
                    if (value != 0)
                    {
                        row.EffortByType[SpEffortColumns[i]] = value;
                    }
                }
            }

            results.Add(row);
        }

        _logger.LogDebug(
            "Year statistics SP for term {TermCode}: {RowCount} rows returned",
            termCode, results.Count);

        return results;
    }

    // ============================================
    // PDF Generation
    // ============================================

    public Task<byte[]> GenerateReportPdfAsync(YearStatisticsReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);
        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 7f : 9f;
        var headerFontSize = compact ? 6.5f : 8f;
        var hMargin = compact ? 0.3f : 0.4f;
        var cellPadV = compact ? 1f : 2f;
        var effortWidth = compact ? 22f : 28f;

        var document = Document.Create(container =>
        {
            // Generate pages for each sub-report that has data
            var subReports = new[]
            {
                report.Svm,
                report.Dvm,
                report.Resident,
                report.UndergradGrad
            };

            foreach (var sub in subReports)
            {
                if (sub.InstructorCount == 0) continue;

                container.Page(page =>
                {
                    page.Size(PageSizes.Legal.Landscape());
                    page.MarginHorizontal(hMargin, Unit.Inch);
                    page.MarginVertical(0.25f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(fontSize));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                        });
                        col.Item().PaddingVertical(4).Row(row =>
                        {
                            row.RelativeItem().Text("Year Statistics Report").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text(sub.Label).SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text($"Academic Year {report.AcademicYear}").SemiBold().FontSize(12);
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // Instructor
                            columns.ConstantColumn(45); // Department
                            columns.ConstantColumn(60); // Job Group
                            foreach (var _ in orderedTypes)
                            {
                                columns.ConstantColumn(effortWidth);
                            }
                            columns.ConstantColumn(35); // Teaching Hours
                        });

                        var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                        table.Header(header =>
                        {
                            header.Cell().PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Dept").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Title").Style(hdrStyle);
                            foreach (var type in orderedTypes)
                            {
                                header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                            }
                            header.Cell().PaddingVertical(cellPadV).Text("Tch Hrs").Style(hdrStyle);
                        });

                        // Instructor rows
                        foreach (var instructor in sub.Instructors)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(instructor.Instructor);
                            table.Cell().PaddingVertical(cellPadV).Text(instructor.Department);
                            table.Cell().PaddingVertical(cellPadV).Text(instructor.JobGroup);
                            foreach (var type in orderedTypes)
                            {
                                var val = instructor.Efforts.GetValueOrDefault(type);
                                table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "");
                            }
                            table.Cell().PaddingVertical(cellPadV).Text(
                                instructor.TeachingHours > 0 ? instructor.TeachingHours.ToString() : "");
                        }

                        // Summary rows
                        AddSummaryRow(table, "Sum:", orderedTypes, sub.Sums, sub.TeachingHoursSum, cellPadV, "#E8E8E8");
                        AddSummaryRow(table, $"Average (n={sub.InstructorCount}):", orderedTypes, sub.Averages, sub.TeachingHoursAverage, cellPadV, "#E0E0E0");
                        AddSummaryRow(table, "Median:", orderedTypes, sub.Medians, sub.TeachingHoursMedian, cellPadV, "#E8E8E8");
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
            }
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static void AddSummaryRow(
        TableDescriptor table, string label, List<string> effortTypes,
        Dictionary<string, decimal> values, decimal teachingHoursValue,
        float cellPadV, string bgColor)
    {
        table.Cell().Background(bgColor).BorderTop(1f).BorderColor("#999999")
            .PaddingVertical(cellPadV).Text(label).Bold();
        table.Cell().Background(bgColor).BorderTop(1f).BorderColor("#999999").PaddingVertical(cellPadV);
        table.Cell().Background(bgColor).BorderTop(1f).BorderColor("#999999").PaddingVertical(cellPadV);

        foreach (var type in effortTypes)
        {
            var val = values.GetValueOrDefault(type);
            table.Cell().Background(bgColor).BorderTop(1f).BorderColor("#999999")
                .PaddingVertical(cellPadV).Text(val != 0 ? val.ToString("F1") : "");
        }

        table.Cell().Background(bgColor).BorderTop(1f).BorderColor("#999999")
            .PaddingVertical(cellPadV).Text(teachingHoursValue != 0 ? teachingHoursValue.ToString("F1") : "").Bold();
    }
}
