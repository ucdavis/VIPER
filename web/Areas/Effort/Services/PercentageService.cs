using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for percentage assignment operations.
/// </summary>
public class PercentageService : IPercentageService
{
    private readonly EffortDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEffortAuditService _auditService;
    private readonly IUserHelper _userHelper;

    private const string LeaveTypeClass = "Leave";

    public PercentageService(
        EffortDbContext context,
        IMapper mapper,
        IEffortAuditService auditService,
        IUserHelper userHelper)
    {
        _context = context;
        _mapper = mapper;
        _auditService = auditService;
        _userHelper = userHelper;
    }

    /// <inheritdoc />
    public async Task<List<PercentageDto>> GetPercentagesForPersonAsync(int personId, CancellationToken ct = default)
    {
        var percentages = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.StartDate)
            .ThenBy(p => p.PercentAssignType.Name)
            .ToListAsync(ct);

        return percentages.Select(EnrichDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PercentageDto?> GetPercentageAsync(int id, CancellationToken ct = default)
    {
        var percentage = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return percentage == null ? null : EnrichDto(percentage);
    }

    /// <inheritdoc />
    public async Task<PercentageDto> CreatePercentageAsync(CreatePercentageRequest request, CancellationToken ct = default)
    {
        await ValidateRequestAsync(request, ct);

        var academicYear = AcademicYearHelper.GetAcademicYearString(request.StartDate);

        var percentage = new Percentage
        {
            PersonId = request.PersonId,
            PercentAssignTypeId = request.PercentAssignTypeId,
            UnitId = request.UnitId,
            Modifier = request.Modifier?.Trim(),
            Comment = request.Comment?.Trim(),
            PercentageValue = (double)request.PercentageValue / 100.0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Compensated = request.Compensated,
            AcademicYear = academicYear,
            ModifiedDate = DateTime.Now,
            ModifiedBy = GetCurrentPersonId()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Percentages.Add(percentage);
        await _context.SaveChangesAsync(ct);

        var type = await _context.PercentAssignTypes
            .AsNoTracking()
            .FirstAsync(t => t.Id == request.PercentAssignTypeId, ct);
        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);

        await _auditService.LogPercentageChangeAsync(percentage.Id, null, EffortAuditActions.CreatePercent,
            null,
            new
            {
                TypeName = type.Name,
                TypeClass = type.Class,
                UnitName = unit?.Name,
                PercentageValue = request.PercentageValue,
                percentage.StartDate,
                percentage.EndDate,
                percentage.Compensated
            }, ct);

        await transaction.CommitAsync(ct);

        return await GetPercentageAsync(percentage.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve created percentage.");
    }

    /// <inheritdoc />
    public async Task<PercentageDto?> UpdatePercentageAsync(int id, UpdatePercentageRequest request, CancellationToken ct = default)
    {
        var percentage = await _context.Percentages
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (percentage == null)
        {
            return null;
        }

        var createRequest = new CreatePercentageRequest
        {
            PersonId = percentage.PersonId,
            PercentAssignTypeId = request.PercentAssignTypeId,
            UnitId = request.UnitId,
            Modifier = request.Modifier,
            Comment = request.Comment,
            PercentageValue = request.PercentageValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Compensated = request.Compensated
        };
        await ValidateRequestAsync(createRequest, ct);

        var oldValues = new
        {
            TypeName = percentage.PercentAssignType.Name,
            TypeClass = percentage.PercentAssignType.Class,
            UnitName = percentage.Unit?.Name,
            PercentageValue = percentage.PercentageValue * 100,
            percentage.StartDate,
            percentage.EndDate,
            percentage.Compensated
        };

        percentage.PercentAssignTypeId = request.PercentAssignTypeId;
        percentage.UnitId = request.UnitId;
        percentage.Modifier = request.Modifier?.Trim();
        percentage.Comment = request.Comment?.Trim();
        percentage.PercentageValue = (double)request.PercentageValue / 100.0;
        percentage.StartDate = request.StartDate;
        percentage.EndDate = request.EndDate;
        percentage.Compensated = request.Compensated;
        percentage.AcademicYear = AcademicYearHelper.GetAcademicYearString(request.StartDate);
        percentage.ModifiedDate = DateTime.Now;
        percentage.ModifiedBy = GetCurrentPersonId();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        await _context.SaveChangesAsync(ct);

        var type = await _context.PercentAssignTypes
            .AsNoTracking()
            .FirstAsync(t => t.Id == request.PercentAssignTypeId, ct);
        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);

        await _auditService.LogPercentageChangeAsync(id, null, EffortAuditActions.UpdatePercent,
            oldValues,
            new
            {
                TypeName = type.Name,
                TypeClass = type.Class,
                UnitName = unit?.Name,
                PercentageValue = request.PercentageValue,
                request.StartDate,
                request.EndDate,
                request.Compensated
            }, ct);

        await transaction.CommitAsync(ct);

        return await GetPercentageAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeletePercentageAsync(int id, CancellationToken ct = default)
    {
        var percentage = await _context.Percentages
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (percentage == null)
        {
            return false;
        }

        var oldValues = new
        {
            TypeName = percentage.PercentAssignType.Name,
            TypeClass = percentage.PercentAssignType.Class,
            UnitName = percentage.Unit?.Name,
            PercentageValue = percentage.PercentageValue * 100,
            percentage.StartDate,
            percentage.EndDate,
            percentage.Compensated
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Percentages.Remove(percentage);
        await _context.SaveChangesAsync(ct);

        await _auditService.LogPercentageChangeAsync(id, null, EffortAuditActions.DeletePercent,
            oldValues, null, ct);

        await transaction.CommitAsync(ct);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<PercentageDto>> GetPercentagesForPersonByDateRangeAsync(
        int personId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var percentages = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .Where(p => p.PersonId == personId)
            .Where(p => start <= (p.EndDate ?? DateTime.MaxValue) && end >= p.StartDate)
            .OrderByDescending(p => p.StartDate)
            .ThenBy(p => p.PercentAssignType.Name)
            .ToListAsync(ct);

        return percentages.Select(EnrichDto).ToList();
    }

    /// <inheritdoc />
    public async Task<PercentageValidationResult> ValidatePercentageAsync(
        CreatePercentageRequest request, int? excludeId = null, CancellationToken ct = default)
    {
        var result = new PercentageValidationResult { IsValid = true };

        var type = await _context.PercentAssignTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.PercentAssignTypeId, ct);

        if (type == null)
        {
            result.Errors.Add("Invalid percent assignment type.");
            result.IsValid = false;
        }
        else if (!type.IsActive)
        {
            result.Errors.Add($"Percent assignment type '{type.Name}' is inactive.");
            result.IsValid = false;
        }

        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);

        if (unit == null)
        {
            result.Errors.Add("Invalid unit.");
            result.IsValid = false;
        }
        else if (!unit.IsActive)
        {
            result.Errors.Add($"Unit '{unit.Name}' is inactive.");
            result.IsValid = false;
        }

        if (request.PercentageValue < 0 || request.PercentageValue > 100)
        {
            result.Errors.Add("Percentage must be between 0 and 100.");
            result.IsValid = false;
        }

        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
        {
            result.Errors.Add("End date cannot be before start date.");
            result.IsValid = false;
        }

        if (!result.IsValid)
        {
            return result;
        }

        var existingPercentages = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Where(p => p.PersonId == request.PersonId)
            .Where(p => excludeId == null || p.Id != excludeId)
            .ToListAsync(ct);

        var overlappingWithSameType = existingPercentages
            .Where(p => p.PercentAssignTypeId == request.PercentAssignTypeId)
            .Where(p => HasOverlap(request.StartDate, request.EndDate, p.StartDate, p.EndDate))
            .ToList();

        if (overlappingWithSameType.Count > 0)
        {
            result.Warnings.Add($"This overlaps with {overlappingWithSameType.Count} existing record(s) of the same type.");
        }

        var isNewActive = IsActive(request.EndDate);
        // Calculate totals using decimal to avoid floating-point precision issues
        var activeNonLeaveTotal = existingPercentages
            .Where(p => IsActive(p.EndDate))
            .Where(p => !string.Equals(p.PercentAssignType.Class, LeaveTypeClass, StringComparison.OrdinalIgnoreCase))
            .Sum(p => (decimal)Math.Round(p.PercentageValue * 100, 2));

        if (isNewActive && type != null && !string.Equals(type.Class, LeaveTypeClass, StringComparison.OrdinalIgnoreCase))
        {
            var newTotal = activeNonLeaveTotal + Math.Round(request.PercentageValue, 2);
            if (newTotal > 100)
            {
                result.Warnings.Add($"Total active percentage ({newTotal:F0}%) exceeds 100%.");
            }
        }

        return result;
    }

    private PercentageDto EnrichDto(Percentage entity)
    {
        var dto = _mapper.Map<PercentageDto>(entity);
        dto.PercentageValue = (decimal)(entity.PercentageValue * 100);
        dto.TypeName = entity.PercentAssignType.Name;
        dto.TypeClass = entity.PercentAssignType.Class;
        dto.UnitName = entity.Unit?.Name;
        dto.IsActive = IsActive(entity.EndDate);
        return dto;
    }

    private static bool IsActive(DateTime? endDate)
    {
        return endDate == null || endDate >= DateTime.Today;
    }

    private static bool HasOverlap(DateTime newStart, DateTime? newEnd, DateTime existingStart, DateTime? existingEnd)
    {
        var newEndDate = newEnd ?? DateTime.MaxValue;
        var existingEndDate = existingEnd ?? DateTime.MaxValue;
        return newStart <= existingEndDate && newEndDate >= existingStart;
    }

    private async Task ValidateRequestAsync(CreatePercentageRequest request, CancellationToken ct)
    {
        var type = await _context.PercentAssignTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.PercentAssignTypeId, ct);

        if (type == null)
        {
            throw new InvalidOperationException("Invalid percent assignment type.");
        }

        if (!type.IsActive)
        {
            throw new InvalidOperationException($"Percent assignment type '{type.Name}' is inactive.");
        }

        var unit = await _context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);

        if (unit == null)
        {
            throw new InvalidOperationException("Invalid unit.");
        }

        if (!unit.IsActive)
        {
            throw new InvalidOperationException($"Unit '{unit.Name}' is inactive.");
        }

        if (request.PercentageValue < 0 || request.PercentageValue > 100)
        {
            throw new InvalidOperationException("Percentage must be between 0 and 100.");
        }

        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
        {
            throw new InvalidOperationException("End date cannot be before start date.");
        }
    }

    private int? GetCurrentPersonId()
    {
        var user = _userHelper.GetCurrentUser();
        if (user != null && user.AaudUserId > 0)
        {
            return user.AaudUserId;
        }

        return null;
    }
}
