namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Result of a harvest operation.
/// </summary>
public class HarvestResultDto
{
    public bool Success { get; set; }
    public int TermCode { get; set; }
    public DateTime? HarvestedDate { get; set; }
    public HarvestSummary Summary { get; set; } = new();
    public List<HarvestWarning> Warnings { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
