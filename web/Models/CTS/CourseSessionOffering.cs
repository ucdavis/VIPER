namespace Viper.Models.CTS
{
    public class CourseSessionOffering
    {
        public int CourseId { get; set; }
        public int SessionId { get; set; }
        public string BlockType { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public string? Crn { get; set; }
        public string? SsaCourseNum { get; set; }
        public string? SessionType { get; set; }
        public string Title { get; set; } = null!;
        public DateTime? FromDate { get; set; }
        public DateTime? ThruDate { get; set; }
        public string? FromTime { get; set; }
        public string? ThruTime { get; set; }
        public string? Room { get; set; }
        public int? TypeOrder { get; set; }
        public string? StudentGroup { get; set; }
        public int EduTaskOfferid { get; set; }
        public string? ReadingRequired { get; set; }
        public string? ReadingRecommended { get; set; }
        public string? ReadingSessionMaterial { get; set; }
        public string? KeyConcept { get; set; }
        public string? Equipment { get; set; }
        public string? Notes { get; set; }
        public DateTime? ModifyDate { get; set; }
        public int? ModifyPersonId { get; set; }
        public int? PaceOrder { get; set; }
        public string? Vocabulary { get; set; }
        public int? Supplemental { get; set; }
        public string? OfferingNotes { get; set; }
        public string? SeqNumb { get; set; }
        public int? SvmBlockId { get; set; }
        public int CreatePersonId { get; set; }
        public string? MediasiteSchedule { get; set; }
        public string? MediasitePresentation { get; set; }
        public bool? MediasiteLive { get; set; }
        public string? MediasiteTemplate { get; set; }
        public int? CanvasCourseId { get; set; }
        public int? CanvasEventId { get; set; }
    }
}
