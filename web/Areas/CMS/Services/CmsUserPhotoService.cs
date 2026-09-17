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
        /// Get a user's primary photo by any supported id (MailId, LoginId, IamId, or MothraId).
        /// Resolution matches legacy userPhoto.cfc: id-card photo (by MailId), then the default
        /// "no picture" image - this always returns a result, which makes it the right choice
        /// for most places a photo is displayed. The result carries a Last-Modified value for
        /// conditional caching.
        /// </summary>
        Task<CmsUserPhotoResult> GetUserPhotoAsync(string? mailId, string? loginId, string? iamId, string? mothraId,
            CancellationToken ct = default);

        /// <summary>
        /// Get a user's alternate profile photo (by IamId) from the ProfilePhotos store. Unlike
        /// GetUserPhotoAsync, this has no placeholder fallback: it returns null when the person
        /// has no alternate photo, so a caller that only wants to show it when one actually
        /// exists (e.g. UserInfo's second photo slot) can tell that apart from a successful
        /// lookup, instead of it being replaced by the primary photo.
        /// </summary>
        Task<CmsUserPhotoResult?> GetAlternatePhotoAsync(string? mailId, string? loginId, string? iamId,
            string? mothraId, CancellationToken ct = default);
    }

    /// <summary>
    /// A served photo's bytes plus the timestamp to publish as Last-Modified/use for
    /// If-Modified-Since comparisons. Always truncated to whole seconds, since HTTP dates
    /// have second precision.
    /// </summary>
    public sealed record CmsUserPhotoResult(byte[] Bytes, DateTimeOffset LastModified);

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

        // The delegated Students photo pipeline (id-card photo and its nopic fallback) doesn't
        // expose a per-file timestamp, so those two outcomes share this stable per-process
        // proxy for Last-Modified: it only changes on deploy/restart, which is exactly when a
        // browser-cached copy should be treated as stale.
        private static readonly DateTimeOffset DelegatedPhotoLastModified = TruncateToSeconds(DateTimeOffset.UtcNow);

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

        public async Task<CmsUserPhotoResult> GetUserPhotoAsync(string? mailId, string? loginId, string? iamId,
            string? mothraId, CancellationToken ct = default)
        {
            // Only mailId is needed for the id-card photo; skip resolving iamId.
            (mailId, _) = await ResolveIdsAsync(mailId, loginId, iamId, mothraId, needIamId: false, ct);

            var bytes = await _photoService.GetStudentPhotoAsync(mailId ?? string.Empty);
            return new CmsUserPhotoResult(bytes, DelegatedPhotoLastModified);
        }

        public async Task<CmsUserPhotoResult?> GetAlternatePhotoAsync(string? mailId, string? loginId,
            string? iamId, string? mothraId, CancellationToken ct = default)
        {
            // The alternate photo is keyed by iamId; resolve it if the caller didn't already
            // have it (e.g. UserInfo's <img> tags only send mailId).
            (_, iamId) = await ResolveIdsAsync(mailId, loginId, iamId, mothraId, needIamId: true, ct);

            // Unlike GetUserPhotoAsync, there's no "nopic" placeholder fallback here - a missing
            // alternate photo returns null so the caller can leave the image out entirely rather
            // than silently substituting the primary photo in its place.
            return iamId != null ? await ReadAltPhotoAsync(iamId, ct) : null;
        }

        private async Task<(string? MailId, string? IamId)> ResolveIdsAsync(string? mailId, string? loginId,
            string? iamId, string? mothraId, bool needIamId, CancellationToken ct)
        {
            // Skip the lookup when every id the caller needs is already known. A mailId alone
            // serves the id-card photo with no DB query (the legacy hot path for <img> tags).
            if (!string.IsNullOrEmpty(mailId) && (!string.IsNullOrEmpty(iamId) || !needIamId))
            {
                return (mailId, iamId);
            }

            var query = _aaudContext.AaudUsers.AsNoTracking().Where(u => u.Current != 0 || u.Future != 0);
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
            else if (!string.IsNullOrEmpty(mothraId))
            {
                query = query.Where(u => u.MothraId == mothraId);
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

        private async Task<CmsUserPhotoResult?> ReadAltPhotoAsync(string iamId, CancellationToken ct)
        {
            if (!SafeIdRegex().IsMatch(iamId))
            {
                _logger.LogWarning("Rejected alt photo request with invalid iamId {IamId}", LogSanitizer.SanitizeId(iamId));
                return null;
            }

            var photoPath = Path.GetFullPath(Path.Join(_profilePhotoPath, Path.GetFileName(iamId) + ".jpg"));
            var root = Path.GetFullPath(_profilePhotoPath + Path.DirectorySeparatorChar);
            if (!photoPath.StartsWith(root, CmsFilePathSafety.PathComparison) || !File.Exists(photoPath))
            {
                return null;
            }

            try
            {
                var bytes = await File.ReadAllBytesAsync(photoPath, ct);
                var lastModified = TruncateToSeconds(new DateTimeOffset(File.GetLastWriteTimeUtc(photoPath), TimeSpan.Zero));
                return new CmsUserPhotoResult(bytes, lastModified);
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

        // HTTP dates (and If-Modified-Since comparisons) only have second precision; truncating
        // here keeps a stable value fed back on the next request's comparison.
        private static DateTimeOffset TruncateToSeconds(DateTimeOffset value)
        {
            return value.AddTicks(-(value.UtcTicks % TimeSpan.TicksPerSecond));
        }
    }
}
