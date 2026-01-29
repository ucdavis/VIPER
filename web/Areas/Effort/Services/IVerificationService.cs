using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for effort verification operations.
/// Handles self-service verification by instructors and admin email notifications.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// Get the current user's effort data for self-service verification.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The instructor's effort data, or null if not found.</returns>
    Task<MyEffortDto?> GetMyEffortAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Verify the current user's effort for a term.
    /// Sets the EffortVerified timestamp on the EffortPerson record.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    Task<VerificationResult> VerifyEffortAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if an instructor can verify their effort.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating whether verification is possible.</returns>
    Task<CanVerifyResult> CanVerifyAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Send a verification reminder email to an instructor.
    /// </summary>
    /// <param name="personId">The instructor's person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<EmailSendResult> SendVerificationEmailAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Send verification emails to all unverified instructors in a department.
    /// </summary>
    /// <param name="departmentCode">The department code.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with counts and any failures.</returns>
    Task<BulkEmailResult> SendBulkVerificationEmailsAsync(string departmentCode, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get the email history for an instructor (verification emails sent).
    /// </summary>
    /// <param name="personId">The instructor's person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of email history entries.</returns>
    Task<List<EmailHistoryDto>> GetEmailHistoryAsync(int personId, int termCode, CancellationToken ct = default);
}
