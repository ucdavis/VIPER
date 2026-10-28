using Viper.Areas.Students.Models;

namespace Viper.Areas.Students.Services;

/// <summary>
/// The career selection dropdown lists - career directions, species and post-graduation plans -
/// and their admin management. Each list is its own table, and <see cref="CareerOptionType"/>
/// selects between them, so callers work with one set of methods rather than three.
/// <para>
/// The writes report a rejected name as a <see cref="CareerSelectionOptionWriteResult"/> rather
/// than by throwing: a duplicate or an option still in use is something an admin can correct, so
/// it reaches the controller as a message to show rather than an error.
/// </para>
/// </summary>
public interface ICareerSelectionOptionService
{
    /// <summary>
    /// Every option of one type, catch-all last. With <paramref name="includeUsage"/> each option
    /// carries the number of career selections using it; otherwise the counts are left at zero and
    /// never queried. Usage covers every stored selection, not just current students: a graduate's
    /// record still holds the foreign key that would block a delete.
    /// </summary>
    Task<List<CareerSelectionOptionDto>> GetOptionsAsync(CareerOptionType type, bool includeUsage, CancellationToken ct = default);

    /// <summary>
    /// Adds an option. The name is trimmed and must be unique within its list, ignoring case.
    /// </summary>
    Task<CareerSelectionOptionWriteResult> CreateOptionAsync(CareerOptionType type, string label, CancellationToken ct = default);

    /// <summary>
    /// Renames an option. The catch-all cannot be renamed. Changing only the case of a name is
    /// allowed, since the option does not count as a duplicate of itself. The returned option
    /// carries no usage count; the caller reloads the list for that.
    /// </summary>
    Task<CareerSelectionOptionWriteResult> UpdateOptionAsync(CareerOptionType type, int id, string label, CancellationToken ct = default);

    /// <summary>
    /// Deletes an option no career selection uses. The catch-all cannot be deleted, and an option
    /// a student has already chosen comes back as a conflict rather than being removed.
    /// </summary>
    Task<CareerSelectionOptionWriteResult> DeleteOptionAsync(CareerOptionType type, int id, CancellationToken ct = default);
}
