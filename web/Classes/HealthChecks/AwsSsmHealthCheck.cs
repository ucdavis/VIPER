using Amazon;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Verifies AWS SSM Parameter Store is reachable with the app's credentials.
    /// Uses a lightweight DescribeParameters probe (MaxResults=1) so the check
    /// does not actually fetch any parameter values.
    /// </summary>
    public class AwsSsmHealthCheck : IHealthCheck
    {
        private readonly RegionEndpoint _region;
        private readonly bool _healthyWhenMissing;

        /// <param name="healthyWhenMissing">
        /// If true, missing credentials or client-side SDK errors return Healthy
        /// with a "skipped" description. Use for Development where local machines
        /// may not have AWS credentials configured.
        /// </param>
        public AwsSsmHealthCheck(RegionEndpoint? region = null, bool healthyWhenMissing = false)
        {
            _region = region ?? RegionEndpoint.USWest1;
            _healthyWhenMissing = healthyWhenMissing;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new AmazonSimpleSystemsManagementClient(_region);
                await client.DescribeParametersAsync(
                    new DescribeParametersRequest { MaxResults = 1 },
                    cancellationToken);
                return HealthCheckResult.Healthy("AWS SSM reachable.");
            }
            catch (AmazonServiceException ex)
            {
                return _healthyWhenMissing
                    ? HealthCheckResult.Healthy("AWS SSM not configured (skipped).")
                    : HealthCheckResult.Unhealthy($"AWS SSM unreachable: {ex.ErrorCode}.");
            }
            catch (AmazonClientException)
            {
                return _healthyWhenMissing
                    ? HealthCheckResult.Healthy("AWS SSM not configured (skipped).")
                    : HealthCheckResult.Unhealthy("AWS SSM client error (credentials or network).");
            }
        }
    }
}
