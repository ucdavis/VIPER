using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Controllers
{
	[Route("/cts/domains")]
	public class DomainController : ApiController
	{
		private readonly VIPERContext context;

		public DomainController(VIPERContext context)
		{
			this.context = context;
		}

		[HttpGet]
		public async Task<ActionResult<List<Domain>>> Index()
		{
			return await context.Domains.OrderBy(d => d.Order).ToListAsync();
		}

		[HttpGet("{domainId}")]
		public async Task<ActionResult<Domain?>> GetDomain(int domainId)
		{
			return await context.Domains.FindAsync(domainId);
		}
	}
}
