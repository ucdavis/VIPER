using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class MeritReportService : BaseReportService, IMeritReportService
{
    private readonly ITermService _termService;
    private readonly ILogger<MeritReportService> _logger;

    public MeritReportService(
        EffortDbContext context,
        ITermService termService,
        ILogger<MeritReportService> logger)
        : base(context)
    {
        _termService = termService;
        _logger = logger;
    }

    // ============================================
    // Merit Detail Report
    // ============================================

    public async Task<MeritDetailReport> GetMeritDetailReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteMeritDetailForDepartmentsAsync(personId, termCode, termCode, departments, role, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        var report = new MeritDetailReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildMeritDetailReport(report, rows);
    }

    public async Task<MeritDetailReport> GetMeritDetailReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        if (termCodes.Count == 0)
        {
            return new MeritDetailReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept,
                FilterPersonId = personId,
                FilterRole = role
            };
        }

        var minTermCode = termCodes.Min();
        var maxTermCode = termCodes.Max();

        var rows = await ExecuteMeritDetailForDepartmentsAsync(personId, minTermCode, maxTermCode, departments, role, ct);

        var report = new MeritDetailReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildMeritDetailReport(report, rows);
    }

    /// <summary>
    /// Execute the merit detail SP for each department in the list, once with null for all departments,
    /// or return empty when the list is explicitly empty (unauthorized request).
    /// </summary>
    private async Task<List<MeritDetailRow>> ExecuteMeritDetailForDepartmentsAsync(
        int? personId, int startTermCode, int endTermCode,
        IReadOnlyList<string>? departments, string? role, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteMeritReportSpAsync(personId, startTermCode, endTermCode, null, role, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteMeritReportSpAsync(personId, startTermCode, endTermCode, departments[0], role, ct);
        }

        var allRows = new List<MeritDetailRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteMeritReportSpAsync(personId, startTermCode, endTermCode, dept, role, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    /// <summary>
    /// Raw row from sp_merit_report.
    /// </summary>
    private sealed class MeritDetailRow
    {
        public int TermCode { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string Course { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public int Enrollment { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public string EffortTypeId { get; set; } = string.Empty;
        public decimal Effort { get; set; }
    }

    private static MeritDetailReport BuildMeritDetailReport(MeritDetailReport report, List<MeritDetailRow> rows)
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
            .Select(deptGroup => BuildMeritDetailDeptGroup(deptGroup.Key, deptGroup.ToList(), effortTypes))
            .ToList();

        return report;
    }

    private static MeritDetailDepartmentGroup BuildMeritDetailDeptGroup(
        string department, List<MeritDetailRow> rows, List<string> effortTypes)
    {
        var instructorGroups = rows
            .GroupBy(r => r.MothraId)
            .OrderBy(g => g.First().Instructor)
            .Select(instGroup => BuildMeritDetailInstructorGroup(instGroup.First(), instGroup.ToList(), effortTypes))
            .ToList();

        var deptTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = instructorGroups.Sum(i => i.InstructorTotals.GetValueOrDefault(effortType));
            if (total != 0)
            {
                deptTotals[effortType] = total;
            }
        }

        return new MeritDetailDepartmentGroup
        {
            Department = department,
            Instructors = instructorGroups,
            DepartmentTotals = deptTotals
        };
    }

    private static MeritDetailInstructorGroup BuildMeritDetailInstructorGroup(
        MeritDetailRow firstRow, List<MeritDetailRow> rows, List<string> effortTypes)
    {
        // Group by course (TermCode + CourseId + RoleId)
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
                        effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.Effort;
                    }
                }

                return new MeritDetailCourseRow
                {
                    TermCode = first.TermCode,
                    CourseId = first.CourseId,
                    Course = first.Course.Trim(),
                    Units = first.Units,
                    Enrollment = first.Enrollment,
                    RoleId = first.RoleId.Trim(),
                    EffortByType = effortByType
                };
            })
            .ToList();

        var instructorTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = courseRows.Sum(c => c.EffortByType.GetValueOrDefault(effortType));
            if (total != 0)
            {
                instructorTotals[effortType] = total;
            }
        }

        return new MeritDetailInstructorGroup
        {
            MothraId = firstRow.MothraId.Trim(),
            Instructor = firstRow.Instructor.Trim(),
            JobGroupId = "",
            JobGroupDescription = null,
            Courses = courseRows,
            InstructorTotals = instructorTotals
        };
    }

    private async Task<List<MeritDetailRow>> ExecuteMeritReportSpAsync(
        int? personId,
        int startTermCode,
        int endTermCode,
        string? department,
        string? role,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<MeritDetailRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_report]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@PersonId", (object?)personId ?? DBNull.Value);
        command.Parameters.AddWithValue("@StartTermCode", startTermCode);
        command.Parameters.AddWithValue("@EndTermCode", endTermCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
        command.Parameters.AddWithValue("@Role", (object?)role ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // SP columns: TermCode, MothraId, Instructor, Department, CourseId, Course, Units, Enrollment, RoleId, EffortTypeId, Effort
            results.Add(new MeritDetailRow
            {
                TermCode = reader.GetInt32(0),
                MothraId = reader.GetString(1),
                Instructor = reader.GetString(2),
                Department = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                CourseId = reader.GetInt32(4),
                Course = reader.GetString(5),
                Units = await reader.IsDBNullAsync(6, ct) ? 0m : reader.GetDecimal(6),
                Enrollment = await reader.IsDBNullAsync(7, ct) ? 0 : reader.GetInt32(7),
                RoleId = await reader.IsDBNullAsync(8, ct) ? "" : reader.GetString(8),
                EffortTypeId = await reader.IsDBNullAsync(9, ct) ? "" : reader.GetString(9),
                Effort = await reader.IsDBNullAsync(10, ct) ? 0m : reader.GetInt32(10)
            });
        }

        _logger.LogDebug("Merit detail report: {RowCount} rows returned", results.Count);
        return results;
    }

    // ============================================
    // Merit Average Report
    // ============================================

    public async Task<MeritAverageReport> GetMeritAverageReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteMeritAverageForDepartmentsAsync(termCode, departments, personId, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;
        var academicYear = AcademicYearHelper.GetAcademicYearFromTermCode(termCode);
        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new MeritAverageReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept,
            FilterPersonId = personId
        };

        return BuildMeritAverageReport(report, rows, _termService, clinicalMothraIds);
    }

    public async Task<MeritAverageReport> GetMeritAverageReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        if (termCodes.Count == 0)
        {
            return new MeritAverageReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept,
                FilterPersonId = personId
            };
        }

        // sp_merit_average only accepts a single term code, so call for each term and merge
        var allRows = new List<MeritAverageRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteMeritAverageForDepartmentsAsync(tc, departments, personId, ct);
            allRows.AddRange(rows);
        }

        var clinicalMothraIds = await GetClinicalFacultyMothraIdsAsync(academicYear, ct);

        var report = new MeritAverageReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            FilterPersonId = personId
        };

        return BuildMeritAverageReport(report, allRows, _termService, clinicalMothraIds);
    }

    /// <summary>
    /// Execute the merit average SP for each department in the list, once with null for all departments,
    /// or return empty when the list is explicitly empty (unauthorized request).
    /// </summary>
    private async Task<List<MeritAverageRow>> ExecuteMeritAverageForDepartmentsAsync(
        int termCode, IReadOnlyList<string>? departments, int? personId, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteMeritAverageSpAsync(termCode, null, personId, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteMeritAverageSpAsync(termCode, departments[0], personId, ct);
        }

        var allRows = new List<MeritAverageRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteMeritAverageSpAsync(termCode, dept, personId, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    /// <summary>
    /// Raw row from sp_merit_average, tagged with the term code it was queried for.
    /// </summary>
    private sealed class MeritAverageRow
    {
        public int TermCode { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobGroupId { get; set; } = string.Empty;
        public string JobGroupDescription { get; set; } = string.Empty;
        public decimal PercentAdmin { get; set; }
        public string EffortTypeId { get; set; } = string.Empty;
        public decimal TotalEffort { get; set; }
    }

    private static MeritAverageReport BuildMeritAverageReport(MeritAverageReport report, List<MeritAverageRow> rows, ITermService termService, HashSet<string> clinicalMothraIds)
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

        // Group by JobGroupDescription -> Department -> Instructor
        report.JobGroups = rows
            .GroupBy(r => r.JobGroupDescription.Trim())
            .OrderBy(g => g.Key)
            .Select(jgGroup =>
            {
                var deptGroups = jgGroup
                    .GroupBy(r => r.Department.Trim())
                    .OrderBy(g => g.Key)
                    .Select(deptGroup => BuildMeritAverageDeptGroup(deptGroup.Key, deptGroup.ToList(), effortTypes, termService, clinicalMothraIds))
                    .ToList();

                return new MeritAverageJobGroup
                {
                    JobGroupDescription = jgGroup.Key,
                    Departments = deptGroups
                };
            })
            .ToList();

        return report;
    }

    private static MeritAverageDepartmentGroup BuildMeritAverageDeptGroup(
        string department, List<MeritAverageRow> rows, List<string> effortTypes, ITermService termService, HashSet<string> clinicalMothraIds)
    {
        // Aggregate per instructor with per-term breakdown
        var instructorRows = rows
            .GroupBy(r => r.MothraId)
            .OrderBy(g => g.First().Instructor)
            .Select(instGroup =>
            {
                var first = instGroup.First();

                // Build per-term effort rows
                var terms = instGroup
                    .GroupBy(r => r.TermCode)
                    .OrderBy(g => g.Key)
                    .Select(termGroup =>
                    {
                        var termEffort = new Dictionary<string, decimal>();
                        foreach (var row in termGroup)
                        {
                            var typeId = row.EffortTypeId.Trim();
                            if (!string.IsNullOrWhiteSpace(typeId))
                            {
                                termEffort[typeId] = termEffort.GetValueOrDefault(typeId) + row.TotalEffort;
                            }
                        }
                        return new MeritAverageTermRow
                        {
                            TermCode = termGroup.Key,
                            TermName = termService.GetTermName(termGroup.Key),
                            EffortByType = termEffort
                        };
                    })
                    .ToList();

                // Instructor totals = sum across all terms
                var effortByType = new Dictionary<string, decimal>();
                foreach (var row in instGroup)
                {
                    var typeId = row.EffortTypeId.Trim();
                    if (!string.IsNullOrWhiteSpace(typeId))
                    {
                        effortByType[typeId] = effortByType.GetValueOrDefault(typeId) + row.TotalEffort;
                    }
                }

                return new MeritAverageInstructorRow
                {
                    MothraId = first.MothraId.Trim(),
                    Instructor = first.Instructor.Trim(),
                    JobGroupId = first.JobGroupId.Trim(),
                    JobGroupDescription = first.JobGroupDescription.Trim(),
                    PercentAdmin = first.PercentAdmin,
                    Terms = terms,
                    EffortByType = effortByType
                };
            })
            .ToList();

        var facultyCount = instructorRows.Count;
        // "Faculty with assigned CLI" = instructors with clinical percent assignments for the year
        var facultyWithCliCount = instructorRows.Count(i => clinicalMothraIds.Contains(i.MothraId));

        var groupTotals = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = instructorRows.Sum(i => i.EffortByType.GetValueOrDefault(effortType));
            if (total != 0)
            {
                groupTotals[effortType] = total;
            }
        }

        return new MeritAverageDepartmentGroup
        {
            Department = department,
            Instructors = instructorRows,
            GroupTotals = groupTotals,
            GroupAverages = CalculateAverages(groupTotals, effortTypes, facultyCount, facultyWithCliCount),
            FacultyCount = facultyCount,
            FacultyWithCliCount = facultyWithCliCount
        };
    }

    private async Task<List<MeritAverageRow>> ExecuteMeritAverageSpAsync(
        int termCode,
        string? department,
        int? personId,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<MeritAverageRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_average]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
        command.Parameters.AddWithValue("@PersonId", personId.HasValue ? personId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@Role", DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            // SP columns: MothraId, Instructor, Department, JobGroupId, JobGroupDescription, PercentAdmin, EffortTypeId, TotalEffort
            results.Add(new MeritAverageRow
            {
                TermCode = termCode,
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                Department = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                JobGroupId = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                JobGroupDescription = await reader.IsDBNullAsync(4, ct) ? "" : reader.GetString(4),
                PercentAdmin = await reader.IsDBNullAsync(5, ct) ? 0m : (decimal)reader.GetDouble(5),
                EffortTypeId = await reader.IsDBNullAsync(6, ct) ? "" : reader.GetString(6),
                TotalEffort = await reader.IsDBNullAsync(7, ct) ? 0m : reader.GetInt32(7)
            });
        }

        _logger.LogDebug("Merit average report for term {TermCode}: {RowCount} rows returned", termCode, results.Count);
        return results;
    }

    // ============================================
    // PDF Generation
    // ============================================

    public Task<byte[]> GenerateMeritDetailPdfAsync(MeritDetailReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        // Compact when 12+ effort types: with fixed columns, non-compact overflows the page
        var compact = orderedTypes.Count > 11;
        var fontSize = compact ? 8.5f : 10f;
        var headerFontSize = compact ? 7.5f : 8.5f;
        var hMargin = compact ? 0.35f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var qtrWidth = compact ? 36f : 48f;
        var roleWidth = compact ? 22f : 32f;
        var instructorWidth = compact ? 90f : 130f;
        var courseWidth = compact ? 70f : 100f;
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
                            row.RelativeItem().Text("Merit & Promotion Detail Report").SemiBold().FontSize(12);
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

                                foreach (var type in orderedTypes)
                                {
                                    var val = course.EffortByType.GetValueOrDefault(type, 0);
                                    table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                                }
                            }

                            // Instructor totals row
                            table.Cell().ColumnSpan(4).Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                                .Text("Instructor Totals:").Italic().Bold();
                            foreach (var type in orderedTypes)
                            {
                                var val = instructor.InstructorTotals.GetValueOrDefault(type, 0);
                                table.Cell().Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                    .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                            }
                        }

                        // Re-display effort type headers before department totals
                        table.Cell().ColumnSpan(4).BorderTop(1).BorderColor("#999999")
                            .PaddingVertical(cellPadV).Text("");
                        foreach (var type in orderedTypes)
                        {
                            table.Cell().BorderTop(1).BorderColor("#999999")
                                .PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                        }

                        // Department totals row
                        table.Cell().ColumnSpan(4).Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
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
                            ("Faculty", report.FilterPersonId?.ToString()));
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

    public Task<byte[]> GenerateMeritAveragePdfAsync(MeritAverageReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8.5f : 10f;
        var headerFontSize = compact ? 7.5f : 8.5f;
        var hMargin = compact ? 0.35f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var instructorWidth = compact ? 110f : 150f;
        var termWidth = compact ? 60f : 80f;
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
                                row.RelativeItem().Text("Merit & Promotion Average Report").SemiBold().FontSize(12);
                                row.RelativeItem().AlignCenter().Text(dept.Department).SemiBold().FontSize(12);
                                row.RelativeItem().AlignRight().Text(report.TermName).SemiBold().FontSize(12);
                            });
                            col.Item().Text(jobGroup.JobGroupDescription).SemiBold().FontSize(11);
                        });

                        var totalColumns = (uint)(2 + orderedTypes.Count);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(instructorWidth);
                                columns.RelativeColumn(termWidth);
                                foreach (var type in orderedTypes)
                                {
                                    columns.ConstantColumn(SpacerColumns.Contains(type) ? spacerWidth : effortWidth);
                                }
                            });

                            var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                            table.Header(header =>
                            {
                                header.Cell().ColumnSpan(2).PaddingVertical(cellPadV).Text("Instructor").Style(hdrStyle);
                                foreach (var type in orderedTypes)
                                {
                                    header.Cell().PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                                }
                            });

                            foreach (var instructor in dept.Instructors)
                            {
                                var terms = instructor.Terms;
                                var termCount = terms.Count > 0 ? terms.Count : 1;

                                if (terms.Count > 0)
                                {
                                    for (int i = 0; i < terms.Count; i++)
                                    {
                                        var term = terms[i];

                                        if (i == 0)
                                        {
                                            table.Cell().RowSpan((uint)termCount).PaddingVertical(cellPadV)
                                                .Text(instructor.Instructor).SemiBold();
                                        }

                                        table.Cell().PaddingVertical(cellPadV)
                                            .Text(term.TermName).FontSize(headerFontSize).FontColor("#555555");

                                        foreach (var type in orderedTypes)
                                        {
                                            var val = term.EffortByType.GetValueOrDefault(type, 0);
                                            table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                                        }
                                    }
                                }
                                else
                                {
                                    // Fallback: single row with aggregated totals
                                    table.Cell().PaddingVertical(cellPadV).Text(instructor.Instructor).SemiBold();
                                    table.Cell().PaddingVertical(cellPadV).Text("");
                                    foreach (var type in orderedTypes)
                                    {
                                        var val = instructor.EffortByType.GetValueOrDefault(type, 0);
                                        table.Cell().PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0");
                                    }
                                }

                                // Instructor totals row
                                table.Cell().ColumnSpan(2).Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                    .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                                    .Text("INSTRUCTOR TOTALS:").Italic().Bold();
                                foreach (var type in orderedTypes)
                                {
                                    var val = instructor.EffortByType.GetValueOrDefault(type, 0);
                                    table.Cell().Background("#F8F8F8").BorderTop(0.5f).BorderColor("#CCCCCC")
                                        .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                                }
                            }

                            // Re-display effort type headers before group summary
                            table.Cell().ColumnSpan(2).BorderTop(1).BorderColor("#999999")
                                .PaddingVertical(cellPadV).Text("");
                            foreach (var type in orderedTypes)
                            {
                                table.Cell().BorderTop(1).BorderColor("#999999")
                                    .PaddingVertical(cellPadV).Text(type).Style(hdrStyle);
                            }

                            // Faculty Count Total row
                            table.Cell().ColumnSpan(totalColumns).PaddingVertical(cellPadV)
                                .Text($"Faculty Count Total: {dept.FacultyCount}");

                            // Faculty with assigned CLI row
                            table.Cell().ColumnSpan(totalColumns).PaddingVertical(cellPadV)
                                .Text($"Faculty with assigned CLI: {dept.FacultyWithCliCount}");

                            // Group totals
                            table.Cell().ColumnSpan(2).Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                .PaddingVertical(cellPadV).AlignRight().PaddingRight(8)
                                .Text("Totals:").Italic().Bold();
                            foreach (var type in orderedTypes)
                            {
                                var val = dept.GroupTotals.GetValueOrDefault(type, 0);
                                table.Cell().Background("#E8E8E8").BorderTop(1.5f).BorderColor("#666666")
                                    .PaddingVertical(cellPadV).Text(val > 0 ? val.ToString() : "0").Bold();
                            }

                            // Group averages
                            table.Cell().ColumnSpan(2).Background("#E0E0E0").PaddingVertical(cellPadV)
                                .AlignRight().PaddingRight(8)
                                .Text("Averages:").Italic().Bold();
                            foreach (var type in orderedTypes)
                            {
                                var val = dept.GroupAverages.GetValueOrDefault(type, 0);
                                table.Cell().Background("#E0E0E0").PaddingVertical(cellPadV)
                                    .Text(val.ToString("F1")).Bold();
                            }
                        });

                        page.Footer().Column(col =>
                        {
                            AddPdfFilterLine(col.Item(),
                                ("Dept", report.FilterDepartment), ("Faculty", report.FilterPersonId?.ToString()));
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
