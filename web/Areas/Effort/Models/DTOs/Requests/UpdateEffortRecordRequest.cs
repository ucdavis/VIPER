namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing effort record.
/// Course cannot be changed; only effort type, role, and value can be updated.
/// </summary>
public class UpdateEffortRecordRequest
{
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

    /// <summary>
    /// The ModifiedDate from when the record was loaded.
    /// Used for optimistic concurrency - update will fail if record was modified by another user.
    /// For legacy records with null ModifiedDate, send null (first-edit-wins).
    /// </summary>
    public DateTime? OriginalModifiedDate { get; set; }
}
