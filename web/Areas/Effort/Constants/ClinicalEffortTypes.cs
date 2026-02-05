namespace Viper.Areas.Effort.Constants;

/// <summary>
/// Defines which EffortType values are considered "clinical" for import/delete operations.
/// Used by ClinicalImportService to scope operations to clinical records only.
/// </summary>
public static class ClinicalEffortTypes
{
    /// <summary>
    /// The clinical effort type ID used by harvest (EffortConstants.ClinicalEffortType).
    /// </summary>
    public const string Clinical = "CLI";

    /// <summary>
    /// Check if an effort type is considered clinical.
    /// </summary>
    public static bool IsClinical(string? effortType) =>
        string.Equals(effortType, Clinical, StringComparison.OrdinalIgnoreCase);
}
