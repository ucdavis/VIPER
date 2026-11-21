using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The parts of a run's results.json that a comparison reads: each result's identity, status, counts,
    /// example keys, latest affected date, and grid rows. Read back from the file, and built from the
    /// current run through the same JSON, so both sides have the same shape.
    /// </summary>
    public sealed class RunSnapshot
    {
        public string RanAt { get; init; } = "";
        public List<ResultSnapshot> Results { get; init; } = [];
    }

    public sealed class ResultSnapshot
    {
        public string Name { get; init; } = "";
        public string? Table { get; init; }
        public ResultKind Kind { get; init; }
        public Severity Severity { get; init; }
        public CheckStatus Status { get; init; }
        public List<ResultCount> Counts { get; init; } = [];
        public List<string> Examples { get; init; } = [];
        public long ExampleTotal { get; init; }
        public string? Latest { get; init; }
        public List<GridSnapshot> Grids { get; init; } = [];
    }

    public sealed class GridSnapshot
    {
        public string Title { get; init; } = "";
        public List<string> KeyColumns { get; init; } = [];
        public List<List<string>> Rows { get; init; } = [];
        public long Total { get; init; }
    }

    /// <summary>
    /// What changed since the last run in the same output folder, for report.txt: results found now and
    /// not before, found before and not now, other status changes, and for results found both times, what
    /// moved (counts, grid rows, the latest affected date, example keys). Results are matched by check
    /// name and subject. Context results are not compared, since their counts move with every write.
    /// </summary>
    public static class RunComparison
    {
        /// <summary>
        /// Example keys listed per result as new or no longer listed; the rest are counted.
        /// </summary>
        private const int KeysShown = 10;

        public static RunSnapshot Snapshot(AnalysisRun run) =>
            JsonSerializer.Deserialize<RunSnapshot>(AnalysisReport.ToJson(run), AnalysisReport.JsonOptions)!;

        /// <summary>
        /// The run saved in results.json, or null with the reason when there is none or it cannot be read.
        /// </summary>
        public static (RunSnapshot? Snapshot, string? Problem) Read(string path)
        {
            if (!File.Exists(path))
            {
                return (null, "no earlier run in this folder");
            }
            try
            {
                return (JsonSerializer.Deserialize<RunSnapshot>(File.ReadAllText(path), AnalysisReport.JsonOptions), null);
            }
            catch (JsonException ex)
            {
                return (null, $"the last run's results.json could not be read: {ex.Message}");
            }
        }

        public static List<string> Lines(RunSnapshot previous, RunSnapshot current)
        {
            var pairs = Pair(previous.Results, current.Results);
            var foundNow = new List<string>();
            var foundBefore = new List<string>();
            var otherStatus = new List<string>();
            var changed = new List<string>();
            var unchanged = 0;

            foreach (var (before, now) in pairs)
            {
                var wasFound = before?.Status == CheckStatus.ProblemsFound;
                var isFound = now?.Status == CheckStatus.ProblemsFound;
                if (isFound && !wasFound)
                {
                    foundNow.Add($"{Heading(now!)} ({(before == null ? "not in the last run" : $"was {StatusText(before)}")})");
                }
                else if (wasFound && !isFound)
                {
                    foundBefore.Add($"{Heading(before!)} ({(now == null ? "not in this run" : $"now {StatusText(now)}")})");
                }
                else if (before != null && now != null && before.Status != now.Status)
                {
                    otherStatus.Add($"{Heading(now)} (was {StatusText(before)}, now {StatusText(now)})");
                }
                else if (before != null && now != null && isFound && now.Kind != ResultKind.Context
                         && Differences(before, now) is { Count: > 0 } differences)
                {
                    changed.Add(Heading(now));
                    changed.AddRange(differences.Select(d => $"    {d}"));
                }
                else if (before != null && now != null)
                {
                    unchanged++;
                }
            }

            var lines = new List<string>
            {
                $"Last run: {previous.RanAt}. Results are matched by check and table, so a check renamed or reworded since " +
                "shows as gone and new. Context is not compared."
            };
            Add(lines, "Found now, not in the last run", foundNow);
            Add(lines, "Found in the last run, not now", foundBefore);
            Add(lines, "Other status changes", otherStatus);
            Add(lines, "Found both times, changed", changed, changed.Count(l => !l.StartsWith(' ')));
            lines.Add($"Unchanged results: {unchanged:N0}");
            return lines;
        }

        private static void Add(List<string> lines, string title, List<string> items, int? count = null)
        {
            if (items.Count == 0)
            {
                return;
            }
            lines.Add($"{title} ({count ?? items.Count:N0}):");
            lines.AddRange(items.Select(i => $"  {i}"));
        }

        /// <summary>
        /// Results with the same check name and subject, paired in order, so two declarations producing the
        /// same name on one table still pair one to one. Unpaired results come with null on the other side.
        /// </summary>
        private static List<(ResultSnapshot? Before, ResultSnapshot? Now)> Pair(List<ResultSnapshot> previous, List<ResultSnapshot> current)
        {
            static (string, string?, int) Key(ResultSnapshot r, int occurrence) => (r.Name, r.Table, occurrence);
            static Dictionary<(string, string?, int), ResultSnapshot> Keyed(List<ResultSnapshot> results) =>
                results.GroupBy(r => (r.Name, r.Table))
                    .SelectMany(g => g.Select((r, i) => (Key: Key(r, i), Result: r)))
                    .ToDictionary(k => k.Key, k => k.Result);

            var before = Keyed(previous);
            var now = Keyed(current);
            return now.Keys.Concat(before.Keys.Where(k => !now.ContainsKey(k)))
                .Select(k => (before.GetValueOrDefault(k), now.GetValueOrDefault(k)))
                .ToList();
        }

        private static List<string> Differences(ResultSnapshot before, ResultSnapshot now)
        {
            var differences = new List<string>();

            var previousCounts = before.Counts.GroupBy(c => c.Label).ToDictionary(g => g.Key, g => g.First().Value);
            differences.AddRange(now.Counts
                .Where(c => previousCounts.TryGetValue(c.Label, out var was) && was != c.Value)
                .Select(c => $"{c.Label}: {previousCounts[c.Label]:N0} to {c.Value:N0}"));

            var previousGrids = before.Grids.GroupBy(g => g.Title).ToDictionary(g => g.Key, g => g.First());
            differences.AddRange(now.Grids
                .Where(g => previousGrids.ContainsKey(g.Title))
                .SelectMany(g => GridDifferences(previousGrids[g.Title], g)));

            if (before.Latest != now.Latest)
            {
                differences.Add($"Latest affected row: {before.Latest ?? "-"} to {now.Latest ?? "-"}");
            }

            // Only the first keys are kept, so a key is new or gone only when the other run kept all of its keys.
            if (before.Examples.Count == before.ExampleTotal)
            {
                AddKeys(differences, "Keys not listed before", now.Examples.Except(before.Examples, StringComparer.Ordinal).ToList());
            }
            if (now.Examples.Count == now.ExampleTotal)
            {
                AddKeys(differences, "Keys no longer listed", before.Examples.Except(now.Examples, StringComparer.Ordinal).ToList());
            }
            return differences;
        }

        /// <summary>
        /// A grid's rows matched by their key cells, so a row whose values moved (a count in a design
        /// observation's row) is one changed row, named by its key, not one removed and one added. Rows
        /// past the listed ones are not kept, so a grid listing only some also says how its total moved.
        /// </summary>
        private static List<string> GridDifferences(GridSnapshot before, GridSnapshot now)
        {
            var keyIndexes = KeyIndexes(before, now);
            Dictionary<string, HashSet<string>> ByKey(GridSnapshot grid) => grid.Rows
                .GroupBy(r => KeyText(r, keyIndexes), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(RowText).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);

            var was = ByKey(before);
            var rows = ByKey(now);
            var differences = new List<string>();
            AddKeys(differences, $"{now.Title} added", rows.Keys.Where(k => !was.ContainsKey(k)).ToList());
            AddKeys(differences, $"{now.Title} removed", was.Keys.Where(k => !rows.ContainsKey(k)).ToList());
            AddKeys(differences, $"{now.Title} changed", rows.Keys.Where(k => was.TryGetValue(k, out var old) && !old.SetEquals(rows[k])).ToList());
            if (before.Total != now.Total && (before.Rows.Count < before.Total || now.Rows.Count < now.Total))
            {
                differences.Add($"{now.Title} in all: {before.Total:N0} to {now.Total:N0}");
            }
            return differences;
        }

        /// <summary>
        /// The key columns filled on every row of both grids. A key column left blank on some rows heads a
        /// block of rows, such as a group's shared column count, so it does not identify a row.
        /// </summary>
        private static List<int> KeyIndexes(GridSnapshot before, GridSnapshot now)
        {
            var keys = Enumerable.Range(0, now.KeyColumns.Count).ToList();
            var filled = keys.Where(i => before.Rows.Concat(now.Rows).All(r => i < r.Count && r[i].Length > 0)).ToList();
            return filled.Count > 0 ? filled : keys;
        }

        private static string KeyText(List<string> row, List<int> keyIndexes)
        {
            var cells = keyIndexes.Where(i => i < row.Count).Select(i => row[i]).ToList();
            return cells.Count == 1 ? cells[0] : $"({string.Join(", ", cells)})";
        }

        private static void AddKeys(List<string> differences, string label, List<string> keys)
        {
            if (keys.Count > 0)
            {
                var more = keys.Count > KeysShown ? $" and {keys.Count - KeysShown:N0} more" : "";
                differences.Add($"{label} ({keys.Count:N0}): {string.Join(", ", keys.Take(KeysShown))}{more}");
            }
        }

        private static string RowText(List<string> row) => string.Join("\u001F", row);

        private static string Heading(ResultSnapshot result) =>
            $"[{result.Severity.ToString().ToUpperInvariant()}] {result.Name}{AnalysisReport.SubjectText(result.Table)}";

        private static string StatusText(ResultSnapshot result) => AnalysisReport.StatusText(result.Kind, result.Status);
    }
}
