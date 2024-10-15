using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Viper.Areas.CTS.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
	[Route("/api/cts/domains")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class DomainController : ApiController
	{
		private readonly VIPERContext context;

		public DomainController(VIPERContext context)
		{
			this.context = context;
		}

		[HttpGet]
		public async Task<ActionResult<List<DomainDto>>> Index()
		{
			return await context.Domains.OrderBy(d => d.Order).Select(d => new DomainDto(d)).ToListAsync();
		}

		[HttpGet("{domainId}")]
		public async Task<ActionResult<DomainDto?>> GetDomain(int domainId)
		{
			var d = await context.Domains.FindAsync(domainId);

            return d == null ? NotFound() : new DomainDto(d);
		}

		[HttpPost]
		[Permission(Allow = "SVMSecure.CTS.Manage")]
		public async Task<ActionResult<DomainDto>> CreateDomain(Domain domain)
		{
			if(domain.DomainId != 0)
			{
				return BadRequest();
			}

			var domainWithNameOrOrder = context.Domains
				.Any(d => d.Name == domain.Name || d.Order == domain.Order);
			if(domainWithNameOrOrder)
			{
				return BadRequest("Domain with the same name or order exists");
			}

			context.Add(domain);
			await context.SaveChangesAsync();

			return new DomainDto(domain);
		}

		[HttpPut("{domainId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<DomainDto>> UpdateDomain(int domainId, Domain domain)
		{
			if(domain.DomainId != domainId)
			{
				return BadRequest();
			}
            var domainWithNameOrOrder = context.Domains
                .Any(d => d.DomainId != domain.DomainId 
						&& (d.Name == domain.Name || d.Order == domain.Order));
            if (domainWithNameOrOrder)
            {
                return BadRequest("Domain with the same name or order exists");
            }

            context.Domains.Update(domain);
			await context.SaveChangesAsync();
			return new DomainDto(domain);
        }

		[HttpDelete("{domainId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<DomainDto>> DeleteDomain(int domainId)
		{
			var domain = await context.Domains.FindAsync(domainId);
			if(domain == null)
			{
				return NotFound();
			}

			context.Domains.Remove(domain);
			try
			{
				await context.SaveChangesAsync();
			}
			catch(Exception)
			{
				return BadRequest("Could not remove domain. It may be linked to other objects.");
			}
			return new DomainDto(domain);
		}
	}
}
