namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Response DTO for can-delete check endpoints.
/// </summary>
public class CanDeleteResponse
{
    /// <summary>
    /// Whether the entity can be deleted. False when there are related records preventing deletion.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// The number of related records referencing this entity.
    /// </summary>
    public int UsageCount { get; set; }
}
