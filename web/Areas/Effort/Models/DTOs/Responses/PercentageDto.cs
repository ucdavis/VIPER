namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for percentage assignment data returned from the API.
/// </summary>
public class PercentageDto
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public int PercentAssignTypeId { get; set; }

    /// <summary>
    /// Name of the percent assignment type (e.g., "Teaching", "Research").
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Class of the percent assignment type (Admin, Clinical, Other).
    /// </summary>
    public string TypeClass { get; set; } = string.Empty;

    public int? UnitId { get; set; }

    /// <summary>
    /// Name of the unit if assigned.
    /// </summary>
    public string? UnitName { get; set; }

    /// <summary>
    /// Optional modifier such as "Acting" or "Interim".
    /// </summary>
    public string? Modifier { get; set; }

    public string? Comment { get; set; }

    /// <summary>
    /// Percentage value displayed as 0-100.
    /// </summary>
    public decimal PercentageValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool Compensated { get; set; }

    /// <summary>
    /// True if EndDate is null or >= current date.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
