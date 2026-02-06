using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Effort type is required")]
    public string EffortTypeId { get; set; } = string.Empty;

    /// <summary>
    /// The role ID.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Role is required")]
    public int RoleId { get; set; }

    /// <summary>
    /// The effort value (hours or weeks depending on effort type and term).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Effort value is required")]
    public int EffortValue { get; set; }

    /// <summary>
    /// The ModifiedDate from when the record was loaded.
    /// Used for optimistic concurrency - update will fail if record was modified by another user.
    /// For legacy records with null ModifiedDate, send null (first-edit-wins).
    /// </summary>
    public DateTime? OriginalModifiedDate { get; set; }
}
