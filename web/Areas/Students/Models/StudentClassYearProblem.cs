namespace Viper.Areas.Students.Models
{
    public class StudentClassYearProblem : Student
    {
        public int? ExpectedClassYear { get; set; }
        public string Problems { get; set; } = string.Empty;

        public StudentClassYearProblem(Student s) : base(s) { }
    }
}
