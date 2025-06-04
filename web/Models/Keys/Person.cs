using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class Person
{
    public int PersonId { get; set; }

    public string ClientId { get; set; } = null!;

    public string? IamId { get; set; }

    public string MothraId { get; set; } = null!;

    public string? LoginId { get; set; }

    public string? MailId { get; set; }

    public string? SpridenId { get; set; }

    public string? Pidm { get; set; }

    public string? EmployeeId { get; set; }

    public string? PpsId { get; set; }

    public int? VmacsId { get; set; }

    public string? VmcasId { get; set; }

    public string? UnexId { get; set; }

    public int? MivId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string FullName { get; set; } = null!;

    public bool CurrentStudent { get; set; }

    public bool FutureStudent { get; set; }

    public bool CurrentEmployee { get; set; }

    public bool FutureEmployee { get; set; }

    public int? StudentTerm { get; set; }

    public int? EmployeeTerm { get; set; }

    public int Current { get; set; }

    public int Future { get; set; }

    public bool? Ross { get; set; }

    public DateTime? Added { get; set; }

    public DateTime? Inactivated { get; set; }

    public virtual ICollection<CtsAudit> CtsAudits { get; set; } = new List<CtsAudit>();

    public virtual ICollection<Encounter> EncounterClinicians { get; set; } = new List<Encounter>();

    public virtual ICollection<Encounter> EncounterEnteredByNavigations { get; set; } = new List<Encounter>();

    public virtual ICollection<EncounterInstructor> EncounterInstructors { get; set; } = new List<EncounterInstructor>();

    public virtual ICollection<Encounter> EncounterStudentUsers { get; set; } = new List<Encounter>();

    public virtual ICollection<StudentClassYear> StudentClassYearAddedByNavigations { get; set; } = new List<StudentClassYear>();

    public virtual ICollection<StudentClassYear> StudentClassYearPeople { get; set; } = new List<StudentClassYear>();

    public virtual ICollection<StudentClassYear> StudentClassYearUpdatedByNavigations { get; set; } = new List<StudentClassYear>();

    public virtual ICollection<StudentCompetency> StudentCompetencyStudentUsers { get; set; } = new List<StudentCompetency>();

    public virtual ICollection<StudentCompetency> StudentCompetencyVerifiedByNavigations { get; set; } = new List<StudentCompetency>();

    public virtual ICollection<StudentContact> StudentContacts { get; set; } = new List<StudentContact>();
}
