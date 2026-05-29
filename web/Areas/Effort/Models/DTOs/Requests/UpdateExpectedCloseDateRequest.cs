namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating the expected close date of a term.
/// </summary>
public class UpdateExpectedCloseDateRequest
{
    /// <summary>
    /// The expected close date. Set to null to clear.
    /// </summary>
    public DateTime? ExpectedCloseDate { get; set; }
}
