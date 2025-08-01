using Viper.Classes;
using Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Controllers
{
    /// <summary>
    /// Controller for managing competency bundle associations
    /// Provides filtering capabilities for identifying unbundled competencies
    /// </summary>
    [Route("/api/cts/competencies/bundle-associations")]
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
            // Build query with includes
            var query = context.Competencies
                .Include(c => c.Domain)
                .Include(c => c.Parent)
                .Include(c => c.BundleCompetencies)
                    .ThenInclude(bc => bc.Bundle)
                .AsNoTracking();

            // Apply database-side filtering to avoid loading unnecessary data
            if (!clinical.HasValue && !assessment.HasValue && !milestone.HasValue)
            {
                // No filters provided, fetch only unbundled competencies
                query = ApplyUnbundledFilter(query).OrderBy(c => c.Order);
            }
            else
            {
                // Check if any filter is set to true
                bool hasActiveFilters = clinical == true || assessment == true || milestone == true;

                if (hasActiveFilters)
                {
                    // Include competencies that have bundles matching active filters (OR logic)
                    query = query.Where(c => c.BundleCompetencies.Any(bc =>
                        (clinical == true && bc.Bundle.Clinical) ||
                        (assessment == true && bc.Bundle.Assessment) ||
                        (milestone == true && bc.Bundle.Milestone)
                    )).OrderBy(c => c.Order);
                }
                else
                {
                    // All filters are false, show unbundled competencies
                    query = ApplyUnbundledFilter(query).OrderBy(c => c.Order);
                }
            }

            // Execute the query and map results to DTOs
            var filteredCompetencies = await query.ToListAsync();
            var result = filteredCompetencies
                .Select(c => new CompetencyBundleAssociationDto(c))
                .ToList();

            return result;
        }

        /// <summary>
        /// Applies filter to return only competencies that are not associated with any bundles
        /// </summary>
        /// <param name="query">The competency query to filter</param>
        /// <returns>Filtered query showing only unbundled competencies</returns>
        private static IQueryable<Competency> ApplyUnbundledFilter(IQueryable<Competency> query)
        {
            return query.Where(c => !c.BundleCompetencies.Any());
        }
    }
}