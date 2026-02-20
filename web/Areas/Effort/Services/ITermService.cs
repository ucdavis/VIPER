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

    /// <summary>
    /// Get the term type (e.g., "Fall Semester", "Fall Quarter") from the VIPER terms table.
    /// </summary>
    Task<string?> GetTermTypeAsync(int termCode, CancellationToken ct = default);

    // Term Management Operations (requires ManageTerms permission)

    /// <summary>
    /// Create a new term with initial status "Created".
    /// </summary>
    Task<TermDto> CreateTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Delete a term. Only succeeds if no courses, persons, or records exist.
    /// </summary>
    /// <returns>True if deleted, false if term has related data.</returns>
    Task<bool> DeleteTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Open a term for effort entry. Sets status to "Opened" and OpenedDate.
    /// </summary>
    Task<TermDto?> OpenTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Close a term. Validates no courses have zero enrollment.
    /// </summary>
    /// <returns>Tuple of (success, errorMessage). Error message contains count of zero-enrollment courses if validation fails.</returns>
    Task<(bool Success, string? ErrorMessage)> CloseTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Reopen a closed term. Clears ClosedDate and sets status back to "Opened".
    /// </summary>
    Task<TermDto?> ReopenTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Revert an open term back to harvested/created state. Clears OpenedDate.
    /// </summary>
    Task<TermDto?> UnopenTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if a term can be deleted (no related courses, persons, or records).
    /// </summary>
    Task<bool> CanDeleteTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if a term can be closed (no courses with zero enrollment).
    /// </summary>
    /// <returns>Tuple of (canClose, zeroEnrollmentCount).</returns>
    Task<(bool CanClose, int ZeroEnrollmentCount)> CanCloseTermAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get future terms from vwTerms that are not yet in the Effort system.
    /// </summary>
    Task<List<AvailableTermDto>> GetAvailableTermsAsync(CancellationToken ct = default);
}
