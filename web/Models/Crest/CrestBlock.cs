namespace Viper.Models.Crest;

/// <summary>
/// CREST block (course) data from tbl_Block.
/// Used to get the master list of courses before querying session offerings.
/// </summary>
public class CrestBlock
{
    public int EdutaskId { get; set; }
    public string AcademicYear { get; set; } = null!;
    public string? SsaCourseNum { get; set; }
}
