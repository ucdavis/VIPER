using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Services;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.CMS.Services
{
    public interface ICmsUserPhotoService
    {
        /// <summary>
        /// Get a user photo by any supported id. Resolution order matches legacy userPhoto.cfc:
        /// alternate profile photo (by IamId, only when requested via preferAltPhoto),
        /// then id-card photo (by MailId), then the default "no picture" image.
        /// </summary>
        Task<byte[]> GetUserPhotoAsync(string? mailId, string? loginId, string? iamId, bool preferAltPhoto, CancellationToken ct = default);
    }

    /// <summary>
    /// User photos for the CMS area (legacy userPhoto.cfc). Id-card photo lookup, caching, and
    /// the default-photo fallback are delegated to the Students area IPhotoService so there is
    /// a single photo pipeline; this service adds AAUD id resolution and the alternate
    /// ProfilePhotos store.
    /// </summary>
    public partial class CmsUserPhotoService : ICmsUserPhotoService
    {
        private readonly AAUDContext _aaudContext;
        private readonly IPhotoService _photoService;
        private readonly ILogger<CmsUserPhotoService> _logger;
        private readonly string _profilePhotoPath;

        [GeneratedRegex("^[a-zA-Z0-9.-]+$")]
        private static partial Regex SafeIdRegex();

        public CmsUserPhotoService(AAUDContext aaudContext, IPhotoService photoService,
            IConfiguration configuration, ILogger<CmsUserPhotoService> logger)
        {
            _aaudContext = aaudContext;
            _photoService = photoService;
            _logger = logger;
            // Alternate profile photos live beside IDCardPhotos under the file storage root.
            _profilePhotoPath = configuration["CMS:ProfilePhotoPath"]
                ?? Path.Join(Data.CMS.GetRootFileFolder(), "ProfilePhotos");
        }

        public async Task<byte[]> GetUserPhotoAsync(string? mailId, string? loginId, string? iamId,
            bool preferAltPhoto, CancellationToken ct = default)
        {
            // Resolve whichever id was provided to the person's mailId (+ iamId when needed).
            (mailId, iamId) = await ResolveIdsAsync(mailId, loginId, iamId, needIamId: preferAltPhoto, ct);

            if (preferAltPhoto && iamId != null)
            {
                var altPhoto = await ReadAltPhotoAsync(iamId, ct);
                if (altPhoto != null)
                {
                    return altPhoto;
                }
            }

            return await _photoService.GetStudentPhotoAsync(mailId ?? string.Empty);
        }

        private async Task<(string? MailId, string? IamId)> ResolveIdsAsync(string? mailId, string? loginId,
            string? iamId, bool needIamId, CancellationToken ct)
        {
            // Skip the lookup when every id the caller needs is already known. A mailId alone
            // serves the id-card photo with no DB query (the legacy hot path for <img> tags).
            if (!string.IsNullOrEmpty(mailId) && (!string.IsNullOrEmpty(iamId) || !needIamId))
            {
                return (mailId, iamId);
            }

            var query = _aaudContext.AaudUsers.AsNoTracking().Where(u => u.Current != 0);
            if (!string.IsNullOrEmpty(mailId))
            {
                query = query.Where(u => u.MailId == mailId);
            }
            else if (!string.IsNullOrEmpty(loginId))
            {
                query = query.Where(u => u.LoginId == loginId);
            }
            else if (!string.IsNullOrEmpty(iamId))
            {
                query = query.Where(u => u.IamId == iamId);
            }
            else
            {
                return (null, null);
            }

            var user = await query
                .Select(u => new { u.MailId, u.IamId })
                .FirstOrDefaultAsync(ct);
            return user == null ? (mailId, iamId) : (user.MailId ?? mailId, user.IamId ?? iamId);
        }

        private async Task<byte[]?> ReadAltPhotoAsync(string iamId, CancellationToken ct)
        {
            if (!SafeIdRegex().IsMatch(iamId))
            {
                _logger.LogWarning("Rejected alt photo request with invalid iamId {IamId}", LogSanitizer.SanitizeId(iamId));
                return null;
            }

            var photoPath = Path.GetFullPath(Path.Join(_profilePhotoPath, Path.GetFileName(iamId) + ".jpg"));
            var root = Path.GetFullPath(_profilePhotoPath + Path.DirectorySeparatorChar);
            if (!photoPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(photoPath))
            {
                return null;
            }

            try
            {
                return await System.IO.File.ReadAllBytesAsync(photoPath, ct);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading alt photo for {IamId}", LogSanitizer.SanitizeId(iamId));
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied reading alt photo for {IamId}", LogSanitizer.SanitizeId(iamId));
                return null;
            }
        }
    }
}
