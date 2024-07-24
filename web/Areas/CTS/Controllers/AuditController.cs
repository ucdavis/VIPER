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
    [Route("/api/cts/audit")]
    [Permission(Allow = "SVMSecure.CTS.Manage")]
    public class AuditController : ApiController
    {
        private readonly VIPERContext context;
        private AuditService auditService;

        public AuditController(VIPERContext context)
        {
            this.context = context;
            auditService = new AuditService(context);
        }

        [HttpGet]
        [ApiPagination(DefaultPerPage = 25, MaxPerPage = 100)]
        public async Task<ActionResult<List<AuditRow>>> GetAudit(string? area, string? action, int? modifiedPersonId,
            int? modifiedById, DateTime? dateFrom, DateTime? dateTo, ApiPagination? pagination,
            string? sortBy = null, bool descending = false)
        {
            var audit = context.CtsAudits
                .Include(a => a.Encounter)
                    .ThenInclude(e => e!.Student)
                .Include(a => a.Modifier)
                .AsQueryable();
            if (area != null)
            {
                audit = audit.Where(a => a.Area.ToLower() == area.ToLower());
            }
            if (action != null)
            {
                audit = audit.Where(a => a.Action.ToLower() == action.ToLower());
            }
            if (modifiedPersonId != null)
            {
                audit = audit.Where(a => a.Encounter != null && a.Encounter.StudentUserId == modifiedPersonId);
            }
            if (modifiedById != null)
            {
                audit = audit.Where(a => a.ModifiedBy == modifiedById);
            }
            if(dateFrom != null)
            {
                audit = audit.Where(a => a.TimeStamp >= dateFrom);
            }
            if(dateTo != null)
            {
                audit = audit.Where(a => a.TimeStamp <= dateTo);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "timestamp": audit = descending ? audit.OrderByDescending(a => a.TimeStamp) : audit.OrderBy(a => a.TimeStamp); break;
                    case "action": audit = descending ? audit.OrderByDescending(a => a.Action) : audit.OrderBy(a => a.Action); break;
                    case "area": audit = descending ? audit.OrderByDescending(a => a.Area) : audit.OrderBy(a => a.Area); break;
                    case "modifiedperson":
                        audit = descending
                            ? audit.OrderByDescending(a => a.Encounter != null ? a.Encounter.Student.LastName : "")
                                .ThenByDescending(a => a.Encounter != null ? a.Encounter.Student.FirstName : "")
                            : audit.OrderBy(a => a.Encounter != null ? a.Encounter.Student.LastName : "")
                                .ThenBy(a => a.Encounter != null ? a.Encounter.Student.FirstName : "");
                            break;
                    case "modifiedby":
                        audit = descending
                            ? audit.OrderByDescending(a => a.Modifier.LastName)
                                .ThenByDescending(a => a.Modifier.FirstName)
                            : audit.OrderBy(a => a.Modifier.LastName)
                                .ThenBy(a => a.Modifier.FirstName);
                        break;
                }
            }
            if (pagination != null)
            {
                pagination.TotalRecords = audit.Count();
                audit = GetPage(audit, pagination);
            }

            var rows = await audit.Select(a => new AuditRow(a))
                .ToListAsync();

            return rows;
        }

        [HttpGet("areas")]
        public ActionResult<List<string>> GetAuditAreas()
        {
            return AuditService.AuditAreas;
        }

        [HttpGet("actions")]
        public async Task<ActionResult<List<string>>> GetAuditActions()
        {
            return await context.CtsAudits
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        [HttpGet("modifiers")]
        public async Task<ActionResult<List<PersonSimple>>> GetModifiers()
        {
            return await context.CtsAudits
                .Select(a => a.Modifier)
                .Distinct()
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName)
                .ThenBy(m => m.MailId)
                .ThenBy(m => m.PersonId)
                .Select(m => new PersonSimple(m))
                .ToListAsync();
        }

    }
}
