using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/cts/epas")]
    public class EpaController : ApiController
    {
        private readonly VIPERContext context;
        private readonly HtmlSanitizer sanitizer; //EPAs contain HTML in the description. Sanitize on both input and output.

        public EpaController(VIPERContext context)
        {
            this.context = context;
            sanitizer = new HtmlSanitizer();
        }

        [HttpGet]
        public async Task<ActionResult<List<Epa>>> GetEpas(int? serviceId)
        {
            var epas = await context.Epas
                .OrderBy(e => e.Name)
                .ToListAsync();

            epas.ForEach(e => e.Description = e.Description != null ? sanitizer.Sanitize(e.Description) : null);

            return epas;
        }

        [HttpGet("{epaId}")]
        public async Task<ActionResult<Epa>> GetEpa(int epaId)
        {
            var epa = await context.Epas.FindAsync(epaId);
            if(epa == null)
            {
                return NotFound();
            }
            epa.Description = epa.Description != null ? sanitizer.Sanitize(epa.Description) : null;
            return epa;
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<Epa>> AddEpa(Epa epa)
        {
            if (epa.Description != null)
            {
                epa.Description = sanitizer.Sanitize(epa.Description);
            }
            context.Epas.Add(epa);
            await context.SaveChangesAsync();
            return epa;
        }

        [HttpPut("{epaId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<Epa>> UpdateEpa(int epaId, Epa epa)
        {
            if(epaId != epa.EpaId)
            {
                return BadRequest();
            }
            if (epa.Description != null)
            {
                epa.Description = sanitizer.Sanitize(epa.Description);
            }

            context.Epas.Update(epa);
            await context.SaveChangesAsync();
            return epa;
        }

        [HttpDelete("{epaId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<Epa>> DeleteEpa(int epaId)
        {
            var epa = await context.Epas.FindAsync(epaId);
            if(epa == null)
            {
                return NotFound();
            }

            try
            {
                context.Entry(epa).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return epa;
        }
    }
}
