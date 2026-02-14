using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public partial class TeachingActivityService : ITeachingActivityService
{
    private readonly EffortDbContext _context;
    private readonly ITermService _termService;
    private readonly ILogger<TeachingActivityService> _logger;

    public TeachingActivityService(
        EffortDbContext context,
        ITermService termService,
        ILogger<TeachingActivityService> logger)
    {
        _context = context;
        _termService = termService;
        _logger = logger;
    }

    [GeneratedRegex(@"^(\d{4})-(\d{4})$")]
    private static partial Regex AcademicYearRegex();

    public async Task<TeachingActivityReport> GetTeachingActivityReportAsync(
        int termCode,
        string? department = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteReportSpAsync(termCode, department, personId, role, jobGroupId, ct);
        var term = await _termService.GetTermAsync(termCode, ct);

        var report = new TeachingActivityReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = department,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, rows);
    }

    public async Task<TeachingActivityReport> GetTeachingActivityReportByYearAsync(
        string academicYear,
        string? department = null,
        int? personId = null,
        string? role = null,
        string? jobGroupId = null,
        CancellationToken ct = default)
    {
        var match = AcademicYearRegex().Match(academicYear);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid academic year format: {academicYear}. Expected format: YYYY-YYYY");
        }

        var startYear = int.Parse(match.Groups[1].Value);

        // Resolve all term codes in this academic year from TermStatus
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);

        if (termCodes.Count == 0)
        {
            return new TeachingActivityReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = department,
                FilterPerson = personId?.ToString(),
                FilterRole = role,
                FilterTitle = jobGroupId
            };
        }

        // Call SP for each term and merge all rows
        var allRows = new List<TeachingActivityRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteReportSpAsync(tc, department, personId, role, jobGroupId, ct);
            allRows.AddRange(rows);
        }

        var report = new TeachingActivityReport
        {
            TermCode = termCodes.First(),
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = department,
            FilterPerson = personId?.ToString(),
            FilterRole = role,
            FilterTitle = jobGroupId
        };

        return BuildReport(report, allRows);
    }

    /// <summary>
    /// Get academic year string for a term code. Fall (month >= 9) starts a new academic year.
    /// E.g., 202409 → "2024-2025", 202501 → "2024-2025".
    /// </summary>
    public static string GetAcademicYear(int termCode)
    {
        var year = termCode / 100;
        var month = termCode % 100;
        var startYear = month >= 9 ? year : year - 1;
        return $"{startYear}-{startYear + 1}";
    }

    /// <summary>
    /// Get all term codes from TermStatus that belong to the given academic year.
    /// Academic year starts with Fall (month >= 9) of startYear through Summer (month &lt; 9) of startYear + 1.
    /// </summary>
    private async Task<List<int>> GetTermCodesForAcademicYearAsync(int startYear, CancellationToken ct)
    {
        var allTerms = await _context.Terms
            .Select(t => t.TermCode)
            .ToListAsync(ct);

        return allTerms
            .Where(tc => GetAcademicYear(tc) == $"{startYear}-{startYear + 1}")
            .OrderByDescending(tc => tc)
            .ToList();
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
                        effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.Hours;
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

    private async Task<List<TeachingActivityRow>> ExecuteReportSpAsync(
        int termCode,
        string? department,
        int? personId,
        string? role,
        string? jobGroupId,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<TeachingActivityRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_effort_general_report]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
        command.Parameters.AddWithValue("@PersonId", personId.HasValue ? personId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@Role", (object?)role ?? DBNull.Value);
        command.Parameters.AddWithValue("@JobGroupId", (object?)jobGroupId ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TeachingActivityRow
            {
                TermCode = termCode,
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                JobGroupId = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Department = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CourseId = reader.GetInt32(4),
                Course = reader.GetString(5),
                Crn = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Units = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7),
                Enrollment = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                RoleId = reader.IsDBNull(9) ? "" : reader.GetString(9),
                EffortTypeId = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Hours = reader.IsDBNull(11) ? 0m : reader.GetInt32(11),
                Weeks = reader.IsDBNull(12) ? 0m : reader.GetInt32(12)
            });
        }

        _logger.LogDebug("Teaching activity report for term {TermCode}: {RowCount} rows returned", termCode, results.Count);
        return results;
    }

    private static readonly string[] AlwaysShowEffortTypes = ["CLI", "VAR", "LEC", "LAB", "DIS", "PBL", "CBL", "TBL", "PRS", "JLC", "EXM"];
    private static readonly HashSet<string> SpacerColumns = new() { "VAR", "EXM" };

    public Task<byte[]> GenerateReportPdfAsync(TeachingActivityReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        // Adaptive layout: compact when many effort type columns (e.g., academic year reports)
        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8.5f : 10f;
        var headerFontSize = compact ? 7.5f : 8.5f;
        var hMargin = compact ? 0.35f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var qtrWidth = compact ? 36f : 48f;
        var roleWidth = compact ? 18f : 24f;
        var instructorWidth = compact ? 90f : 130f;
        var unitsWidth = compact ? 22f : 30f;
        var enrlWidth = compact ? 22f : 30f;
        var effortWidth = compact ? 22f : 30f;
        var spacerWidth = compact ? 28f : 40f;

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
                            columns.ConstantColumn(instructorWidth);
                            columns.RelativeColumn();  // Course (fills remaining space)
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

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static List<string> GetOrderedEffortTypes(List<string> effortTypes)
    {
        var ordered = new List<string>(AlwaysShowEffortTypes);
        var remaining = effortTypes
            .Where(t => !AlwaysShowEffortTypes.Contains(t))
            .OrderBy(t => t)
            .ToList();
        ordered.AddRange(remaining);
        return ordered;
    }

    private static TextStyle HeaderStyle()
    {
        return TextStyle.Default.FontSize(7).Underline();
    }
}
