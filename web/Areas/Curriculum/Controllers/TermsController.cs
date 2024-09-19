using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;
using Web.Authorization;

namespace Viper.Areas.Curriculum.Controllers
{
	[Route("/curriculum/terms")]
    [Route("/api/curriculum/terms")]
    [Permission(Allow = "SVMSecure.Curriculum")]
	public class TermsController : ApiController
	{
		private readonly VIPERContext _context;

		public TermsController(VIPERContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult<List<Term>>> GetTerms(bool? current = null, bool? currentMulti = null)
		{
			var terms = _context.Terms
				.Where(t => t.TermCode != 999999);
			if(current != null)
			{
				terms = terms.Where(t => t.CurrentTerm == current);
			}
			if(currentMulti != null)
			{
				terms = terms.Where(t => t.CurrentTermMulti == currentMulti);
			}
			return await terms
				.OrderByDescending(t => t.TermCode)
				.ToListAsync();
		}
	}
}
