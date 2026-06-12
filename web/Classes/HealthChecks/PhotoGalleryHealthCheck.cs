using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Verifies the ID-card photo directory is reachable and holds more than one
    /// photo.
    /// Severity reflects user impact: an unreachable directory is Degraded (the app
    /// still works, falling back to the default image), while a reachable but
    /// near-empty directory is Unhealthy (the photo data itself is gone).
    /// Photos are stored flat as "&lt;mailId&gt;.jpg" (see PhotoService), so a
    /// top-level *.jpg scan is sufficient.
    /// </summary>
    public class PhotoGalleryHealthCheck : IHealthCheck
    {
        private readonly string? _photoPath;
        private readonly bool _healthyWhenMissing;

        // A healthy gallery holds real student photos, not just the placeholder
        // (nopic.jpg). Requiring more than one rejects a share that is empty or
        // holds only the default image.
        private const int MinimumPhotoCount = 2;

        /// <summary>
        /// Creates a check for the given ID-card photo directory.
        /// </summary>
        /// <param name="photoPath">
        /// Directory holding the ID-card photos (PhotoGallery:IDCardPhotoPath).
        /// </param>
        /// <param name="healthyWhenMissing">
        /// If true, a missing directory returns Healthy with a "skipped"
        /// description rather than Unhealthy. Use in Development, where the photo
        /// share is a network path not mounted on developer machines.
        /// </param>
        public PhotoGalleryHealthCheck(string? photoPath, bool healthyWhenMissing = false)
        {
            _photoPath = photoPath;
            _healthyWhenMissing = healthyWhenMissing;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_photoPath))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Photo path is not configured."));
            }

            if (!Directory.Exists(_photoPath))
            {
                return Task.FromResult(_healthyWhenMissing
                    ? HealthCheckResult.Healthy($"Photo directory '{_photoPath}' not mounted (skipped).")
                    : HealthCheckResult.Degraded($"Photo directory '{_photoPath}' is not reachable; serving the default image."));
            }

            int photoCount;
            try
            {
                // MatchType.Simple avoids the Win32 quirk where "*.jpg" also matches
                // longer extensions (e.g. ".jpginfo"); CaseInsensitive keeps ".JPG"
                // counted regardless of the platform default.
                var options = new EnumerationOptions
                {
                    MatchType = MatchType.Simple,
                    MatchCasing = MatchCasing.CaseInsensitive,
                };
                photoCount = Directory.EnumerateFiles(_photoPath, "*.jpg", options).Count();
            }
            catch (UnauthorizedAccessException)
            {
                // Can't read the share, so we can't judge the photo count - treat
                // as unreachable (Degraded), not empty (Unhealthy).
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Photo directory '{_photoPath}' not readable: access denied."));
            }
            catch (IOException ex)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Photo directory '{_photoPath}' not readable: {ex.Message}"));
            }

            var data = new Dictionary<string, object>
            {
                ["path"] = _photoPath,
                ["photo_count"] = photoCount,
            };

            if (photoCount < MinimumPhotoCount)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Photo directory '{_photoPath}' has too few photos: {photoCount} (expected at least {MinimumPhotoCount}).",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Photo gallery OK: {photoCount} photos.", data: data));
        }
    }
}
