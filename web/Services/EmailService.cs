using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Viper.Services
{
    /// <summary>
    /// Email service implementation with Mailpit support for development
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IHostEnvironment hostEnvironment)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null)
        {
            await SendEmailAsync(new[] { to }, subject, body, isHtml, from);
        }

        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? from = null)
        {
            var mailMessage = new MailMessage();

            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }

            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = isHtml;
            mailMessage.From = new MailAddress(from ?? _emailSettings.DefaultFromAddress);

            await SendEmailAsync(mailMessage);
        }

        public async Task SendEmailAsync(MailMessage mailMessage)
        {
            try
            {
                using var smtpClient = CreateSmtpClient();

                // Log email details for debugging
                _logger.LogInformation("Sending email via {SmtpHost}:{SmtpPort} - To: {To}, Subject: {Subject}",
                    _emailSettings.SmtpHost, _emailSettings.SmtpPort,
                    string.Join(", ", mailMessage.To.Select(t => t.Address)), mailMessage.Subject);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Recipients}",
                    string.Join(", ", mailMessage.To.Select(t => t.Address)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipients}. Subject: {Subject}",
                    string.Join(", ", mailMessage.To.Select(t => t.Address)), mailMessage.Subject);

                // In development, if Mailpit is not available, log warning and continue
                if (_hostEnvironment.IsDevelopment() && _emailSettings.UseMailpit)
                {
                    _logger.LogWarning("Mailpit not available, skipping email to {Recipients}",
                        string.Join(", ", mailMessage.To.Select(t => t.Address)));
                    return; // Silent skip in development
                }
                else
                {
                    throw;
                }
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

        private SmtpClient CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                UseDefaultCredentials = _emailSettings.UseDefaultCredentials
            };

            return smtpClient;
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

    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public bool UseDefaultCredentials { get; set; } = true;
        public string DefaultFromAddress { get; set; } = "noreply@example.com";
        public bool UseMailpit { get; set; } = false;
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
