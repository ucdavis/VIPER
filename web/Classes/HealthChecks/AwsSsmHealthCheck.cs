using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Verifies AWS SSM Parameter Store is reachable with the app's credentials.
    /// Probes via GetParametersByPath against the same path prefix the
    /// configuration loader uses (Program.cs .AddSystemsManager). This shares
    /// the ssm:GetParametersByPath permission the app already needs, so a
    /// least-privilege role passes here exactly when startup config load works -
    /// DescribeParameters would require a separate ssm:DescribeParameters grant.
    /// </summary>
    public class AwsSsmHealthCheck : IHealthCheck
    {
        private readonly RegionEndpoint _region;
        private readonly string _probePath;
        private readonly bool _healthyWhenMissing;

        /// <param name="probePath">
        /// SSM path prefix to probe (e.g. "/Shared"). Use a path the app's
        /// configuration loader already reads so this check exercises the same
        /// IAM permission.
        /// </param>
        /// <param name="healthyWhenMissing">
        /// If true, missing credentials or client-side SDK errors return Healthy
        /// with a "skipped" description. Use for Development where local machines
        /// may not have AWS credentials configured.
        /// </param>
        public AwsSsmHealthCheck(
            string probePath = "/Shared",
            RegionEndpoint? region = null,
            bool healthyWhenMissing = false)
        {
            _region = region ?? RegionEndpoint.USWest1;
            _probePath = probePath;
            _healthyWhenMissing = healthyWhenMissing;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new AmazonSimpleSystemsManagementClient(_region);
                await client.GetParametersByPathAsync(
                    new GetParametersByPathRequest
                    {
                        Path = _probePath,
                        MaxResults = 1,
                    },
                    cancellationToken);
                return HealthCheckResult.Healthy("AWS SSM reachable.");
            }
            catch (AmazonServiceException ex)
            {
                return _healthyWhenMissing
                    ? HealthCheckResult.Healthy("AWS SSM not configured (skipped).")
                    : HealthCheckResult.Unhealthy(
                        $"AWS SSM unreachable: {ex.ErrorCode}: {ex.Message}", ex);
            }
            catch (AmazonClientException ex)
            {
                return _healthyWhenMissing
                    ? HealthCheckResult.Healthy("AWS SSM not configured (skipped).")
                    : HealthCheckResult.Unhealthy(
                        $"AWS SSM client error: {ex.Message}", ex);
            }
        }
    }
}
