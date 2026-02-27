using System.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public class ClinicalEffortService : BaseReportService, IClinicalEffortService
{
    private readonly ILogger<ClinicalEffortService> _logger;

    public ClinicalEffortService(
        EffortDbContext context,
        ILogger<ClinicalEffortService> logger)
        : base(context)
    {
        _logger = logger;
    }

    // ============================================
    // Clinical Effort Report
    // ============================================

    public async Task<ClinicalEffortReport> GetClinicalEffortReportAsync(
        string academicYear,
        int clinicalType,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCode = startYear.ToString();
        var clinicalTypeName = MapClinicalTypeName(clinicalType);

        var (rows, effortTypes) = await ExecuteClinicalEffortSpAsync(termCode, clinicalType, ct);

        if (rows.Count == 0)
        {
            return new ClinicalEffortReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                ClinicalType = clinicalType,
                ClinicalTypeName = clinicalTypeName
            };
        }

        // Fiscal year date range: July 1 of startYear to June 30 of startYear+1
        var fiscalStart = new DateOnly(startYear, 7, 1);
        var fiscalEnd = new DateOnly(startYear + 1, 6, 30);

        var clinicalPercents = await GetClinicalPercentsAsync(clinicalType, fiscalStart, fiscalEnd, ct);

        // Look up department for each instructor via ViperPerson → EffortPerson.
        // EffortPerson has composite PK (PersonId, TermCode), so filter to the
        // academic year's terms and pick the most recent term's department.
        var mothraIds = rows.Select(r => r.MothraId).Distinct().ToList();
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var departmentJoin = await _context.ViperPersons
            .AsNoTracking()
            .Where(vp => mothraIds.Contains(vp.MothraId))
            .Join(_context.Persons.AsNoTracking()
                    .Where(ep => termCodes.Contains(ep.TermCode)),
                vp => vp.PersonId,
                ep => ep.PersonId,
                (vp, ep) => new { vp.MothraId, ep.EffortDept, ep.TermCode })
            .ToListAsync(ct);
        var departmentMap = departmentJoin
            .GroupBy(x => x.MothraId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.TermCode).First().EffortDept,
                StringComparer.OrdinalIgnoreCase);

        // Build instructor rows with clinical percent and CLI ratio
        var instructorRows = rows.Select(row =>
        {
            var clinicalPercent = clinicalPercents.GetValueOrDefault(row.MothraId);
            var cliEffort = row.EffortByType.GetValueOrDefault("CLI");
            decimal? cliRatio = clinicalPercent > 0
                ? Math.Round(cliEffort / clinicalPercent, 1)
                : null;

            return new ClinicalEffortInstructorRow
            {
                MothraId = row.MothraId,
                Instructor = row.Instructor,
                Department = departmentMap.GetValueOrDefault(row.MothraId, ""),
                ClinicalPercent = (decimal)EffortConstants.ToDisplayPercent((double)clinicalPercent),
                EffortByType = row.EffortByType,
                CliRatio = cliRatio
            };
        }).ToList();

        // Group by JGDDesc (job group description)
        var jobGroups = instructorRows
            .GroupBy(r => rows.First(raw => raw.MothraId == r.MothraId).JGDDesc)
            .OrderBy(g => g.Key)
            .Select(g => new ClinicalEffortJobGroup
            {
                JobGroupDescription = g.Key,
                Instructors = g.OrderBy(i => i.Instructor).ToList()
            })
            .ToList();

        // Filter effort types to only AlwaysShowEffortTypes (matches legacy column set)
        var filteredEffortTypes = effortTypes
            .Where(t => AlwaysShowEffortTypes.Contains(t))
            .ToList();

        _logger.LogDebug(
            "Clinical effort report for {AcademicYear} type {ClinicalType}: {InstructorCount} instructors, {EffortTypeCount} effort types",
            LogSanitizer.SanitizeString(academicYear), clinicalType, instructorRows.Count, filteredEffortTypes.Count);

        return new ClinicalEffortReport
        {
            TermName = academicYear,
            AcademicYear = academicYear,
            ClinicalType = clinicalType,
            ClinicalTypeName = clinicalTypeName,
            EffortTypes = filteredEffortTypes,
            JobGroups = jobGroups
        };
    }

    // ── Clinical Percent Calculation ─────────────────────────────────

    /// <summary>
    /// Calculate weighted average clinical percent for each instructor over the fiscal year.
    /// Ports logic from legacy percentAssignment.cfc getAveragedPercent(): for each percent
    /// record, clamp to fiscal year, calculate months active, then divide by months that
    /// have assignment data (not by 12). Months with no assignment don't dilute the average.
    /// </summary>
    private async Task<Dictionary<string, decimal>> GetClinicalPercentsAsync(
        int clinicalType, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        // Get all percentage records for this clinical type that overlap the fiscal year
        var percentRecords = await _context.Percentages
            .AsNoTracking()
            .Where(p => p.PercentAssignTypeId == clinicalType
                && p.PercentageValue > 0
                && p.StartDate <= endDate.ToDateTime(TimeOnly.MinValue)
                && (p.EndDate == null || p.EndDate >= startDate.ToDateTime(TimeOnly.MinValue)))
            .Select(p => new
            {
                p.PersonId,
                p.PercentageValue,
                p.StartDate,
                p.EndDate
            })
            .ToListAsync(ct);

        if (percentRecords.Count == 0)
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        // Get PersonId -> MothraId mapping for all relevant persons
        var personIds = percentRecords.Select(p => p.PersonId).Distinct().ToList();
        var personMothraMap = await _context.ViperPersons
            .AsNoTracking()
            .Where(vp => personIds.Contains(vp.PersonId))
            .ToDictionaryAsync(vp => vp.PersonId, vp => vp.MothraId, ct);

        var fiscalStartDt = startDate.ToDateTime(TimeOnly.MinValue);
        var fiscalEndDt = endDate.ToDateTime(TimeOnly.MinValue);

        // Group by PersonId and calculate weighted average
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in percentRecords.GroupBy(p => p.PersonId))
        {
            if (!personMothraMap.TryGetValue(group.Key, out var mothraId))
            {
                continue;
            }

            // Track which months have assignment data (legacy: monthsWithPA)
            var monthsWithPA = new HashSet<(int Year, int Month)>();
            decimal weightedSum = 0;
            foreach (var record in group)
            {
                // Clamp start/end to fiscal year boundaries
                var effectiveStart = record.StartDate < fiscalStartDt ? fiscalStartDt : record.StartDate;
                var effectiveEnd = record.EndDate == null || record.EndDate > fiscalEndDt
                    ? fiscalEndDt
                    : record.EndDate.Value;

                // Calculate months active (inclusive of both start and end months)
                var months = ((effectiveEnd.Year - effectiveStart.Year) * 12)
                    + effectiveEnd.Month - effectiveStart.Month + 1;

                weightedSum += months * (decimal)record.PercentageValue;

                // Record which months have data
                var monthCursor = effectiveStart;
                while (monthCursor <= effectiveEnd)
                {
                    monthsWithPA.Add((monthCursor.Year, monthCursor.Month));
                    monthCursor = monthCursor.AddMonths(1);
                }
            }

            // Divide by months with assignment data, not by 12 (matches legacy getAveragedPercent)
            var denominator = monthsWithPA.Count > 0 ? monthsWithPA.Count : 12;
            result[mothraId] = weightedSum / denominator;
        }

        return result;
    }

    // ── SP Execution ─────────────────────────────────────────────────

    /// <summary>
    /// Raw row from sp_merit_clinical_percent before pivot processing.
    /// </summary>
    private sealed class ClinicalEffortRawRow
    {
        public string MothraId { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string JGDDesc { get; set; } = string.Empty;
        public Dictionary<string, decimal> EffortByType { get; set; } = new();
    }

    /// <summary>
    /// Execute effort.sp_merit_clinical_percent and read dynamic pivot columns.
    /// The SP returns MothraID, Instructor, JGDDesc, then dynamic effort type columns
    /// whose names vary based on what effort types exist in the data.
    /// </summary>
    private async Task<(List<ClinicalEffortRawRow> Rows, List<string> EffortTypes)> ExecuteClinicalEffortSpAsync(
        string termCode, int clinicalType, CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_merit_clinical_percent]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@type", clinicalType);

        await using var reader = await command.ExecuteReaderAsync(ct);

        // Read column schema to discover effort type columns (columns 3+ are dynamic)
        var effortTypes = new List<string>();
        for (int i = 3; i < reader.FieldCount; i++)
        {
            effortTypes.Add(reader.GetName(i));
        }

        var rows = new List<ClinicalEffortRawRow>();
        while (await reader.ReadAsync(ct))
        {
            var row = new ClinicalEffortRawRow
            {
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                JGDDesc = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                EffortByType = new Dictionary<string, decimal>()
            };

            for (int i = 3; i < reader.FieldCount; i++)
            {
                if (!await reader.IsDBNullAsync(i, ct))
                {
                    var value = reader.GetInt32(i);
                    if (value != 0)
                    {
                        row.EffortByType[effortTypes[i - 3]] = value;
                    }
                }
            }

            rows.Add(row);
        }

        _logger.LogDebug(
            "Clinical effort SP: {RowCount} rows, {EffortTypeCount} effort types for term {TermCode} type {ClinicalType}",
            rows.Count, effortTypes.Count, termCode, clinicalType);

        return (rows, effortTypes);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static string MapClinicalTypeName(int clinicalType) => clinicalType switch
    {
        1 => "VMTH",
        25 => "CAHFS",
        _ => $"Type {clinicalType}"
    };

    // ============================================
    // PDF Generation
    // ============================================

    public Task<byte[]> GenerateReportPdfAsync(ClinicalEffortReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedTypes = GetOrderedEffortTypes(report.EffortTypes);

        var compact = orderedTypes.Count > 14;
        var fontSize = compact ? 8f : 10f;
        var headerFontSize = compact ? 7f : 8.5f;
        var hMargin = compact ? 0.3f : 0.5f;
        var cellPadV = compact ? 1.5f : 2f;
        var instructorWidth = compact ? 110f : 150f;
        var percentWidth = compact ? 50f : 65f;
        var ratioWidth = compact ? 50f : 65f;
        var effortWidth = compact ? 24f : 32f;

        var document = Document.Create(container =>
        {
            foreach (var jobGroup in report.JobGroups)
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
                            row.RelativeItem().Text($"Merit & Promotion Report - Clinical Effort - {report.ClinicalTypeName}").SemiBold().FontSize(12);
                            row.RelativeItem().AlignRight().Text($"Academic Year {report.TermName}").SemiBold().FontSize(12);
                        });
                        col.Item().BorderBottom(1.5f).BorderColor(Colors.Black)
                            .PaddingBottom(3).Text(jobGroup.JobGroupDescription).SemiBold().FontSize(11);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(instructorWidth);
                            columns.ConstantColumn(percentWidth);
                            // CLI column
                            columns.ConstantColumn(effortWidth);
                            // CLI Ratio column
                            columns.ConstantColumn(ratioWidth);
                            // Other effort type columns (skip CLI since it's shown separately)
                            foreach (var type in orderedTypes)
                            {
                                if (string.Equals(type, "CLI", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                columns.ConstantColumn(effortWidth);
                            }
                        });

                        var hdrStyle = TextStyle.Default.FontSize(headerFontSize).Bold().Underline();
                        table.Header(header =>
                        {
                            header.Cell().PaddingVertical(cellPadV).AlignMiddle().Text("Instructor").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).AlignMiddle().AlignCenter().Text("Clinical %").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).AlignMiddle().AlignCenter().Text("CLI").Style(hdrStyle);
                            header.Cell().PaddingVertical(cellPadV).AlignCenter().Column(col =>
                            {
                                col.Item().AlignCenter().Text("CLI Ratio").Style(hdrStyle);
                                col.Item().AlignCenter().Text("CLI Weeks / Percent").Style(hdrStyle);
                            });
                            foreach (var type in orderedTypes)
                            {
                                if (string.Equals(type, "CLI", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                header.Cell().PaddingVertical(cellPadV).AlignMiddle().AlignCenter().Text(type).Style(hdrStyle);
                            }
                        });

                        foreach (var instructor in jobGroup.Instructors)
                        {
                            table.Cell().PaddingVertical(cellPadV).Text(instructor.Instructor);
                            table.Cell().PaddingVertical(cellPadV).AlignCenter().Text(instructor.ClinicalPercent.ToString("F1"));

                            var cliVal = instructor.EffortByType.GetValueOrDefault("CLI");
                            table.Cell().PaddingVertical(cellPadV).AlignCenter().Text(cliVal > 0 ? cliVal.ToString() : "0");

                            table.Cell().PaddingVertical(cellPadV).AlignCenter().Text(
                                instructor.CliRatio.HasValue ? instructor.CliRatio.Value.ToString("F1") : "-");

                            foreach (var type in orderedTypes)
                            {
                                if (string.Equals(type, "CLI", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                var val = instructor.EffortByType.GetValueOrDefault(type);
                                table.Cell().PaddingVertical(cellPadV).AlignCenter().Text(val > 0 ? val.ToString() : "0");
                            }
                        }
                    });

                    page.Footer().Column(col =>
                    {
                        AddPdfFilterLine(col.Item(), ("Type", report.ClinicalTypeName));
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
