namespace Viper.Areas.Students.Models;

/// <summary>
/// The three option tables behind the career selection form's dropdowns.
/// </summary>
public enum CareerOptionType
{
    Career,
    Species,
    PostGrad
}

public static class CareerOptionTypes
{
    private static readonly Dictionary<string, CareerOptionType> BySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["career"] = CareerOptionType.Career,
        ["species"] = CareerOptionType.Species,
        ["post-grad"] = CareerOptionType.PostGrad,
    };

    /// <summary>Resolves the URL segment the option management routes are keyed on.</summary>
    public static bool TryParseSlug(string? slug, out CareerOptionType type)
    {
        if (slug != null && BySlug.TryGetValue(slug, out type))
        {
            return true;
        }

        type = default;
        return false;
    }
}
