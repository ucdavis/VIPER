using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;
using Viper.Models.Students;

namespace Viper.Areas.Students.Services
{
	public class StudentList
    {
        private readonly VIPERContext _context;
        public StudentList(VIPERContext context) { 
            _context = context;
        }

        public async Task<List<Student>> GetStudents(string? classLevel = null, int? classYear = null, bool currentYearsOnly = true,
                bool includeRoss = true, bool activeYearOnly = true)
        {
            var termCodeService = new TermCodeService(_context);
            int termCode = (await termCodeService.GetTerms(current: true)).First().TermCode;
            if (!string.IsNullOrEmpty(classLevel))
            {
                var gradYearFromClassLevel = GradYearClassLevel.GetGradYear(classLevel, termCode);
                if(classYear != null && classYear != gradYearFromClassLevel)
                {
                    return new List<Student>();
                }
                if(classYear == null)
                {
					classYear = gradYearFromClassLevel;
                }
            }

            List<int> activeClassYear = await termCodeService.GetActiveClassYears(termCode);
            var q = _context.StudentClassYears
                .Include(q => q.ClassYearLeftReason)
                .Include(q => q.Student)
                .ThenInclude(q => q.StudentInfo)
                .Where(q => includeRoss || !q.Ross);

            if (activeYearOnly)
            {
                if (classYear != null)
                {
                    q = q.Where(q => q.ClassYear == classYear);
                }
                q = q.Where(q => q.Active);
            }
			else if (classYear != null)
			{
                //get all students that have a class year entry in this year
                var studentPersonIds = _context.StudentClassYears.Where(g => g.ClassYear == classYear).Select(g => g.PersonId).ToList();
				q = q.Where(q => q.Student != null && studentPersonIds.Contains(q.Student.PersonId));
			}
            if(currentYearsOnly)
            {
                q = q.Where(q => activeClassYear.Contains(q.ClassYear));
            }

            q = q
                .OrderBy(q => q.Student == null ? "" : q.Student.LastName)
                .ThenBy(q => q.Student == null ? "" : q.Student.FirstName)
                .ThenBy(q => q.Student == null ? 0 : q.Student.PersonId)
                .ThenBy(q => q.Active ? 1 : 0)
                .ThenBy(q => q.ClassYear);
                
            var studentList = CreateStudentListFromStudentGradYears(await q.ToListAsync(), activeYearOnly: activeYearOnly);
            foreach(var s in studentList)
            {
                s.CurrentClassYear = s.ClassYear != null && activeClassYear.Contains((int)s.ClassYear);
            }
            return studentList;
        }

        public async Task<List<Student>> GetStudentsByTermCodeAndClassLevel(int termCode, string classLevel)
        {
            //Get students based on AAUD Student info for the given term
            var students = _context.People
                .Include(p => p.StudentInfo)
                .Where(p => p.StudentInfo != null && p.StudentInfo.TermCode == termCode && p.StudentInfo.ClassLevel == classLevel)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName);
            var studentList = await students
                .Select(p => new Student(p))
                .ToListAsync();
            //get student grad years
            var studentIds = studentList.Select(s => s.PersonId).ToList();
            var gradYears = _context.StudentClassYears
                .Include(s => s.ClassYearLeftReason)
                .Where(s => studentIds.Contains(s.PersonId))
                .OrderBy(g => g.Active ? 0 : 1)
                .ThenByDescending(g => g.ClassYear);
            foreach (var student in studentList)
            {
                var studentGradYears = gradYears
					.Where(g => g.PersonId == student.PersonId)
					.ToList();
                student.ClassYears = studentGradYears;
                student.ClassYear = studentGradYears?.FirstOrDefault()?.ClassYear;
            }
            return studentList;
        }

		private List<Student> CreateStudentListFromStudentGradYears(List<StudentClassYear> studentGradYears, bool activeYearOnly = true)
        {
            var students = new List<Student>();
	        foreach (var student in studentGradYears.GroupBy(s => s.PersonId))
            {
                var std = student.First();
			    if (std.Student != null)
                {
                    var newStd = new Student()
                    {
                        PersonId = std.PersonId,
                        MailId = std.Student.MailId,
                        FirstName = std.Student.FirstName,
                        LastName = std.Student.LastName,
                        MiddleName = std.Student.MiddleName,
                        FullName = std.Student.FullName,
                        ClassLevel = std.Student?.StudentInfo?.ClassLevel,
                        ClassYear = std.ClassYear,
                        Active = std?.Student?.Current == 1 || std?.Student?.Future == 1
				    };
					if (!activeYearOnly)
                    {
                        newStd.ClassYears = student.ToList();
					}
                    else
                    {
                        newStd.ClassYears = student.Where(s => s.Active).ToList();
                    }
                    students.Add(newStd);
                }
            }

            return students;
        }
    }
}
