using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Classes.SQLContext;
using Viper.Models.Students;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Viper.Areas.Students.Services
{
    public class StudentList
    {
        private readonly VIPERContext _context;
        public StudentList(VIPERContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get a list of students using the given parameters. Calculate their current class year and all class years they've been a part of.
        /// </summary>
        /// <param name="classLevel"></param>
        /// <param name="classYear"></param>
        /// <param name="currentYearsOnly"></param>
        /// <param name="includeRoss"></param>
        /// <param name="activeYearOnly"></param>
        /// <returns></returns>
        public async Task<List<Student>> GetStudents(string? classLevel = null, int? classYear = null, int? personId = null,
                bool currentYearsOnly = true, bool includeRoss = true, bool activeYearOnly = true)
        {
            var termCodeService = new TermCodeService(_context);
            int termCode = (await termCodeService.GetTerms(current: true)).First().TermCode;
            if (!string.IsNullOrEmpty(classLevel))
            {
                var gradYearFromClassLevel = GradYearClassLevel.GetGradYear(classLevel, termCode);
                if (classYear != null && classYear != gradYearFromClassLevel)
                {
                    return new List<Student>();
                }
                if (classYear == null)
                {
                    classYear = gradYearFromClassLevel;
                }
            }

            List<int> activeClassYear = await termCodeService.GetActiveClassYears(termCode);
            var q = _context.StudentClassYears
                .Include(q => q.ClassYearLeftReason)
                .Include(q => q.Student)
                .ThenInclude(q => q!.StudentInfo)
                .Where(q => includeRoss || !q.Ross);

            //include only the active class year for this student
            if (activeYearOnly)
            {
                if (classYear != null)
                {
                    q = q.Where(q => q.ClassYear == classYear);
                }
                q = q.Where(q => q.Active);
            }
            //include all class years, as long as at least one of them is the given year
            else if (classYear != null)
            {
                //get all students that have a class year entry in this year
                var studentPersonIds = _context.StudentClassYears.Where(g => g.ClassYear == classYear).Select(g => g.PersonId).ToList();
                q = q.Where(q => q.Student != null && studentPersonIds.Contains(q.Student.PersonId));
            }

            if(personId != null)
            {
                q = q.Where(q => q.PersonId == personId);
            }

            //include only active class years, i.e. V1-V4 students now
            if (currentYearsOnly)
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
            foreach (var s in studentList)
            {
                s.CurrentClassYear = s.ClassYear != null && activeClassYear.Contains((int)s.ClassYear);
            }
            return studentList;
        }

		public async Task<Student?> GetStudent(int personId)
		{
            return (await GetStudents(personId: personId)).FirstOrDefault();
		}

		/// <summary>
		/// Get students from AAUD based on term code and class level
		/// </summary>
		/// <param name="termCode"></param>
		/// <param name="classLevel"></param>
		/// <returns></returns>
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
                .ThenByDescending(g => g.ClassYear)
                .AsNoTracking()
                .ToList();
            foreach (var student in studentList)
            {
                var studentGradYears = gradYears
                    .Where(g => g.PersonId == student.PersonId)
                    .ToList();
                student.ClassYears = new List<StudentClassYear>();
                foreach (var cy in studentGradYears)
                {
                    student.ClassYears.Add(new StudentClassYear(cy));
                }
                student.ClassYear = studentGradYears?.FirstOrDefault()?.ClassYear;
            }
            return studentList;
        }

        /// <summary>
        /// Return a list of students, either for a single class year or for all active class years, 
        /// that appear to be in a different class year based on their class level for the current term
        /// </summary>
        /// <returns></returns>
        public async Task<List<StudentClassYearProblem>> GetStudentClassYearProblems(int? classYear = null)
        {
            var termCodeService = new TermCodeService(_context);
            int termCode = (await termCodeService.GetTerms(current: true)).First().TermCode;

            if (classYear != null)
            {
                return await GetStudentClassYearProblemsForOneYear((int)classYear, termCode);
            }

            List<int> activeClassYears = await termCodeService.GetActiveClassYears(termCode);
            List<StudentClassYearProblem> students = new();
            foreach (var cy in activeClassYears)
            {
                var classYearProblems = await GetStudentClassYearProblemsForOneYear(cy, termCode);
                students = students
                    .Concat(classYearProblems.Where(newProblem => !students.Any(s => s.PersonId == newProblem.PersonId)))
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();
            }

            return students;
        }

        /// <summary>
        /// Return a list of students for a single class year that appear to be in a different class year based on 
        /// their class level for the current term, or should be in this class year but aren't
        /// </summary>
        /// <returns></returns>
        private async Task<List<StudentClassYearProblem>> GetStudentClassYearProblemsForOneYear(int classYear, int currentTerm)
        {
            List<StudentClassYearProblem> studentProblems = new();

            //Get all students in this class year currently
            var studentsInClassYear = await GetStudents(classYear: classYear);

            //Get the best class level and term code to use, based on the class year and current term
            var (termCode, classLevel) = GradYearClassLevel.GetTermCodeAndClassLevelForGradYear(classYear, currentTerm);

            //Get all students that, in the current term, should be considered a part of this class year
            var expectedStudents = await GetStudentsByTermCodeAndClassLevel(termCode, classLevel);

            //For each student in this class year currently, check that they are in the expected students
            foreach (var s in studentsInClassYear)
            {
                if (!expectedStudents.Any(e => e.PersonId == s.PersonId))
                {
                    var expected = (s.ClassLevel != null && s.TermCode != null) ? GradYearClassLevel.GetGradYear(s.ClassLevel, (int)s.TermCode) : null;
                    studentProblems.Add(new StudentClassYearProblem(s)
                    {
                        ExpectedClassYear = expected,
                        Problems = expected != null
                            ? string.Format("Student is expected in class year {0}.", expected)
                            : string.Format("Student is not expected in class year {0}.", s.ClassYear)
                    });
                }
            }

            //For each student that should be considered part of this class year based on class level and term code, check that they are in the class year.
            foreach (var e in expectedStudents)
            {
                if (!studentsInClassYear.Any(s => s.PersonId == e.PersonId) && !studentProblems.Any(p => p.PersonId == e.PersonId))
                {
                    var expected = (e.ClassLevel != null && e.TermCode != null) ? GradYearClassLevel.GetGradYear(e.ClassLevel, (int)e.TermCode) : null;
                    studentProblems.Add(new StudentClassYearProblem(e)
                    {
                        ExpectedClassYear = expected,
                        Problems = expected != null
                            ? string.Format("Student is expected in class year {0}.", expected)
                            : string.Format("Student is not expected in class year {0}.", e.ClassYear)
                    });
                }
            }
            return studentProblems;
        }

        /// <summary>
        /// Given a list of grad years for a student, create a student object with all class years they've been a part of
        /// </summary>
        /// <param name="studentGradYears"></param>
        /// <param name="activeYearOnly"></param>
        /// <returns></returns>
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
                        TermCode = std.Student?.StudentInfo?.TermCode,
                        ClassYear = std.ClassYear,
                        Active = std?.Student?.Current == 1 || std?.Student?.Future == 1
                    };

                    List<StudentClassYear> classYears = new List<StudentClassYear>();
                    if (!activeYearOnly)
                    {
                        classYears = student.ToList();
                    }
                    else
                    {
                        classYears = student.Where(s => s.Active).ToList();
                    }

                    newStd.ClassYears = new List<StudentClassYear>();
                    foreach (var cy in classYears)
                    {
                        newStd.ClassYears.Add(new StudentClassYear(cy));
                    }

                    students.Add(newStd);
                }
            }

            return students;
        }

	}
}
