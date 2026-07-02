using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Areas.CMS.Validation;

/// <summary>
/// Validates that every string element in a collection is at most <see cref="Length"/> characters.
/// [MaxLength] on a collection property bounds only the element count, not each element, so this
/// guards individual entries against their backing column size (e.g. permission strings vs the
/// varchar(500) LeftNavItemToPermission.permission column).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class MaxLengthEachAttribute : ValidationAttribute
{
    public MaxLengthEachAttribute(int length)
    {
        Length = length;
    }

    public int Length { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable items)
        {
            return ValidationResult.Success;
        }

        foreach (var item in items)
        {
            if (item is string s && s.Length > Length)
            {
                return new ValidationResult($"Each value must be {Length} characters or fewer.");
            }
        }

        return ValidationResult.Success;
    }
}
