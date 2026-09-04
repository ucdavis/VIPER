using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhoneSVMSectionService
    {
        private readonly PhonesDbContext _context;
        public PhoneSVMSectionService(PhonesDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all Sections for the SVM phone list.
        /// </summary>
        public async Task<List<SVMSection>> GetSVMSections(CancellationToken ct = default)
        {
            var allSections = await _context.SVMSection
                .AsNoTracking()
                .OrderBy(t => t.SortOrder == null)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            return allSections;
        }
    }
}
