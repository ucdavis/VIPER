using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class VwCourseSessionOffering
{
    public int Courseid { get; set; }

    public int Sessionid { get; set; }

    public string Blocktype { get; set; } = null!;

    public string Academicyear { get; set; } = null!;

    public string? Crn { get; set; }

    public string? Ssacoursenum { get; set; }

    public string? Sessiontype { get; set; }

    public string Title { get; set; } = null!;

    public DateTime? Fromdate { get; set; }

    public DateTime? Thrudate { get; set; }

    public string? Fromtime { get; set; }

    public string? Thrutime { get; set; }

    public string? Room { get; set; }

    public int? TypeOrder { get; set; }

    public string? Studentgroup { get; set; }

    public int EduTaskOfferid { get; set; }

    public string? ReadingRequired { get; set; }

    public string? ReadingRecommended { get; set; }

    public string? ReadingSessionmaterial { get; set; }

    public string? Keyconcept { get; set; }

    public string? Equipment { get; set; }

    public string? Notes { get; set; }

    public DateTime? Modifydate { get; set; }

    public int? Modifypersonid { get; set; }

    public int? PaceOrder { get; set; }

    public string? Vocabulary { get; set; }

    public int? Supplemental { get; set; }

    public string? OfferingNotes { get; set; }

    public string? SeqNumb { get; set; }

    public int? SvmBlockId { get; set; }

    public int Createpersonid { get; set; }

    public string? MediasiteSchedule { get; set; }

    public string? MediasitePresentation { get; set; }

    public bool? MediasiteLive { get; set; }

    public string? MediasiteTemplate { get; set; }

    public int? CanvasCourseId { get; set; }

    public int? CanvasEventId { get; set; }
}
