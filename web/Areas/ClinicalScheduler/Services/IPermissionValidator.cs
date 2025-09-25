using Viper.Models.AAUD;

namespace Viper.Areas.ClinicalScheduler.Services;

/// <summary>
/// High-level permission validation service that encapsulates complex permission logic
/// </summary>
public interface IPermissionValidator
{
    /// <summary>
    /// Validates that the current user has permission to edit a schedule and returns the user
    /// </summary>
    /// <param name="rotationId">Rotation ID</param>
    /// <param name="targetMothraId">Target user's mothra ID (for own schedule editing)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user if authorized</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if user lacks permission</exception>
    Task<AaudUser> ValidateEditPermissionAndGetUserAsync(int rotationId, string targetMothraId, CancellationToken cancellationToken = default);
}
