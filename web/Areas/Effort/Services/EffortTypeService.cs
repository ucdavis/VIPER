using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for effort type-related operations.
/// </summary>
public class EffortTypeService : IEffortTypeService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly IMapper _mapper;

    public EffortTypeService(EffortDbContext context, IEffortAuditService auditService, IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
    }

    private static string NormalizeEffortType(string id) => id.Trim().ToUpperInvariant();

    private static object GetEffortTypeSnapshot(EffortType et) => new
    {
        et.Id,
        et.Description,
        et.UsesWeeks,
        et.IsActive,
        et.FacultyCanEnter,
        et.AllowedOnDvm,
        et.AllowedOn199299,
        et.AllowedOnRCourses
    };

    public async Task<List<EffortTypeDto>> GetEffortTypesAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.EffortTypes.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var effortTypes = await query
            .OrderBy(s => s.Description)
            .ToListAsync(ct);

        // Get usage counts for all effort types
        var effortTypeIds = effortTypes.Select(s => s.Id).ToList();
        var usageCounts = await _context.Records
            .Where(r => effortTypeIds.Contains(r.EffortTypeId))
            .GroupBy(r => r.EffortTypeId)
            .Select(g => new { EffortTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EffortTypeId, x => x.Count, ct);

        return effortTypes.Select(s =>
        {
            var dto = _mapper.Map<EffortTypeDto>(s);
            dto.UsageCount = usageCounts.GetValueOrDefault(s.Id, 0);
            dto.CanDelete = dto.UsageCount == 0;
            return dto;
        }).ToList();
    }

    public async Task<EffortTypeDto?> GetEffortTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(id);
        var effortType = await _context.EffortTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == normalizedId, ct);

        if (effortType == null) return null;

        var dto = _mapper.Map<EffortTypeDto>(effortType);
        dto.UsageCount = await GetUsageCountAsync(normalizedId, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<EffortTypeDto> CreateEffortTypeAsync(CreateEffortTypeRequest request, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(request.Id);

        // Check for duplicate ID
        if (await _context.EffortTypes.AnyAsync(s => s.Id == normalizedId, ct))
        {
            throw new InvalidOperationException($"An effort type with ID '{normalizedId}' already exists.");
        }

        var effortType = new EffortType
        {
            Id = normalizedId,
            Description = request.Description.Trim(),
            UsesWeeks = request.UsesWeeks,
            IsActive = true,
            FacultyCanEnter = request.FacultyCanEnter,
            AllowedOnDvm = request.AllowedOnDvm,
            AllowedOn199299 = request.AllowedOn199299,
            AllowedOnRCourses = request.AllowedOnRCourses
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.EffortTypes.Add(effortType);
        await _context.SaveChangesAsync(ct);

        _auditService.AddEffortTypeChangeAudit(effortType.Id, EffortAuditActions.CreateEffortType,
            null,
            GetEffortTypeSnapshot(effortType));
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<EffortTypeDto>(effortType);
        dto.UsageCount = 0;
        dto.CanDelete = true;
        return dto;
    }

    public async Task<EffortTypeDto?> UpdateEffortTypeAsync(string id, UpdateEffortTypeRequest request, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(id);
        var effortType = await _context.EffortTypes.FirstOrDefaultAsync(s => s.Id == normalizedId, ct);
        if (effortType == null) return null;

        var oldState = GetEffortTypeSnapshot(effortType);

        effortType.Description = request.Description.Trim();
        effortType.UsesWeeks = request.UsesWeeks;
        effortType.IsActive = request.IsActive;
        effortType.FacultyCanEnter = request.FacultyCanEnter;
        effortType.AllowedOnDvm = request.AllowedOnDvm;
        effortType.AllowedOn199299 = request.AllowedOn199299;
        effortType.AllowedOnRCourses = request.AllowedOnRCourses;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddEffortTypeChangeAudit(normalizedId, EffortAuditActions.UpdateEffortType,
            oldState,
            GetEffortTypeSnapshot(effortType));
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<EffortTypeDto>(effortType);
        dto.UsageCount = await GetUsageCountAsync(normalizedId, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<bool> DeleteEffortTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(id);
        if (!await CanDeleteEffortTypeAsync(normalizedId, ct))
        {
            return false;
        }

        var effortType = await _context.EffortTypes.FirstOrDefaultAsync(s => s.Id == normalizedId, ct);
        if (effortType == null) return false;

        var oldState = GetEffortTypeSnapshot(effortType);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.EffortTypes.Remove(effortType);
        _auditService.AddEffortTypeChangeAudit(normalizedId, EffortAuditActions.DeleteEffortType,
            oldState,
            null);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    public async Task<bool> CanDeleteEffortTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(id);
        return await GetUsageCountAsync(normalizedId, ct) == 0;
    }

    private async Task<int> GetUsageCountAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeEffortType(id);
        return await _context.Records
            .CountAsync(r => r.EffortTypeId == normalizedId, ct);
    }
}
