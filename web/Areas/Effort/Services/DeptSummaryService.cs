using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class DeptSummaryService : BaseReportService, IDeptSummaryService
{
    private readonly ITermService _termService;

    public DeptSummaryService(
        EffortDbContext context,
        ITermService termService)
        : base(context)
    {
        _termService = termService;
    }

    public async Task<DeptSummaryReport> GetDeptSummaryReportAsync(
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

        var report = new DeptSummaryReport
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

    public async Task<DeptSummaryReport> GetDeptSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        if (termCodes.Count == 0)
        {
            return new DeptSummaryReport
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

        var report = new DeptSummaryReport
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

    private static DeptSummaryReport BuildReport(
        DeptSummaryReport report, List<TeachingActivityRow> rows, HashSet<string> clinicalMothraIds)
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

        report.Departments = rows
            .GroupBy(r => r.Department.Trim())
            .OrderBy(g => g.Key)
            .Select(deptGroup => BuildDepartmentGroup(deptGroup.Key, deptGroup.ToList(), effortTypes, clinicalMothraIds))
            .ToList();

        return report;
    }

    private static DeptSummaryDepartmentGroup BuildDepartmentGroup(
        string department, List<TeachingActivityRow> rows, List<string> effortTypes,
        HashSet<string> clinicalMothraIds)
    {
        // Aggregate per instructor: sum effort by type across all courses
        var instructorRows = rows
            .GroupBy(r => r.MothraId)
            .OrderBy(g => g.First().Instructor)
            .Select(instGroup =>
            {
                var first = instGroup.First();
                var effortByType = new Dictionary<string, decimal>();

                foreach (var row in instGroup)
                {
                    var typeId = row.EffortTypeId.Trim();
                    if (!string.IsNullOrWhiteSpace(typeId))
                    {
                        effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.EffortValue;
                    }
                }

                return new DeptSummaryInstructorRow
                {
                    MothraId = first.MothraId.Trim(),
                    Instructor = first.Instructor.Trim(),
                    JobGroupId = first.JobGroupId.Trim(),
                    EffortByType = effortByType
                };
            })
            .ToList();

        var facultyCount = instructorRows.Count;
        var facultyWithCliCount = instructorRows.Count(i => clinicalMothraIds.Contains(i.MothraId));

        // Department totals
        var deptTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = instructorRows.Sum(i => i.EffortByType.GetValueOrDefault(effortType));
            if (total != 0)
            {
                deptTotals[effortType] = total;
            }
        }

        return new DeptSummaryDepartmentGroup
        {
            Department = department,
            Instructors = instructorRows,
            DepartmentTotals = deptTotals,
            FacultyCount = facultyCount,
            FacultyWithCliCount = facultyWithCliCount,
            DepartmentAverages = CalculateAverages(deptTotals, effortTypes, facultyCount, facultyWithCliCount)
        };
    }

    public Task<byte[]> GenerateReportPdfAsync(DeptSummaryReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8.5f : 10f;
        var headerFontSize = compact ? 7.5f : 8.5f;
        var hMargin = compact ? 0.35f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var instructorWidth = compact ? 110f : 150f;
        var effortWidth = compact ? 24f : 32f;
        var spacerWidth = compact ? 42f : 70f;

        var document = Document.Create(container =>
        {
            foreach (var dept in report.Departments)
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
                            row.RelativeItem().Text("Department Summary Report").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text(dept.Department).SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(instructorWidth);
                            foreach (var type in orderedTypes)
                            {
                                columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                            }
                        });

                        var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                        table.Header(header =>
                        {
                            header.Cell().PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
                            foreach (var type in orderedTypes)
                            {
                                header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                            }
                        });

                        foreach (var instructor in dept.Instructors)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(instructor.Instructor).SemiBold();
                            foreach (var type in orderedTypes)
                            {
                                var val = instructor.EffortByType.GetValueOrDefault(type, 0);
                                table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                            }
                        }

                        // Re-display effort type headers before totals
                        table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text("");
                        foreach (var type in orderedTypes)
                        {
                            table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                        }

                        // Department totals row
                        table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                            .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                            .Text("Department Totals:").Italic().Bold();
                        foreach (var type in orderedTypes)
                        {
                            var val = dept.DepartmentTotals.GetValueOrDefault(type, 0);
                            table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                        }

                        // Number Faculty row
                        var totalColumns = (uint)(1 + orderedTypes.Count);
                        table.Cell().ColumnSpan(totalColumns).PaddingVertical(cellPadV)
                            .Text($"Number Faculty: {dept.FacultyCount}");

                        // Faculty w/ CLI assigned + averages row
                        table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                            .Text($"Faculty w/ CLI assigned: {dept.FacultyWithCliCount}    Average").Italic().Bold();
                        foreach (var type in orderedTypes)
                        {
                            var val = dept.DepartmentAverages.GetValueOrDefault(type, 0);
                            table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                                .Text(val.ToString("F1")).Bold();
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
            }
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
