namespace Viper.Areas.Students.Models;

/// <summary>
/// The optional body for a career selection export: the grid's rows, filtered and sorted as the
/// user sees them, so the file matches the screen.
/// </summary>
/// <remarks>
/// The client sends row keys rather than its search text and sort, because re-running the grid's
/// search on the server would mean copying rules that only hold in the browser: Last Updated is
/// searched as the browser's locale date, the overview's completeness columns are searched as
/// true/false, and the grid's default sort orders blank values (Last Updated, Mentor, Species 2)
/// inconsistently. Keys can only narrow and reorder the rows the caller may already see.
/// </remarks>
public class CareerSelectionExportRequest
{
    /// <summary>
    /// The rows to export, in order. Null exports everything the caller may see; empty exports
    /// headers only, as when the grid's search matches nothing.
    /// </summary>
    public List<string>? RowKeys { get; set; }

    /// <summary>
    /// Narrows <paramref name="rows"/> to the requested keys, in the order requested. A key outside
    /// the rows is ignored, so a request cannot reach a student the caller may not see, and a
    /// repeated key keeps its first position.
    /// </summary>
    public static List<T> ApplyRowKeys<T>(List<T> rows, CareerSelectionExportRequest? request)
        where T : StudentCareerRowDto
    {
        if (request?.RowKeys == null)
        {
            return rows;
        }

        var positionByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        // A null in the list names no row; skipping it keeps a malformed body from throwing.
        foreach (var key in request.RowKeys.OfType<string>())
        {
            positionByKey.TryAdd(key, positionByKey.Count);
        }

        return rows
            .Where(r => positionByKey.ContainsKey(r.RowKey))
            .OrderBy(r => positionByKey[r.RowKey])
            .ToList();
    }
}
