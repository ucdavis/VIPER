using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public interface ISabbaticalService
{
    /// <summary>
    /// Get sabbatical exclusion data for a person.
    /// Returns null if no record exists.
    /// </summary>
    Task<SabbaticalDto?> GetByPersonIdAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Resolve a person's effort department from the most recent EffortPerson record.
    /// Used for department-scoped authorization checks.
    /// </summary>
    Task<string?> GetPersonDepartmentAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Upsert sabbatical exclusion data for a person.
    /// Updates existing record or inserts new one.
    /// </summary>
    Task<SabbaticalDto> SaveAsync(
        int personId,
        string? excludeClinicalTerms,
        string? excludeDidacticTerms,
        int modifiedBy,
        CancellationToken ct = default);
}
