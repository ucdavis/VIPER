using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Probes the SMTP relay with a MailKit Connect + NoOp + Disconnect. Proves
    /// TCP reachability, EHLO handshake, and - when StartTls is configured -
    /// STARTTLS negotiation with cert validation. Mirrors the connect path in
    /// Services/EmailService.cs minus the DATA command.
    /// </summary>
    public class SmtpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly SecureSocketOptions _socketOptions;
        private readonly bool _healthyWhenMissing;
        private readonly TimeSpan _timeout;

        /// <param name="healthyWhenMissing">
        /// If true, a SocketException returns Healthy with a "skipped" description.
        /// Use in Development where the SMTP target (e.g. Mailpit) may not be running.
        /// </param>
        public SmtpHealthCheck(
            string host,
            int port,
            SecureSocketOptions socketOptions,
            bool healthyWhenMissing = false,
            TimeSpan? timeout = null)
        {
            _host = host;
            _port = port;
            _socketOptions = socketOptions;
            _healthyWhenMissing = healthyWhenMissing;
            _timeout = timeout ?? TimeSpan.FromSeconds(5);
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            using var client = new SmtpClient
            {
                Timeout = (int)_timeout.TotalMilliseconds,
            };

            try
            {
                await client.ConnectAsync(_host, _port, _socketOptions, timeoutCts.Token);
                await client.NoOpAsync(timeoutCts.Token);
                await client.DisconnectAsync(true, timeoutCts.Token);

                return HealthCheckResult.Healthy("SMTP reachable (EHLO ok).");
            }
            catch (SslHandshakeException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"SMTP TLS handshake failed: {ex.Message}", ex);
            }
            catch (SocketException ex)
            {
                return _healthyWhenMissing
                    ? HealthCheckResult.Healthy("SMTP not running (skipped).")
                    : HealthCheckResult.Unhealthy(
                        $"SMTP unreachable: {ex.SocketErrorCode}.", ex);
            }
            catch (SmtpCommandException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"SMTP command failed ({ex.StatusCode}): {ex.Message}", ex);
            }
            catch (SmtpProtocolException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"SMTP protocol error: {ex.Message}", ex);
            }
            catch (OperationCanceledException)
            {
                // CreateLinkedTokenSource + CancelAfter means OCE can come from
                // either external shutdown (caller token) or our own timeout.
                return cancellationToken.IsCancellationRequested
                    ? HealthCheckResult.Unhealthy("SMTP probe cancelled.")
                    : HealthCheckResult.Unhealthy("SMTP timed out.");
            }
        }
    }
}
