using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.CMS.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SafeUrlAttribute : ValidationAttribute
{
    private static readonly string[] AllowedSchemes = { "http", "https", "mailto", "tel" };

    /// <summary>
    /// Also accept relative URLs (e.g. "/CMS/files", "~/page", "page.cfm?x=1").
    /// Anything a browser could parse as a scheme is still restricted to the allowlist.
    /// </summary>
    public bool AllowRelative { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string url || string.IsNullOrEmpty(url))
        {
            return ValidationResult.Success;
        }

        url = url.Trim();

        // Browsers strip tabs/newlines when parsing hrefs, so "java\tscript:" still runs
        if (url.Any(char.IsControl))
        {
            return new ValidationResult("URL contains invalid characters.");
        }

        // A colon in the first path segment is what browsers parse as a scheme;
        // checking it directly avoids Uri's platform-dependent handling of
        // rooted paths like "/CMS/files" (parsed as file:// on Unix)
        if (!url.Split('/', '?', '#')[0].Contains(':'))
        {
            if (!AllowRelative)
            {
                return new ValidationResult("URL must be a full address starting with http, https, mailto, or tel.");
            }

            // Reject protocol-relative ("network-path") references like "//evil.example/x":
            // a browser navigates those off-site even though they carry no scheme. Backslashes
            // are normalized to forward slashes when parsing URLs, so "/\", "\\", and "\/" are
            // equivalent bypasses and must be rejected too.
            if (url.Length >= 2 && (url[0] == '/' || url[0] == '\\') && (url[1] == '/' || url[1] == '\\'))
            {
                return new ValidationResult("URL must not start with // (that would point to another site).");
            }

            return ValidationResult.Success;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase)
            ? ValidationResult.Success
            : new ValidationResult("URL protocol must be http, https, mailto, or tel.");
    }
}
