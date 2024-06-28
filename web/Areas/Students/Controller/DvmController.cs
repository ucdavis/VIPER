using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.ComponentModel;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using Viper.Models.Students;
using Web.Authorization;

namespace Viper.Areas.Students.Controller
{
    [Route("/api/students/dvm")]
    [Permission(Allow = "SVMSecure.Students")]
    public class DvmStudentsController : ApiController
    {
        private readonly VIPERContext context;
        private readonly RAPSContext rapsContext;
        private readonly StudentList studentList;
        private readonly UserHelper userHelper;

        public DvmStudentsController(VIPERContext context, RAPSContext rapsContext)
        {
            this.context = context;
            this.rapsContext = rapsContext;
            studentList = new StudentList(this.context);
            userHelper = new();
        }

        /// <summary>
        /// Get DVM Students by class year or class level (for the current term)
        /// Note that including all class years is restricted
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="classLevel"></param>
        /// <param name="includeAllClassYears"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission(Allow = "SVMSecure.Students")]
        public async Task<ActionResult<IEnumerable<Models.Student>>> GetDvmStudents(int? classYear, string? classLevel, bool includeAllClassYears = false)
        {
            if (!userHelper.HasPermission(rapsContext, userHelper.GetCurrentUser(), "SVMSecure.SIS.AllStudents"))
            {
                includeAllClassYears = false;
            }
            var students = await studentList.GetStudents(classLevel: classLevel, classYear: classYear, activeYearOnly: !includeAllClassYears);
            return students;
        }

        /// <summary>
        /// Get all student class years records for a given class year. Returns students with the class year as their active year only.
        /// </summary>
        /// <param name="classYear"></param>
        /// <returns></returns>
        [HttpGet("byClassYear/{classYear}")]
        [Permission(Allow = "SVMSecure.SIS.AllStudents")]
        public async Task<ActionResult<IEnumerable<StudentClassYear>>> GetDvmStudentGradYears(int classYear)
        {
            //get students with all grad years
            var students = context.StudentClassYears
                .Where(s => s.ClassYear == classYear && s.Active)
                .OrderBy(s => s.Student == null ? "" : s.Student.LastName)
                .ThenBy(s => s.Student == null ? "" : s.Student.FirstName);

            return await students.ToListAsync();
        }

        /// <summary>
        /// Get students by term and class level. Uses AAUD student info table, which is driven by registration data and exceptions.
        /// </summary>
        /// <param name="termCode"></param>
        /// <param name="classLevel"></param>
        /// <returns></returns>
        [HttpGet("byTermAndClassLevel/{termcode}/{classLevel}")]
        [Permission(Allow = "SVMSecure.SIS.AllStudents")]
        public async Task<ActionResult<IEnumerable<Models.Student>>> GetStudentsByTermAndClassLevel(int termCode, string classLevel)
        {
            return await studentList.GetStudentsByTermCodeAndClassLevel(termCode, classLevel);
        }

        /// <summary>
        /// Import the listed people into a class year. Must be their first class year.
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="personIds"></param>
        /// <returns></returns>
        [HttpPost("{classYear}/import")]
        [Permission(Allow = "SVMSecure.SIS.AllStudents")]
        public async Task<ActionResult<StudentClassYear>> ImportClassYear(int classYear, List<int> personIds)
        {
            var user = userHelper.GetCurrentUser();
            foreach (int id in personIds)
            {
                await InsertStudentRecord(classYear, id, user);
            }

            return NoContent();
        }

        /// <summary>
        /// Add a person to the class year. Must be the first class year they are added to.
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
		[HttpPost("{classYear}/{personId}")]
        [Permission(Allow = "SVMSecure.SIS.AllStudents")]
        public async Task<ActionResult<StudentClassYear>> AddStudentToClassYear(int classYear, int personId)
        {
            var record = await InsertStudentRecord(classYear, personId);
            if (record == null)
            {
                return BadRequest();
            }
            return Ok(record);
        }

        /// <summary>
        /// Inserts a student class year. This should be the first class year for the student. If the student is being moved from one class year
        /// to another, the modify function should be used instead (to deactivate the old year with a reason).
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="personId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task<StudentClassYear?> InsertStudentRecord(int classYear, int personId, AaudUser? user = null)
        {
            //if the student is in any class year, they should be modified, not added
            var existing = await context.StudentClassYears
                .Where(s => s.PersonId == personId)
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                return null;
            }

            if (user == null)
            {
                user = userHelper.GetCurrentUser();
            }

            StudentClassYear sgy = new()
            {
                ClassYear = classYear,
                PersonId = personId,
                Active = true,
                Added = DateTime.Now,
                AddedBy = user != null ? user.AaudUserId : 0
            };
            context.StudentClassYears.Add(sgy);
            await context.SaveChangesAsync();
            return sgy;
        }

        /// <summary>
        /// Update a student class year. If updating the active record and changing the grad year, insert a new record and mark the old one inactive.
        /// If setting an inactive year to active, mark the active one inactive.
        /// </summary>
        /// <param name="studentClassYear"></param>
        /// <returns></returns>
        [HttpPut("{classYear}/{personId}")]
        [Permission(Allow = "SVMSecure.SIS.AllStudents")]
        public async Task<ActionResult<StudentClassYear>> UpdateClassYear(int classYear, int personId, StudentClassYearUpdate studentClassYear)
        {
            var user = userHelper.GetCurrentUser();
            var record = context.StudentClassYears.Find(studentClassYear.StudentClassYearId);
            if (record == null)
            {
                return NotFound();
            }
            if (record.PersonId != personId)
            {
                return BadRequest();
            }

            //deactivate active record if setting this to active
            if (studentClassYear.Active && !record.Active)
            {
                var activeRecord = await context.StudentClassYears.Where(s => s.Active).FirstOrDefaultAsync();
                if (activeRecord != null)
                {
                    activeRecord.Active = false;
                    context.Entry(activeRecord).State = EntityState.Modified;
                    await context.SaveChangesAsync();
                }
            }

            if (record.Active && record.ClassYear != classYear)
            {   //deactivate the existing record and insert a new record
                record.Active = false;
                record.Updated = DateTime.Now;
                record.UpdatedBy = user?.AaudUserId;
                record.LeftReason = studentClassYear.LeftReason;
                record.LeftTerm = studentClassYear.LeftTerm;
                record.Comment = studentClassYear.Comment;
                context.Entry(record).State = EntityState.Modified;
                await context.SaveChangesAsync();

                record = new StudentClassYear()
                {
                    Active = true,
                    Added = DateTime.Now,
                    AddedBy = user?.AaudUserId,
                    ClassYear = classYear,
                    Ross = studentClassYear.Ross ?? false,
                    PersonId = personId
                };
                context.Add(record);
                await context.SaveChangesAsync();
            }
            else
            {   //updating a record in place
                record.Active = studentClassYear.Active;
                record.Updated = DateTime.Now;
                record.UpdatedBy = user?.AaudUserId;
                record.Ross = studentClassYear.Ross ?? false;
                record.ClassYear = classYear;
                record.LeftReason = studentClassYear.LeftReason;
                record.LeftTerm = studentClassYear.LeftTerm;
                record.Comment = studentClassYear.Comment;
                context.Entry(record).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            return Ok(record);
        }

        /// <summary>
        /// Delete a student class year record (i.e. they were never in this class)
        /// </summary>
        /// <param name="studentClassYearId"></param>
        /// <returns></returns>
        [HttpDelete("studentClassYears/{studentClassYearId}")]
        public async Task<ActionResult> DeleteStudentClassYear(int studentClassYearId)
        {
            var record = context.StudentClassYears.Find(studentClassYearId);
            if (record == null)
            {
                return NotFound();
            }

            context.StudentClassYears.Remove(record);
            await context.SaveChangesAsync();
            return NoContent();
        }


        /// <summary>
        /// Get reasons a student left their class year
        /// </summary>
        /// <returns></returns>
        [HttpGet("leftReasons")]
        public async Task<ActionResult<List<ClassYearLeftReason>>> GetReasons()
        {
            return await context.ClassYearLeftReasons
                .OrderBy(r => r.Reason)
                .ToListAsync();
        }

        [HttpGet("classYears")]
        public async Task<ActionResult<List<int>>> GetClassYears(bool activeOnly = true, int? minClassYear = null)
        {
            var termCodeService = new TermCodeService(context);
            List<int> activeClassYears = (await termCodeService.GetActiveClassYears((await termCodeService.GetActiveTerm()).TermCode));
            if(!activeOnly)
            {
                var minCY = activeClassYears[0];
                for(var i = minCY - 1; i >= (minClassYear ?? minCY - 10); i--)
                {
                    activeClassYears.Prepend(i);
                }
            }
            return activeClassYears;
        }
    }
}