using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for unit-related operations.
/// </summary>
public class UnitService : IUnitService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly IMapper _mapper;

    public UnitService(EffortDbContext context, IEffortAuditService auditService, IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
    }

    public async Task<List<UnitDto>> GetUnitsAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.Units.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(u => u.IsActive);
        }

        var units = await query
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

        // Get usage counts for all units
        var unitIds = units.Select(u => u.Id).ToList();
        var usageCounts = await _context.Percentages
            .Where(p => p.UnitId != null && unitIds.Contains(p.UnitId.Value))
            .GroupBy(p => p.UnitId)
            .Select(g => new { UnitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UnitId!.Value, x => x.Count, ct);

        return units.Select(u =>
        {
            var dto = _mapper.Map<UnitDto>(u);
            dto.UsageCount = usageCounts.GetValueOrDefault(u.Id, 0);
            dto.CanDelete = dto.UsageCount == 0;
            return dto;
        }).ToList();
    }

    public async Task<UnitDto?> GetUnitAsync(int id, CancellationToken ct = default)
    {
        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null) return null;

        var dto = _mapper.Map<UnitDto>(unit);
        dto.UsageCount = await GetUsageCountAsync(id, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<UnitDto> CreateUnitAsync(CreateUnitRequest request, CancellationToken ct = default)
    {
        var normalizedName = await ValidateAndNormalizeNameAsync(request.Name, ct: ct);

        var unit = new Unit
        {
            Name = normalizedName,
            IsActive = true
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Units.Add(unit);
        await _context.SaveChangesAsync(ct);

        _auditService.AddUnitChangeAudit(unit.Id, EffortAuditActions.CreateUnit,
            null,
            new { unit.Name, unit.IsActive });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<UnitDto>(unit);
        dto.UsageCount = 0;
        dto.CanDelete = true;
        return dto;
    }

    public async Task<UnitDto?> UpdateUnitAsync(int id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit == null) return null;

        var normalizedName = await ValidateAndNormalizeNameAsync(request.Name, excludeId: id, ct: ct);

        var oldState = new { unit.Name, unit.IsActive };

        unit.Name = normalizedName;
        unit.IsActive = request.IsActive;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddUnitChangeAudit(id, EffortAuditActions.UpdateUnit,
            oldState,
            new { unit.Name, unit.IsActive });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<UnitDto>(unit);
        dto.UsageCount = await GetUsageCountAsync(id, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<bool> DeleteUnitAsync(int id, CancellationToken ct = default)
    {
        if (!await CanDeleteUnitAsync(id, ct))
        {
            return false;
        }

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit == null) return false;

        var oldState = new { unit.Name, unit.IsActive };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Units.Remove(unit);
        _auditService.AddUnitChangeAudit(id, EffortAuditActions.DeleteUnit,
            oldState,
            null);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    public async Task<bool> CanDeleteUnitAsync(int id, CancellationToken ct = default)
    {
        return await GetUsageCountAsync(id, ct) == 0;
    }

    public async Task<int> GetUsageCountAsync(int id, CancellationToken ct = default)
    {
        return await _context.Percentages
            .CountAsync(p => p.UnitId == id, ct);
    }

    /// <summary>
    /// Validates and normalizes a unit name, checking for duplicates.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="excludeId">Optional unit ID to exclude from duplicate check (for updates).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The normalized (trimmed) name.</returns>
    /// <exception cref="InvalidOperationException">Thrown if name is empty or a duplicate exists.</exception>
    private async Task<string> ValidateAndNormalizeNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Unit name is required.");
        }

        var query = _context.Units.Where(u => u.Name.ToLower() == normalizedName.ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }

        if (await query.AnyAsync(ct))
        {
            throw new InvalidOperationException($"A unit with name '{normalizedName}' already exists");
        }

        return normalizedName;
    }
}
