using System.Collections.Frozen;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Hard-coded overrides for the Year Statistics ("Lairmore Report").
/// These match legacy yearStats.cfc behavior where specific instructors are
/// assigned to different departments, disciplines, or titles than their
/// default values, or are excluded entirely from the report.
/// Source: C:\Sites\https\VIPER\effort\reports\yearStats_includes\yearStats.cfc
/// </summary>
public static class YearStatsConstants
{
    /// <summary>
    /// Effort types excluded from Teaching Hours calculation.
    /// Teaching Hours = sum of all effort types EXCEPT these.
    /// </summary>
    public static readonly FrozenSet<string> NonTeachingEffortTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CLI", "VAR" }.ToFrozenSet();

    /// <summary>
    /// Department overrides by MothraId.
    /// These instructors are assigned to a different academic department in the
    /// Year Statistics report than what the SP returns.
    /// Note: For instructors whose current dept is CAHFS/VMTH/WHC, legacy only
    /// overrides acadDept (academic grouping), not the clinical dept column.
    /// </summary>
    public static readonly FrozenDictionary<string, string> DepartmentOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["00744318"] = "PMI", // Lairmore
            ["00003370"] = "VSR", // Pascoe
            ["00188023"] = "VMB", // Kynch
            ["00043562"] = "VSR", // Beck
            ["00297346"] = "VSR", // Borchers
            ["00739860"] = "VME", // Burgdorf-Moisuk
            ["00660915"] = "VME", // Fiorello
            ["00161161"] = "VME", // Frank
            ["00034897"] = "VME", // Jurewicz
            ["00339350"] = "VME", // Kelly, Terra
            ["00042485"] = "VSR", // Montgomery
            ["00352510"] = "VME", // Peyton
            ["00531568"] = "VSR", // Runyan
            ["00930438"] = "VME", // Uhart
            ["00029760"] = "PMI", // Adaska
            ["00042940"] = "VME", // Delany
            ["00050926"] = "VME", // Ellis, K
            ["01139259"] = "VME", // Gjeltema
            ["01027512"] = "VSR", // Johnson, Lane
            ["01026355"] = "VSR", // Kan-Rohrer, Kimi (also excluded)
            ["00742773"] = "VME", // Karsten
            ["00600429"] = "VSR", // La Rue
            ["00033056"] = "PMI", // Ochoa, Jennine
            ["00043393"] = "VSR", // Prator
            ["00039410"] = "PHR", // Sacks
            ["00215394"] = "VME", // Stelow
            ["00937422"] = "VSR", // Surmick
            ["00469687"] = "VSR", // Sutton
            ["01031280"] = "VME", // Vishkautsan
            ["00030211"] = "VME", // Ziccardi
            ["00472620"] = "PHR", // Stoute, Simone
            ["01263826"] = "PHR", // Cain (added 11/13/2017)
            ["00190201"] = "VME", // Gaydos (added 11/13/2017)
            ["00531940"] = "VSR", // Kohen (added 11/13/2017)
            ["00219787"] = "VME", // Papageorgiou (added 11/13/2017)
            ["00841015"] = "VME", // Ueda (added 11/13/2017)
            ["01265662"] = "VME", // Rodrigues Costa (added 12/11/2017)
            ["00733394"] = "VSR", // Henry (added 12/11/2017)
            ["00241407"] = "VME", // Kraus (added 12/11/2017)
            ["00289431"] = "VME", // Zylberberg (added 08/17/2018)
            ["00021249"] = "PHR", // Champagne (added 08/17/2018)
            ["00013330"] = "VME", // Davidson, Autumn (added 08/17/2018)
            ["00181048"] = "VME", // Depenbrock (added 08/17/2018)
            ["01256043"] = "VME", // Eichstadt Forsythe, Lauren (added 08/17/2018)
            ["00345440"] = "VME", // Frankfurter (added 08/17/2018)
            ["00599221"] = "VSR", // Hoareau (added 08/17/2018)
            ["01132823"] = "VME", // Rogers, Catherine (added 08/17/2018)
            ["01031302"] = "VME", // Wensley (added 08/17/2018)
            ["00829528"] = "PHR", // Williams, Deniece (added 08/17/2018)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Discipline overrides by MothraId.
    /// These instructors are assigned to a different discipline than their default
    /// IDService field value.
    /// </summary>
    public static readonly FrozenDictionary<string, string> DisciplineOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["00744318"] = "Virology", // Lairmore
            ["00003370"] = "Equine Surgery", // Pascoe
            ["00034897"] = "Community Practice", // Jurewicz (added 11/13/2017)
            ["00003133"] = "Anatomy", // Kasper
            ["00002287"] = "Cell Biology", // Wu, Reen
            ["00164992"] = "Anatomic Pathology", // Imai-Leonard
            ["00666552"] = "Community Medicine", // Clark, James
            ["00752071"] = "Food Animal Reproduction & Herd Health", // Varga
            ["00003249"] = "Laboratory Animal Medicine", // Roberts, Jeffery
            ["00004011"] = "Virology", // Miller, Chris
            ["00472620"] = "Anatomic Pathology", // Stoute, Simone
            ["01365868"] = "Community Practice", // Hershberger (added 11/13/2017)
            ["01140997"] = "Immunology", // Sciammas (added 11/13/2017)
            ["00219787"] = "Wildlife Health", // Papageorgiou (added 11/13/2017)
            ["00841015"] = "Wildlife Health", // Ueda (added 11/13/2017)
            ["00537849"] = "Livestock Medicine & Surgery", // Lehenbauer
            ["00201229"] = "Small Animal Medicine", // Sykes
            ["01265662"] = "Equine Medicine", // Rodrigues Costa (added 08/17/2018)
            ["01131970"] = "Regenerative Medicine", // Simpson (added 12/11/2017)
            ["00202021"] = "Food Safety", // Silva Del Rio (added 08/17/2018)
            ["00239620"] = "Behavior", // Grigg (added 08/17/2018)
            ["00289431"] = "Epidemiology", // Zylberberg (added 08/17/2018)
            ["00126933"] = "Livestock Reproduction & Herd Health", // McNabb (added 08/17/2018)
            ["01143997"] = "Livestock Reproduction & Herd Health", // Pereira (added 08/17/2018)
            ["00003538"] = "Livestock Reproduction & Herd Health", // Rowe (added 08/17/2018)
            ["00021249"] = "Livestock Medicine & Surgery", // Champagne (added 08/17/2018)
            ["00181048"] = "Livestock Medicine & Surgery", // Depenbrock (added 08/17/2018)
            ["01256043"] = "Pharmacology", // Eichstadt Forsythe, Lauren (added 08/17/2018)
            ["00829528"] = "Livestock Medicine & Surgery", // Williams, Deniece (added 08/17/2018)
            ["00129003"] = "Epidemiology", // Hullinger (added 08/17/2018)
            ["01030728"] = "Anatomic Pathology", // Keesler (added 08/17/2018)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Title overrides by MothraId.
    /// Legacy yearStats.cfc has infrastructure for title overrides but no entries
    /// were ever added. Kept for future use if needed.
    /// </summary>
    public static readonly FrozenDictionary<string, string> TitleOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// MothraIds of instructors excluded entirely from the Year Statistics report.
    /// </summary>
    public static readonly FrozenSet<string> ExcludedInstructors =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "00671560", // Olson, James
            "00597966", // Athanasiou, Kyriacos
            "00002939", // Solnick, Jay
            "01026355", // Kan-Rohrer, Kimi (added 08/17/2018)
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Apply department override if one exists for this instructor.
    /// </summary>
    public static string ResolveDepartment(string mothraId, string defaultDepartment)
    {
        return DepartmentOverrides.TryGetValue(mothraId, out var dept) ? dept : defaultDepartment;
    }

    /// <summary>
    /// Apply discipline override if one exists for this instructor.
    /// </summary>
    public static string ResolveDiscipline(string mothraId, string defaultDiscipline)
    {
        return DisciplineOverrides.TryGetValue(mothraId, out var disc) ? disc : defaultDiscipline;
    }

    /// <summary>
    /// Apply title override if one exists for this instructor.
    /// </summary>
    public static string ResolveTitle(string mothraId, string defaultTitle)
    {
        return TitleOverrides.TryGetValue(mothraId, out var title) ? title : defaultTitle;
    }
}
