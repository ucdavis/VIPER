using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services;

/// <summary>
/// The career selection dropdown options: the lists the form offers, and their admin management.
/// </summary>
public class CareerSelectionOptionService : ICareerSelectionOptionService
{
    private readonly VIPERContext _context;

    public CareerSelectionOptionService(VIPERContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<CareerSelectionOptionDto>> GetOptionsAsync(CareerOptionType type, bool includeUsage, CancellationToken ct = default)
    {
        var options = await OptionsQuery(type).ToListAsync(ct);
        if (!includeUsage)
        {
            return options;
        }

        var usage = await UsageCountsAsync(type, ct);
        foreach (var option in options)
        {
            option.UsageCount = usage.GetValueOrDefault(option.Id);
        }
        return options;
    }

    /// <inheritdoc/>
    public async Task<CareerSelectionOptionWriteResult> CreateOptionAsync(CareerOptionType type, string label, CancellationToken ct = default)
    {
        var trimmed = label.Trim();
        var error = ValidateLabel(type, trimmed);
        if (error != null)
        {
            return CareerSelectionOptionWriteResult.Invalid(error);
        }

        var options = await LoadOptionsAsync(type, ct);
        if (IsDuplicate(options, trimmed, excludeId: null))
        {
            return CareerSelectionOptionWriteResult.Conflict(DuplicateMessage(trimmed));
        }

        var option = NewOption(type, trimmed);
        _context.Add(option);
        return await SaveAsync(option, ct);
    }

    /// <inheritdoc/>
    public async Task<CareerSelectionOptionWriteResult> UpdateOptionAsync(CareerOptionType type, int id, string label, CancellationToken ct = default)
    {
        var options = await LoadOptionsAsync(type, ct);
        var option = options.FirstOrDefault(o => o.Id == id);
        if (option == null)
        {
            return CareerSelectionOptionWriteResult.NotFound();
        }
        if (option.IsOther)
        {
            return CareerSelectionOptionWriteResult.Invalid("The Other option cannot be renamed.");
        }

        var trimmed = label.Trim();
        var error = ValidateLabel(type, trimmed);
        if (error != null)
        {
            return CareerSelectionOptionWriteResult.Invalid(error);
        }
        if (IsDuplicate(options, trimmed, excludeId: id))
        {
            return CareerSelectionOptionWriteResult.Conflict(DuplicateMessage(trimmed));
        }

        option.Label = trimmed;
        return await SaveAsync(option, ct);
    }

    /// <inheritdoc/>
    public async Task<CareerSelectionOptionWriteResult> DeleteOptionAsync(CareerOptionType type, int id, CancellationToken ct = default)
    {
        var options = await LoadOptionsAsync(type, ct);
        var option = options.FirstOrDefault(o => o.Id == id);
        if (option == null)
        {
            return CareerSelectionOptionWriteResult.NotFound();
        }
        if (option.IsOther)
        {
            return CareerSelectionOptionWriteResult.Invalid("The catch-all option cannot be deleted.");
        }

        var inUseMessage = $"\"{option.Label}\" cannot be deleted because a student has selected it.";
        if (await IsInUseAsync(type, id, ct))
        {
            return CareerSelectionOptionWriteResult.Conflict(inUseMessage);
        }

        _context.Remove(option);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsDataRejection())
        {
            // A student chose the option between the check above and this delete, and the
            // foreign key refused it.
            return CareerSelectionOptionWriteResult.Conflict(inUseMessage);
        }
        return CareerSelectionOptionWriteResult.Success();
    }

    /// <summary>
    /// Commits a create or rename and shapes the outcome into a result. The duplicate check runs
    /// before this, so a rejection here means the list changed underneath us rather than that the
    /// admin did anything wrong, and the message says so.
    /// </summary>
    private async Task<CareerSelectionOptionWriteResult> SaveAsync(ICareerSelectionOption option, CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsDataRejection())
        {
            // Only reachable if the data changed after it was checked, most likely another admin
            // saving the same name at the same moment.
            return CareerSelectionOptionWriteResult.Conflict(
                "The option could not be saved because the list has changed. Reload the page and try again.");
        }
        return CareerSelectionOptionWriteResult.Success(
            new CareerSelectionOptionDto { Id = option.Id, Label = option.Label, IsOther = option.IsOther });
    }

    /// <summary>
    /// Checks a trimmed name against the rules every list shares, returning the message to show
    /// the admin, or null when the name is acceptable.
    /// </summary>
    private static string? ValidateLabel(CareerOptionType type, string trimmed)
    {
        if (trimmed.Length == 0)
        {
            return "Please enter a name.";
        }
        var maxLength = MaxLabelLength(type);
        return trimmed.Length > maxLength ? $"Name must be {maxLength} characters or fewer." : null;
    }

    /// <summary>
    /// Matches the label column length of each option table, so an over-long name is reported as
    /// a validation message rather than truncated or rejected by the database.
    /// </summary>
    private static int MaxLabelLength(CareerOptionType type) => type switch
    {
        CareerOptionType.Career => 100,
        CareerOptionType.Species => 100,
        CareerOptionType.PostGrad => 200,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type."),
    };

    /// <summary>
    /// Whether another option in the list already has this name. Compared in memory so the rule
    /// holds whatever the database collation, and so stored labels with stray whitespace still
    /// count as a match. A rename passes its own id as <paramref name="excludeId"/> so an option
    /// is never a duplicate of itself.
    /// </summary>
    private static bool IsDuplicate(IEnumerable<ICareerSelectionOption> options, string trimmed, int? excludeId) =>
        options.Any(o => o.Id != excludeId && string.Equals(o.Label.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Shared by create and rename so both report a name collision in the same words.
    /// </summary>
    private static string DuplicateMessage(string trimmed) => $"An option named \"{trimmed}\" already exists.";

    /// <summary>
    /// Loads every option of a type, tracked for editing. The tables are small, and
    /// the shared interface members that paper over their differing column names cannot be
    /// translated into SQL, so the whole list is worked on in memory.
    /// </summary>
    private async Task<List<ICareerSelectionOption>> LoadOptionsAsync(CareerOptionType type, CancellationToken ct)
    {
        switch (type)
        {
            case CareerOptionType.Career:
                return [.. await _context.CareerOptions.ToListAsync(ct)];
            case CareerOptionType.Species:
                return [.. await _context.SpeciesOptions.ToListAsync(ct)];
            case CareerOptionType.PostGrad:
                return [.. await _context.PostGradOptions.ToListAsync(ct)];
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type.");
        }
    }

    /// <summary>
    /// Builds the entity for the table this type maps to. New options are never the catch-all:
    /// that row is seeded with the schema and cannot be created from the admin screen.
    /// </summary>
    private static ICareerSelectionOption NewOption(CareerOptionType type, string label) => type switch
    {
        CareerOptionType.Career => new CareerOption { Career = label },
        CareerOptionType.Species => new SpeciesOption { Species = label },
        CareerOptionType.PostGrad => new PostGradOption { PostGrad = label },
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type."),
    };

    /// <summary>
    /// Whether any stored career selection points at this option. Species is checked in both of
    /// its columns, so an option used only as someone's second choice still blocks a delete.
    /// </summary>
    private Task<bool> IsInUseAsync(CareerOptionType type, int id, CancellationToken ct) => type switch
    {
        CareerOptionType.Career => _context.CareerSelections.AnyAsync(c => c.Career == id, ct),
        CareerOptionType.Species => _context.CareerSelections.AnyAsync(c => c.FirstSpecies == id || c.SecondSpecies == id, ct),
        CareerOptionType.PostGrad => _context.CareerSelections.AnyAsync(c => c.PostGrad == id, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type."),
    };

    /// <summary>
    /// The read query for one option list, projected to the shared DTO so the three tables' own
    /// column names stay inside this class. Ordered with the catch-all last, then alphabetically,
    /// which is the order the form presents.
    /// </summary>
    private IQueryable<CareerSelectionOptionDto> OptionsQuery(CareerOptionType type) => type switch
    {
        CareerOptionType.Career => _context.CareerOptions
            .AsNoTracking()
            .OrderBy(o => o.IsOther)
            .ThenBy(o => o.Career)
            .Select(o => new CareerSelectionOptionDto { Id = o.CareerOptionId, Label = o.Career, IsOther = o.IsOther }),
        CareerOptionType.Species => _context.SpeciesOptions
            .AsNoTracking()
            .OrderBy(o => o.IsOther)
            .ThenBy(o => o.Species)
            .Select(o => new CareerSelectionOptionDto { Id = o.SpeciesOptionId, Label = o.Species, IsOther = o.IsOther }),
        CareerOptionType.PostGrad => _context.PostGradOptions
            .AsNoTracking()
            .OrderBy(o => o.IsOther)
            .ThenBy(o => o.PostGrad)
            .Select(o => new CareerSelectionOptionDto { Id = o.PostGradOptionId, Label = o.PostGrad, IsOther = o.IsOther }),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type."),
    };

    /// <summary>
    /// How many career selections use each option of a type, keyed by option id. An option nobody
    /// has chosen is absent from the dictionary rather than present with a zero.
    /// </summary>
    private async Task<Dictionary<int, int>> UsageCountsAsync(CareerOptionType type, CancellationToken ct)
    {
        var selections = _context.CareerSelections.AsNoTracking();
        switch (type)
        {
            case CareerOptionType.Career:
                return await CountByOptionAsync(selections.Select(c => c.Career), ct);
            case CareerOptionType.PostGrad:
                return await CountByOptionAsync(selections.Select(c => c.PostGrad), ct);
            case CareerOptionType.Species:
                // Species is referenced from two columns. A selection naming the same species in
                // both counts once, so that overlap is subtracted from the per-column totals.
                var first = await CountByOptionAsync(selections.Select(c => c.FirstSpecies), ct);
                var second = await CountByOptionAsync(selections.Select(c => c.SecondSpecies), ct);
                var both = await CountByOptionAsync(
                    selections.Where(c => c.FirstSpecies == c.SecondSpecies).Select(c => c.FirstSpecies), ct);
                return first.Keys.Union(second.Keys).ToDictionary(
                    id => id,
                    id => first.GetValueOrDefault(id) + second.GetValueOrDefault(id) - both.GetValueOrDefault(id));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown career option type.");
        }
    }

    /// <summary>
    /// Groups one nullable option-id column into per-option counts, in SQL rather than by loading
    /// the selections. Unanswered rows are null and drop out of the count.
    /// </summary>
    private static async Task<Dictionary<int, int>> CountByOptionAsync(IQueryable<int?> optionIds, CancellationToken ct)
    {
        return await optionIds
            .Where(id => id != null)
            .GroupBy(id => id!.Value)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OptionId, x => x.Count, ct);
    }
}
