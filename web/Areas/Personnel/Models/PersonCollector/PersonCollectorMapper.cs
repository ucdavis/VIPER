using Riok.Mapperly.Abstractions;
using Viper.Areas.Personnel.Services;

namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>
/// Maps the Person Collector procedures' rows to the people the API returns, trimming every
/// value and turning blanks into null. Kept apart from <see cref="PersonnelMapper"/> because the
/// default string mapping here would otherwise also trim the phone-list mappings.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class PersonCollectorMapper
{
    public static partial PersonCollectorPerson ToPerson(PersonCollectorRow row);

    [UserMapping(Default = true)]
    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
