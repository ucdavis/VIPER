using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/cts/studentEpa")]
    [Permission(Allow = "SVMSecure")]
    public class StudentEpaController : ApiController
    {
        private readonly VIPERContext context;
        private AuditService auditService;
        private CtsSecurityService ctsSecurityService;

        public StudentEpaController(VIPERContext _context, RAPSContext rapsContext)
        {
            context = _context;
            auditService = new AuditService(context);
            ctsSecurityService = new CtsSecurityService(rapsContext, _context);
        }

        /// <summary>
        /// Get student epas. Permissions have to be checked based on the arguments provided. Students can access their own,
        /// assessors can access those they've entered, managers can access all.
        /// </summary>
        /// <param name="studentId"></param>
        /// <param name="enteredById"></param>
        /// <param name="serviceId"></param>
        /// <param name="epaId"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical")]
        public async Task<ActionResult<List<StudentEpaAssessment>>> GetStudentEpas(int? studentId, int? enteredById, int? serviceId,
            int? epaId, DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (!ctsSecurityService.CheckStudentAssessmentViewAccess(studentId, enteredById))
            {
                return Forbid();
            }
            var epas = context.StudentEpas
                .Include(se => se.Epa)
                .Include(se => se.Level)
                .Include(se => se.Encounter)
                    .ThenInclude(e => e.EnteredByPerson)
                .Include(se => se.Encounter)
                    .ThenInclude(e => e.Student)
                .Include(se => se.Encounter)
                    .ThenInclude(e => e.Service)
                .Include(se => se.Encounter)
                    .ThenInclude(e => e.Offering)
                .AsQueryable();
            if (studentId != null)
            {
                epas = epas.Where(se => se.Encounter.StudentUserId == studentId);
            }
            if (enteredById != null)
            {
                epas = epas.Where(se => se.Encounter.EnteredBy == enteredById);
            }
            if (serviceId != null)
            {
                epas = epas.Where(se => se.Encounter.ServiceId == serviceId);
            }
            if (epaId != null)
            {
                epas = epas.Where(se => se.EpaId == epaId);
            }
            if (dateFrom != null)
            {
                epas = epas.Where(se => se.Encounter.EncounterDate >= ((DateOnly)dateFrom).ToDateTime(new TimeOnly(0, 0, 0)));
            }
            if (dateTo != null)
            {
                epas = epas.Where(se => se.Encounter.EncounterDate <= ((DateOnly)dateTo).ToDateTime(new TimeOnly(0, 0, 0)));
            }
            var epaList = await epas.ToListAsync();

            //if this is not a student viewing their own assessments, set the editable flag
            return epaList
                .Select(e => new StudentEpaAssessment(e))
                .ToList();
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.AssessClinical,SVMSecure.CTS.Manage")]
        public async Task<ActionResult<StudentEpa>> CreateStudentEpa(CreateUpdateStudentEpa epaData)
        {
            var student = await context.People
                    .Include(p => p.StudentInfo)
                    .Where(p => p.PersonId == epaData.StudentId)
                    .FirstOrDefaultAsync();
            if (student == null || student?.StudentInfo?.ClassLevel == null)
            {
                return BadRequest("Student not found");
            }

            var personId = new UserHelper()?.GetCurrentUser()?.AaudUserId;
            if (personId == null)
            {
                return Unauthorized(); //shouldn't happen
            }

            using var trans = context.Database.BeginTransaction();
            var encounter = EncounterCreationService.CreateEncounterForEpa(epaData.StudentId, student.StudentInfo.ClassLevel, (int)personId, epaData.ServiceId);
            context.Add(encounter);
            await context.SaveChangesAsync();

            var studentEpa = new StudentEpa()
            {
                EpaId = epaData.EpaId,
                LevelId = epaData.LevelId,
                EncounterId = encounter.EncounterId,
                Comment = epaData.Comment
            };
            context.Add(studentEpa);
            await context.SaveChangesAsync();

            await auditService.AuditStudentEpa(encounter, studentEpa, AuditService.AuditActionType.Create, (int)personId);
            await trans.CommitAsync();

            return studentEpa;
        }
    }
}
