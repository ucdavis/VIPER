namespace Viper.Models.Crest;

/// <summary>
/// CREST course session offering view data.
/// Maps to vw_vm_course_session_offering in CREST database.
/// </summary>
public class CrestCourseSessionOffering
{
    public int CourseId { get; set; }
    public int EdutaskOfferId { get; set; }
    public string AcademicYear { get; set; } = null!;
    public string? Crn { get; set; }
    public string? SsaCourseNum { get; set; }
    public string? SessionType { get; set; }
    public string? SeqNumb { get; set; }
    public DateTime? FromDate { get; set; }
    public string? FromTime { get; set; }
    public DateTime? ThruDate { get; set; }
    public string? ThruTime { get; set; }
}
