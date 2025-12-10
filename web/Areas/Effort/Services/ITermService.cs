using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for term-related operations.
/// </summary>
public interface ITermService
{
    /// <summary>
    /// Get all terms with effort status.
    /// </summary>
    Task<List<TermDto>> GetTermsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a specific term by term code.
    /// </summary>
    Task<TermDto?> GetTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get the current open term (most recent with status "Opened").
    /// </summary>
    Task<TermDto?> GetCurrentTermAsync(CancellationToken ct = default);

    /// <summary>
    /// Get term name from term code using TermCodeService.
    /// </summary>
    string GetTermName(int termCode);
}
