using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class Encounter
{
    public int EncounterId { get; set; }

    public int StudentUserId { get; set; }

    public int EncounterType { get; set; }

    public DateTime EncounterDate { get; set; }

    public DateTime EnteredOn { get; set; }

    public int EnteredBy { get; set; }

    public int? RoleId { get; set; }

    public int? OfferingId { get; set; }

    public int? ServiceId { get; set; }

    public int? PatientId { get; set; }

    public int? ClinicianId { get; set; }

    public string? VisitNumber { get; set; }

    public string? PresentingComplaint { get; set; }

    public string? Diagnosis { get; set; }

    public int? TermCode { get; set; }

    public bool Complete { get; set; }

    public string? StudentLevel { get; set; }

    public string? Comment { get; set; }

    public string? EditComment { get; set; }

    public int? LevelId { get; set; }

    public int? EpaId { get; set; }

    public virtual Person? Clinician { get; set; }

    public virtual ICollection<CtsAudit> CtsAudits { get; set; } = new List<CtsAudit>();

    public virtual ICollection<EncounterInstructor> EncounterInstructors { get; set; } = new List<EncounterInstructor>();

    public virtual Person EnteredByNavigation { get; set; } = null!;

    public virtual Epa? Epa { get; set; }

    public virtual Level? Level { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual ICollection<StudentCompetency> StudentCompetencies { get; set; } = new List<StudentCompetency>();

    public virtual ICollection<StudentEpa> StudentEpas { get; set; } = new List<StudentEpa>();

    public virtual Person StudentUser { get; set; } = null!;
}
