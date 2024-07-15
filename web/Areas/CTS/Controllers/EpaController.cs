using AngleSharp.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/epas")]
    [Permission(Allow = "SVMSecure")]
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
        public async Task<ActionResult<List<Models.Epa>>> GetEpas(int? serviceId)
        {
            var epas = await context.Epas
                .AsNoTracking()
                .Include(e => e.Services)
                .Where(e => serviceId == null || e.Services.Any(s => s.ServiceId == serviceId))
                .OrderBy(e => e.Name)
                .Select(e => new Models.Epa(e))
                .ToListAsync();
            
            epas.ForEach(e =>
            {
                e.Description = e.Description != null ? sanitizer.Sanitize(e.Description) : null;
                e.Services = e.Services.OrderBy(s => s.ServiceName).ToList();
            });

            return epas;
        }

        [HttpGet("{epaId}")]
        public async Task<ActionResult<Models.Epa>> GetEpa(int epaId)
        {
            var epa = await context.Epas
                .AsNoTracking()
                .Include(e => e.Services)
                .Where(e => e.EpaId == epaId)
                .Select(e => new Models.Epa(e))
                .FirstOrDefaultAsync();
            if (epa == null)
            {
                return NotFound();
            }
            epa.Description = epa.Description != null ? sanitizer.Sanitize(epa.Description) : null;
            epa.Services = epa.Services.OrderBy(s => s.ServiceName).ToList();
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
            if (epaId != epa.EpaId)
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
            if (epa == null)
            {
                return NotFound();
            }

            try
            {
                context.Entry(epa).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return epa;
        }

        [HttpPut("{epaId}/services")]
        public async Task<ActionResult> UpdateServices(int epaId, List<int> serviceIds)
        {
            var epa = await context.Epas
                .Include(e => e.Services)
                .Where(e => e.EpaId == epaId)
                .FirstOrDefaultAsync();
            if (epa == null)
            {
                return NotFound();
            }

            int removed = epa.Services.RemoveAll(s => !serviceIds.Contains(s.ServiceId));
            bool modified = removed > 0;

            //look for services to add
            foreach (var serviceId in serviceIds)
            {
                if (epa.Services.Find(e => e.ServiceId == serviceId) == null)
                {
                    var s = await context.Services.FindAsync(serviceId);
                    if (s != null)
                    {
                        epa.Services.Add(s);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                context.Entry(epa).State = EntityState.Modified;
            }

            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
