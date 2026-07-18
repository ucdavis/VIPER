using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Verifies the CMS file store is writable: the store root plus every top-level "VIPER app"
    /// folder under it (the same folders the upload form lists). Uploads move files into these
    /// folders, so a read-only share or a folder the app-pool account can't write to only surfaces
    /// when someone uploads. Probing each folder names which one(s) regressed rather than only
    /// reporting that the drive is mounted (disk-space-cms) or has free space.
    /// </summary>
    public class CmsFileStoreHealthCheck : IHealthCheck
    {
        private readonly string? _rootPath;
        private readonly bool _healthyWhenMissing;

        public CmsFileStoreHealthCheck(string? rootPath, bool healthyWhenMissing = false)
        {
            _rootPath = rootPath;
            _healthyWhenMissing = healthyWhenMissing;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_rootPath))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("CMS file store root is not configured."));
            }
            if (!Directory.Exists(_rootPath))
            {
                return Task.FromResult(_healthyWhenMissing
                    ? HealthCheckResult.Healthy($"Store root '{_rootPath}' not present (skipped).")
                    : HealthCheckResult.Unhealthy($"Store root '{_rootPath}' does not exist."));
            }

            // Listing the root is the read test; the probe below is the write test. Probe the root
            // (needed to create new app folders) as well as every existing top-level folder.
            List<string> targets;
            try
            {
                targets = new DirectoryInfo(_rootPath).GetDirectories().Select(d => d.FullName).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Store root '{_rootPath}' is not readable: {ex.Message}"));
            }
            targets.Insert(0, _rootPath);

            var notWritable = targets.Select(ProbeWritable).OfType<string>().ToList();

            var data = new Dictionary<string, object>
            {
                ["folders_checked"] = targets.Count,
                ["not_writable"] = notWritable,
            };
            if (notWritable.Count > 0)
            {
                // One folder per line; the dashboard renders \n via white-space: pre-line
                // (healthchecks-ui-branding.css).
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{notWritable.Count} of {targets.Count} CMS folder(s) not writable:\n{string.Join("\n", notWritable)}",
                    data: data));
            }
            return Task.FromResult(HealthCheckResult.Healthy(
                $"CMS file store writable ({targets.Count} folder(s), incl. root).", data: data));
        }

        // Returns null when the directory accepts and releases a probe file; otherwise
        // "<dir> (<reason>)". Both halves matter: uploads create files and CMS replace/delete
        // remove them, so a folder that can be written but not cleaned up is not fully usable
        // (and an undeletable probe would otherwise accumulate on every poll).
        private static string? ProbeWritable(string dir)
        {
            // Unique probe name per invocation so overlapping polls don't race on a shared file.
            var probePath = Path.Join(dir, $".health-probe-{Environment.ProcessId}-{Guid.NewGuid():N}");
            try
            {
                File.WriteAllBytes(probePath, Array.Empty<byte>());
            }
            // A short reason, not ex.Message, which would repeat the probe's full path and GUID.
            catch (UnauthorizedAccessException)
            {
                return $"{dir} (access denied)";
            }
            catch (IOException)
            {
                return $"{dir} (not writable)";
            }
            try
            {
                File.Delete(probePath);
            }
            catch (UnauthorizedAccessException)
            {
                return $"{dir} (created but not deletable: access denied)";
            }
            catch (IOException)
            {
                return $"{dir} (created but not deletable)";
            }
            return null;
        }
    }
}
