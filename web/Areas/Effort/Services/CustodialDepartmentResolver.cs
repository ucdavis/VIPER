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
    /// Equivalent to <see cref="ResolveWithCustodialCode"/> with no IOR-resolved custodial code.
    /// </summary>
    public static string Resolve(string? subjCode, string? bannerDeptCode)
        => ResolveWithCustodialCode(subjCode, bannerDeptCode, null);

    /// <summary>
    /// Resolve a custodial department using the IOR-resolved <c>custodial_dept_code</c> from
    /// <c>vw_xtnd_baseinfo</c>. This mirrors the legacy Course.cfc <c>getCustDept</c> chain used by
    /// the harvest, which maps the IOR-resolved custodial code (not the raw baseinfo dept) when the
    /// course's baseinfo dept is not one of the SVM academic departments:
    /// <list type="number">
    /// <item>Subject code, if it is a valid SVM department (SVM courses).</item>
    /// <item>Banner (baseinfo) dept code, if it is already a valid SVM department.</item>
    /// <item>The IOR-resolved <paramref name="custodialDeptCode"/>, mapped from its numeric Banner code.</item>
    /// <item>The baseinfo dept code, mapped from its numeric Banner code (defensive fallback).</item>
    /// <item>"UNK".</item>
    /// </list>
    /// The legacy importer-department fallback (getCustDept tier 3) is intentionally omitted:
    /// the automated harvest has no single importing user.
    /// </summary>
    public static string ResolveWithCustodialCode(string? subjCode, string? bannerDeptCode, string? custodialDeptCode)
    {
        var trimmedSubj = subjCode?.Trim();
        if (!string.IsNullOrEmpty(trimmedSubj) && ValidCustDepts.Contains(trimmedSubj))
        {
            return trimmedSubj.ToUpperInvariant();
        }

        var trimmedDept = bannerDeptCode?.Trim();
        if (!string.IsNullOrEmpty(trimmedDept) && ValidCustDepts.Contains(trimmedDept))
        {
            return trimmedDept.ToUpperInvariant();
        }

        // Legacy getCustDept maps the IOR-resolved custodial_dept_code (not the raw baseinfo dept)
        // when the baseinfo dept is not an SVM academic department.
        if (TryMapBannerNumeric(custodialDeptCode, out var iorDept))
        {
            return iorDept;
        }

        if (TryMapBannerNumeric(bannerDeptCode, out var bannerDept))
        {
            return bannerDept;
        }

        return "UNK";
    }

    /// <summary>
    /// Map a numeric Banner department code (e.g., "072067" or "72067") to an SVM custodial
    /// department, normalizing to 6 digits with leading zeros first.
    /// </summary>
    private static bool TryMapBannerNumeric(string? code, out string custDept)
    {
        custDept = "";
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = code.Trim().PadLeft(6, '0');
        if (BannerDeptMapping.TryGetValue(normalizedCode, out var mapped))
        {
            custDept = mapped;
            return true;
        }

        return false;
    }
}
