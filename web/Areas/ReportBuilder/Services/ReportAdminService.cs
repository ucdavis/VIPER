using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>Creates, edits, deletes, restores and previews reports, keeping a copy of every version.</summary>
public interface IReportAdminService
{
    /// <summary>Every report, by area and title; deleted ones only when asked.</summary>
    Task<IReadOnlyList<ReportAdminListItem>> ListAsync(bool includeDeleted, CancellationToken ct);

    /// <summary>A report for the edit form, deleted or not; null when there's none.</summary>
    Task<ReportAdminDefinition?> GetAsync(int id, CancellationToken ct);

    Task<ReportAdminOutcome<ReportAdminDefinition>> CreateAsync(ReportEditRequest request, CancellationToken ct);

    /// <summary>Saves the form, unless someone else saved the report after the editor loaded it.</summary>
    Task<ReportAdminOutcome<ReportAdminDefinition>> UpdateAsync(int id, ReportEditRequest request, CancellationToken ct);

    /// <summary>Hides the report from viewers; it can be restored.</summary>
    Task<ReportAdminOutcome<ReportAdminDefinition>> DeleteAsync(int id, CancellationToken ct);

    Task<ReportAdminOutcome<ReportAdminDefinition>> RestoreAsync(int id, CancellationToken ct);

    /// <summary>The report's saved versions, newest first; null when there's no such report.</summary>
    Task<IReadOnlyList<ReportHistoryItem>?> GetHistoryAsync(int id, CancellationToken ct);

    /// <summary>One saved version with the report as it was; null when there's none.</summary>
    Task<ReportHistoryVersion?> GetHistoryVersionAsync(int id, int historyId, CancellationToken ct);

    /// <summary>Checks an unsaved report and shows a page of it.</summary>
    Task<ReportRunOutcome> PreviewAsync(ReportPreviewRequest request, CancellationToken ct);
}

public sealed class ReportAdminService : IReportAdminService
{
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web);

    private readonly ReportBuilderDbContext _context;
    private readonly IReportDefinitionValidator _validator;
    private readonly IReportPermissionLookupService _permissions;
    private readonly IReportUserService _users;
    private readonly IReportRunService _runs;
    private readonly TimeProvider _clock;

    public ReportAdminService(
        ReportBuilderDbContext context,
        IReportDefinitionValidator validator,
        IReportPermissionLookupService permissions,
        IReportUserService users,
        IReportRunService runs,
        TimeProvider? clock = null)
    {
        _context = context;
        _validator = validator;
        _permissions = permissions;
        _users = users;
        _runs = runs;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<ReportAdminListItem>> ListAsync(bool includeDeleted, CancellationToken ct)
    {
        List<ReportDefinition> reports = await _context.Reports
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => includeDeleted || r.DeletedOn == null)
            .OrderBy(r => r.Area)
            .ThenBy(r => r.Title)
            .ToListAsync(ct);
        IReadOnlyDictionary<string, string> existing = await _permissions.FindAsync(
            reports.SelectMany(r => r.Permissions).Select(p => p.Permission), ct);

        return [.. reports.Select(report =>
        {
            IReadOnlyList<string> names = PermissionNames(report);
            return ReportBuilderMapper.ToAdminListItem(report) with
            {
                Permissions = names,
                MissingPermissions = [.. names.Where(name => !existing.ContainsKey(name))],
            };
        })];
    }

    public async Task<ReportAdminDefinition?> GetAsync(int id, CancellationToken ct)
    {
        ReportDefinition? report = await Load(id).AsNoTracking().FirstOrDefaultAsync(ct);
        return report is null ? null : await ToAdminDefinitionAsync(report, ct);
    }

    public async Task<ReportAdminOutcome<ReportAdminDefinition>> CreateAsync(ReportEditRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportValidation validation = await _validator.ValidateAsync(request, ct);
        if (validation.Errors.Count > 0)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.Invalid, [.. validation.Errors]);
        }

        (DateTime now, string? who) = Stamp();
        var report = new ReportDefinition
        {
            Slug = Guid.NewGuid(),
            CreatedOn = now,
            CreatedBy = who,
            ModifiedOn = now,
            ModifiedBy = who,
        };
        Apply(report, request, validation);
        report.Permissions = [.. validation.Permissions.Select(name => new ReportPermission { Permission = name })];
        _context.Reports.Add(report);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await _context.SaveChangesAsync(ct);
        await AddHistoryAsync(report, ReportHistoryAction.Created, now, who, ct);
        await transaction.CommitAsync(ct);

        return ReportAdminOutcome.Ok((await GetAsync(report.Id, ct))!);
    }

    public async Task<ReportAdminOutcome<ReportAdminDefinition>> UpdateAsync(int id, ReportEditRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportDefinition? report = await Load(id).FirstOrDefaultAsync(ct);
        if (report is null)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.NotFound);
        }

        if (report.DeletedOn is not null)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.Invalid, "Restore the report before editing it.");
        }

        if (request.LastModifiedOn != report.ModifiedOn)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(
                ReportAdminStatus.Conflict,
                "Someone else saved this report after you opened it. Reload it to see their changes, then make yours again.");
        }

        ReportValidation validation = await _validator.ValidateAsync(request, ct);
        if (validation.Errors.Count > 0)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.Invalid, [.. validation.Errors]);
        }

        _context.ReportColumns.RemoveRange(report.Columns);
        _context.ReportParameters.RemoveRange(report.Parameters);
        Apply(report, request, validation);
        ReplacePermissions(report, validation.Permissions);
        (DateTime now, string? who) = Stamp();
        report.ModifiedOn = now;
        report.ModifiedBy = who;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The in-memory check above already catches the common case; this closes the gap
            // between that check and the write itself.
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(
                ReportAdminStatus.Conflict,
                "Someone else saved this report after you opened it. Reload it to see their changes, then make yours again.");
        }

        await AddHistoryAsync(report, ReportHistoryAction.Updated, now, who, ct);
        await transaction.CommitAsync(ct);
        return ReportAdminOutcome.Ok((await GetAsync(id, ct))!);
    }

    public async Task<ReportAdminOutcome<ReportAdminDefinition>> DeleteAsync(int id, CancellationToken ct)
    {
        return await SetDeletedAsync(id, deleted: true, ct);
    }

    public async Task<ReportAdminOutcome<ReportAdminDefinition>> RestoreAsync(int id, CancellationToken ct)
    {
        return await SetDeletedAsync(id, deleted: false, ct);
    }

    public async Task<IReadOnlyList<ReportHistoryItem>?> GetHistoryAsync(int id, CancellationToken ct)
    {
        if (!await _context.Reports.AnyAsync(r => r.Id == id, ct))
        {
            return null;
        }

        List<ReportDefinitionHistory> versions = await _context.ReportHistory
            .AsNoTracking()
            .Where(h => h.ReportId == id)
            .OrderByDescending(h => h.ChangedOn)
            .ThenByDescending(h => h.Id)
            .ToListAsync(ct);
        return [.. versions.Select(ReportBuilderMapper.ToHistoryItem)];
    }

    public async Task<ReportHistoryVersion?> GetHistoryVersionAsync(int id, int historyId, CancellationToken ct)
    {
        ReportDefinitionHistory? version = await _context.ReportHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == historyId && h.ReportId == id, ct);
        if (version is null)
        {
            return null;
        }

        return new ReportHistoryVersion { Version = ReportBuilderMapper.ToHistoryItem(version), Report = ReadSnapshot(version.Snapshot) };
    }

    public async Task<ReportRunOutcome> PreviewAsync(ReportPreviewRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportValidation validation = await _validator.ValidateAsync(request.Report, ct);
        if (validation.Errors.Count > 0)
        {
            return ReportRunOutcome.Fail(ReportRunStatus.Invalid, [.. validation.Errors]);
        }

        var report = new ReportDefinition();
        Apply(report, request.Report, validation);
        return await _runs.PreviewAsync(report, request.Run, ct);
    }

    private IQueryable<ReportDefinition> Load(int id)
    {
        return _context.Reports
            .Include(r => r.Permissions)
            .Include(r => r.Columns)
            .Include(r => r.Parameters)
            .Where(r => r.Id == id);
    }

    private async Task<ReportAdminOutcome<ReportAdminDefinition>> SetDeletedAsync(int id, bool deleted, CancellationToken ct)
    {
        ReportDefinition? report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report is null)
        {
            return ReportAdminOutcome.Fail<ReportAdminDefinition>(ReportAdminStatus.NotFound);
        }

        // Deleting a deleted report, or restoring a live one, changes nothing and records nothing.
        if ((report.DeletedOn is not null) != deleted)
        {
            (DateTime now, string? who) = Stamp();
            report.DeletedOn = deleted ? now : null;
            report.DeletedBy = deleted ? who : null;
            report.ModifiedOn = now;
            report.ModifiedBy = who;

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ReportAdminOutcome.Fail<ReportAdminDefinition>(
                    ReportAdminStatus.Conflict,
                    "Someone else changed this report just now. Reload it and try again.");
            }

            await AddHistoryAsync(report, deleted ? ReportHistoryAction.Deleted : ReportHistoryAction.Restored, now, who, ct);
            await transaction.CommitAsync(ct);
        }

        return ReportAdminOutcome.Ok((await GetAsync(id, ct))!);
    }

    private (DateTime Now, string? Who) Stamp()
    {
        return (_clock.GetLocalNow().DateTime, _users.GetCurrentUser()?.LoginId);
    }

    /// <summary>Records the report as it now is; the snapshot is read back from the database.</summary>
    private async Task AddHistoryAsync(ReportDefinition report, ReportHistoryAction action, DateTime now, string? who, CancellationToken ct)
    {
        ReportDefinition saved = (await Load(report.Id).AsNoTracking().FirstOrDefaultAsync(ct))!;
        _context.ReportHistory.Add(new ReportDefinitionHistory
        {
            ReportId = report.Id,
            Action = action,
            Snapshot = JsonSerializer.Serialize(ToEditRequest(saved), SnapshotJson),
            ChangedOn = now,
            ChangedBy = who,
        });
        await _context.SaveChangesAsync(ct);
    }

    private static ReportEditRequest? ReadSnapshot(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReportEditRequest>(snapshot, SnapshotJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Keeps the permissions still wanted, removes the rest and adds the new ones.</summary>
    private void ReplacePermissions(ReportDefinition report, IReadOnlyList<string> names)
    {
        var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        List<ReportPermission> removed = [.. report.Permissions.Where(p => !wanted.Contains(p.Permission))];
        _context.ReportPermissions.RemoveRange(removed);
        foreach (ReportPermission permission in removed)
        {
            report.Permissions.Remove(permission);
        }

        var kept = new HashSet<string>(report.Permissions.Select(p => p.Permission), StringComparer.OrdinalIgnoreCase);
        foreach (string name in names.Where(name => !kept.Contains(name)))
        {
            report.Permissions.Add(new ReportPermission { ReportId = report.Id, Permission = name });
        }
    }

    /// <summary>
    /// Copies the form onto the entity, with names as the catalog spells them and settings that
    /// don't apply (an operator for a procedure, choices for a text field) left empty.
    /// </summary>
    private static void Apply(ReportDefinition report, ReportEditRequest request, ReportValidation validation)
    {
        ReportSourceDetail source = validation.Source!;
        bool isView = source.Kind == ReportSourceKind.View;
        report.Title = request.Title.Trim();
        report.Description = Clean(request.Description);
        report.Area = Clean(request.Area);
        report.Source = source.Name;
        report.Paginate = request.Paginate;
        report.PageSize = request.PageSize;
        report.AllowExcel = request.AllowExcel;
        report.AllowPdf = request.AllowPdf;
        report.PdfLandscape = request.PdfLandscape;
        report.SensitiveNotice = Clean(request.SensitiveNotice);
        report.OrderBy = string.IsNullOrWhiteSpace(request.OrderBy) ? null : SourceName(request.OrderBy, validation);
        report.OrderByDescending = request.OrderByDescending;
        report.Columns = [.. request.Columns.Select((column, index) => new ReportColumn
        {
            ReportId = report.Id,
            SourceColumn = SourceName(column.SourceColumn, validation),
            Label = Clean(column.Label),
            Format = column.Format,
            Sortable = column.Sortable,
            Visible = column.Visible,
            DisplayOrder = index + 1,
        })];
        report.Parameters = [.. request.Parameters.Select((parameter, index) => ToEntity(report.Id, parameter, index + 1, isView, validation))];
    }

    private static ReportParameter ToEntity(int reportId, ReportParameterEdit parameter, int order, bool isView, ReportValidation validation)
    {
        bool fromUser = parameter.Binding == ReportParameterBinding.UserInput;
        bool isChoice = fromUser && ReportDefinitionValidator.IsChoice(parameter.Type);
        bool hasList = isChoice && parameter.ChoicesList is { Count: > 0 };
        return new ReportParameter
        {
            ReportId = reportId,
            Name = SourceName(parameter.Name, validation),
            Label = Clean(parameter.Label),
            DisplayOrder = order,
            Type = fromUser ? parameter.Type : ReportParameterType.Text,
            Operator = isView ? parameter.Operator ?? ReportFilterOperator.Equals : null,
            Required = fromUser && parameter.Required,
            DefaultValue = fromUser ? Clean(parameter.DefaultValue) : null,
            ChoicesSource = isChoice && !hasList ? Clean(parameter.ChoicesSource) : null,
            ChoicesList = hasList
                ? JsonSerializer.Serialize(parameter.ChoicesList!.Select(c => new ReportChoice { Value = c.Value.Trim(), Label = Clean(c.Label) ?? c.Value.Trim() }), SnapshotJson)
                : null,
            Binding = parameter.Binding,
        };
    }

    private async Task<ReportAdminDefinition> ToAdminDefinitionAsync(ReportDefinition report, CancellationToken ct)
    {
        IReadOnlyList<string> names = PermissionNames(report);
        IReadOnlyDictionary<string, string> existing = await _permissions.FindAsync(names, ct);
        return new ReportAdminDefinition
        {
            Id = report.Id,
            Slug = report.Slug,
            Report = ToEditRequest(report) with { LastModifiedOn = report.ModifiedOn },
            CreatedOn = report.CreatedOn,
            CreatedBy = report.CreatedBy,
            ModifiedOn = report.ModifiedOn,
            ModifiedBy = report.ModifiedBy,
            DeletedOn = report.DeletedOn,
            DeletedBy = report.DeletedBy,
            MissingPermissions = [.. names.Where(name => !existing.ContainsKey(name))],
        };
    }

    /// <summary>The report as the form edits it, with the defaults the runner applies to empty settings.</summary>
    internal static ReportEditRequest ToEditRequest(ReportDefinition report)
    {
        return new ReportEditRequest
        {
            Title = report.Title,
            Description = report.Description,
            Area = report.Area,
            Source = report.Source,
            Paginate = ReportAccess.Paginates(report),
            PageSize = report.PageSize,
            AllowExcel = report.AllowExcel,
            AllowPdf = report.AllowPdf,
            PdfLandscape = report.PdfLandscape == true,
            SensitiveNotice = report.SensitiveNotice,
            OrderBy = report.OrderBy,
            OrderByDescending = report.OrderByDescending == true,
            Permissions = PermissionNames(report),
            Columns = [.. report.Columns.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).Select(c => new ReportColumnEdit
            {
                SourceColumn = c.SourceColumn ?? string.Empty,
                Label = c.Label,
                Format = c.Format ?? ReportColumnFormat.Text,
                Sortable = c.Sortable,
                Visible = c.Visible,
            })],
            Parameters = [.. report.Parameters.OrderBy(p => p.DisplayOrder ?? int.MaxValue).ThenBy(p => p.Id).Select(p => new ReportParameterEdit
            {
                Name = p.Name ?? string.Empty,
                Label = p.Label,
                Type = p.Type ?? ReportParameterType.Text,
                Operator = p.Operator,
                Required = p.Required == true,
                DefaultValue = p.DefaultValue,
                ChoicesSource = p.ChoicesSource,
                ChoicesList = ReportChoiceList.Parse(p.ChoicesList),
                Binding = p.Binding ?? ReportParameterBinding.UserInput,
            })],
        };
    }

    private static IReadOnlyList<string> PermissionNames(ReportDefinition report)
    {
        return [.. report.Permissions.Select(p => p.Permission).Order(StringComparer.OrdinalIgnoreCase)];
    }

    private static string SourceName(string name, ReportValidation validation)
    {
        string trimmed = name.Trim();
        return validation.SourceNames.GetValueOrDefault(trimmed, trimmed);
    }

    private static string? Clean(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
