using System.ComponentModel.DataAnnotations.Schema;
using Viper.Models.VIPER;

namespace Viper.Models.Students
{
    public sealed class StudentClassYear
    {

        public int StudentClassYearId { get; set; }
        public int PersonId { get; set; }
        public int ClassYear { get; set; }
        public bool Active { get; set; }
        public bool Graduated { get; set; }
        public bool Ross { get; set; }
        public int? LeftTerm { get; set; }
        public int? LeftReason { get; set; }
        public DateTime Added { get; set; }
        public int? AddedBy { get; set; }
        public DateTime? Updated { get; set; }
        public int? UpdatedBy { get; set; }
        public string? Comment { get; set; }

        public Person? Student { get; set; }
        public ClassYearLeftReason? ClassYearLeftReason { get; set; }
        public Person? AddedByPerson { get; set; }
        public Person? UpdatedByPerson { get; set; }

        [NotMapped]
        public string? LeftReasonText
        {
            get
            {
                return ClassYearLeftReason?.Reason;
            }
        }

        public StudentClassYear()
        {

        }
        public StudentClassYear(StudentClassYear cy)
        {
            StudentClassYearId = cy.StudentClassYearId;
            PersonId = cy.PersonId;
            ClassYear = cy.ClassYear;
            Active = cy.Active;
            Graduated = cy.Graduated;
            Ross = cy.Ross;
            LeftTerm = cy.LeftTerm;
            LeftReason = cy.LeftReason;
            Added = cy.Added;
            Updated = cy.Updated;
            AddedBy = cy.AddedBy;
            UpdatedBy = cy.UpdatedBy;
            Comment = cy.Comment;
            ClassYearLeftReason = cy.ClassYearLeftReason;
        }
    }
}
