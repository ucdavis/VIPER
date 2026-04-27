using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.Versioning;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Performs a real LDAPS bind using the same host, port, and service
    /// credential as LdapService. One probe verifies TCP reachability, TLS
    /// handshake (cert chain + hostname), and that the service account still
    /// authenticates.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LdapHealthCheck : IHealthCheck
    {
        private const string _ldapUsername = "UID=vetmed,OU=Special Users,DC=ucdavis,DC=edu";
        private const string _ldapServer = "ldap.ucdavis.edu";
        private const int _ldapSSLPort = 636;
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var cred = HttpHelper.GetSetting<string>("Credentials", "UCDavisDirectoryLDAP");
            if (string.IsNullOrEmpty(cred))
            {
                return HealthCheckResult.Unhealthy(
                    "LDAP bind credential not configured (Credentials:UCDavisDirectoryLDAP).");
            }

            try
            {
                await Task.Run(() =>
                {
                    var ldapIdentifier = new LdapDirectoryIdentifier(_ldapServer, _ldapSSLPort);
                    using var lc = new LdapConnection(
                        ldapIdentifier,
                        new NetworkCredential(_ldapUsername, cred),
                        AuthType.Basic);
                    lc.SessionOptions.ProtocolVersion = 3;
                    lc.SessionOptions.SecureSocketLayer = true;
                    lc.Timeout = _timeout;
                    lc.Bind();
                }, cancellationToken);

                return HealthCheckResult.Healthy("LDAP bind succeeded.");
            }
            catch (LdapException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"LDAP bind failed: {ex.Message}", ex);
            }
            catch (DirectoryOperationException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"LDAP directory error: {ex.Message}", ex);
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy("LDAP bind timed out.");
            }
        }
    }
}
