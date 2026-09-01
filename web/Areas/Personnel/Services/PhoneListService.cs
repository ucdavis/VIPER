using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhoneListService
    {
        private readonly PhonesDbContext _context;
        public PhoneListService(PhonesDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves a list by its stable Code (e.g. "VMDO"). Every list-scoped request enters
        /// through here, so the list the caller named is the same one used for the permission
        /// check and the data query.
        /// </summary>
        public async Task<PhoneList> GetListByCode(string code, CancellationToken ct = default)
        {
            var list = await _context.PhoneList
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == code, ct);
            if (list == null)
            {
                throw new InvalidOperationException("Phone list not found");
            }
            return list;
        }
    }
}
