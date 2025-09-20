using System.Net.Mail;

namespace Viper.Services
{
    /// <summary>
    /// Interface for sending emails with environment-specific configuration
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body content is HTML</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null);

        /// <summary>
        /// Sends an email with multiple recipients asynchronously
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body content is HTML</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? from = null);

        /// <summary>
        /// Sends a MailMessage asynchronously
        /// </summary>
        /// <param name="mailMessage">The mail message to send</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(MailMessage mailMessage);

        /// <summary>
        /// Checks if the email service is available and properly configured
        /// </summary>
        /// <returns>True if the service is available, false otherwise</returns>
        Task<bool> IsServiceAvailableAsync();

        /// <summary>
        /// Gets the current email configuration status
        /// </summary>
        /// <returns>String describing the current email configuration</returns>
        string GetConfigurationStatus();
    }
}
