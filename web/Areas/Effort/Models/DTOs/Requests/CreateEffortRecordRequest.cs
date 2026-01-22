namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new effort record.
/// </summary>
public class CreateEffortRecordRequest
{
    /// <summary>
    /// The person (instructor) ID.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// The term code.
    /// </summary>
    public required int TermCode { get; set; }

    /// <summary>
    /// The course ID.
    /// </summary>
    public required int CourseId { get; set; }

    /// <summary>
    /// The effort type ID (e.g., "LEC", "LAB", "CLI").
    /// </summary>
    public required string EffortTypeId { get; set; }

    /// <summary>
    /// The role ID.
    /// </summary>
    public required int RoleId { get; set; }

    /// <summary>
    /// The effort value (hours or weeks depending on effort type and term).
    /// </summary>
    public required int EffortValue { get; set; }
}
