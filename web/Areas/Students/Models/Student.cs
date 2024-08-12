using Viper.Models.Students;
using Viper.Models.VIPER;

namespace Viper.Areas.Students.Models
{
    public class Student
    {
        public int PersonId { get; set; }
        public string MailId { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string FullName { get; set; } = null!;
        public string? ClassLevel { get; set; }
        public int? TermCode { get; set; }
        public int? ClassYear { get; set; }
        public string Email => MailId + "@ucdavis.edu";
        public bool CurrentClassYear { get; set; }
        public bool Active { get; set; }

        public List<StudentClassYear>? ClassYears { get; set; }

        public Student()
        {

        }

        public Student(Student s)
        {
            PersonId = s.PersonId;
            MailId = s.MailId;
            LastName = s.LastName;
            FirstName = s.FirstName;
            MiddleName = s.MiddleName;
            FullName = s.FullName;
            ClassLevel = s.ClassLevel;
            TermCode = s.TermCode;
            ClassYear = s.ClassYear;
            CurrentClassYear = s.CurrentClassYear;
            Active = s.Active;
            ClassYears = s.ClassYears;
        }

        public Student(Person p)
        {
            PersonId = p.PersonId;
            MailId = p.MailId;
            LastName = p.LastName;
            FirstName = p.FirstName;
            MiddleName = p.MiddleName;
            FullName = p.FullName;
            ClassLevel = p.StudentInfo?.ClassLevel;
            TermCode = p.StudentInfo?.TermCode;
            Active = p.CurrentStudent || p.FutureStudent;
            CurrentClassYear = false;
        }

    }

}
