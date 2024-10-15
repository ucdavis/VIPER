using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Viper.Areas.CTS.Models;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/assessments")]
    [Permission(Allow = "SVMSecure")]
    public class AssessmentController : ApiController
    {
        private readonly VIPERContext context;
        private readonly RAPSContext rapsContext;
        private readonly AuditService auditService;
        private readonly CtsSecurityService ctsSecurityService;

        public AssessmentController(VIPERContext _context, RAPSContext rapsContext)
        {
            context = _context;
            this.rapsContext = rapsContext;
            auditService = new AuditService(context);
            ctsSecurityService = new CtsSecurityService(rapsContext, _context);
        }

        /// <summary>
        /// Generic assessment get with params - note that this returns StudentAssessments of derived types
        /// </summary>
        /// <param name="type"></param>
        /// <param name="studentUserId"></param>
        /// <param name="enteredById"></param>
        /// <param name="serviceId"></param>
        /// <param name="epaId"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical")]
        [ApiPagination(DefaultPerPage = 100, MaxPerPage = 100)]
        public async Task<ActionResult<List<StudentAssessment>>> GetAssessments(int? type, int? studentUserId, int? enteredById, int? serviceId,
            int? epaId, DateOnly? dateFrom, DateOnly? dateTo, ApiPagination? pagination,
            string? sortBy = null, bool descending = false)
        {
            if (!ctsSecurityService.CheckStudentAssessmentViewAccess(studentUserId, enteredById))
            {
                return (ActionResult<List<StudentAssessment>>)ForbidApi();
            }

            var assessments = context.Encounters
                .Include(e => e.Epa)
                .Include(e => e.Level)
                .Include(e => e.EnteredByPerson)
                .Include(e => e.Student)
                .Include(e => e.Service)
                .Include(e => e.Offering)
                .AsQueryable();
            if (type != null)
            {
                assessments = assessments.Where(e => e.EncounterType == type);
            }
            if (studentUserId != null)
            {
                assessments = assessments.Where(e => e.StudentUserId == studentUserId);
            }
            if (enteredById != null)
            {
                assessments = assessments.Where(e => e.EnteredBy == enteredById);
            }
            if (serviceId != null)
            {
                assessments = assessments.Where(e => e.ServiceId == serviceId);
            }
            if (epaId != null)
            {
                assessments = assessments.Where(e => e.EpaId == epaId);
            }
            if (dateFrom != null)
            {
                assessments = assessments.Where(e => e.EncounterDate >= ((DateOnly)dateFrom).ToDateTime(new TimeOnly(0, 0, 0)));
            }
            if (dateTo != null)
            {
                assessments = assessments.Where(e => e.EncounterDate <= ((DateOnly)dateTo).ToDateTime(new TimeOnly(0, 0, 0)));
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "enteredon": assessments = descending ? assessments.OrderByDescending(a => a.EnteredOn) : assessments.OrderBy(a => a.EnteredOn); break;
                    case "enteredbyname":
                        assessments = descending
                            ? assessments.OrderByDescending(a => a.EnteredByPerson.LastName)
                                .ThenByDescending(a => a.EnteredByPerson.FirstName)
                            : assessments.OrderBy(a => a.EnteredByPerson.LastName)
                                .ThenBy(a => a.EnteredByPerson.FirstName);
                        break;
                    case "levelname":
                        assessments = descending
                            ? assessments.OrderByDescending(a => a.Level != null ? a.Level.LevelName : "")
                            : assessments.OrderBy(a => a.Level != null ? a.Level.LevelName : ""); break;
                    case "servicename":
                        assessments = descending
                            ? assessments.OrderByDescending(a => a.Service != null ? a.Service.ServiceName : "")
                            : assessments.OrderBy(a => a.Service != null ? a.Service.ServiceName : ""); break;
                    case "epaname":
                        assessments = descending
                            ? assessments.OrderByDescending(a => a.Epa != null ? a.Epa.Name : "")
                            : assessments.OrderBy(a => a.Epa != null ? a.Epa.Name : ""); break;
                    case "studentname":
                        assessments = descending
                            ? assessments.OrderByDescending(a => a.Student.LastName)
                                .ThenByDescending(a => a.Student.FirstName)
                            : assessments.OrderBy(a => a.Student.LastName)
                                .ThenBy(a => a.Student.FirstName);
                        break;
                }
            }
            if (pagination != null)
            {
                var s = (pagination.PerPage - 1) * pagination.Page;
                pagination.TotalRecords = assessments.Count();
                assessments = assessments
                    .Skip((pagination.Page - 1) * pagination.PerPage)
                    .Take(pagination.PerPage);
            }

            var assessmentsList = await assessments
                .ToListAsync();

            //if this is not a student viewing their own assessments, set the editable flag
            var userHelper = new UserHelper();

            List<StudentAssessment> studentAssessments = new();
            foreach (var a in assessmentsList)
            {
                var sa = CreateStudentAssessment(a);
                sa.Editable = ctsSecurityService.CanEditStudentAssessment(sa.EnteredBy);
                studentAssessments.Add(sa);
            }

            return studentAssessments;
        }

        [HttpGet("assessors")]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical")]
        public async Task<ActionResult<List<Assessor>>> GetAssessors(int? type, int? serviceId)
        {
            var encounters = context.Encounters.AsQueryable();
            if (type != null)
            {
                encounters = encounters.Where(a => a.EncounterType == type);
            }
            if (serviceId != null)
            {
                encounters = encounters.Where(a => a.ServiceId == serviceId);
            }

            var assessors = await encounters.Select(s => s.EnteredByPerson)
                .Distinct()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
            var list = new List<Assessor>();
            foreach (var a in assessors)
            {
                list.Add(new Assessor(a));
            }
            return list;
        }

        /*
         * 
         * epa/ routes
         * 
         */

        /// <summary>
        /// Get single student epa assessment
        /// </summary>
        /// <param name="encounterId"></param>
        /// <returns></returns>
        [HttpGet("{encounterId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical")]
        public async Task<ActionResult<StudentAssessment>> GetStudentAssessment(int encounterId)
        {
            var encounter = await context.Encounters
                .Include(e => e.Epa)
                .Include(e => e.EnteredByPerson)
                .Include(e => e.Student)
                .Include(e => e.Service)
                .Include(e => e.Offering)
                .Include(e => e.Level)
                .SingleOrDefaultAsync(e => e.EncounterId == encounterId);
            if (encounter == null)
            {
                return NotFound();
            }
            if (!ctsSecurityService.CheckStudentAssessmentViewAccess(encounter.StudentUserId, encounter.EnteredBy))
            {
                return (ActionResult<StudentAssessment>)ForbidApi();
            }
            var sa = CreateStudentAssessment(encounter);
            sa.Editable = ctsSecurityService.CanEditStudentAssessment(sa.EnteredBy);
            return sa;
        }

        /// <summary>
        /// Given an Eval360 instance id, get the list of student evalautees for this evaluator and whether or not they have an EPA during
        /// this rotation.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        [HttpGet("epacompletion")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<List<EvaluateeWithEpaCompletion>>> EvalauteeStudentsWithEpas(int instanceId)
        {
            List<EvaluateeWithEpaCompletion> evaluateesWithCompletion = new();
            var userHelper = new UserHelper();
            var user = userHelper.GetCurrentUser();
            //get instance and check to make sure it belongs to the logged in user, or the logged in user has an admin permission
            var instance = await context.Instances.FindAsync(instanceId);
            if (instance == null)
            {
                return NotFound();
            }

            if (instance.InstanceMothraId != user?.MothraId
                && !userHelper.HasPermission(rapsContext, user, "SVMSecure.Eval.ViewStudentClinResultsAll")
                && !userHelper.HasPermission(rapsContext, user, "SVMSecure.CTS.Manage"))
            {
                return Forbid();
            }

            //get evaluatees for this eval that are on the rotation for at least one week the logged in user is on this rotation
            var evaluatees = await context.EvaluateesByInstances
                .Where(e => e.InstanceId == instanceId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ThenBy(e => e.PersonId)
                .ToListAsync();

            //for each evaluatee, get the epas for this service that are dated within the block start/end, and mark that an EPA has been done or not done
            foreach (var e in evaluatees)
            {
                var epaAssessments = await context.Encounters
                    .Where(enc => enc.EncounterType == (int)EncounterCreationService.EncounterType.Epa)
                    .Where(enc => enc.EncounterDate >= e.StartDate && enc.EncounterDate <= e.EndDate)
                    .Where(enc => enc.Student.PersonId == e.PersonId)
                    .Where(enc => enc.ServiceId == e.ServiceId)
                    .CountAsync();
                evaluateesWithCompletion.Add(new(e, epaAssessments > 0));
            }

            return evaluateesWithCompletion;
        }

        /// <summary>
        /// Create a new epa assessment
        /// </summary>
        /// <param name="epaData"></param>
        /// <returns></returns>
        [HttpPost("epa")]
        [Permission(Allow = "SVMSecure.CTS.AssessClinical,SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CreateUpdateStudentEpa>> CreateStudentEpa(CreateUpdateStudentEpa epaData)
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
            var encounter = EncounterCreationService.CreateEncounterForEpa(epaData.StudentId, student.StudentInfo.ClassLevel, (int)personId, epaData.ServiceId,
                epaData.EpaId, epaData.LevelId, epaData.Comment);
            context.Add(encounter);
            await context.SaveChangesAsync();

            await auditService.AuditStudentEpa(encounter, AuditService.AuditActionType.Create, (int)personId);
            await trans.CommitAsync();

            return new CreateUpdateStudentEpa()
            {
                EncounterId = encounter.EncounterId,
                EpaId = epaData.EpaId,
                ServiceId = encounter.ServiceId ?? 0,
                StudentId = encounter.Student.PersonId,
                LevelId = epaData.LevelId,
                Comment = encounter.Comment,
                EncounterDate = encounter.EncounterDate
            };
        }

        /// <summary>
        /// Update an EPA
        /// </summary>
        /// <param name="encounterId"></param>
        /// <param name="epaData"></param>
        /// <returns></returns>
        [HttpPut("epa/{encounterId}")]
        [Permission(Allow = "SVMSecure.CTS.AssessClinical,SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CreateUpdateStudentEpa>> UpdateStudentEpa(int encounterId, CreateUpdateStudentEpa epaData)
        {
            var encounter = await context.Encounters
                .SingleOrDefaultAsync(e => e.EncounterId == encounterId);
            if (encounter == null)
            {
                return NotFound();
            }
            //check the logged in user can edit
            if (!ctsSecurityService.CanEditStudentAssessment(encounter.EnteredBy))
            {
                return Forbid();
            }

            var personId = new UserHelper()?.GetCurrentUser()?.AaudUserId;
            if (personId == null)
            {
                return Unauthorized(); //shouldn't happen
            }

            using var trans = context.Database.BeginTransaction();
            encounter.Comment = epaData.Comment;
            encounter.LevelId = epaData.LevelId;
            if (epaData.EncounterDate != null)
            {
                encounter.EncounterDate = (DateTime)epaData.EncounterDate;
            }

            context.Update(encounter);
            await context.SaveChangesAsync();

            await auditService.AuditStudentEpa(encounter, AuditService.AuditActionType.Update, (int)personId);
            await trans.CommitAsync();

            return new CreateUpdateStudentEpa()
            {
                EncounterId = encounter.EncounterId,
                EpaId = epaData.EpaId,
                ServiceId = encounter.ServiceId ?? 0,
                StudentId = epaData.StudentId,
                LevelId = epaData.LevelId,
                Comment = encounter.Comment,
                EncounterDate = encounter.EncounterDate
            };
        }

        private static StudentAssessment CreateStudentAssessment(Encounter e)
        {
            switch ((EncounterCreationService.EncounterType)e.EncounterType)
            {
                case EncounterCreationService.EncounterType.Epa:
                    return new StudentEpaAssessment(e);
                default:
                    return new StudentAssessment(e, null);
            }
        }
    }
}
