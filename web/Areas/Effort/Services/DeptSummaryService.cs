using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class DeptSummaryService : BaseReportService, IDeptSummaryService
{
    public DeptSummaryService(
        EffortDbContext context,
        ITermService termService)
        : base(context, termService)
    {
    }

    public async Task<DeptSummaryReport> GetDeptSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var ctx = await LoadSingleTermContextAsync(termCode, departments, personId, role, jobGroupId, ct);

        var report = new DeptSummaryReport
        {
            TermCode = termCode,
            TermName = ctx.TermName,
            FilterDepartment = ctx.FilterDepartment,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, ctx.Rows, ctx.ClinicalMothraIds);
    }

    public async Task<DeptSummaryReport> GetDeptSummaryReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var ctx = await LoadYearlyReportContextAsync(academicYear, departments, personId, role, jobGroupId, ct);

        if (ctx.TermCodes.Count == 0)
        {
            return new DeptSummaryReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = ctx.FilterDepartment,
                FilterPerson = personId?.ToString(),
                FilterRole = role,
                FilterTitle = jobGroupId
            };
        }

        var report = new DeptSummaryReport
        {
            TermCode = ctx.TermCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = ctx.FilterDepartment,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, ctx.Rows, ctx.ClinicalMothraIds);
    }

    private static DeptSummaryReport BuildReport(
        DeptSummaryReport report, List<TeachingActivityRow> rows, HashSet<string> clinicalMothraIds)
    {
        if (rows.Count == 0)
        {
            return report;
        }

        var effortTypes = ExtractDistinctEffortTypes(rows);
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
        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        const string reportTitle = "Department Summary Report";
        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? ReportPdfSettings.FontSizeCompact : ReportPdfSettings.FontSize;
        var headerFontSize = compact ? ReportPdfSettings.HeaderFontSizeCompact : ReportPdfSettings.HeaderFontSize;
        var hMargin = compact ? ReportPdfSettings.HMarginCompact : ReportPdfSettings.HMargin;
        var cellPadV = compact ? ReportPdfSettings.CellPadVCompact : ReportPdfSettings.CellPadV;
        var instructorWidth = compact ? 110f : 150f;
        var effortWidth = compact ? ReportPdfSettings.EffortWidthCompact : ReportPdfSettings.EffortWidth;
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
                        col.Item().SemanticIgnore().Row(row =>
                        {
                            row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                        });
                        col.Item().PaddingVertical(6).Row(row =>
                        {
                            row.RelativeItem().SemanticHeader1().Text(reportTitle).SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().SemanticHeader2().Text(dept.Department).SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                        });
                    });

                    page.Content().SemanticTable().Table(table =>
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
                        table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
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

                        // Number Faculty row (single label/value spanning the full row).
                        // Trailing ConstantItem absorbs the N effort columns' widths so the
                        // leading RelativeItem only fills column 1, keeping the right-aligned
                        // label visually anchored to column 1's right edge (matching the
                        // "Faculty w/ CLI assigned:" row below).
                        var numFacAbsorbedWidth = orderedTypes.Sum(t => SpacerColumns.Contains(t) ? spacerWidth : effortWidth);
                        table.Cell().ColumnSpan((uint)(orderedTypes.Count + 1))
                            .PaddingVertical(cellPadV).PaddingRight(8).Row(row =>
                        {
                            row.RelativeItem().AlignRight().Text("Number Faculty:").SemiBold();
                            row.ConstantItem(30).AlignRight().Text(dept.FacultyCount.ToString()).SemiBold();
                            row.ConstantItem(55);
                            row.ConstantItem(numFacAbsorbedWidth);
                        });

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

                    AddPdfPageNumberFooter(page.Footer(),
                        ("Dept", report.FilterDepartment), ("Role", report.FilterRole),
                        ("Faculty", report.FilterPerson), ("Title", report.FilterTitle));
                });
            }
        })
        .WithAccessibility(reportTitle, subject: $"Department effort summary for {report.TermName}");

        return Task.FromResult(document.GeneratePdf());
    }

    public MemoryStream GenerateReportExcel(DeptSummaryReport report)
    {
        var wb = new XLWorkbook();
        const string reportTitle = "Department Summary Report";
        var termName = report.AcademicYear ?? report.TermName;
        ExcelAccessibilityHelper.SetCoreProperties(wb, reportTitle,
            subject: $"Department effort summary for {termName}");

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);
        var effortCols = BuildExcelEffortColumns(orderedTypes);
        var lastCol = 1 + effortCols.Count;

        foreach (var dept in report.Departments)
        {
            var ws = wb.Worksheets.Add(dept.Department);

            // Header rows matching PDF
            int row = AddExcelHeader(ws, reportTitle, termName, dept.Department);
            row = AddExcelFilterLine(ws, row,
                ("Dept", report.FilterDepartment), ("Role", report.FilterRole),
                ("Faculty", report.FilterPerson), ("Title", report.FilterTitle));
            row++; // blank separator

            // Column headers
            int headerRow = row;
            int col = 1;
            ws.Cell(row, col++).Value = "Instructor";
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = type;
                col++;
            }
            ws.Range($"{row}:{row}").Style.Font.Bold = true;
            ws.SheetView.FreezeRows(row);
            row++;

            foreach (var instructor in dept.Instructors)
            {
                col = 1;
                ws.Cell(row, col++).Value = ExcelHelper.SanitizeStringCell(instructor.Instructor);
                foreach (var (type, isSpacer) in effortCols)
                {
                    if (!isSpacer) ws.Cell(row, col).Value = instructor.EffortByType.GetValueOrDefault(type, 0);
                    col++;
                }
                row++;
            }

            // Promote the instructor rows (header + data) to a structured table so
            // screen readers announce each instructor's effort columns by name.
            // Summary rows (totals/faculty counts) intentionally sit outside.
            if (row > headerRow + 1)
            {
                ExcelAccessibilityHelper.PromoteToAccessibleTable(
                    ws.Range(headerRow, 1, row - 1, lastCol),
                    $"DeptSummary_{dept.Department}");
            }

            // Re-display effort type headers before totals
            col = 1;
            ws.Cell(row, col++).Value = "";
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = type;
                col++;
            }
            ws.Range($"{row}:{row}").Style.Font.Bold = true;
            row++;

            // Department Totals
            col = 1;
            ws.Cell(row, col++).Value = "Department Totals:";
            ws.Cell(row, col - 1).Style.Font.Bold = true;
            ws.Cell(row, col - 1).Style.Font.Italic = true;
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = dept.DepartmentTotals.GetValueOrDefault(type, 0);
                col++;
            }
            ws.Range(row, 1, row, lastCol).Style.Font.Bold = true;
            ShadeExcelRow(ws, row, lastCol, "#E8E8E8");
            row++;

            // Number Faculty row
            ws.Cell(row, 1).Value = "Number Faculty:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = dept.FacultyCount;
            ws.Cell(row, 2).Style.Font.Bold = true;
            row++;

            // Faculty w/ CLI assigned + averages row
            ws.Cell(row, 1).Value = "Faculty w/ CLI assigned:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.Italic = true;
            ws.Cell(row, 2).Value = dept.FacultyWithCliCount;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.Font.Italic = true;
            ws.Cell(row, 3).Value = "Average";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Font.Italic = true;
            col = 2;
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer)
                {
                    var val = dept.DepartmentAverages.GetValueOrDefault(type, 0);
                    ws.Cell(row, col).Value = val;
                    ws.Cell(row, col).Style.NumberFormat.Format = "0.0";
                    ws.Cell(row, col).Style.Font.Bold = true;
                }
                col++;
            }
            ShadeExcelRow(ws, row, lastCol, "#E0E0E0");

            ws.Columns().AdjustToContents();
            ApplyExcelSpacerWidths(ws, 2, effortCols);
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        wb.Dispose();
        stream.Position = 0;
        return stream;
    }
}
