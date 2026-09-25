using Viper.Areas.Students.Models.Entities;

namespace Viper.Areas.Students.Models;

/// <summary>
/// Deliberately not a Mapperly [Mapper]: the DTO carries a dropdown option object where the
/// entity keeps a nullable option id beside a free-text column, so there is no field the
/// generator could map.
/// </summary>
public static class CareerSelectionMapper
{
    /// <summary>
    /// Applies the editable career selection fields to the entity.
    /// Every choice is a stored option row, the catch-all included, so a null option means
    /// nothing was selected. The caller is expected to have normalized the submitted options and
    /// to have refreshed IsOther from the option tables.
    /// FacultyMothraId is deliberately absent — the mentor is admin-only, so it is resolved in
    /// the service, which knows who the caller is.
    /// </summary>
    public static void ApplyStudentInfoToEntity(StudentCareerInfoDto source, CareerSelection target)
    {
        target.Career = source.Direction?.Value;
        target.CareerOther = OtherTextFor(source.Direction, source.DirectionOther);
        target.FirstSpecies = source.PrimaryFocus?.Value;
        target.FirstSpeciesOther = OtherTextFor(source.PrimaryFocus, source.PrimaryFocusOther);
        target.SecondSpecies = source.SecondaryFocus?.Value;
        target.SecondSpeciesOther = OtherTextFor(source.SecondaryFocus, source.SecondaryFocusOther);
        target.PostGrad = source.PostGrad?.Value;
        target.ShortTermStatement = Normalize(source.ShortTermPlans);
        target.LongTermStatement = Normalize(source.LongTermPlans);
    }

    /// <summary>
    /// The free text means something only while the catch-all option is selected. Anything else
    /// clears it, so a stale value cannot reappear when the student switches back to "Other".
    /// </summary>
    private static string OtherTextFor(CareerDropdownOption? option, string? text)
        => option?.IsOther == true ? Normalize(text) : string.Empty;

    // Trimmed, and empty rather than null so every text column stores the same "unfilled" value.
    private static string Normalize(string? value) => (value ?? string.Empty).Trim();
}
