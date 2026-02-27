using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class SchoolSummaryService : BaseReportService, ISchoolSummaryService
{
    private readonly ITermService _termService;

    public SchoolSummaryService(
        EffortDbContext context,
        ITermService termService)
        : base(context)
    {
        _termService = termService;
    }

    public async Task<SchoolSummaryReport> GetSchoolSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteGeneralReportForDepartmentsAsync(termCode, departments, personId, role, jobGroupId, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        var academicYear = AcademicYearHelper.GetAcademicYearFromTermCode(termCode);
        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new SchoolSummaryReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, rows, clinicalMothraIds);
    }

    public async Task<SchoolSummaryReport> GetSchoolSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);

        if (termCodes.Count == 0)
        {
            return new SchoolSummaryReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept,
                FilterPerson = personId?.ToString(),
                FilterRole = role,
                FilterTitle = jobGroupId
            };
        }

        var allRows = new List<TeachingActivityRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteGeneralReportForDepartmentsAsync(tc, departments, personId, role, jobGroupId, ct);
            allRows.AddRange(rows);
        }

        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new SchoolSummaryReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, allRows, clinicalMothraIds);
    }

    private static SchoolSummaryReport BuildReport(
        SchoolSummaryReport report, List<TeachingActivityRow> rows, HashSet<string> clinicalMothraIds)
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

        // Group by department to build per-department aggregates
        var deptGroups = rows
            .GroupBy(r => r.Department.Trim())
            .OrderBy(g => g.Key);

        var departmentRows = new List<SchoolSummaryDepartmentRow>();

        foreach (var deptGroup in deptGroups)
        {
            // Group by instructor to count faculty and compute effort totals
            var instructorGroups = deptGroup.GroupBy(r => r.MothraId).ToList();
            var facultyCount = instructorGroups.Count;
            var facultyWithCliCount = instructorGroups.Count(g => clinicalMothraIds.Contains(g.Key));

            var instructors = instructorGroups
                .Select(instGroup =>
                {
                    var effortByType = new Dictionary<string, decimal>();
                    foreach (var row in instGroup)
                    {
                        var typeId = row.EffortTypeId.Trim();
                        if (!string.IsNullOrWhiteSpace(typeId))
                        {
                            effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.EffortValue;
                        }
                    }
                    return effortByType;
                })
                .ToList();

            var effortTotals = new Dictionary<string, decimal>();
            foreach (var effortType in effortTypes)
            {
                var total = instructors.Sum(i => i.GetValueOrDefault(effortType));
                if (total != 0)
                {
                    effortTotals[effortType] = total;
                }
            }

            departmentRows.Add(new SchoolSummaryDepartmentRow
            {
                Department = deptGroup.Key,
                EffortTotals = effortTotals,
                FacultyCount = facultyCount,
                FacultyWithCliCount = facultyWithCliCount,
                Averages = CalculateAverages(effortTotals, effortTypes, facultyCount, facultyWithCliCount)
            });
        }

        report.Departments = departmentRows;

        // Grand totals across all departments
        var grandFacultyCount = departmentRows.Sum(d => d.FacultyCount);
        var grandFacultyWithCliCount = departmentRows.Sum(d => d.FacultyWithCliCount);
        var grandEffortTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = departmentRows.Sum(d => d.EffortTotals.GetValueOrDefault(effortType));
            if (total != 0)
            {
                grandEffortTotals[effortType] = total;
            }
        }

        report.GrandTotals = new SchoolSummaryTotalsRow
        {
            EffortTotals = grandEffortTotals,
            FacultyCount = grandFacultyCount,
            FacultyWithCliCount = grandFacultyWithCliCount,
            Averages = CalculateAverages(grandEffortTotals, effortTypes, grandFacultyCount, grandFacultyWithCliCount)
        };

        return report;
    }

    public Task<byte[]> GenerateReportPdfAsync(SchoolSummaryReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8.5f : 10f;
        var headerFontSize = compact ? 7.5f : 8.5f;
        var hMargin = compact ? 0.35f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var deptWidth = compact ? 60f : 80f;
        var effortWidth = compact ? 24f : 32f;
        var spacerWidth = compact ? 42f : 70f;
        var facultyLabelWidth = compact ? 90f : 115f;
        var facultyCountWidth = compact ? 20f : 25f;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
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
                        row.RelativeItem().Text("School Summary Report").SemiBold().FontSize(12);
                        row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                    });
                });

                page.Content().Table(table =>
                {
                    var totalColumns = (uint)(1 + orderedTypes.Count);

                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(deptWidth);
                        foreach (var type in orderedTypes)
                        {
                            columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                        }
                    });

                    var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                    table.Header(header =>
                    {
                        header.Cell().PaddingVertical(cellPadV).Text("Department").Style(hdrStyle);
                        foreach (var type in orderedTypes)
                        {
                            header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                        }
                    });

                    for (int deptIdx = 0; deptIdx < report.Departments.Count; deptIdx++)
                    {
                        var dept = report.Departments[deptIdx];

                        // Department totals row
                        table.Cell().PaddingVertical(cellPadV).Text(dept.Department).SemiBold();
                        foreach (var type in orderedTypes)
                        {
                            var val = dept.EffortTotals.GetValueOrDefault(type, 0);
                            table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                        }

                        // # Faculty row
                        table.Cell().PaddingVertical(cellPadV).Row(row =>
                        {
                            row.ConstantItem(facultyLabelWidth).Text("# Faculty").SemiBold();
                            row.AutoItem().Text(dept.FacultyCount.ToString()).SemiBold();
                        });
                        foreach (var _ in orderedTypes)
                            table.Cell().PaddingVertical(cellPadV).Text("");

                        // # Faculty with CLI + averages row
                        table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV).Row(row =>
                        {
                            row.ConstantItem(facultyLabelWidth).Text("# Faculty with CLI").SemiBold().Italic();
                            row.ConstantItem(facultyCountWidth).Text(dept.FacultyWithCliCount.ToString()).SemiBold().Italic();
                            row.AutoItem().PaddingLeft(10).Text("Average").SemiBold().Italic();
                        });
                        foreach (var type in orderedTypes)
                        {
                            var val = dept.Averages.GetValueOrDefault(type, 0);
                            table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                                .Text(val.ToString("F1")).Italic();
                        }

                        // Divider between departments
                        if (deptIdx < report.Departments.Count - 1)
                        {
                            table.Cell().ColumnSpan(totalColumns).PaddingVertical(4)
                                .BorderBottom(0.5f).BorderColor("#CCCCCC").Text("");
                        }
                    }

                    // Re-display effort type headers before grand totals
                    table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text("");
                    foreach (var type in orderedTypes)
                    {
                        table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV)
                            .Text(type).Style(hdrStyle);
                    }

                    // Grand totals
                    table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                        .PaddingVertical(cellPadV).Text("Grand Total").Bold();
                    foreach (var type in orderedTypes)
                    {
                        var val = report.GrandTotals.EffortTotals.GetValueOrDefault(type, 0);
                        table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                            .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                    }

                    // # Faculty row (grand)
                    table.Cell().PaddingVertical(cellPadV).Row(row =>
                    {
                        row.ConstantItem(facultyLabelWidth).Text("# Faculty").Bold();
                        row.AutoItem().Text(report.GrandTotals.FacultyCount.ToString()).Bold();
                    });
                    foreach (var _ in orderedTypes)
                        table.Cell().PaddingVertical(cellPadV).Text("");

                    // # Faculty with CLI + grand averages row
                    table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV).Row(row =>
                    {
                        row.ConstantItem(facultyLabelWidth).Text("# Faculty with CLI").Bold().Italic();
                        row.ConstantItem(facultyCountWidth).Text(report.GrandTotals.FacultyWithCliCount.ToString()).Bold().Italic();
                        row.AutoItem().PaddingLeft(10).Text("Average").Bold().Italic();
                    });
                    foreach (var type in orderedTypes)
                    {
                        var val = report.GrandTotals.Averages.GetValueOrDefault(type, 0);
                        table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                            .Text(val.ToString("F1")).Bold().Italic();
                    }
                });

                page.Footer().Column(col =>
                {
                    AddPdfFilterLine(col.Item(),
                        ("Dept", report.FilterDepartment), ("Role", report.FilterRole),
                        ("Faculty", report.FilterPerson), ("Title", report.FilterTitle));
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
}
