using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public class EvaluationReportService : BaseReportService, IEvaluationReportService
{
    private readonly ITermService _termService;
    private readonly ILogger<EvaluationReportService> _logger;

    public EvaluationReportService(
        EffortDbContext context,
        ITermService termService,
        ILogger<EvaluationReportService> logger)
        : base(context)
    {
        _termService = termService;
        _logger = logger;
    }

    // ============================================
    // Eval Summary Report
    // ============================================

    public async Task<EvalSummaryReport> GetEvalSummaryReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteEvalQueryForDepartmentsAsync(termCode, departments, personId, role, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        var report = new EvalSummaryReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildEvalSummaryReport(report, rows);
    }

    public async Task<EvalSummaryReport> GetEvalSummaryReportByYearAsync(
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
            return new EvalSummaryReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept,
                FilterPersonId = personId,
                FilterRole = role
            };
        }

        var allRows = new List<EvalRawRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteEvalQueryForDepartmentsAsync(tc, departments, personId, role, ct);
            allRows.AddRange(rows);
        }

        var report = new EvalSummaryReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildEvalSummaryReport(report, allRows);
    }

    // ============================================
    // Eval Detail Report
    // ============================================

    public async Task<EvalDetailReport> GetEvalDetailReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        int? personId = null,
        string? role = null,
        CancellationToken ct = default)
    {
        var rows = await ExecuteEvalQueryForDepartmentsAsync(termCode, departments, personId, role, ct);
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        var report = new EvalDetailReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildEvalDetailReport(report, rows);
    }

    public async Task<EvalDetailReport> GetEvalDetailReportByYearAsync(
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
            return new EvalDetailReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept,
                FilterPersonId = personId,
                FilterRole = role
            };
        }

        var allRows = new List<EvalRawRow>();
        foreach (var tc in termCodes)
        {
            var rows = await ExecuteEvalQueryForDepartmentsAsync(tc, departments, personId, role, ct);
            allRows.AddRange(rows);
        }

        var report = new EvalDetailReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            FilterPersonId = personId,
            FilterRole = role
        };

        return BuildEvalDetailReport(report, allRows);
    }

    // ============================================
    // SP Execution
    // ============================================

    /// <summary>
    /// Raw row from the evaluation query against evalharvest database.
    /// </summary>
    private sealed class EvalRawRow
    {
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Crn { get; set; } = string.Empty;
        public int TermCode { get; set; }
        public decimal QuantMean { get; set; }
        public int N1 { get; set; }
        public int N2 { get; set; }
        public int N3 { get; set; }
        public int N4 { get; set; }
        public int N5 { get; set; }
        public int NumResponses { get; set; }
        public int NumEnrolled { get; set; }
        public int RoleId { get; set; }
    }

    private async Task<List<EvalRawRow>> ExecuteEvalQueryForDepartmentsAsync(
        int termCode, IReadOnlyList<string>? departments, int? personId, string? role, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteEvalQueryAsync(termCode, null, personId, role, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteEvalQueryAsync(termCode, departments[0], personId, role, ct);
        }

        var allRows = new List<EvalRawRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteEvalQueryAsync(termCode, dept, personId, role, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    /// <summary>
    /// Query the evalharvest database for evaluation data, joined with effort persons
    /// for job group qualification and department filtering.
    /// Follows the same cross-database query pattern as sp_instructor_evals_multiyear.
    /// </summary>
    private async Task<List<EvalRawRow>> ExecuteEvalQueryAsync(
        int termCode, string? department, int? personId, string? role, CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<EvalRawRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // Cross-database query matching sp_instructor_evals_multiyear pattern
        var sql = @"
            SELECT
                people_MothraID AS MothraId,
                RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) AS Instructor,
                p.EffortDept AS Department,
                RTRIM(course_subj_code) + ' ' + RTRIM(course_crse_numb) +
                    ' (' + ISNULL(bi.baseinfo_title, '') + ')' AS Course,
                course_CRN AS CRN,
                course_TermCode AS TermCode,
                quant_mean AS QuantMean,
                quant_1_n AS N1,
                quant_2_n AS N2,
                quant_3_n AS N3,
                quant_4_n AS N4,
                quant_5_n AS N5,
                quant_respondants AS NumResponses,
                course_enrollment AS NumEnrolled,
                CASE WHEN poa.poa_mailID IS NULL THEN 2 ELSE 1 END AS RoleId
            FROM evalharvest.dbo.eh_questions
            INNER JOIN evalharvest.dbo.eh_quant ON quest_ID = quant_QuestionID_FK
            INNER JOIN evalharvest.dbo.eh_Courses ON quest_CRN = course_CRN
                AND quest_TermCode = course_termCode
                AND ISNULL(quest_facilitator_evalid, 0) = course_facilitator_evalid
            INNER JOIN evalharvest.dbo.eh_People ON quant_mailID = people_mailid
                AND people_TermCode = course_TermCode
            LEFT JOIN evalharvest.dbo.eh_POA poa ON course_CRN = poa_crn
                AND course_TermCode = poa_termcode
                AND people_mailid = poa_mailID
            LEFT JOIN Courses.dbo.baseinfo bi ON quest_TermCode = bi.baseinfo_term_code
                AND quest_CRN = bi.baseinfo_crn
            INNER JOIN [users].[Person] up ON people_MothraID = up.MothraId
            INNER JOIN [effort].[Persons] p ON up.PersonId = p.PersonId AND people_TermCode = p.TermCode
            WHERE course_TermCode = @TermCode
                AND quant_mailID IS NOT NULL
                AND quest_overall = 1
                AND EXISTS (
                    SELECT 1 FROM effort.fn_qualified_job_groups() q
                    WHERE q.JobGroupId = p.JobGroupId
                    AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
                )
                AND p.EffortDept <> 'OTH'
                AND p.VolunteerWos = 0
                AND (@Department IS NULL OR p.EffortDept = @Department)
                AND (@PersonId IS NULL OR up.PersonId = @PersonId)
                AND (@Role IS NULL OR (CASE WHEN poa.poa_mailID IS NULL THEN 2 ELSE 1 END) = @Role)
                AND quant_mean > 0
            ORDER BY p.EffortDept, p.LastName, p.FirstName, course_subj_code, course_crse_numb";

        await using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
        command.Parameters.AddWithValue("@PersonId", (object?)personId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Role", (object?)ParseRoleFilter(role) ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new EvalRawRow
            {
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                Department = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                Course = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                Crn = await reader.IsDBNullAsync(4, ct) ? "" : reader.GetString(4),
                TermCode = reader.GetInt32(5),
                QuantMean = await reader.IsDBNullAsync(6, ct) ? 0m : (decimal)reader.GetDouble(6),
                N1 = await reader.IsDBNullAsync(7, ct) ? 0 : reader.GetInt32(7),
                N2 = await reader.IsDBNullAsync(8, ct) ? 0 : reader.GetInt32(8),
                N3 = await reader.IsDBNullAsync(9, ct) ? 0 : reader.GetInt32(9),
                N4 = await reader.IsDBNullAsync(10, ct) ? 0 : reader.GetInt32(10),
                N5 = await reader.IsDBNullAsync(11, ct) ? 0 : reader.GetInt32(11),
                NumResponses = await reader.IsDBNullAsync(12, ct) ? 0 : reader.GetInt32(12),
                NumEnrolled = await reader.IsDBNullAsync(13, ct) ? 0 : reader.GetInt32(13),
                RoleId = reader.GetInt32(14)
            });
        }

        _logger.LogDebug("Evaluation report for term {TermCode}: {RowCount} rows returned", termCode, results.Count);
        return results;
    }

    // ============================================
    // Report Building
    // ============================================

    private static EvalSummaryReport BuildEvalSummaryReport(EvalSummaryReport report, List<EvalRawRow> rows)
    {
        if (rows.Count == 0)
        {
            return report;
        }

        report.Departments = rows
            .GroupBy(r => r.Department.Trim())
            .OrderBy(g => g.Key)
            .Select(deptGroup =>
            {
                var instructors = deptGroup
                    .GroupBy(r => r.MothraId, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.First().Instructor)
                    .Select(instrGroup =>
                    {
                        var instrRows = instrGroup.ToList();
                        var totalPts = instrRows.Sum(r =>
                            r.N5 * 5m + r.N4 * 4m + r.N3 * 3m + r.N2 * 2m + r.N1 * 1m);
                        var totalResponses = instrRows.Sum(r => r.N5 + r.N4 + r.N3 + r.N2 + r.N1);
                        var weightedAvg = totalResponses > 0
                            ? Math.Round(totalPts / totalResponses, 2)
                            : 0m;

                        return new EvalInstructorSummary
                        {
                            MothraId = instrGroup.Key,
                            Instructor = instrRows[0].Instructor,
                            WeightedAverage = weightedAvg,
                            TotalResponses = instrRows.Sum(r => r.NumResponses),
                            TotalEnrolled = instrRows.Sum(r => r.NumEnrolled)
                        };
                    })
                    .ToList();

                // Department average: weighted across all courses in the department
                var deptTotalPts = deptGroup.Sum(r =>
                    r.N5 * 5m + r.N4 * 4m + r.N3 * 3m + r.N2 * 2m + r.N1 * 1m);
                var deptTotalResponses = deptGroup.Sum(r => r.N5 + r.N4 + r.N3 + r.N2 + r.N1);

                return new EvalDepartmentGroup
                {
                    Department = deptGroup.Key,
                    Instructors = instructors,
                    DepartmentAverage = deptTotalResponses > 0
                        ? Math.Round(deptTotalPts / deptTotalResponses, 2)
                        : 0m,
                    TotalResponses = deptGroup.Sum(r => r.NumResponses)
                };
            })
            .ToList();

        return report;
    }

    private static EvalDetailReport BuildEvalDetailReport(EvalDetailReport report, List<EvalRawRow> rows)
    {
        if (rows.Count == 0)
        {
            return report;
        }

        report.Departments = rows
            .GroupBy(r => r.Department.Trim())
            .OrderBy(g => g.Key)
            .Select(deptGroup =>
            {
                var instructors = deptGroup
                    .GroupBy(r => r.MothraId, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.First().Instructor)
                    .Select(instrGroup =>
                    {
                        var instrRows = instrGroup.ToList();
                        var courses = instrRows
                            .Select(r => new EvalCourseDetail
                            {
                                Course = r.Course,
                                Crn = r.Crn,
                                TermCode = r.TermCode,
                                Role = r.RoleId == 1 ? "I" : "F",
                                Average = r.QuantMean,
                                Median = CalculateMedian(r.N1, r.N2, r.N3, r.N4, r.N5),
                                NumResponses = r.NumResponses,
                                NumEnrolled = r.NumEnrolled
                            })
                            .ToList();

                        // Instructor weighted average: totalPts / totalResponses
                        var totalPts = instrRows.Sum(r =>
                            r.N5 * 5m + r.N4 * 4m + r.N3 * 3m + r.N2 * 2m + r.N1 * 1m);
                        var totalResponses = instrRows.Sum(r => r.N5 + r.N4 + r.N3 + r.N2 + r.N1);

                        // Instructor median from combined distribution
                        var totalN1 = instrRows.Sum(r => r.N1);
                        var totalN2 = instrRows.Sum(r => r.N2);
                        var totalN3 = instrRows.Sum(r => r.N3);
                        var totalN4 = instrRows.Sum(r => r.N4);
                        var totalN5 = instrRows.Sum(r => r.N5);

                        return new EvalDetailInstructor
                        {
                            MothraId = instrGroup.Key,
                            Instructor = instrRows[0].Instructor,
                            Courses = courses,
                            InstructorAverage = totalResponses > 0
                                ? Math.Round(totalPts / totalResponses, 2)
                                : 0m,
                            InstructorMedian = CalculateMedian(totalN1, totalN2, totalN3, totalN4, totalN5)
                        };
                    })
                    .ToList();

                // Department weighted average
                var deptTotalPts = deptGroup.Sum(r =>
                    r.N5 * 5m + r.N4 * 4m + r.N3 * 3m + r.N2 * 2m + r.N1 * 1m);
                var deptTotalResponses = deptGroup.Sum(r => r.N5 + r.N4 + r.N3 + r.N2 + r.N1);

                return new EvalDetailDepartmentGroup
                {
                    Department = deptGroup.Key,
                    Instructors = instructors,
                    DepartmentAverage = deptTotalResponses > 0
                        ? Math.Round(deptTotalPts / deptTotalResponses, 2)
                        : 0m
                };
            })
            .ToList();

        return report;
    }

    /// <summary>
    /// Parse the role filter string ("1" = Course Leader, "2" = Teacher) to an integer for SQL.
    /// Returns null if the role string is null or not a valid integer.
    /// </summary>
    private static int? ParseRoleFilter(string? role)
    {
        if (role != null && int.TryParse(role, out var roleId))
        {
            return roleId;
        }
        return null;
    }

    /// <summary>
    /// Calculate median from n1-n5 score distribution counts.
    /// Returns null if no responses.
    /// </summary>
    private static decimal? CalculateMedian(int n1, int n2, int n3, int n4, int n5)
    {
        var total = n1 + n2 + n3 + n4 + n5;
        if (total == 0) return null;

        int[] counts = [n1, n2, n3, n4, n5];
        var cumulative = 0;

        // For even n, average the two middle values; for odd, both positions are the same
        var leftPos = (total + 1) / 2;
        var rightPos = (total + 2) / 2;
        int? left = null;
        int? right = null;

        for (int i = 0; i < counts.Length; i++)
        {
            cumulative += counts[i];
            if (left == null && cumulative >= leftPos) left = i + 1;
            if (right == null && cumulative >= rightPos) right = i + 1;
            if (left != null && right != null) break;
        }

        if (left != null && right != null)
        {
            return (left.Value + right.Value) / 2m;
        }

        return null;
    }

    // ============================================
    // PDF Generation — Summary
    // ============================================

    public Task<byte[]> GenerateSummaryPdfAsync(EvalSummaryReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.4f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                    });
                    col.Item().PaddingVertical(6).Text("Evaluation Summary Report").SemiBold().FontSize(12);
                    col.Item().BorderBottom(1.5f).BorderColor(Colors.Black)
                        .PaddingBottom(3).Text(report.AcademicYear ?? report.TermName).SemiBold().FontSize(11);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();        // Instructor
                        columns.ConstantColumn(80);      // Weighted Avg
                        columns.ConstantColumn(80);      // Responses
                        columns.ConstantColumn(80);      // Enrolled
                    });

                    var hdrStyle = TextStyle.Default.FontSize(9).Bold().Underline();
                    table.Header(header =>
                    {
                        header.Cell().PaddingVertical(2).Text("Instructor").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Weighted Avg").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Responses").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Enrolled").Style(hdrStyle);
                    });

                    foreach (var dept in report.Departments)
                    {
                        // Department header
                        table.Cell().ColumnSpan(4).Background("#E0E0E0").PaddingVertical(3)
                            .PaddingLeft(4).Text(text =>
                            {
                                text.Span(dept.Department).Bold();
                                text.Span($"  (Avg: {dept.DepartmentAverage:F2}, Responses: {dept.TotalResponses})").FontSize(9);
                            });

                        foreach (var instructor in dept.Instructors)
                        {
                            table.Cell().PaddingVertical(2).PaddingLeft(8).Text(instructor.Instructor);
                            table.Cell().PaddingVertical(2).AlignCenter().Text(instructor.WeightedAverage.ToString("F2"));
                            table.Cell().PaddingVertical(2).AlignCenter().Text(instructor.TotalResponses.ToString());
                            table.Cell().PaddingVertical(2).AlignCenter().Text(instructor.TotalEnrolled.ToString());
                        }
                    }
                });

                page.Footer().Column(col =>
                {
                    AddPdfFilterLine(col.Item(),
                        ("Dept", report.FilterDepartment),
                        ("Person", report.FilterPersonId?.ToString()),
                        ("Role", report.FilterRole));
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

    // ============================================
    // PDF Generation — Detail
    // ============================================

    public Task<byte[]> GenerateDetailPdfAsync(EvalDetailReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.4f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("UCD School of Veterinary Medicine").Bold().FontSize(11);
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("d MMMM yyyy")).Bold().FontSize(11);
                    });
                    col.Item().PaddingVertical(6).Text("Evaluation Detail Report").SemiBold().FontSize(12);
                    col.Item().BorderBottom(1.5f).BorderColor(Colors.Black)
                        .PaddingBottom(3).Text(report.AcademicYear ?? report.TermName).SemiBold().FontSize(11);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();        // Course
                        columns.ConstantColumn(35);      // Role
                        columns.ConstantColumn(55);      // Average
                        columns.ConstantColumn(55);      // Median
                        columns.ConstantColumn(70);      // Responses
                        columns.ConstantColumn(60);      // Enrolled
                    });

                    var hdrStyle = TextStyle.Default.FontSize(8).Bold().Underline();
                    table.Header(header =>
                    {
                        header.Cell().PaddingVertical(2).Text("Course").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Role").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Average").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Median").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Responses").Style(hdrStyle);
                        header.Cell().PaddingVertical(2).AlignCenter().Text("Enrolled").Style(hdrStyle);
                    });

                    foreach (var dept in report.Departments)
                    {
                        // Department header
                        table.Cell().ColumnSpan(6).Background("#D0D0D0").PaddingVertical(3)
                            .PaddingLeft(4).Text(text =>
                            {
                                text.Span(dept.Department).Bold();
                                text.Span($"  (Dept Avg: {dept.DepartmentAverage:F2})").FontSize(8);
                            });

                        foreach (var instructor in dept.Instructors)
                        {
                            // Instructor sub-header
                            table.Cell().ColumnSpan(6).Background("#E8E8E8").PaddingVertical(2)
                                .PaddingLeft(8).Text(text =>
                                {
                                    text.Span(instructor.Instructor).SemiBold();
                                    text.Span($"  (Avg: {instructor.InstructorAverage:F2}");
                                    if (instructor.InstructorMedian.HasValue)
                                    {
                                        text.Span($", Median: {instructor.InstructorMedian.Value:F2}");
                                    }
                                    text.Span(")");
                                });

                            foreach (var course in instructor.Courses)
                            {
                                table.Cell().PaddingVertical(1.5f).PaddingLeft(16).Text(course.Course);
                                table.Cell().PaddingVertical(1.5f).AlignCenter().Text(course.Role);
                                table.Cell().PaddingVertical(1.5f).AlignCenter().Text(course.Average.ToString("F2"));
                                table.Cell().PaddingVertical(1.5f).AlignCenter()
                                    .Text(course.Median.HasValue ? course.Median.Value.ToString("F2") : "-");
                                table.Cell().PaddingVertical(1.5f).AlignCenter().Text(course.NumResponses.ToString());
                                table.Cell().PaddingVertical(1.5f).AlignCenter().Text(course.NumEnrolled.ToString());
                            }
                        }
                    }
                });

                page.Footer().Column(col =>
                {
                    AddPdfFilterLine(col.Item(),
                        ("Dept", report.FilterDepartment),
                        ("Person", report.FilterPersonId?.ToString()),
                        ("Role", report.FilterRole));
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
