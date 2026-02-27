using System.Collections.Frozen;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Resolves Banner course data to Effort custodial department codes.
/// Shared between CourseService (manual imports) and harvest phases.
/// </summary>
/// <remarks>
/// Resolution chain (mirrors legacy Course.cfc getCustDept):
/// 1. If subject code is a valid SVM department → use it
/// 2. If Banner dept code is already a valid SVM department → use it
/// 3. Map numeric Banner dept codes (e.g., "072030" → "VME")
/// 4. Fallback: "UNK"
/// </remarks>
public static class CustodialDepartmentResolver
{
    /// <summary>
    /// Valid custodial department codes for the Effort system.
    /// </summary>
    public static readonly FrozenSet<string> ValidCustDepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "APC", "VMB", "VME", "VSR", "PMI", "PHR", "UNK", "DVM", "VET"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Mapping from Banner numeric department codes to Effort custodial departments.
    /// Codes are normalized to 6 digits with leading zeros.
    /// </summary>
    private static readonly Dictionary<string, string> BannerDeptMapping = new()
    {
        { "072030", "VME" },
        { "072035", "VSR" },
        { "072037", "APC" },
        { "072047", "VMB" },
        { "072057", "PMI" },
        { "072067", "PHR" }
    };

    /// <summary>
    /// Resolve a custodial department from subject code and/or Banner department code.
    /// For SVM courses, the subject code (e.g., "VME", "PMI") often IS the custodial department.
    /// Falls back to department code mapping for non-SVM subject codes.
    /// </summary>
    public static string Resolve(string? subjCode, string? bannerDeptCode)
    {
        // First check if subject code is a valid SVM department (most common case for SVM courses)
        if (!string.IsNullOrWhiteSpace(subjCode))
        {
            var trimmedSubj = subjCode.Trim().ToUpperInvariant();
            if (ValidCustDepts.Contains(trimmedSubj))
            {
                return trimmedSubj;
            }
        }

        if (string.IsNullOrWhiteSpace(bannerDeptCode))
        {
            return "UNK";
        }

        // Then check if dept code is already a valid SVM department code
        var trimmed = bannerDeptCode.Trim();
        if (ValidCustDepts.Contains(trimmed))
        {
            return trimmed.ToUpperInvariant();
        }

        // Try to look up by numeric Banner code — normalize to 6 digits with leading zero
        var normalizedCode = trimmed.PadLeft(6, '0');
        if (BannerDeptMapping.TryGetValue(normalizedCode, out var custDept))
        {
            return custDept;
        }

        return "UNK";
    }
}
