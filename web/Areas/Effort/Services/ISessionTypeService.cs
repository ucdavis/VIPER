using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for session type operations.
/// </summary>
public interface ISessionTypeService
{
    /// <summary>
    /// Get all session types with usage counts.
    /// </summary>
    /// <param name="activeOnly">If true, only return active session types.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<SessionTypeDto>> GetSessionTypesAsync(bool activeOnly = false, CancellationToken ct = default);

    /// <summary>
    /// Get a single session type by ID.
    /// </summary>
    Task<SessionTypeDto?> GetSessionTypeAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Create a new session type.
    /// </summary>
    Task<SessionTypeDto> CreateSessionTypeAsync(CreateSessionTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing session type.
    /// </summary>
    Task<SessionTypeDto?> UpdateSessionTypeAsync(string id, UpdateSessionTypeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a session type. Only succeeds if no records reference it.
    /// </summary>
    Task<bool> DeleteSessionTypeAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Check if a session type can be deleted (no records reference it).
    /// </summary>
    Task<bool> CanDeleteSessionTypeAsync(string id, CancellationToken ct = default);
}
