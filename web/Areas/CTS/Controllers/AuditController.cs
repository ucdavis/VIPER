using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
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
        [ApiPagination(DefaultPerPage = 100, MaxPerPage = 1000)]
        public async Task<ActionResult<List<CtsAudit>>> GetAudit()
        {
            return new List<CtsAudit>();
        }

        [HttpGet("/areas")]
        public ActionResult<List<string>> GetAuditAreas()
        {
            return AuditService.AuditAreas;
        }

    }
}
