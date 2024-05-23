using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/cts/epas")]
    [Permission(Allow = "SVMSecure")]
    public class StudentEpaController : ApiController
    {
        private readonly VIPERContext context;
        public StudentEpaController(VIPERContext _context)
        {
            context = _context;
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.AssessClinical")]
        public async Task<ActionResult> CreateStudentEpa(CreateUpdateStudentEpa epaData)
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
            if(personId == null)
            {
                return Unauthorized(); //shouldn't happen
            }

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
            context.Add(encounter);
            await context.SaveChangesAsync();

            return Ok();
        }
    }
}
