using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IUserHelper userHelper;

        public AssessmentController(VIPERContext _context, RAPSContext rapsContext, CtsSecurityService? ctsSecurityService = null, IUserHelper? userHelper = null)
        {
            context = _context;
            this.rapsContext = rapsContext;
            auditService = new AuditService(context);
            this.ctsSecurityService = ctsSecurityService ?? new CtsSecurityService(rapsContext, _context);
            this.userHelper = userHelper ?? new UserHelper();
        }

        /// <summary>
        /// Generic assessment get with params - note that this returns StudentAssessments of derived types
        /// </summary>
        [HttpGet]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical,SVMSecure.CTS.MyAssessments")]
        [ApiPagination(DefaultPerPage = 100, MaxPerPage = 100)]
        public async Task<ActionResult<List<StudentAssessment>>> GetAssessments(int? type, int? studentUserId, int? enteredById, int? serviceId,
            int? epaId, DateOnly? dateFrom, DateOnly? dateTo, ApiPagination? pagination,
            string? sortBy = null, bool descending = false)
        {
            if (!ctsSecurityService.CheckStudentAssessmentViewAccess(studentUserId, enteredById, serviceId))
            {
                return ForbidApi();
            }

            var assessments = context.Encounters
                .AsNoTracking()
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
                pagination.TotalRecords = await assessments.CountAsync();
                assessments = assessments
                    .Skip((pagination.Page - 1) * pagination.PerPage)
                    .Take(pagination.PerPage);
            }

            var assessmentsList = await assessments
                .ToListAsync();

            List<StudentAssessment> studentAssessments = new();
            foreach (var a in assessmentsList)
            {
                var sa = CreateStudentAssessment(a);
                sa.Editable = ctsSecurityService.CanEditStudentAssessment(sa.EnteredBy, sa.EnteredOn);
                studentAssessments.Add(sa);
            }

            return studentAssessments;
        }

        [HttpGet("assessors")]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical")]
        public async Task<ActionResult<List<Assessor>>> GetAssessors(int? type, int? serviceId)
        {
            var personId = userHelper.GetCurrentUser()?.AaudUserId;
            if (personId == null)
            {
                return ForbidApi();
            }

            var encounters = context.Encounters.AsQueryable();
            if (type != null)
            {
                encounters = encounters.Where(a => a.EncounterType == type);
            }
            if (serviceId != null)
            {
                encounters = encounters.Where(a => a.ServiceId == serviceId);
            }
            else if (!ctsSecurityService.CheckStudentAssessmentViewAccess())
            {
                //if they can't see all assessments, they can only see assessors on a service they are chief of
                var serviceChiefServices = await context.ServiceChiefs.Where(c => c.PersonId == personId).Select(c => c.ServiceId).ToListAsync();
                encounters = encounters.Where(a => a.ServiceId != null && serviceChiefServices.Contains((int)a.ServiceId));
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
        [HttpGet("{encounterId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage,SVMSecure.CTS.StudentAssessments,SVMSecure.CTS.AssessClinical,SVMSecure.CTS.MyAssessments")]
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
            if (!ctsSecurityService.CheckStudentAssessmentViewAccess(encounter.StudentUserId, encounter.EnteredBy, encounter.ServiceId))
            {
                return ForbidApi();
            }
            var sa = CreateStudentAssessment(encounter);
            sa.Editable = ctsSecurityService.CanEditStudentAssessment(sa.EnteredBy, sa.EnteredOn);
            return sa;
        }

        /// <summary>
        /// Given an Eval360 instance id, get the list of student evalautees for this evaluator and whether or not they have an EPA during
        /// this rotation.
        /// </summary>
        [HttpGet("epacompletion")]
        [Permission(Allow = "SVMSecure")]
        public async Task<ActionResult<List<EvaluateeWithEpaCompletion>>> EvalauteeStudentsWithEpas(int instanceId)
        {
            List<EvaluateeWithEpaCompletion> evaluateesWithCompletion = new();
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
                return ForbidApi();
            }

            //get evaluatees for this eval that are on the rotation for at least one week the logged in user is on this rotation
            var evaluatees = await context.EvaluateesByInstances
                .Where(e => e.InstanceId == instanceId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ThenBy(e => e.PersonId)
                .ToListAsync();

            if (evaluatees.Count > 0)
            {
                var personIds = evaluatees.Select(e => e.PersonId).Distinct().ToList();
                var serviceIds = evaluatees.Select(e => e.ServiceId).Distinct().ToList();
                var minDate = evaluatees.Min(e => e.StartDate);
                var maxDate = evaluatees.Max(e => e.EndDate);

                var epaEncounters = await context.Encounters
                    .Where(enc => enc.EncounterType == (int)EncounterCreationService.EncounterType.Epa)
                    .Where(enc => personIds.Contains(enc.StudentUserId))
                    .Where(enc => enc.ServiceId != null && serviceIds.Contains((int)enc.ServiceId))
                    .Where(enc => enc.EncounterDate >= minDate && enc.EncounterDate <= maxDate)
                    .Select(enc => new { enc.StudentUserId, enc.ServiceId, enc.EncounterDate })
                    .ToListAsync();

                foreach (var e in evaluatees)
                {
                    var hasEpa = epaEncounters.Any(enc =>
                        enc.StudentUserId == e.PersonId
                        && enc.ServiceId == e.ServiceId
                        && enc.EncounterDate >= e.StartDate
                        && enc.EncounterDate <= e.EndDate);
                    evaluateesWithCompletion.Add(new(e, hasEpa));
                }
            }

            return evaluateesWithCompletion;
        }

        /// <summary>
        /// Create a new epa assessment
        /// </summary>
        [HttpPost("epa")]
        [Permission(Allow = "SVMSecure.CTS.AssessClinical,SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CreateUpdateStudentEpa>> CreateStudentEpa(CreateUpdateStudentEpa epaData)
        {
            var student = await context.People
                    .Include(p => p.StudentInfo)
                    .Where(p => p.PersonId == epaData.StudentId)
                    .FirstOrDefaultAsync();
            if (student == null || student.StudentInfo?.ClassLevel == null)
            {
                return BadRequest("Student not found");
            }

            var personId = userHelper.GetCurrentUser()?.AaudUserId;
            if (personId == null)
            {
                return Unauthorized(); //shouldn't happen
            }

            using var trans = await context.Database.BeginTransactionAsync();
            var encounter = EncounterCreationService.CreateEncounterForEpa(epaData.StudentId, student.StudentInfo.ClassLevel, (int)personId, epaData.ServiceId,
                epaData.EpaId, epaData.LevelId, epaData.Comment);
            context.Add(encounter);
            await context.SaveChangesAsync();

            await auditService.AuditStudentEpa(encounter, AuditService.AuditActionType.Create, (int)personId);
            await trans.CommitAsync();

            return new CreateUpdateStudentEpa
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
            if (!ctsSecurityService.CanEditStudentAssessment(encounter.EnteredBy, encounter.EnteredOn))
            {
                return ForbidApi("This EPA cannot be edited at this time.");
            }

            var personId = userHelper.GetCurrentUser()?.AaudUserId;
            if (personId == null)
            {
                return Unauthorized(); //shouldn't happen
            }

            using var trans = await context.Database.BeginTransactionAsync();
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

            return new CreateUpdateStudentEpa
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
