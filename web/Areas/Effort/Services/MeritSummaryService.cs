using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class MeritSummaryService : BaseReportService, IMeritSummaryService
{
    private readonly ITermService _termService;
    private readonly ILogger<MeritSummaryService> _logger;

    public MeritSummaryService(
        EffortDbContext context,
        ITermService termService,
        ILogger<MeritSummaryService> logger)
        : base(context)
    {
        _termService = termService;
        _logger = logger;
    }

    // ============================================
    // Merit Summary Report
    // ============================================

    public async Task<MeritSummaryReport> GetMeritSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteMeritSummaryForDepartmentsAsync(termCode, departments, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;
        var academicYear = AcademicYearHelper.GetAcademicYearFromTermCode(termCode);
        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new MeritSummaryReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept
        };

        return BuildMeritSummaryReport(report, rows, clinicalMothraIds);
    }

    public async Task<MeritSummaryReport> GetMeritSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        if (termCodes.Count == 0)
        {
            return new MeritSummaryReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept
            };
        }

        // sp_merit_summary only accepts a single term code, so call for each term and merge
        var allRows = new List<MeritSummaryRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteMeritSummaryForDepartmentsAsync(tc, departments, ct);
            allRows.AddRange(rows);
        }

        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new MeritSummaryReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept
        };

        return BuildMeritSummaryReport(report, allRows, clinicalMothraIds);
    }

    /// <summary>
    /// Execute the merit summary SP for each department in the list, once with null for all departments,
    /// or return empty when the list is explicitly empty (unauthorized request).
    /// </summary>
    private async Task<List<MeritSummaryRow>> ExecuteMeritSummaryForDepartmentsAsync(
        int termCode, IReadOnlyList<string>? departments, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteMeritSummarySpAsync(termCode, null, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteMeritSummarySpAsync(termCode, departments[0], ct);
        }

        var allRows = new List<MeritSummaryRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteMeritSummarySpAsync(termCode, dept, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    /// <summary>
    /// Raw row from sp_merit_summary.
    /// </summary>
    private sealed class MeritSummaryRow
    {
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobGroupDescription { get; set; } = string.Empty;
        public decimal PercentAdmin { get; set; }
        public string EffortTypeId { get; set; } = string.Empty;
        public decimal TotalEffort { get; set; }
    }

    private static MeritSummaryReport BuildMeritSummaryReport(
        MeritSummaryReport report, List<MeritSummaryRow> rows, HashSet<string> clinicalMothraIds)
    {
        if (rows.Count == 0)
        {
            return report;
        }

        var effortTypes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EffortTypeId))
            .Select(r => r.EffortTypeId.Trim())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        report.EffortTypes = effortTypes;

        // Group by JobGroupDescription -> Department
        report.JobGroups = rows
            .GroupBy(r => r.JobGroupDescription.Trim())
            .OrderBy(g => g.Key)
            .Select(jgGroup =>
            {
                var deptGroups = jgGroup
                    .GroupBy(r => r.Department.Trim())
                    .OrderBy(g => g.Key)
                    .Select(deptGroup => BuildMeritSummaryDeptGroup(deptGroup.Key, deptGroup.ToList(), effortTypes, clinicalMothraIds))
                    .ToList();

                return new MeritSummaryJobGroup
                {
                    JobGroupDescription = jgGroup.Key,
                    Departments = deptGroups
                };
            })
            .ToList();

        return report;
    }

    private static MeritSummaryDepartmentGroup BuildMeritSummaryDeptGroup(
        string department, List<MeritSummaryRow> rows, List<string> effortTypes, HashSet<string> clinicalMothraIds)
    {
        var distinctInstructors = rows
            .Select(r => r.MothraId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var facultyCount = distinctInstructors.Count;
        var facultyWithCliCount = distinctInstructors.Count(m => clinicalMothraIds.Contains(m));

        // Sum effort by type across all instructors
        var deptTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = rows
                .Where(r => string.Equals(r.EffortTypeId.Trim(), effortType, StringComparison.Ordinal))
                .Sum(r => r.TotalEffort);
            if (total != 0)
            {
                deptTotals[effortType] = total;
            }
        }

        return new MeritSummaryDepartmentGroup
        {
            Department = department,
            DepartmentTotals = deptTotals,
            DepartmentAverages = CalculateAverages(deptTotals, effortTypes, facultyCount, facultyWithCliCount),
            FacultyCount = facultyCount,
            FacultyWithCliCount = facultyWithCliCount
        };
    }

    private async Task<List<MeritSummaryRow>> ExecuteMeritSummarySpAsync(
        int termCode,
        string? department,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<MeritSummaryRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_summary]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // SP columns: MothraId, Instructor, Department, JobGroupDescription, PercentAdmin, EffortTypeId, TotalEffort
            results.Add(new MeritSummaryRow
            {
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                Department = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                JobGroupDescription = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                PercentAdmin = await reader.IsDBNullAsync(4, ct) ? 0m : (decimal)reader.GetDouble(4),
                EffortTypeId = await reader.IsDBNullAsync(5, ct) ? "" : reader.GetString(5),
                TotalEffort = await reader.IsDBNullAsync(6, ct) ? 0m : reader.GetInt32(6)
            });
        }

        _logger.LogDebug("Merit summary report for term {TermCode}: {RowCount} rows returned", termCode, results.Count);
        return results;
    }

    // ============================================
    // PDF Generation
    // ============================================

    public Task<byte[]> GenerateReportPdfAsync(MeritSummaryReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8f : 10f;
        var headerFontSize = compact ? 7f : 8.5f;
        var hMargin = compact ? 0.3f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var effortWidth = compact ? 24f : 32f;
        var spacerWidth = compact ? 42f : 70f;

        var document = Document.Create(container =>
        {
            foreach (var jobGroup in report.JobGroups)
            {
                foreach (var dept in jobGroup.Departments)
                {
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
                            col.Item().PaddingVertical(6).Row(row =>
                            {
                                row.RelativeItem().Text("Merit & Promotion Summary Report").SemiBold().FontSize(12);
                                row.RelativeItem().AlignCenter().Text(dept.Department).SemiBold().FontSize(12);
                                row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                            });
                            col.Item().Text(jobGroup.JobGroupDescription).SemiBold().FontSize(11);
                        });

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                foreach (var type in orderedTypes)
                                {
                                    columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                                }
                            });

                            var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                            table.Header(header =>
                            {
                                header.Cell().PaddingVertical(cellPadV).Text("").Style(hdrStyle);
                                foreach (var type in orderedTypes)
                                {
                                    header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                                }
                            });

                            // Totals row
                            table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                                .Text("Totals:").Italic().Bold();
                            foreach (var type in orderedTypes)
                            {
                                var val = dept.DepartmentTotals.GetValueOrDefault(type, 0);
                                table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                    .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                            }

                            // Number Faculty row
                            table.Cell().PaddingVertical(cellPadV).PaddingRight(8).Row(row =>
                            {
                                row.RelativeItem().AlignRight().Text("Number Faculty:").SemiBold();
                                row.ConstantItem(30).AlignRight().Text(dept.FacultyCount.ToString()).SemiBold();
                                row.ConstantItem(55);
                            });
                            foreach (var _ in orderedTypes)
                                table.Cell().PaddingVertical(cellPadV);

                            // Faculty w/ CLI assigned + averages row
                            table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV).PaddingRight(8).Row(row =>
                            {
                                row.RelativeItem().AlignRight().Text("Faculty w/ CLI assigned:").SemiBold().Italic();
                                row.ConstantItem(30).AlignRight().Text(dept.FacultyWithCliCount.ToString()).SemiBold().Italic();
                                row.ConstantItem(55).AlignRight().Text("Average").SemiBold().Italic();
                            });
                            foreach (var type in orderedTypes)
                            {
                                var val = dept.DepartmentAverages.GetValueOrDefault(type, 0);
                                table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                                    .Text(val.ToString("F1")).Bold();
                            }
                        });

                        page.Footer().Column(col =>
                        {
                            AddPdfFilterLine(col.Item(), ("Dept", report.FilterDepartment));
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
            }
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
