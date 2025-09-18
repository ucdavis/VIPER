using Microsoft.Extensions.Options;

namespace Viper.Services
{
    /// <summary>
    /// Validates email notification configuration at startup
    /// </summary>
    public class EmailNotificationSettingsValidator : IValidateOptions<EmailNotificationSettings>
    {
        public ValidateOptionsResult Validate(string? name, EmailNotificationSettings options)
        {
            if (options.PrimaryEvaluatorRemoved.To == null || !options.PrimaryEvaluatorRemoved.To.Any())
            {
                return ValidateOptionsResult.Fail("PrimaryEvaluatorRemoved.To must contain at least one email address");
            }

            // Validate email addresses are not empty or whitespace
            var invalidEmails = options.PrimaryEvaluatorRemoved.To.Where(email => string.IsNullOrWhiteSpace(email)).ToList();
            if (invalidEmails.Any())
            {
                return ValidateOptionsResult.Fail("PrimaryEvaluatorRemoved.To contains empty or whitespace-only email addresses");
            }

            // Validate From address
            if (string.IsNullOrWhiteSpace(options.PrimaryEvaluatorRemoved.From))
            {
                return ValidateOptionsResult.Fail("PrimaryEvaluatorRemoved.From must contain a valid email address");
            }


            return ValidateOptionsResult.Success;
        }
    }
}
