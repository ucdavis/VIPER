using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

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
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PhotoService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _idCardPhotoPath;
        private readonly string _defaultPhotoFile;
        private readonly int _cacheDurationHours;

        public PhotoService(IConfiguration configuration, IMemoryCache cache, ILogger<PhotoService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;

            _idCardPhotoPath = _configuration["PhotoGallery:IDCardPhotoPath"] ?? @"S:\Files\IDCardPhotos\";
            _defaultPhotoFile = _configuration["PhotoGallery:DefaultPhotoFile"] ?? "nopic.jpg";
            _cacheDurationHours = int.Parse(_configuration["PhotoGallery:CacheDurationHours"] ?? "24");
        }

        public async Task<byte[]> GetStudentPhotoAsync(string mailId)
        {
            if (string.IsNullOrEmpty(mailId))
            {
                return await GetDefaultPhotoAsync();
            }

            var cacheKey = $"photo_{mailId}";

            if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedPhoto))
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading photo for student {MailId}", mailId);
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
            if (!Regex.IsMatch(mailId, "^[a-z0-9.-]+$", RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Rejected photo request with invalid mailId {MailId}", mailId);
                return null;
            }

            // Use Path.GetFileName to strip any path separators
            var fileName = Path.GetFileName(mailId) + ".jpg";

            var idCardPhotoPath = Path.Combine(_idCardPhotoPath, fileName);
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

            if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedDefault))
            {
                return cachedDefault;
            }

            try
            {
                // Try configured path first (e.g., S:\Files\IDCardPhotos\ in production)
                if (!string.IsNullOrEmpty(_idCardPhotoPath))
                {
                    var defaultPath = Path.Combine(_idCardPhotoPath, _defaultPhotoFile);
                    if (File.Exists(defaultPath))
                    {
                        var defaultData = await File.ReadAllBytesAsync(defaultPath);

                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromHours(_cacheDurationHours * 2));

                        _cache.Set(cacheKey, defaultData, cacheOptions);

                        return defaultData;
                    }
                }

                // Fallback to wwwroot/images/nopic.jpg
                var wwwrootPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", _defaultPhotoFile);
                if (File.Exists(wwwrootPath))
                {
                    var defaultData = await File.ReadAllBytesAsync(wwwrootPath);

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromHours(_cacheDurationHours * 2));

                    _cache.Set(cacheKey, defaultData, cacheOptions);

                    _logger.LogInformation("Using default photo from wwwroot: {Path}", wwwrootPath);
                    return defaultData;
                }

                _logger.LogError("Default photo not found in configured path or wwwroot/images");
                throw new FileNotFoundException($"Default photo {_defaultPhotoFile} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading default photo");
                throw;
            }
        }
    }
}
