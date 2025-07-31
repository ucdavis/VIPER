using Viper.Classes;
using Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;

namespace Viper.Areas.CTS.Controllers
{
    /// <summary>
    /// Controller for managing competency bundle associations
    /// Provides filtering capabilities for identifying unbundled competencies
    /// </summary>
    [Route("/api/cts/competency-bundle-associations")]
    [Permission(Allow = "SVMSecure.CTS.Manage")]
    public class CompetencyBundleAssociationController : ApiController
    {
        private readonly VIPERContext context;

        public CompetencyBundleAssociationController(VIPERContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets competencies with their bundle associations
        /// </summary>
        /// <param name="clinical">Filter by clinical flag</param>
        /// <param name="assessment">Filter by assessment flag</param>
        /// <param name="milestone">Filter by milestone flag</param>
        /// <returns>List of competencies with their associated bundles</returns>
        /// <remarks>
        /// When no filters are provided, returns only unbundled competencies.
        /// When filters are active, returns competencies associated with bundles matching the filters.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<List<CompetencyBundleAssociationDto>>> GetCompetencyBundleAssociations(
            bool? clinical = null, 
            bool? assessment = null, 
            bool? milestone = null)
        {
            // Fetch all competencies with their domains and bundle associations
            var allCompetencies = await context.Competencies
                .Include(c => c.Domain)
                .Include(c => c.BundleCompetencies)
                    .ThenInclude(bc => bc.Bundle)
                .OrderBy(c => c.Order)
                .ToListAsync();

            var result = new List<CompetencyBundleAssociationDto>();

            // Process each competency to determine if it should be included based on filters
            foreach (var competency in allCompetencies)
            {
                var dto = new CompetencyBundleAssociationDto(competency);
                
                bool includeCompetency;

                // When no filters are provided, show only unbundled competencies
                if (!clinical.HasValue && !assessment.HasValue && !milestone.HasValue)
                {
                    includeCompetency = dto.Bundles.Count == 0;
                }
                else
                {
                    // Check if any filter is set to true
                    bool anyFilterActive = clinical == true || assessment == true || milestone == true;
                    
                    if (anyFilterActive)
                    {
                        // Include competencies that have bundles matching active filters
                        includeCompetency = dto.Bundles.Any(b => 
                            (clinical == true && b.Clinical) ||
                            (assessment == true && b.Assessment) ||
                            (milestone == true && b.Milestone)
                        );
                    }
                    else
                    {
                        // All filters are false, show unbundled competencies
                        includeCompetency = dto.Bundles.Count == 0;
                    }
                }

                if (includeCompetency)
                {
                    result.Add(dto);
                }
            }

            return result;
        }
    }
}