using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class SafeUrlAttribute : ValidationAttribute
{
    private static readonly string[] AllowedSchemes = { "http", "https", "mailto", "tel" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return ValidationResult.Success;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return new ValidationResult("URL must be a full address starting with http, https, mailto, or tel.");
        }

        if (!AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult("URL protocol must be http, https, mailto, or tel.");
        }

        return ValidationResult.Success;
    }
}
