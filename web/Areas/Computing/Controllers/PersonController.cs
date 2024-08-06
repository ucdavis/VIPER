using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Computing.Model;
using Viper.Areas.Computing.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models;
using Web.Authorization;

namespace Viper.Areas.Computing.Controllers
{
    [Route("/api/people/")]
    [Permission(Allow = "SVMSecure")]
    public class PersonController : ApiController
    {
        private readonly VIPERContext context;
        private readonly IHttpClientFactory httpFactory;
        public PersonController(VIPERContext context, IHttpClientFactory factory)
        {
            this.context = context;
            httpFactory = factory;
        }

        [HttpGet]
        public async Task<ActionResult<List<PersonSimple>>> GetPeople(bool? active = null, string? name = null)
        {
            var people = context.People
                .AsQueryable();
            if (active != null)
            {
                people = people.Where(p => p.Current == 1 || p.Future == 1);
            }
            if(name != null)
            {
                people = people.Where(p => p.FirstName.Contains(name) || p.LastName.Contains(name));
            }
            return await people
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ThenBy(p => p.MailId)
                .ThenBy(p => p.PersonId)
                .Select(p => new PersonSimple(p))
                .ToListAsync();
        }

        [HttpPost("biorenderStudents")]
        [Permission(Allow = "SVMSecure.CATS.BiorenderStudentLookup")]
        public async Task<ActionResult<List<BiorenderStudent>>> GetBiorenderStudentList(List<string> emails)
        {
            var list = await new BiorenderStudentLookup(new Classes.Utilities.IamApi(httpFactory))
                .GetBiorenderStudentInfo(emails);

            return list;
        }
    }
}
