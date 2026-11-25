using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>What's wrong with reports: the runs that failed, and the reports that no longer check out.</summary>
public interface IReportProblemService
{
    /// <summary>
    /// Failed runs from the last <paramref name="days"/> days (kept between 1 and a year),
    /// newest first; at most <see cref="ReportLimits.MaxFailures"/>.
    /// </summary>
    Task<IReadOnlyList<ReportFailure>> GetFailuresAsync(int? days, CancellationToken ct);

    /// <summary>Checks every live report as the admin form would on save.</summary>
    Task<ReportCheckResult> CheckAsync(CancellationToken ct);
}

public sealed class ReportProblemService : IReportProblemService
{
    private readonly ReportBuilderDbContext _context;
    private readonly IReportDefinitionValidator _validator;
    private readonly TimeProvider _clock;

    public ReportProblemService(ReportBuilderDbContext context, IReportDefinitionValidator validator, TimeProvider? clock = null)
    {
        _context = context;
        _validator = validator;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<ReportFailure>> GetFailuresAsync(int? days, CancellationToken ct)
    {
        int window = Math.Clamp(days ?? ReportLimits.DefaultFailureDays, 1, ReportLimits.MaxFailureDays);
        DateTime since = _clock.GetLocalNow().DateTime.Date.AddDays(1 - window);

        return await (
            from log in _context.ReportRunLog.AsNoTracking()
            join report in _context.Reports.AsNoTracking() on log.ReportId equals report.Id
            where log.ErrorMessage != null && log.RanOn >= since
            orderby log.RanOn descending, log.Id descending
            select new ReportFailure
            {
                Id = log.Id,
                ReportId = report.Id,
                ReportTitle = report.Title,
                ReportDeleted = report.DeletedOn != null,
                Action = log.Action,
                LoginId = log.LoginId,
                RanOn = log.RanOn,
                Parameters = log.Parameters,
                ErrorNumber = log.ErrorNumber,
                ErrorMessage = log.ErrorMessage ?? string.Empty,
            })
            .Take(ReportLimits.MaxFailures)
            .ToListAsync(ct);
    }

    public async Task<ReportCheckResult> CheckAsync(CancellationToken ct)
    {
        List<ReportDefinition> reports = await _context.Reports
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.Columns)
            .Include(r => r.Parameters)
            .AsSplitQuery()
            .Where(r => r.DeletedOn == null)
            .OrderBy(r => r.Area)
            .ThenBy(r => r.Title)
            .ToListAsync(ct);

        var failing = new List<ReportCheckProblem>();
        foreach (ReportDefinition report in reports)
        {
            // One at a time: the validator shares this request's database connections.
            ReportValidation validation = await _validator.ValidateAsync(ReportAdminService.ToEditRequest(report), ct);
            if (validation.Errors.Count > 0)
            {
                failing.Add(ReportBuilderMapper.ToCheckProblem(report) with { Problems = validation.Errors });
            }
        }

        return new ReportCheckResult { Checked = reports.Count, CheckedOn = _clock.GetLocalNow().DateTime, Failing = failing };
    }
}
