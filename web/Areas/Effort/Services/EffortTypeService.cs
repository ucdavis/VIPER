using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

public class EffortTypeService : IEffortTypeService
{
    private readonly EffortDbContext _context;
    private readonly IMapper _mapper;

    public EffortTypeService(EffortDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EffortTypeDto>> GetEffortTypesAsync(bool? activeOnly = null, CancellationToken ct = default)
    {
        var query = _context.EffortTypes.AsNoTracking();

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        var types = await query
            .OrderBy(t => t.Class)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);

        // Get instructor counts per type (distinct person/year combinations)
        var typeIds = types.Select(t => t.Id).ToList();
        var instructorCounts = typeIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.Percentages
                .AsNoTracking()
                .Where(p => typeIds.Contains(p.EffortTypeId))
                .Select(p => new { p.EffortTypeId, p.PersonId, p.AcademicYear })
                .Distinct()
                .GroupBy(p => p.EffortTypeId)
                .Select(g => new { EffortTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EffortTypeId, x => x.Count, ct);

        return types.Select(t =>
        {
            var dto = _mapper.Map<EffortTypeDto>(t);
            dto.InstructorCount = instructorCounts.GetValueOrDefault(t.Id, 0);
            return dto;
        }).ToList();
    }

    public async Task<EffortTypeDto?> GetEffortTypeAsync(int id, CancellationToken ct = default)
    {
        var type = await _context.EffortTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (type == null) return null;

        return _mapper.Map<EffortTypeDto>(type);
    }

    public async Task<List<string>> GetEffortTypeClassesAsync(CancellationToken ct = default)
    {
        return await _context.EffortTypes
            .AsNoTracking()
            .Select(t => t.Class)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<InstructorsByTypeResponseDto?> GetInstructorsByTypeAsync(int typeId, CancellationToken ct = default)
    {
        var type = await _context.EffortTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId, ct);

        if (type == null) return null;

        // Get all percentages for this type
        var percentages = await _context.Percentages
            .AsNoTracking()
            .Where(p => p.EffortTypeId == typeId)
            .Select(p => new { p.PersonId, p.AcademicYear })
            .Distinct()
            .OrderBy(p => p.AcademicYear)
            .ThenBy(p => p.PersonId)
            .ToListAsync(ct);

        // Get the person IDs
        var personIds = percentages.Select(p => p.PersonId).Distinct().ToList();

        // Get the most recent record for each person to get their name
        var persons = await _context.Persons
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId))
            .GroupBy(p => p.PersonId)
            .Select(g => g.OrderByDescending(p => p.TermCode).First())
            .ToDictionaryAsync(p => p.PersonId, ct);

        // Build result with one entry per person/year combination
        var instructors = percentages.Select(p =>
        {
            var person = persons.GetValueOrDefault(p.PersonId);
            return new InstructorByTypeDto
            {
                PersonId = p.PersonId,
                FirstName = person?.FirstName ?? string.Empty,
                LastName = person?.LastName ?? string.Empty,
                FullName = person != null ? $"{person.LastName}, {person.FirstName}" : string.Empty,
                AcademicYear = p.AcademicYear
            };
        })
        .OrderBy(i => i.AcademicYear)
        .ThenBy(i => i.LastName)
        .ThenBy(i => i.FirstName)
        .ToList();

        return new InstructorsByTypeResponseDto
        {
            TypeId = type.Id,
            TypeName = type.Name,
            TypeClass = type.Class,
            Instructors = instructors
        };
    }
}
