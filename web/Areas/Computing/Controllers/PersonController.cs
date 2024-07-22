﻿using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<List<PersonSimple>>> GetPeople(bool? active = null)
        {
            var people = context.People
                .AsQueryable();
            if (active != null)
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

        [HttpGet("biorenderStudents")]
        [Permission(Allow = "SVMSecure.CATS.BiorenderStudentLookup")]
        public async Task<ActionResult<List<BiorenderStudent>>> GetBiorenderStudentList([FromQuery] List<string> emails)
        {
            var list = await new BiorenderStudentLookup(new Classes.Utilities.IamApi(httpFactory))
                .GetBiorenderStudentInfo(emails);

            return list;
        }
    }
}
