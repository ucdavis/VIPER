using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/clinicalservices")]
    [Permission(Allow = "SVMSecure")]
    public class ClinicalServices : ApiController
    {
        private readonly VIPERContext context;

        public ClinicalServices(VIPERContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Service>>> GetServices(int? chiefId = null)
        {
            var servicesQ = context.Services.AsQueryable();
            if(chiefId != null)
            {
                var serviceChiefs = await context.ServiceChiefs.Where(c => c.PersonId == chiefId).Select(c => c.ServiceId).ToListAsync();
                servicesQ = servicesQ.Where(s => serviceChiefs.Contains(s.ServiceId));
            }
            return await servicesQ.OrderBy(s => s.ServiceName).ToListAsync();
        }
    }
}
