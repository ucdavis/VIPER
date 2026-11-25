using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>
/// The rules every viewer endpoint shares: how a report is found by its slug, who may view it,
/// and the settings that fall back to defaults when the admin left them empty.
/// </summary>
public static class ReportAccess
{
    /// <summary>The report with this slug, with its permissions, columns and parameters; null if there's none or it's deleted.</summary>
    public static async Task<ReportDefinition?> FindAsync(ReportBuilderDbContext context, string slug, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Guid.TryParse(slug, out Guid id))
        {
            return null;
        }

        return await context.Reports
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Columns)
            .Include(r => r.Parameters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Slug == id && r.DeletedOn == null, ct);
    }

    /// <summary>Admins see every report; others need one of its permissions. A report with none is admin-only.</summary>
    public static bool CanView(IReportUserService users, ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(report);
        return users.HasPermission(ReportBuilderPermissions.Admin)
            || report.Permissions.Any(p => !string.IsNullOrWhiteSpace(p.Permission) && users.HasPermission(p.Permission));
    }

    /// <summary>The columns the report shows, in order, named by their source column.</summary>
    public static IReadOnlyList<ReportResultColumn> VisibleColumns(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return [.. report.Columns
            .Where(c => c.Visible && !string.IsNullOrWhiteSpace(c.SourceColumn))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Select(c => new ReportResultColumn
            {
                Name = c.SourceColumn!.Trim(),
                Label = string.IsNullOrWhiteSpace(c.Label) ? c.SourceColumn.Trim() : c.Label,
                Format = c.Format ?? ReportColumnFormat.Text,
                Sortable = c.Sortable,
            })];
    }

    /// <summary>The parameters the user fills in, in order; current-user parameters are left out.</summary>
    public static IReadOnlyList<ReportParameter> UserParameters(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return [.. report.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) && p.Binding is null or ReportParameterBinding.UserInput)
            .OrderBy(p => p.DisplayOrder)];
    }

    /// <summary>Whether the report is shown a page at a time; on unless the admin turned it off.</summary>
    public static bool Paginates(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.Paginate != false;
    }

    /// <summary>The report's page size, or the default, kept within the allowed range.</summary>
    public static int PageSize(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return Math.Clamp(report.PageSize ?? ReportLimits.DefaultPageSize, 1, ReportLimits.MaxPageSize);
    }
}
