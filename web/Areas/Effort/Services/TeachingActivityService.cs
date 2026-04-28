using ClosedXML.Excel;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class TeachingActivityService : BaseReportService, ITeachingActivityService
{
    public TeachingActivityService(
        EffortDbContext context,
        ITermService termService)
        : base(context, termService)
    {
    }

    public async Task<TeachingActivityReport> GetTeachingActivityReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var ctx = await LoadSingleTermContextAsync(termCode, departments, personId, role, jobGroupId, ct);

        var report = new TeachingActivityReport
        {
            TermCode = termCode,
            TermName = ctx.TermName,
            FilterDepartment = ctx.FilterDepartment,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, ctx.Rows);
    }

    public async Task<TeachingActivityReport> GetTeachingActivityReportByYearAsync(
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
            return new TeachingActivityReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = ctx.FilterDepartment,
                FilterPerson = personId?.ToString(),
                FilterRole = role,
                FilterTitle = jobGroupId
            };
        }

        var report = new TeachingActivityReport
        {
            TermCode = ctx.TermCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = ctx.FilterDepartment,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, ctx.Rows);
    }

    private static TeachingActivityReport BuildReport(TeachingActivityReport report, List<TeachingActivityRow> rows)
    {
        if (rows.Count == 0)
        {
            return report;
        }

        // Collect all distinct effort types that appear in the data
        var effortTypes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EffortTypeId))
            .Select(r => r.EffortTypeId.Trim())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        report.EffortTypes = effortTypes;

        // Group rows: Department -> Instructor -> Courses (with effort types pivoted)
        var departmentGroups = rows
            .GroupBy(r => r.Department.Trim())
            .OrderBy(g => g.Key)
            .Select(deptGroup => BuildDepartmentGroup(deptGroup.Key, deptGroup.ToList(), effortTypes))
            .ToList();

        report.Departments = departmentGroups;
        return report;
    }

    private static TeachingActivityDepartmentGroup BuildDepartmentGroup(
        string department, List<TeachingActivityRow> rows, List<string> effortTypes)
    {
        var instructorGroups = rows
            .GroupBy(r => r.MothraId)
            .OrderBy(g => g.First().Instructor)
            .Select(instGroup => BuildInstructorGroup(instGroup.First(), instGroup.ToList(), effortTypes))
            .ToList();

        // Department totals = sum of all instructor totals
        var deptTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = instructorGroups.Sum(i => i.InstructorTotals.GetValueOrDefault(effortType));
            if (total != 0)
            {
                deptTotals[effortType] = total;
            }
        }

        return new TeachingActivityDepartmentGroup
        {
            Department = department,
            Instructors = instructorGroups,
            DepartmentTotals = deptTotals
        };
    }

    private static TeachingActivityInstructorGroup BuildInstructorGroup(
        TeachingActivityRow firstRow, List<TeachingActivityRow> rows, List<string> effortTypes)
    {
        // Group by course (TermCode + CourseId + RoleId so multi-term reports keep terms distinct)
        var courseRows = rows
            .GroupBy(r => new { r.TermCode, r.CourseId, r.RoleId })
            .OrderBy(g => g.First().TermCode)
            .ThenBy(g => g.First().RoleId)
            .ThenBy(g => g.First().Course)
            .Select(courseGroup =>
            {
                var first = courseGroup.First();
                var effortByType = new Dictionary<string, decimal>();

                foreach (var row in courseGroup)
                {
                    var typeId = row.EffortTypeId.Trim();
                    if (!string.IsNullOrWhiteSpace(typeId))
                    {
                        effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.EffortValue;
                    }
                }

                return new TeachingActivityCourseRow
                {
                    TermCode = first.TermCode,
                    CourseId = first.CourseId,
                    Course = first.Course.Trim(),
                    Crn = first.Crn.Trim(),
                    Units = first.Units,
                    Enrollment = first.Enrollment,
                    RoleId = first.RoleId.Trim(),
                    EffortByType = effortByType
                };
            })
            .ToList();

        // Instructor totals = sum of each effort type across all courses
        var instructorTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = courseRows.Sum(c => c.EffortByType.GetValueOrDefault(effortType));
            if (total != 0)
            {
                instructorTotals[effortType] = total;
            }
        }

        return new TeachingActivityInstructorGroup
        {
            MothraId = firstRow.MothraId.Trim(),
            Instructor = firstRow.Instructor.Trim(),
            JobGroupId = firstRow.JobGroupId.Trim(),
            Courses = courseRows,
            InstructorTotals = instructorTotals
        };
    }

    public Task<byte[]> GenerateReportPdfAsync(TeachingActivityReport report)
    {
        Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        // Compact when 12+ effort types: with 6 fixed columns (Qtr, Role, Instructor,
        // Course, Units, Enroll), non-compact overflows the page at 12+ effort types
        var compact = orderedTypes.Count > 11;
        var fontSize = compact ? ReportPdfSettings.FontSizeCompact : ReportPdfSettings.FontSize;
        var headerFontSize = compact ? ReportPdfSettings.HeaderFontSizeCompact : ReportPdfSettings.HeaderFontSize;
        var hMargin = compact ? ReportPdfSettings.HMarginCompact : ReportPdfSettings.HMargin;
        var cellPadV = compact ? ReportPdfSettings.CellPadVCompact : ReportPdfSettings.CellPadV;
        var qtrWidth = compact ? 36f : 48f;
        var roleWidth = compact ? 22f : 32f;
        var instructorWidth = compact ? 90f : 130f;
        var courseWidth = compact ? 70f : 100f;
        var unitsWidth = compact ? 22f : 30f;
        var enrlWidth = compact ? 42f : 70f;
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
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                        });
                        col.Item().PaddingVertical(6).Row(row =>
                        {
                            row.RelativeItem().Text("Teaching Activity Report").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text(dept.Department).SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(qtrWidth);
                            columns.ConstantColumn(roleWidth);
                            columns.RelativeColumn(instructorWidth);
                            columns.RelativeColumn(courseWidth);
                            columns.ConstantColumn(unitsWidth);
                            columns.ConstantColumn(enrlWidth);
                            foreach (var type in orderedTypes)
                            {
                                columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                            }
                        });

                        var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                        table.Header(header =>
                        {
                            header.Cell().PaddingVertical(cellPadV).Text("Qtr").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Role").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Course").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Units").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Enroll").Style(hdrStyle);
                            foreach (var type in orderedTypes)
                            {
                                header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                            }
                        });

                        foreach (var instructor in dept.Instructors)
                        {
                            var courses = instructor.Courses;
                            for (int i = 0; i < courses.Count; i++)
                            {
                                var course = courses[i];
                                table.Cell().PaddingVertical(cellPadV).Text(course.TermCode.ToString());
                                table.Cell().PaddingVertical(cellPadV).PaddingLeft(4).Text(course.RoleId);

                                if (i == 0)
                                {
                                    table.Cell().RowSpan((uint)courses.Count).PaddingVertical(cellPadV)
                                        .Text(instructor.Instructor).SemiBold();
                                }

                                table.Cell().PaddingVertical(cellPadV).Text(course.Course);
                                table.Cell().PaddingVertical(cellPadV).Text(course.Units.ToString("G29"));
                                table.Cell().PaddingVertical(cellPadV).Text(course.Enrollment.ToString());

                                foreach (var type in orderedTypes)
                                {
                                    var val = course.EffortByType.GetValueOrDefault(type, 0);
                                    table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                                }
                            }

                            // Instructor totals row
                            table.Cell().ColumnSpan(6).Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                                .Text($"{instructor.Instructor} Totals:").Italic().Bold();
                            foreach (var type in orderedTypes)
                            {
                                var val = instructor.InstructorTotals.GetValueOrDefault(type, 0);
                                table.Cell().Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                    .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                            }
                        }

                        // Re-display effort type headers before dept totals
                        table.Cell().ColumnSpan(6).BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text("");
                        foreach (var type in orderedTypes)
                        {
                            table.Cell().BorderTop(1).BorderColor("#999999").PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                        }

                        // Department totals row
                        table.Cell().ColumnSpan(6).Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                            .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                            .Text("Department Totals:").Italic().Bold();
                        foreach (var type in orderedTypes)
                        {
                            var val = dept.DepartmentTotals.GetValueOrDefault(type, 0);
                            table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
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

    public Task<byte[]> GenerateIndividualReportPdfAsync(TeachingActivityReport report)
    {
        Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        // Compact when 12+ effort types: with fixed columns, non-compact overflows the page
        var compact = orderedTypes.Count > 11;
        var fontSize = compact ? ReportPdfSettings.FontSizeCompact : ReportPdfSettings.FontSize;
        var headerFontSize = compact ? ReportPdfSettings.HeaderFontSizeCompact : ReportPdfSettings.HeaderFontSize;
        var hMargin = compact ? ReportPdfSettings.HMarginCompact : ReportPdfSettings.HMargin;
        var cellPadV = compact ? ReportPdfSettings.CellPadVCompact : ReportPdfSettings.CellPadV;
        var qtrWidth = compact ? 36f : 48f;
        var roleWidth = compact ? 22f : 32f;
        var courseWidth = compact ? 70f : 100f;
        var unitsWidth = compact ? 22f : 30f;
        var enrlWidth = compact ? 42f : 70f;
        var effortWidth = compact ? ReportPdfSettings.EffortWidthCompact : ReportPdfSettings.EffortWidth;
        var spacerWidth = compact ? 42f : 70f;

        // Flatten all instructors across departments for individual layout
        var allInstructors = report.Departments
            .SelectMany(d => d.Instructors.Select(i => (Dept: d.Department, Instructor: i)))
            .ToList();

        var document = Document.Create(container =>
        {
            // One page per instructor (matching legacy behavior)
            foreach (var (dept, instructor) in allInstructors)
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
                            row.RelativeItem().Text("Teaching Activity Report (Individual)").SemiBold().FontSize(12);
                            row.RelativeItem().AlignCenter().Text(dept).SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                        });
                        // Instructor name below the sub-header
                        col.Item().PaddingBottom(4).Text(instructor.Instructor).Bold().FontSize(11);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(qtrWidth);
                            columns.ConstantColumn(roleWidth);
                            columns.RelativeColumn(courseWidth);
                            columns.ConstantColumn(unitsWidth);
                            columns.ConstantColumn(enrlWidth);
                            foreach (var type in orderedTypes)
                            {
                                columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                            }
                        });

                        var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                        table.Header(header =>
                        {
                            header.Cell().PaddingVertical(cellPadV).Text("Qtr").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Role").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Course").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Units").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).Text("Enrl").Style(hdrStyle);
                            foreach (var type in orderedTypes)
                            {
                                header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                            }
                        });

                        foreach (var course in instructor.Courses)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(course.TermCode.ToString());
                            table.Cell().PaddingVertical(cellPadV).PaddingLeft(4).Text(course.RoleId);
                            table.Cell().PaddingVertical(cellPadV).Text(course.Course);
                            table.Cell().PaddingVertical(cellPadV).Text(course.Units.ToString("G29"));
                            table.Cell().PaddingVertical(cellPadV).Text(course.Enrollment.ToString());
                            foreach (var type in orderedTypes)
                            {
                                var val = course.EffortByType.GetValueOrDefault(type, 0);
                                table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                            }
                        }

                        // Instructor totals row
                        table.Cell().ColumnSpan(5).Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                            .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                            .Text($"{instructor.Instructor} Totals:").Italic().Bold();
                        foreach (var type in orderedTypes)
                        {
                            var val = instructor.InstructorTotals.GetValueOrDefault(type, 0);
                            table.Cell().Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
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

    public MemoryStream GenerateReportExcel(TeachingActivityReport report)
    {
        var wb = new XLWorkbook();
        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);
        var effortCols = BuildExcelEffortColumns(orderedTypes);
        var lastCol = 4 + effortCols.Count;
        var termName = report.AcademicYear ?? report.TermName;

        foreach (var dept in report.Departments)
        {
            var ws = wb.Worksheets.Add(dept.Department);

            // Header rows matching PDF
            int row = AddExcelHeader(ws, "Teaching Activity Report", termName, dept.Department);
            row = AddExcelFilterLine(ws, row,
                ("Dept", report.FilterDepartment), ("Role", report.FilterRole),
                ("Faculty", report.FilterPerson), ("Title", report.FilterTitle));
            row++; // blank separator

            // Column headers
            int col = 1;
            ws.Cell(row, col++).Value = "Instructor";
            ws.Cell(row, col++).Value = "Course";
            ws.Cell(row, col++).Value = "Units";
            ws.Cell(row, col++).Value = "Enrl";
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
                bool firstCourse = true;
                foreach (var course in instructor.Courses)
                {
                    col = 1;
                    if (firstCourse)
                    {
                        ws.Cell(row, col).Value = ExcelHelper.SanitizeStringCell(instructor.Instructor);
                        firstCourse = false;
                    }
                    col++;
                    ws.Cell(row, col++).Value = ExcelHelper.SanitizeStringCell(course.Course);
                    ws.Cell(row, col++).Value = course.Units;
                    ws.Cell(row, col++).Value = course.Enrollment;
                    foreach (var (type, isSpacer) in effortCols)
                    {
                        if (!isSpacer) ws.Cell(row, col).Value = course.EffortByType.GetValueOrDefault(type, 0);
                        col++;
                    }
                    row++;
                }

                // Instructor totals row
                col = 1;
                ws.Cell(row, col).Value = ExcelHelper.SanitizeStringCell($"{instructor.Instructor} Totals");
                ws.Range(row, 1, row, lastCol).Style.Font.Bold = true;
                ShadeExcelRow(ws, row, lastCol, "#F8F8F8");
                col = 5;
                foreach (var (type, isSpacer) in effortCols)
                {
                    if (!isSpacer) ws.Cell(row, col).Value = instructor.InstructorTotals.GetValueOrDefault(type, 0);
                    col++;
                }
                row++;
            }

            // Re-display effort type headers before department totals
            col = 1;
            ws.Cell(row, col++).Value = "";
            ws.Cell(row, col++).Value = "";
            ws.Cell(row, col++).Value = "";
            ws.Cell(row, col++).Value = "";
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = type;
                col++;
            }
            ws.Range($"{row}:{row}").Style.Font.Bold = true;
            row++;

            // Department totals row
            col = 1;
            ws.Cell(row, col).Value = "Department Totals";
            ws.Range(row, 1, row, lastCol).Style.Font.Bold = true;
            ShadeExcelRow(ws, row, lastCol, "#E8E8E8");
            col = 5;
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = dept.DepartmentTotals.GetValueOrDefault(type, 0);
                col++;
            }

            ws.Column(1).Width = 25; // Instructor
            ws.Column(2).Width = 30; // Course
            ws.Column(3).Width = 8;  // Units
            ws.Column(4).Width = 8;  // Enrl
            for (int i = 0; i < effortCols.Count; i++)
            {
                ws.Column(5 + i).Width = effortCols[i].IsSpacer ? 1.5 : 7;
            }
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        wb.Dispose();
        stream.Position = 0;
        return stream;
    }

    public MemoryStream GenerateIndividualReportExcel(TeachingActivityReport report)
    {
        var wb = new XLWorkbook();
        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);
        var effortCols = BuildExcelEffortColumns(orderedTypes);
        var lastCol = 3 + effortCols.Count;
        var termName = report.AcademicYear ?? report.TermName;

        var allInstructors = report.Departments
            .SelectMany(d => d.Instructors.Select(i => (Dept: d.Department, Instructor: i)))
            .ToList();

        foreach (var (dept, instructor) in allInstructors)
        {
            var baseSheetName = ExcelHelper.SanitizeSheetName(instructor.Instructor);
            var sheetName = baseSheetName;
            if (wb.Worksheets.Any(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase)))
            {
                sheetName = ExcelHelper.SanitizeSheetName($"{instructor.Instructor}-{instructor.MothraId}");
            }
            var ws = wb.Worksheets.Add(sheetName);

            // Header rows matching PDF
            int row = AddExcelHeader(ws, "Teaching Activity Report (Individual)",
                termName, dept, instructor.Instructor);
            row = AddExcelFilterLine(ws, row,
                ("Dept", report.FilterDepartment), ("Role", report.FilterRole),
                ("Faculty", report.FilterPerson), ("Title", report.FilterTitle));
            row++; // blank separator

            // Column headers
            int col = 1;
            ws.Cell(row, col++).Value = "Course";
            ws.Cell(row, col++).Value = "Units";
            ws.Cell(row, col++).Value = "Enrl";
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = type;
                col++;
            }
            ws.Range($"{row}:{row}").Style.Font.Bold = true;
            ws.SheetView.FreezeRows(row);
            row++;

            foreach (var course in instructor.Courses)
            {
                col = 1;
                ws.Cell(row, col++).Value = ExcelHelper.SanitizeStringCell(course.Course);
                ws.Cell(row, col++).Value = course.Units;
                ws.Cell(row, col++).Value = course.Enrollment;
                foreach (var (type, isSpacer) in effortCols)
                {
                    if (!isSpacer) ws.Cell(row, col).Value = course.EffortByType.GetValueOrDefault(type, 0);
                    col++;
                }
                row++;
            }

            // Totals row
            col = 1;
            ws.Cell(row, col).Value = ExcelHelper.SanitizeStringCell($"{instructor.Instructor} Totals:");
            ws.Range(row, 1, row, lastCol).Style.Font.Bold = true;
            ws.Range(row, 1, row, lastCol).Style.Font.Italic = true;
            ShadeExcelRow(ws, row, lastCol, "#F8F8F8");
            col = 4;
            foreach (var (type, isSpacer) in effortCols)
            {
                if (!isSpacer) ws.Cell(row, col).Value = instructor.InstructorTotals.GetValueOrDefault(type, 0);
                col++;
            }

            ws.Column(1).Width = 30; // Course
            ws.Column(2).Width = 8;  // Units
            ws.Column(3).Width = 8;  // Enrl
            for (int i = 0; i < effortCols.Count; i++)
            {
                ws.Column(4 + i).Width = effortCols[i].IsSpacer ? 1.5 : 7;
            }
        }

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        wb.Dispose();
        stream.Position = 0;
        return stream;
    }

}
