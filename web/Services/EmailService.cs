using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Viper.Services
{
    /// <summary>
    /// Exception thrown when email sending fails due to SMTP/network errors.
    /// Distinct from InvalidOperationException to allow callers to differentiate
    /// between email send failures and other errors (e.g., template rendering).
    /// </summary>
    public class EmailSendException : Exception
    {
        public EmailSendException(string message) : base(message) { }
        public EmailSendException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Email service implementation using MailKit with Mailpit support for development.
    /// Sends multipart emails with both HTML and plaintext versions.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null)
        {
            await SendEmailAsync(new[] { to }, subject, body, isHtml, from);
        }

        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? from = null)
        {
            if (isHtml)
            {
                // HTML email - send as multipart with auto-generated plaintext
                await SendMultipartEmailAsync(to, subject, body, textBody: null, from: from);
            }
            else
            {
                // Plaintext only
                await SendMultipartEmailAsync(to, subject, htmlBody: null, textBody: body, from: from);
            }
        }

        public async Task SendMultipartEmailAsync(string to, string subject, string? htmlBody, string? textBody = null, string? from = null)
        {
            await SendMultipartEmailAsync(new[] { to }, subject, htmlBody, textBody, from);
        }

        public async Task SendMultipartEmailAsync(IEnumerable<string> to, string subject, string? htmlBody, string? textBody = null, string? from = null)
        {
            if (string.IsNullOrEmpty(htmlBody) && string.IsNullOrEmpty(textBody))
            {
                throw new ArgumentException("At least one of htmlBody or textBody must be provided.");
            }

            using var message = new MimeMessage();
            try
            {
                message.From.Add(MailboxAddress.Parse(from ?? _emailSettings.DefaultFromAddress));

                foreach (var recipient in to)
                {
                    message.To.Add(MailboxAddress.Parse(recipient));
                }

                message.Subject = subject;

                // Build multipart body using MailKit's BodyBuilder
                var builder = new BodyBuilder();

                if (!string.IsNullOrEmpty(htmlBody))
                {
                    builder.HtmlBody = htmlBody;
                    // Auto-generate plaintext if not provided
                    builder.TextBody = textBody ?? HtmlToTextConverter.Convert(htmlBody);
                }
                else if (!string.IsNullOrEmpty(textBody))
                {
                    builder.TextBody = textBody;
                }

                message.Body = builder.ToMessageBody();
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new EmailSendException($"Failed to build email message: {ex.Message}", ex);
            }

            await SendMimeMessageAsync(message);
        }

        private async Task SendMimeMessageAsync(MimeMessage message)
        {
            try
            {
                using var client = new SmtpClient();

                // Determine SSL/TLS options
                var secureSocketOptions = SecureSocketOptions.None;
                if (_emailSettings.EnableSsl)
                {
                    secureSocketOptions = SecureSocketOptions.StartTls;
                }

                // Mailpit and local dev typically use no TLS
                if (_hostEnvironment.IsDevelopment() && _emailSettings.UseMailpit)
                {
                    secureSocketOptions = SecureSocketOptions.None;
                }

                _logger.LogInformation("Sending email via {SmtpHost}:{SmtpPort} from {RequestContext}, Recipients: {RecipientCount}",
                    _emailSettings.SmtpHost, _emailSettings.SmtpPort, GetRequestContext(), message.To.Count);

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions);

                // No authentication configured - this service is designed for localhost/internal
                // SMTP relays that don't require auth. If auth is needed in the future,
                // add Username/Password settings and call client.AuthenticateAsync() here.

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully from {RequestContext}", GetRequestContext());
            }
            catch (SocketException ex)
            {
                // In development, if Mailpit is not available, log warning and continue
                if (_hostEnvironment.IsDevelopment() && _emailSettings.UseMailpit)
                {
                    _logger.LogWarning(ex, "Mailpit not available at {SmtpHost}:{SmtpPort}, skipping email",
                        _emailSettings.SmtpHost, _emailSettings.SmtpPort);
                    return;
                }

                throw new EmailSendException(
                    $"Failed to connect to SMTP server {_emailSettings.SmtpHost}:{_emailSettings.SmtpPort}", ex);
            }
            catch (SmtpCommandException ex)
            {
                throw new EmailSendException(
                    $"SMTP command failed with status {ex.StatusCode}: {ex.Message}", ex);
            }
            catch (SmtpProtocolException ex)
            {
                throw new EmailSendException(
                    $"SMTP protocol error: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is not EmailSendException)
            {
                throw new EmailSendException(
                    $"Unexpected error sending email: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                // If using Mailpit in development, check if it's running
                if (_hostEnvironment.IsDevelopment() && _emailSettings.UseMailpit)
                {
                    return await IsMailpitAvailableAsync();
                }

                // For production, assume the SMTP server is available
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email service availability");
                return false;
            }
        }

        public string GetConfigurationStatus()
        {
            var status = $"SMTP Host: {_emailSettings.SmtpHost}:{_emailSettings.SmtpPort}, SSL: {_emailSettings.EnableSsl}";

            if (_hostEnvironment.IsDevelopment() && _emailSettings.UseMailpit)
            {
                status += " (Mailpit for development)";
            }

            return status;
        }

        private async Task<bool> IsMailpitAvailableAsync()
        {
            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
                return tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private string GetRequestContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "Background";

            var routeData = httpContext.GetRouteData();
            if (routeData == null) return "NonRouted";

            var area = routeData.Values["area"]?.ToString();
            var controller = routeData.Values["controller"]?.ToString();

            return area != null ? $"{area}/{controller}" : controller ?? "Unknown";
        }

    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string DefaultFromAddress { get; set; } = "noreply@example.com";
        public bool UseMailpit { get; set; } = false;

        /// <summary>
        /// Base URL for links in emails (e.g., "https://viper.vetmed.ucdavis.edu/2").
        /// Used to construct absolute URLs for email content.
        /// </summary>
        public string? BaseUrl { get; set; }
    }

    /// <summary>
    /// Email notification configuration for various system notifications
    /// </summary>
    public class EmailNotificationSettings
    {
        public PrimaryEvaluatorRemovedNotificationSettings PrimaryEvaluatorRemoved { get; set; } = new();
    }

    /// <summary>
    /// Configuration for primary evaluator removal notifications
    /// </summary>
    public class PrimaryEvaluatorRemovedNotificationSettings
    {
        /// <summary>
        /// List of email addresses to notify when a primary evaluator is removed
        /// </summary>
        public List<string> To { get; set; } = new();

        /// <summary>
        /// From address for the notification email
        /// </summary>
        public string From { get; set; } = "svmithelp@ucdavis.edu";

    }
}
