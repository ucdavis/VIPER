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
        public string? ClassLevel { get; set; } = null!;
        public int? ClassYear { get; set; }
        public string Email => MailId + "@ucdavis.edu";
        public bool CurrentClassYear { get; set; }
        public bool Active { get; set; }

        public List<StudentClassYear>? ClassYears { get; set; }

        public Student()
        {

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
            Active = p.CurrentStudent || p.FutureStudent;
            CurrentClassYear = false;
        }

    }

}
