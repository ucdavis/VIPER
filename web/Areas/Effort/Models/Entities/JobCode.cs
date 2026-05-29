namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents job codes for effort tracking.
/// Maps to effort.JobCodes table.
/// </summary>
public class JobCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IncludeClinSchedule { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
