using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models;
using Web.Authorization;

namespace Viper.Areas.Computing.Controllers
{
    [Route("/api/people")]
    [Permission(Allow = "SVMSecure")]
    public class PersonController : ApiController
    {
        private readonly VIPERContext context;
        public PersonController(VIPERContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<PersonSimple>>> GetPeople(bool? active = null)
        {
            var people = context.People
                .AsQueryable();
            if(active != null)
            {
                people = people.Where(p => p.Current == 1 || p.Future == 1);
            }
            return await people
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ThenBy(p => p.MailId)
                .ThenBy(p => p.PersonId)
                .Select(p => new PersonSimple(p))
                .ToListAsync();
        }
    }
}
