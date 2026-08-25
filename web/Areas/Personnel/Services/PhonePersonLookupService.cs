using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.Personnel.Services
{
    public class PhonePersonLookupService(PhonesDbContext context, PhonePermissionsService phonePermissionsService)
    {
        private readonly PhonesDbContext _context = context;
        private readonly PhonePermissionsService _phonePermissionsService = phonePermissionsService;

        /// <summary>
        /// Retrieves the PhonePerson records associated with a list of iam IDs.
        /// </summary>
        public async Task<List<PhonePerson>> GetPhonePeople(List<string> iamIds, PhoneList? list = null, CancellationToken ct = default)
        {
            // Avoid returning direct numbers except to users with permissions to access them.
            // This data should only be returned for queries tied to a list for which the user
            // has maintain permissions.
            bool canAccessDirectNumber = list != null && _phonePermissionsService.CanMaintainList(list);

            List<string> cleanedIamIds = [.. iamIds.Where(x => !string.IsNullOrWhiteSpace(x))];

            var searchResults = await _context.PhonePerson
                .AsNoTracking()
                // May contain up to 25 IDs, so use EF.Parameter for better caching.
                .Where(t => EF.Parameter(cleanedIamIds).Contains(t.PersonIam))
                .Select(t => new PhonePerson
                {
                    PersonIam = t.PersonIam,
                    Phone = t.Phone,
                    DirectPhone = canAccessDirectNumber ? t.DirectPhone : "",
                    Office = t.Office,
                    ModifiedDate = t.ModifiedDate,
                    ModifiedBy = t.ModifiedBy
                })
                .ToListAsync(ct);

            return searchResults;
        }

        /// <summary>
        /// Get current employees. Optionally may filter by a partial name match.
        /// Limits the results to 25 at most.
        /// </summary>
        public async Task<List<ViperPerson>> GetViperCurrentEmployees(string? search = null, CancellationToken ct = default)
        {
            search = PersonSearchHelper.Normalize(search);
            if (search == null)
            {
                return [];
            }

            var query = _context.ViperPerson
                .AsNoTracking()
                .Where(t => t.CurrentEmployee)
                .Where(PersonSearchHelper.NameMatches<ViperPerson>(t => t.LastName, t => t.FirstName, search))
                .Select(t => new ViperPerson
                {
                    IamId = t.IamId,
                    FirstName = t.FirstName,
                    LastName = t.LastName,
                    FullName = t.FullName,
                    CurrentEmployee = t.CurrentEmployee,
                    MailId = t.MailId
                });

            return await PersonSearchHelper.OrderAndCap(query, t => t.LastName, t => t.FirstName).ToListAsync(ct);
        }
    }
}
