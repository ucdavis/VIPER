using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for session type-related operations.
/// </summary>
public class SessionTypeService : ISessionTypeService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly IMapper _mapper;

    public SessionTypeService(EffortDbContext context, IEffortAuditService auditService, IMapper mapper)
    {
        _context = context;
        _auditService = auditService;
        _mapper = mapper;
    }

    private static string NormalizeSessionTypeId(string id) => id.Trim().ToUpperInvariant();

    private static object GetSessionTypeSnapshot(SessionType st) => new
    {
        st.Id,
        st.Description,
        st.UsesWeeks,
        st.IsActive,
        st.FacultyCanEnter,
        st.AllowedOnDvm,
        st.AllowedOn199299,
        st.AllowedOnRCourses
    };

    public async Task<List<SessionTypeDto>> GetSessionTypesAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _context.SessionTypes.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var sessionTypes = await query
            .OrderBy(s => s.Description)
            .ToListAsync(ct);

        // Get usage counts for all session types
        var sessionTypeIds = sessionTypes.Select(s => s.Id).ToList();
        var usageCounts = await _context.Records
            .Where(r => sessionTypeIds.Contains(r.SessionType))
            .GroupBy(r => r.SessionType)
            .Select(g => new { SessionType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionType, x => x.Count, ct);

        return sessionTypes.Select(s =>
        {
            var dto = _mapper.Map<SessionTypeDto>(s);
            dto.UsageCount = usageCounts.GetValueOrDefault(s.Id, 0);
            dto.CanDelete = dto.UsageCount == 0;
            return dto;
        }).ToList();
    }

    public async Task<SessionTypeDto?> GetSessionTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(id);
        var sessionType = await _context.SessionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == normalizedId, ct);

        if (sessionType == null) return null;

        var dto = _mapper.Map<SessionTypeDto>(sessionType);
        dto.UsageCount = await GetUsageCountAsync(normalizedId, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<SessionTypeDto> CreateSessionTypeAsync(CreateSessionTypeRequest request, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(request.Id);

        // Check for duplicate ID
        if (await _context.SessionTypes.AnyAsync(s => s.Id == normalizedId, ct))
        {
            throw new InvalidOperationException($"A session type with ID '{normalizedId}' already exists.");
        }

        var sessionType = new SessionType
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
        _context.SessionTypes.Add(sessionType);
        await _context.SaveChangesAsync(ct);

        _auditService.AddSessionTypeChangeAudit(sessionType.Id, EffortAuditActions.CreateSessionType,
            null,
            GetSessionTypeSnapshot(sessionType));
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<SessionTypeDto>(sessionType);
        dto.UsageCount = 0;
        dto.CanDelete = true;
        return dto;
    }

    public async Task<SessionTypeDto?> UpdateSessionTypeAsync(string id, UpdateSessionTypeRequest request, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(id);
        var sessionType = await _context.SessionTypes.FirstOrDefaultAsync(s => s.Id == normalizedId, ct);
        if (sessionType == null) return null;

        var oldState = GetSessionTypeSnapshot(sessionType);

        sessionType.Description = request.Description.Trim();
        sessionType.UsesWeeks = request.UsesWeeks;
        sessionType.IsActive = request.IsActive;
        sessionType.FacultyCanEnter = request.FacultyCanEnter;
        sessionType.AllowedOnDvm = request.AllowedOnDvm;
        sessionType.AllowedOn199299 = request.AllowedOn199299;
        sessionType.AllowedOnRCourses = request.AllowedOnRCourses;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddSessionTypeChangeAudit(normalizedId, EffortAuditActions.UpdateSessionType,
            oldState,
            GetSessionTypeSnapshot(sessionType));
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var dto = _mapper.Map<SessionTypeDto>(sessionType);
        dto.UsageCount = await GetUsageCountAsync(normalizedId, ct);
        dto.CanDelete = dto.UsageCount == 0;
        return dto;
    }

    public async Task<bool> DeleteSessionTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(id);
        if (!await CanDeleteSessionTypeAsync(normalizedId, ct))
        {
            return false;
        }

        var sessionType = await _context.SessionTypes.FirstOrDefaultAsync(s => s.Id == normalizedId, ct);
        if (sessionType == null) return false;

        var oldState = GetSessionTypeSnapshot(sessionType);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.SessionTypes.Remove(sessionType);
        _auditService.AddSessionTypeChangeAudit(normalizedId, EffortAuditActions.DeleteSessionType,
            oldState,
            null);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    public async Task<bool> CanDeleteSessionTypeAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(id);
        return await GetUsageCountAsync(normalizedId, ct) == 0;
    }

    private async Task<int> GetUsageCountAsync(string id, CancellationToken ct = default)
    {
        var normalizedId = NormalizeSessionTypeId(id);
        return await _context.Records
            .CountAsync(r => r.SessionType == normalizedId, ct);
    }
}
