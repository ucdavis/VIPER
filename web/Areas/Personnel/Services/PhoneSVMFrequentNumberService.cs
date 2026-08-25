using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Services
{
    public class PhoneSVMFrequentNumberService(PhonesDbContext context, IUserHelper userHelper)
    {
        private readonly PhonesDbContext _context = context;
        private readonly IUserHelper _userHelper = userHelper;

        /// <summary>
        /// Get all frequent numbers for the SVM phone list.
        /// </summary>
        public async Task<List<SVMFrequentNumber>> GetSVMFrequentNumbers(CancellationToken ct = default)
        {
            return await _context.SVMFrequentNumber
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder == null)
                .ThenBy(t => t.SortOrder)
                .ThenBy(t => t.Label)
                .ToListAsync(ct);
        }

        public async Task<DateTime?> GetSVMFrequentNumbersModifiedDate(CancellationToken ct = default)
        {
            var results = _context.SVMFrequentNumber
                .AsNoTracking()
                .Where(t => t.ModifiedDate != null)
                .OrderByDescending(t => t.ModifiedDate);
            var lastModifiedRecord = await results.FirstOrDefaultAsync(ct);
            return lastModifiedRecord?.ModifiedDate;
        }

        /// <summary>
        /// Adds a row to the SVM frequent numbers list.
        /// </summary>
        /// 
        public async Task AddFrequentNumber(SVMFrequentNumberRequest request, CancellationToken ct = default)
        {
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            if (string.IsNullOrWhiteSpace(request.Label))
            {
                throw new InvalidOperationException("Location must not be empty.");
            }
            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new InvalidOperationException("Phone Number must not be empty.");
            }
            var frequentNumber = new SVMFrequentNumber
            {
                Label = request.Label.Trim(),
                Phone = request.Phone.Trim(),
                ModifiedBy = userIam,
                ModifiedDate = DateTime.Now,
                IsActive = true
            };
            await _context.SVMFrequentNumber.AddAsync(frequentNumber, ct);
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Updates a row in the SVM frequently called numbers list.
        /// </summary>
        public async Task UpdateFrequentNumber(int entryId, SVMFrequentNumberRequest request, CancellationToken ct = default)
        {
            var userIam = _userHelper.GetCurrentUser()?.IamId;
            if (string.IsNullOrWhiteSpace(request.Label))
            {
                throw new InvalidOperationException("Location must not be empty.");
            }
            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new InvalidOperationException("Phone Number must not be empty.");
            }
            var frequentNumber = await _context.SVMFrequentNumber.FindAsync(new object?[] { entryId }, ct);
            if (frequentNumber != null && frequentNumber.IsActive)
            {
                frequentNumber.Label = request.Label.Trim();
                frequentNumber.Phone = request.Phone.Trim();
                frequentNumber.ModifiedBy = userIam;
                frequentNumber.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync(ct);
            }
            else
            {
                throw new InvalidOperationException("Frequent number not found");
            }
        }

        /// <summary>
        /// Removes a frequently called number.
        /// </summary>
        public async Task DeleteFrequentNumber(int entryId, CancellationToken ct = default)
        {
            if (entryId <= 0)
            {
                throw new InvalidOperationException("Frequent number is already deleted");
            }

            var userIam = _userHelper.GetCurrentUser()?.IamId;
            var numberToDelete = await _context.SVMFrequentNumber.FindAsync(new object?[] { entryId }, ct);
            if (numberToDelete != null && numberToDelete.IsActive)
            {
                // Instead of deleting the record, mark it as inactive.
                // This allows more accurate tracking of the time the list
                // was last modified.
                numberToDelete.IsActive = false;
                numberToDelete.ModifiedBy = userIam;
                numberToDelete.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync(ct);
            }
            else
            {
                throw new InvalidOperationException("Frequent number is already deleted");
            }
        }
    }
}
