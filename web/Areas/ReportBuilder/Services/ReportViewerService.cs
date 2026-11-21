using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>The reports the signed-in user may view, and what the viewer needs to show one.</summary>
public interface IReportViewerService
{
    /// <summary>The reports the user may view, by area and then title.</summary>
    Task<IReadOnlyList<ReportCatalogItem>> GetCatalogAsync(CancellationToken ct);

    /// <summary>A report's form and columns; its view permission is checked first.</summary>
    Task<ReportDefinitionOutcome> GetDefinitionAsync(string slug, CancellationToken ct);
}

public sealed class ReportViewerService : IReportViewerService
{
    private readonly ReportBuilderDbContext _context;
    private readonly IReportUserService _users;
    private readonly IReportChoiceService _choices;
    private readonly ILogger<ReportViewerService> _logger;

    public ReportViewerService(
        ReportBuilderDbContext context,
        IReportUserService users,
        IReportChoiceService choices,
        ILogger<ReportViewerService> logger)
    {
        _context = context;
        _users = users;
        _choices = choices;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReportCatalogItem>> GetCatalogAsync(CancellationToken ct)
    {
        if (_users.GetCurrentUser() is null)
        {
            return [];
        }

        List<ReportDefinition> reports = await _context.Reports
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => r.DeletedOn == null)
            .OrderBy(r => r.Area)
            .ThenBy(r => r.Title)
            .ToListAsync(ct);

        return [.. reports.Where(report => ReportAccess.CanView(_users, report)).Select(ReportBuilderMapper.ToCatalogItem)];
    }

    public async Task<ReportDefinitionOutcome> GetDefinitionAsync(string slug, CancellationToken ct)
    {
        if (_users.GetCurrentUser() is null)
        {
            return new ReportDefinitionOutcome { Status = ReportRunStatus.Forbidden };
        }

        ReportDefinition? report = await ReportAccess.FindAsync(_context, slug, ct);
        if (report is null)
        {
            return new ReportDefinitionOutcome { Status = ReportRunStatus.NotFound };
        }

        if (!ReportAccess.CanView(_users, report))
        {
            return new ReportDefinitionOutcome { Status = ReportRunStatus.Forbidden };
        }

        IReadOnlyList<ReportParameter> parameters = ReportAccess.UserParameters(report);
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices;
        try
        {
            choices = await _choices.GetChoicesAsync(parameters, ct);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Couldn't load the choices for report {ReportId}", report.Id);
            return new ReportDefinitionOutcome
            {
                Status = ReportRunStatus.Unavailable,
                Errors = ["This report's choices couldn't be loaded. Try again later."],
            };
        }

        return new ReportDefinitionOutcome
        {
            Status = ReportRunStatus.Ok,
            Definition = new ReportViewerDefinition
            {
                Slug = report.Slug,
                Title = report.Title,
                Description = report.Description,
                Area = report.Area,
                SensitiveNotice = string.IsNullOrWhiteSpace(report.SensitiveNotice) ? null : report.SensitiveNotice.Trim(),
                Paginate = ReportAccess.Paginates(report),
                PageSize = ReportAccess.PageSize(report),
                AllowExcel = report.AllowExcel,
                AllowPdf = report.AllowPdf,
                SortBy = string.IsNullOrWhiteSpace(report.OrderBy) ? null : report.OrderBy.Trim(),
                SortDescending = report.OrderByDescending == true,
                Columns = ReportAccess.VisibleColumns(report),
                Parameters = [.. parameters.Select(p => ToViewerParameter(p, choices))],
            },
        };
    }

    private static ReportViewerParameter ToViewerParameter(
        ReportParameter parameter, IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices)
    {
        ReportParameterType type = parameter.Type ?? ReportParameterType.Text;
        bool isChoice = type is ReportParameterType.Choice or ReportParameterType.MultiChoice;
        return new ReportViewerParameter
        {
            Name = parameter.Name!,
            Label = string.IsNullOrWhiteSpace(parameter.Label) ? parameter.Name! : parameter.Label,
            Type = type,
            Required = parameter.Required == true,
            DefaultValue = string.IsNullOrWhiteSpace(parameter.DefaultValue) ? null : parameter.DefaultValue.Trim(),
            Choices = isChoice ? choices.GetValueOrDefault(parameter.Name!, []) : null,
        };
    }
}
