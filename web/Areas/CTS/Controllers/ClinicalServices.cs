using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/cts/clinicalservices")]
    [Permission(Allow = "SVMSecure")]
    public class ClinicalServices : ApiController
    {
        private readonly VIPERContext context;

        public ClinicalServices(VIPERContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Service>>> GetServices()
        {
            return await context.Services.OrderBy(s => s.ServiceName).ToListAsync();
        }
    }
}
