using Microsoft.CodeAnalysis;
using System.Numerics;
using Viper.Areas.Curriculum.Services;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    /// <summary>
    /// A base class to contain common properties for student assessments. Derived by specific assessment types (epas, dops, etc.)
    /// </summary>
    public class StudentAssessment
    {
        public virtual string AssessmentType { get; } = null!; //should be overridden by derived classes

        //assessment level
        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public int LevelValue { get; set; }

        //Encounter Info
        public int EncounterId { get; set; }
        public int EncounterType { get; set; }
        public DateTime EncounterDate { get; set; }
        public DateTime EnteredOn { get; set; }
        public string? EncounterComment { get; set; }
        public string? EditComment { get; set; }

        //Objects linked to the encounter
        public int StudentUserId { get; set; }
        public string StudentName { get; set; } = null!;
        public string StudentMailId { get; set; } = null!;
        public int EnteredBy { get; set; }
        public string EnteredByName { get; set; } = null!;

        //can the logged in user edit
        public bool? Editable { get; set; }

        //not common properties - should go into derived type when it is created
        //public int? RoleId { get; set; }
        //public int? PatientId { get; set; }
        //public int? ClinicianId { get; set; }
        //public string? VisitNumber { get; set; }
        //public string? PresentingComplaint { get; set; }
        //public string? Diagnosis { get; set; }
        //public int? TermCode { get; set; }
        //public bool Complete { get; set; }
        //public string? StudentLevel { get; set; }

        public StudentAssessment()
        {

        }

        public StudentAssessment(Level level, Encounter enc)
        {
            LevelId = level.LevelId;
            LevelName = level.LevelName;
            LevelValue = level.Order;
            EncounterId = enc.EncounterId;
            StudentUserId = enc.StudentUserId;
            EncounterType = enc.EncounterType;
            EncounterDate = enc.EncounterDate;
            EnteredOn = enc.EnteredOn;
            EnteredBy = enc.EnteredBy;
            EncounterComment = enc.Comment;
            EditComment = enc.EditComment;
            StudentName = enc.Student.FullName;
            StudentMailId = enc.Student.MailId;
            EnteredByName = enc.EnteredByPerson.FullName;
        }
    }
}
