using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Reports free space on the drive hosting the running application.
    /// Resolves the drive at runtime so the same check works on any deploy target
    /// without per-environment config. Thresholds are percent-based so a single
    /// default works across drive sizes (a 1 GB floor is alarming on a 20 GB
    /// drive and meaningless on a 2 TB drive).
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string? _explicitDrivePath;
        private readonly double _criticalFreePercent;
        private readonly double _warningFreePercent;
        private readonly bool _healthyWhenMissing;
        private readonly bool _requirePathExists;
        private readonly bool _verifyWritable;

        /// <param name="explicitDrivePath">
        /// Drive or path to monitor. If null, the drive hosting the running app is used.
        /// Pass e.g. "S:\\" (or any path on that drive) to monitor an alternate volume.
        /// </param>
        /// <param name="criticalFreePercent">
        /// Below this free-space percentage the check returns Unhealthy.
        /// </param>
        /// <param name="warningFreePercent">
        /// Below this free-space percentage (and above criticalFreePercent) the
        /// check returns Degraded.
        /// </param>
        /// <param name="healthyWhenMissing">
        /// If true, a missing or unready drive returns Healthy with a "not mounted"
        /// description. Use for optional drives (e.g., network shares that don't
        /// exist on developer machines). Defaults to false (missing drive = Unhealthy).
        /// </param>
        /// <param name="requirePathExists">
        /// If true, also verify the supplied explicitDrivePath is an existing directory
        /// (not just that its drive is ready). Use for checks where the application
        /// writes to a specific sub-path and its absence is a real failure. Ignored
        /// when explicitDrivePath is null.
        /// </param>
        /// <param name="verifyWritable">
        /// If true, attempt a zero-byte file create + delete in the target directory
        /// to confirm the path is actually writable (catches read-only mounts and
        /// ACL regressions that a disk-space check misses). Windows has no reliable
        /// "can I write?" API short of actually writing, so this is the minimal probe.
        /// Requires explicitDrivePath to be set and point at a directory.
        /// </param>
        public DiskSpaceHealthCheck(
            string? explicitDrivePath = null,
            double criticalFreePercent = 5.0,
            double warningFreePercent = 10.0,
            bool healthyWhenMissing = false,
            bool requirePathExists = false,
            bool verifyWritable = false)
        {
            _explicitDrivePath = explicitDrivePath;
            _criticalFreePercent = criticalFreePercent;
            _warningFreePercent = warningFreePercent;
            _healthyWhenMissing = healthyWhenMissing;
            _requirePathExists = requirePathExists;
            _verifyWritable = verifyWritable;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Caller asked us to verify a configured path but didn't supply one
            // (e.g. LoggingPath is missing/empty in config) - that's a real
            // misconfiguration, not a "drive not mounted" case.
            if ((_requirePathExists || _verifyWritable)
                && string.IsNullOrWhiteSpace(_explicitDrivePath))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Configured path is empty."));
            }

            var driveRoot = _explicitDrivePath is null
                ? Path.GetPathRoot(AppContext.BaseDirectory)
                : Path.GetPathRoot(_explicitDrivePath);
            if (string.IsNullOrEmpty(driveRoot))
            {
                return Task.FromResult(_healthyWhenMissing
                    ? HealthCheckResult.Healthy("Drive not mounted (skipped).")
                    : HealthCheckResult.Unhealthy("Could not determine drive to monitor."));
            }

            var drive = new DriveInfo(driveRoot);
            if (!drive.IsReady)
            {
                return Task.FromResult(_healthyWhenMissing
                    ? HealthCheckResult.Healthy("Drive not mounted (skipped).")
                    : HealthCheckResult.Unhealthy("Drive not ready."));
            }

            // The early empty-path guard above means a true _requirePathExists
            // or _verifyWritable implies _explicitDrivePath is non-null here.
            if (_requirePathExists && !Directory.Exists(_explicitDrivePath!))
            {
                return Task.FromResult(_healthyWhenMissing
                    ? HealthCheckResult.Healthy($"Path '{_explicitDrivePath}' does not exist (skipped).")
                    : HealthCheckResult.Unhealthy($"Path '{_explicitDrivePath}' does not exist."));
            }

            if (_verifyWritable)
            {
                // Unique probe name per invocation - overlapping UI polls + /health/detail
                // requests would otherwise race on a shared file name and produce
                // intermittent false Unhealthy results.
                var probePath = Path.Join(
                    _explicitDrivePath,
                    $".health-probe-{Environment.ProcessId}-{Guid.NewGuid():N}");
                try
                {
                    File.WriteAllBytes(probePath, Array.Empty<byte>());
                }
                catch (UnauthorizedAccessException)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Path '{_explicitDrivePath}' not writable: access denied."));
                }
                catch (IOException ex)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Path '{_explicitDrivePath}' not writable: {ex.Message}"));
                }
                finally
                {
                    if (File.Exists(probePath))
                    {
                        try
                        {
                            File.Delete(probePath);
                        }
                        // Best-effort cleanup; unique name means a missed delete
                        // doesn't break future probes, just leaves a 0-byte file.
                        catch (IOException ex)
                        {
                            _logger.Debug(ex, "Disk-space probe cleanup failed at {Path}", probePath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _logger.Debug(ex, "Disk-space probe cleanup denied at {Path}", probePath);
                        }
                    }
                }
            }

            var freeBytes = drive.AvailableFreeSpace;
            var totalBytes = drive.TotalSize;
            var freeGb = Math.Round(freeBytes / (1024.0 * 1024.0 * 1024.0), 1);
            var freePercent = Math.Round(freeBytes * 100.0 / totalBytes, 1);
            var data = new Dictionary<string, object>
            {
                ["drive"] = drive.Name,
                ["free_gb"] = freeGb,
                ["total_gb"] = Math.Round(totalBytes / (1024.0 * 1024.0 * 1024.0), 1),
                ["free_percent"] = freePercent,
            };

            if (freePercent < _criticalFreePercent)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Low disk space: {freeGb} GB free ({freePercent}%).", data: data));
            }

            if (freePercent < _warningFreePercent)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk space getting low: {freeGb} GB free ({freePercent}%).", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space OK: {freeGb} GB free ({freePercent}%).", data: data));
        }
    }
}
