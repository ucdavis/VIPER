using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>
/// RAPS permissions, for the admin form: reports may only name permissions RAPS has, so who can
/// view a report is always managed in RAPS.
/// </summary>
public interface IReportPermissionLookupService
{
    /// <summary>Permissions whose name contains the text, ignoring case, in name order.</summary>
    Task<IReadOnlyList<ReportPermissionOption>> SearchAsync(string? text, CancellationToken ct);

    /// <summary>The names, as RAPS spells them, of those that exist; matched ignoring case.</summary>
    Task<IReadOnlyDictionary<string, string>> FindAsync(IEnumerable<string> names, CancellationToken ct);
}

public sealed class ReportPermissionLookupService : IReportPermissionLookupService
{
    /// <summary>The most permissions one search returns.</summary>
    internal const int MaxResults = 50;

    private readonly RAPSContext _rapsContext;

    public ReportPermissionLookupService(RAPSContext rapsContext)
    {
        _rapsContext = rapsContext;
    }

    public async Task<IReadOnlyList<ReportPermissionOption>> SearchAsync(string? text, CancellationToken ct)
    {
        string search = text?.Trim() ?? string.Empty;
        return await _rapsContext.TblPermissions
            .AsNoTracking()
            .Where(p => search == string.Empty || p.Permission.Contains(search))
            .OrderBy(p => p.Permission)
            .Take(MaxResults)
            .Select(p => new ReportPermissionOption { Permission = p.Permission, Description = p.Description })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> FindAsync(IEnumerable<string> names, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(names);
        List<string> wanted = [.. names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];
        if (wanted.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // RAPS compares names without regard to case, as SQL Server's collation does.
        List<string> found = await _rapsContext.TblPermissions
            .AsNoTracking()
            .Where(p => wanted.Contains(p.Permission))
            .Select(p => p.Permission)
            .ToListAsync(ct);
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in found)
        {
            byName.TryAdd(name, name);
        }

        return byName;
    }
}
