using Viper.Classes;
using Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Microsoft.EntityFrameworkCore;
using Viper.Models.CTS;
using Viper.Areas.CTS.Models;
using System.DirectoryServices.ActiveDirectory;
using AutoMapper;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/competencies/")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class CompetencyController : ApiController
    {
        private readonly VIPERContext context;
        private readonly IMapper mapper;

        public CompetencyController(VIPERContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompetencyDto>>> Index()
        {
            return await context.Competencies
                .Include(c => c.Domain)
                .OrderBy(c => c.Order)
                .Select(c => new CompetencyDto(c))
                .ToListAsync();
        }

        [HttpGet("{competencyId}")]
        public async Task<ActionResult<CompetencyDto>> GetComp(int competencyId)
        {
            var comp = await context.Competencies.Include(c => c.Domain).Where(c => c.CompetencyId == competencyId).FirstOrDefaultAsync();
            if (comp == null)
            {
                return NotFound();
            }
            return new CompetencyDto(comp);
        }

        [HttpGet("{competencyId}/children")]
        public async Task<ActionResult<List<CompetencyDto>>> GetChildren(int competencyId)
        {
            var comps = await context.Competencies
                .Include(c => c.Domain)
                .Where(c => c.ParentId == competencyId)
                .OrderBy(c => c.Order)
                .Select(c => new CompetencyDto(c))
                .ToListAsync();
            if (comps.Count == 0)
            {
                return NotFound();
            }
            return comps;
        }


        [HttpGet("hierarchy")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<List<CompetencyHierarchyDto>>> GetCompetencyHierarchy()
        {
            var comps = await context.Competencies
                .Include(c => c.Domain)
                .OrderBy(c => c.Domain.Order)
                .ThenBy(c => c.Order)
                .ToListAsync();
            var compHierarchy = new List<CompetencyHierarchyDto>();
            var allCompDtos = mapper.Map<List<CompetencyHierarchyDto>>(comps);
            foreach (var comp in allCompDtos)
            {
                if (comp.ParentId == null)
                {
                    compHierarchy.Add(comp);
                }
                else
                {
                    var parent = allCompDtos.Where(c => c.CompetencyId == comp.ParentId).FirstOrDefault();
                    if (parent != null)
                    {
                        parent.Children.Append(comp);
                    }
                }
            }
            return compHierarchy;
        }

        [HttpPost]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CompetencyDto>> AddCompetency(CompetencyAddUpdate competency)
        {
            if (competency.CompetencyId != null)
            {
                return BadRequest("CompetencyId cannot be set for a new competency.");
            }

            if (competency.Name.Length < 2 || competency.Number.Length < 1)
            {
                return BadRequest("Name and Number are required.");
            }

            var duplicates = await context.Competencies.Where(c => c.Number == competency.Number).ToListAsync();
            if (duplicates.Count > 0)
            {
                return BadRequest("A competency with this number exists already.");
            }

            var comp = new Competency()
            {
                Name = competency.Name,
                Number = competency.Number,
                CanLinkToStudent = competency.CanLinkToStudent,
                Description = competency.Description,
                DomainId = competency.DomainId,
                ParentId = competency.ParentId
            };
            context.Competencies.Add(comp);
            await context.SaveChangesAsync();
            await UpdateCompetencyOrders();
            return await GetComp(comp.CompetencyId);
        }

        [HttpPut("{competencyId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CompetencyDto>> UpdateComptency(int competencyId, CompetencyAddUpdate competency)
        {
            if (competencyId != competency.CompetencyId)
            {
                return BadRequest();
            }

            var c = await context.Competencies.FindAsync(competency.CompetencyId);
            if (c == null)
            {
                return NotFound();
            }

            if (competency.CompetencyId == null || competency.CompetencyId <= 0)
            {
                return BadRequest("CompetencyId is required.");
            }
            if (competency.Name.Length < 2 || competency.Number.Length < 1)
            {
                return BadRequest("Name and Number are required.");
            }

            var duplicates = await context.Competencies
                .Where(c => c.CompetencyId != competency.CompetencyId)
                .Where(c => c.Number == competency.Number)
                .ToListAsync();
            if (duplicates.Count > 0)
            {
                return BadRequest("A competency with this number exists already.");
            }

            c.Name = competency.Name;
            c.Number = competency.Number;
            c.Description = competency.Description;
            c.CanLinkToStudent = competency.CanLinkToStudent;
            c.ParentId = competency.ParentId;

            context.Competencies.Update(c);
            await context.SaveChangesAsync();
            await UpdateCompetencyOrders();
            return await GetComp(c.CompetencyId);
        }

        [HttpDelete("{competencyId}")]
        [Permission(Allow = "SVMSecure.CTS.Manage")]
        public async Task<ActionResult<CompetencyDto>> DeleteCompetency(int competencyId)
        {
            var c = await context.Competencies.FindAsync(competencyId);
            if (c == null)
            {
                return NotFound();
            }
            context.Competencies.Remove(c);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest("Could not remove domain. It may be linked to other objects.");
            }
            await UpdateCompetencyOrders();
            return new CompetencyDto(c);
        }

        private async Task UpdateCompetencyOrders()
        {
            var comps = await context.Competencies
                .ToListAsync();

            comps = comps.OrderBy(c =>
            {
                var numbers = c.Number.Split(".");
                var retval = 0;
                if (numbers.Length > 0 && int.TryParse(numbers[0], out int num1))
                {
                    retval += num1 * 1000000;
                }
                if (numbers.Length > 1 && int.TryParse(numbers[1], out int num2))
                {
                    retval += num2 * 10000;
                }
                if (numbers.Length > 2 && int.TryParse(numbers[2], out int num3))
                {
                    retval += num3 * 100;
                }
                if (numbers.Length > 3 && int.TryParse(numbers[3], out int num4))
                {
                    retval += num4;
                }
                return retval;
            }).ToList();

            int order = 1;
            foreach (var comp in comps)
            {
                if (comp.Order != order)
                {
                    comp.Order = order;
                    context.Update(comp);
                }
                order++;
            }

            await context.SaveChangesAsync();
        }
    }
}
