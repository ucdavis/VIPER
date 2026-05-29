namespace Viper.Services
{
    /// <summary>
    /// Interface for sending emails with environment-specific configuration
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously. If isHtml is true, a plaintext version is auto-generated.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body content is HTML</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, string? from = null);

        /// <summary>
        /// Sends an email with multiple recipients asynchronously. If isHtml is true, a plaintext version is auto-generated.
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body content is HTML</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, string? from = null);

        /// <summary>
        /// Sends a multipart email with both HTML and plaintext versions.
        /// If textBody is null, plaintext is auto-generated from the HTML.
        /// At least one of htmlBody or textBody must be provided.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlBody">HTML body content (optional if textBody provided)</param>
        /// <param name="textBody">Plaintext body (optional, auto-generated from HTML if null)</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        Task SendMultipartEmailAsync(string to, string subject, string? htmlBody, string? textBody = null, string? from = null);

        /// <summary>
        /// Sends a multipart email to multiple recipients with both HTML and plaintext versions.
        /// If textBody is null, plaintext is auto-generated from the HTML.
        /// At least one of htmlBody or textBody must be provided.
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlBody">HTML body content (optional if textBody provided)</param>
        /// <param name="textBody">Plaintext body (optional, auto-generated from HTML if null)</param>
        /// <param name="from">Sender email address (optional, uses default if not specified)</param>
        Task SendMultipartEmailAsync(IEnumerable<string> to, string subject, string? htmlBody, string? textBody = null, string? from = null);

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
