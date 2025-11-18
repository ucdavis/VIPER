using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services
{
    public interface IPhotoService
    {
        Task<byte[]> GetStudentPhotoAsync(string mailId);
        Task<string> GetStudentPhotoUrlAsync(string mailId);
        Task<bool> StudentPhotoExistsAsync(string mailId);
        string GetDefaultPhotoUrl();
    }

    public class PhotoService : IPhotoService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<PhotoService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _idCardPhotoPath;
        private readonly string _defaultPhotoFile;
        private readonly int _cacheDurationHours;

        private const int MinCacheDurationHours = 1;
        private const int MaxCacheDurationHours = 168; // 1 week
        private const int DefaultCacheDurationHours = 24;

        // Compiled regex for mailId validation - compiled once for performance
        private static readonly Regex MailIdValidationRegex = new("^[a-z0-9.-]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public PhotoService(IConfiguration configuration, IMemoryCache cache, ILogger<PhotoService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _cache = cache;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;

            _idCardPhotoPath = configuration["PhotoGallery:IDCardPhotoPath"] ?? @"S:\Files\IDCardPhotos\";
            _defaultPhotoFile = configuration["PhotoGallery:DefaultPhotoFile"] ?? "nopic.jpg";

            if (!int.TryParse(configuration["PhotoGallery:CacheDurationHours"], out var cacheHours) || cacheHours < MinCacheDurationHours || cacheHours > MaxCacheDurationHours)
            {
                cacheHours = DefaultCacheDurationHours;
            }
            _cacheDurationHours = cacheHours;
        }

        public async Task<byte[]> GetStudentPhotoAsync(string mailId)
        {
            if (string.IsNullOrEmpty(mailId))
            {
                return await GetDefaultPhotoAsync();
            }

            var cacheKey = $"photo_{mailId}";

            if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedPhoto) && cachedPhoto != null)
            {
                return cachedPhoto;
            }

            try
            {
                var photoPath = GetPhotoPath(mailId);
                if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
                {
                    var photoData = await File.ReadAllBytesAsync(photoPath);

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours(_cacheDurationHours));

                    _cache.Set(cacheKey, photoData, cacheOptions);

                    return photoData;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied reading photo for student {MailId}", LogSanitizer.SanitizeId(mailId));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading photo for student {MailId}", LogSanitizer.SanitizeId(mailId));
            }

            return await GetDefaultPhotoAsync();
        }

        public async Task<string> GetStudentPhotoUrlAsync(string mailId)
        {
            if (await StudentPhotoExistsAsync(mailId))
            {
                return $"/api/students/photos/student/{mailId}";
            }
            return GetDefaultPhotoUrl();
        }

        public async Task<bool> StudentPhotoExistsAsync(string mailId)
        {
            if (string.IsNullOrEmpty(mailId))
            {
                return false;
            }

            return await Task.Run(() =>
            {
                var photoPath = GetPhotoPath(mailId);
                return !string.IsNullOrEmpty(photoPath) && File.Exists(photoPath);
            });
        }

        public string GetDefaultPhotoUrl()
        {
            return $"/api/students/photos/default";
        }

        private string? GetPhotoPath(string mailId)
        {
            if (string.IsNullOrEmpty(mailId))
            {
                return null;
            }

            // Validate mailId to prevent path traversal - only allow alphanumeric, dots, hyphens
            if (!MailIdValidationRegex.IsMatch(mailId))
            {
                _logger.LogWarning("Rejected photo request with invalid mailId {MailId}", LogSanitizer.SanitizeId(mailId));
                return null;
            }

            // Use Path.GetFileName to strip any path separators
            var fileName = Path.GetFileName(mailId) + ".jpg";

            var idCardPhotoPath = Path.Join(_idCardPhotoPath, fileName);
            if (File.Exists(idCardPhotoPath) && IsUnderRoot(idCardPhotoPath, _idCardPhotoPath))
            {
                return idCardPhotoPath;
            }

            return null;
        }

        private static bool IsUnderRoot(string candidate, string root)
        {
            var fullRoot = Path.GetFullPath(root);
            var fullCandidate = Path.GetFullPath(candidate);
            return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<byte[]> GetDefaultPhotoAsync()
        {
            var cacheKey = "photo_default";

            if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedDefault) && cachedDefault != null)
            {
                return cachedDefault;
            }

            try
            {
                // Try configured path first (e.g., S:\Files\IDCardPhotos\ in production)
                if (!string.IsNullOrEmpty(_idCardPhotoPath))
                {
                    var defaultPath = Path.Join(_idCardPhotoPath, Path.GetFileName(_defaultPhotoFile));
                    if (File.Exists(defaultPath))
                    {
                        var defaultData = await File.ReadAllBytesAsync(defaultPath);

                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromHours((double)_cacheDurationHours * 2));

                        _cache.Set(cacheKey, defaultData, cacheOptions);

                        return defaultData;
                    }
                }

                // Fallback to wwwroot/images/nopic.jpg
                var wwwrootPath = Path.Join(_webHostEnvironment.WebRootPath, "images", Path.GetFileName(_defaultPhotoFile));
                if (File.Exists(wwwrootPath))
                {
                    var defaultData = await File.ReadAllBytesAsync(wwwrootPath);

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours((double)_cacheDurationHours * 2));

                    _cache.Set(cacheKey, defaultData, cacheOptions);

                    _logger.LogInformation("Using default photo from wwwroot: {Path}", wwwrootPath);
                    return defaultData;
                }

                // No default photo found in any location
                var searchedPaths = string.Join(", ", new[] {
                    _idCardPhotoPath,
                    Path.Join(_webHostEnvironment.WebRootPath, "images")
                }.Where(p => !string.IsNullOrEmpty(p)));

                _logger.LogError("Default photo {DefaultPhotoFile} not found in any location. Searched: {SearchedPaths}",
                    _defaultPhotoFile, searchedPaths);
                throw new FileNotFoundException($"Default photo {_defaultPhotoFile} not found in configured paths");
            }
            catch (FileNotFoundException)
            {
                // Already logged above, rethrow as-is
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error reading default photo file {DefaultPhotoFile}", _defaultPhotoFile);
                throw new IOException($"Failed to read default photo file {_defaultPhotoFile}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied reading default photo file {DefaultPhotoFile}", _defaultPhotoFile);
                throw new UnauthorizedAccessException($"Access denied to default photo file {_defaultPhotoFile}", ex);
            }
        }
    }
}
