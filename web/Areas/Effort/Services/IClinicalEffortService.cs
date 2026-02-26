using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for clinical effort report generation.
/// Reports show instructors with clinical percent assignments and their effort data,
/// filtered by clinical type (VMTH=1, CAHFS=25).
/// </summary>
public interface IClinicalEffortService
{
    /// <summary>
    /// Generate a clinical effort report for an academic year (e.g., "2024-2025").
    /// Uses effort.sp_merit_clinical_percent which returns dynamic pivot columns.
    /// </summary>
    Task<ClinicalEffortReport> GetClinicalEffortReportAsync(
        string academicYear,
        int clinicalType,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a PDF document from a clinical effort report.
    /// Landscape legal-size, grouped by job group description.
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(ClinicalEffortReport report);
}
