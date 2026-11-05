using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The counts say which objects the automatic checks visited: those outside include, then those excluded
    /// among the rest, so a total less both counts is what they visited.
    /// </summary>
    public sealed record AnalysisRun(string Connection, string? Project, string Environment, string Server, string Database,
        string ExportGeneratedAt, string RanAt, string Duration, string? DeclarationsFile, IReadOnlyList<string>? Include,
        ObjectCoverage Tables, ObjectCoverage Views, List<CheckResult> Results, IReadOnlyList<MaskedColumn>? MaskedColumns = null);

    /// <summary>
    /// A column some check described by shape rather than showing its values. A person ID column shows
    /// its values of digits, so only its values that are not IDs were hidden; Shapes describes those.
    /// </summary>
    public sealed record MaskedColumn(string Table, string Column, bool IsPersonId = false,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Shapes = null)
    {
        public static MaskedColumn Of(ColumnRef column) => column.IsPersonId
            ? new MaskedColumn(column.Table.Name, column.Name, true,
                column.MaskedValues.Count > 0 ? ResultSql.ShapeSummary([.. column.MaskedValues.Order(StringComparer.Ordinal)], "distinct value") : null)
            : new MaskedColumn(column.Table.Name, column.Name);
    }

    public sealed record ObjectCoverage(int Total, int OutsideInclude, int Excluded)
    {
        public int Checked => Total - OutsideInclude - Excluded;

        public static ObjectCoverage Of(IEnumerable<TableRef> objects)
        {
            var list = objects.ToList();
            return new ObjectCoverage(list.Count, list.Count(t => !t.IsIncluded), list.Count(t => t.IsIncluded && t.IsExcluded));
        }
    }

    /// <summary>
    /// Writes a run as report.txt (problems first, most severe first) and results.json, for comparing runs.
    /// </summary>
    public static class AnalysisReport
    {
        internal static readonly JsonSerializerOptions JsonOptions = new(SchemaExport.JsonOptions)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string ToJson(AnalysisRun run) => JsonSerializer.Serialize(run, JsonOptions);

        /// <summary>
        /// changes holds the lines of CHANGES SINCE THE LAST RUN (RunComparison.Lines), or the one reason
        /// there is nothing to compare with.
        /// </summary>
        public static string ToText(AnalysisRun run, IReadOnlyList<string> changes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"DATA QUALITY ANALYSIS: {run.Connection}{(run.Project != null ? $", project {run.Project}" : "")} ({run.Environment})");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Database:      {run.Server}/{run.Database}");
            sb.AppendLine($"  Schema export: {run.ExportGeneratedAt}");
            sb.AppendLine($"  Declarations:  {run.DeclarationsFile ?? "(none: automatic checks only)"}");
            sb.AppendLine($"  Ran at:        {run.RanAt} (checks took {run.Duration})");
            if (run.Include != null)
            {
                sb.AppendLine($"  Include:       {string.Join(", ", run.Include)}");
            }
            sb.AppendLine($"  Tables:        {CoverageText(run.Tables, run.Include != null)}");
            sb.AppendLine($"  Views:         {CoverageText(run.Views, run.Include != null)}");

            WriteMasked(sb, run.MaskedColumns ?? []);
            WriteDecisions(sb, run.Results);

            sb.AppendLine();
            sb.AppendLine("CHANGES SINCE THE LAST RUN");
            foreach (var line in changes)
            {
                sb.AppendLine($"  {line}");
            }

            // Problem checks that passed get their own section, so PROBLEMS holds only what needs attention.
            WriteSection(sb, "PROBLEMS", run.Results.Where(r => r.Kind == ResultKind.Problem && r.Status != CheckStatus.NoProblemsFound));
            WriteSection(sb, "NO PROBLEMS FOUND", run.Results.Where(r => r.Kind == ResultKind.Problem && r.Status == CheckStatus.NoProblemsFound));
            WriteSection(sb, "CONTEXT", run.Results.Where(r => r.Kind == ResultKind.Context));
            WriteSection(sb, "DESIGN OBSERVATIONS", Combined(run.Results.Where(r => r.Kind == ResultKind.DesignObservation)));

            sb.AppendLine();
            sb.AppendLine("TOTALS (problems)");
            var problems = run.Results.Where(r => r.Kind == ResultKind.Problem).ToList();
            foreach (var status in Enum.GetValues<CheckStatus>())
            {
                sb.AppendLine($"  {(StatusText(ResultKind.Problem, status) + ":").PadRight(20)}{problems.Count(r => r.Status == status)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// "12" when nothing is left out, else "4 of 312 checked automatically (308 not in the include list, 2 excluded)",
        /// with each count only when there is an include list or something excluded.
        /// </summary>
        private static string CoverageText(ObjectCoverage coverage, bool hasInclude)
        {
            if (!hasInclude && coverage.Excluded == 0)
            {
                return $"{coverage.Total:N0}";
            }
            var left = new List<string>();
            if (hasInclude)
            {
                left.Add($"{coverage.OutsideInclude:N0} not in the include list");
            }
            if (coverage.Excluded > 0)
            {
                left.Add($"{coverage.Excluded:N0} excluded");
            }
            return $"{coverage.Checked:N0} of {coverage.Total:N0} checked automatically ({string.Join(", ", left)})";
        }

        /// <summary>
        /// The columns whose values were described by shape, by table, so the ones that hold codes rather
        /// than anything personal can be declared and shown on the next run. Person ID columns come apart:
        /// their IDs were shown, and only the values that are not IDs were hidden, which no declaration
        /// should show.
        /// </summary>
        private static void WriteMasked(StringBuilder sb, IReadOnlyList<MaskedColumn> masked)
        {
            var declarable = masked.Where(m => !m.IsPersonId).ToList();
            if (declarable.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"VALUES DESCRIBED BY SHAPE ({Columns(declarable.Count)}): declare under safeColumns to show");
                foreach (var table in declarable.GroupBy(m => m.Table).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine($"  {table.Key}: {string.Join(", ", table.Select(m => m.Column).Order(StringComparer.Ordinal))}");
                }
            }

            var personIds = masked.Where(m => m.IsPersonId).OrderBy(m => m.Table, StringComparer.Ordinal).ThenBy(m => m.Column, StringComparer.Ordinal).ToList();
            if (personIds.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"PERSON ID COLUMNS WITH VALUES THAT ARE NOT IDS ({Columns(personIds.Count)})");
                sb.AppendLine("  Values of all digits (MothraIds, IAM IDs, PIDMs) are shown as written. Any other value, such as a");
                sb.AppendLine("  LoginId typed into a MothraId column, is described by shape wherever it appears. Each column below");
                sb.AppendLine("  gives the shapes of those other values, each with how many different values have it.");
                foreach (var column in personIds)
                {
                    sb.AppendLine($"  {column.Table}.{column.Column}{(column.Shapes != null ? $": {column.Shapes}" : "")}");
                }
            }

            static string Columns(int count) => count == 1 ? "1 column" : $"{count} columns";
        }

        /// <summary>
        /// One line per decision and check, however many tables it covers, so the section stays short and
        /// points to where the details are.
        /// </summary>
        private static void WriteDecisions(StringBuilder sb, List<CheckResult> results)
        {
            var decisions = results
                .Where(r => r.Decision != null && r.Status == CheckStatus.ProblemsFound)
                .GroupBy(r => (r.Decision!.Question, r.Name))
                .OrderBy(g => g.Key.Question, StringComparer.Ordinal)
                .ToList();

            sb.AppendLine();
            sb.AppendLine($"DECISIONS FOR THE MIGRATION ({decisions.Count})");
            if (decisions.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            foreach (var decision in decisions)
            {
                sb.AppendLine($"  {decision.Key.Question}: columns {decision.Sum(r => r.Decision!.Columns):N0}, " +
                              $"tables {decision.Sum(r => r.Decision!.Tables):N0} (see {decision.Key.Name})");
            }
        }

        /// <summary>
        /// Design observations list columns one table at a time, so a check that finds something in many
        /// tables would repeat its description in a block for each. Results holding nothing but one complete
        /// grid are combined into one result per check, with the table as the grid's first column. Anything
        /// else (a skip, a failure, an override, other lines) keeps its own block. results.json is unchanged.
        /// </summary>
        private static IEnumerable<CheckResult> Combined(IEnumerable<CheckResult> results)
        {
            foreach (var check in results.GroupBy(r => r.Name, StringComparer.Ordinal))
            {
                var combinable = check.Where(IsCombinable).OrderBy(r => r.Table, StringComparer.Ordinal).ToList();
                var layouts = combinable.Select(r => GridLayout(r.Grids[0])).Distinct(StringComparer.Ordinal).Count();
                if (combinable.Count == 0 || layouts > 1)
                {
                    foreach (var result in check)
                    {
                        yield return result;
                    }
                    continue;
                }

                foreach (var result in check.Except(combinable))
                {
                    yield return result;
                }
                var first = combinable[0];
                var grid = first.Grids[0];
                var rows = combinable.SelectMany(r => r.Grids[0].Rows.Select(row => row.Prepend(r.Table!).ToList())).ToList();
                var combined = new CheckResult
                {
                    Name = first.Name,
                    Description = first.Description,
                    Kind = first.Kind,
                    Severity = first.Severity,
                    Status = CheckStatus.ProblemsFound
                };
                combined.Counts.Add(new ResultCount("Tables", combinable.Count));
                combined.Grids.Add(new ResultGrid(grid.Title, ["Table", .. grid.KeyColumns], grid.ValueColumns, rows, rows.Count));
                yield return combined;
            }
        }

        // A table or view name is bracket-quoted ([schema].[name]); checks that group across tables, such as
        // related columns with different types, put a label there instead and are already one block per group.
        private static bool IsCombinable(CheckResult r) =>
            r.Status == CheckStatus.ProblemsFound && r.Table?.StartsWith('[') == true && r.Message == null
            && r.Grids.Count == 1 && r.Grids[0].Rows.Count == r.Grids[0].Total && r.Grids[0].Note == null
            && r.Counts.Count == 0 && r.Examples.Count == 0 && r.Details.Count == 0 && r.Earliest == null && r.Latest == null;

        private static string GridLayout(ResultGrid grid) =>
            string.Join("|", grid.KeyColumns.Concat(grid.ValueColumns).Prepend(grid.Title));

        private static void WriteSection(StringBuilder sb, string title, IEnumerable<CheckResult> results)
        {
            var ordered = results
                .OrderBy(r => r.Severity)
                .ThenBy(r => r.Status == CheckStatus.NoProblemsFound)
                .ThenBy(r => r.Name, StringComparer.Ordinal)
                .ThenBy(r => r.Table, StringComparer.Ordinal)
                .ToList();

            sb.AppendLine();
            sb.AppendLine($"{title} ({ordered.Count})");
            if (ordered.Count == 0)
            {
                sb.AppendLine("  (none)");
            }

            foreach (var result in ordered)
            {
                WriteResult(sb, result);
            }
        }

        private static void WriteResult(StringBuilder sb, CheckResult result)
        {
            var table = SubjectText(result.Table);

            // Context is not a finding, so it only shows a status when it was skipped or failed. A problem
            // check that passed is under its own heading, so it needs neither a status nor a severity.
            var passed = result.Kind == ResultKind.Problem && result.Status == CheckStatus.NoProblemsFound;
            var showStatus = !passed && (result.Kind != ResultKind.Context || result.Status is CheckStatus.Skipped or CheckStatus.FailedToRun);
            var status = showStatus ? $": {StatusText(result.Kind, result.Status)}" : "";
            var severity = passed ? "" : $"[{result.Severity.ToString().ToUpperInvariant()}] ";
            sb.AppendLine();
            sb.AppendLine($"  {severity}{result.Name}{table}{status}");
            if (result.Description != null)
            {
                sb.AppendLine($"      {result.Description}");
            }
            if (result.Message != null)
            {
                sb.AppendLine($"      {result.Message}");
            }
            WriteCounts(sb, ShownCounts(result.Counts).ToList());
            if (result.Examples.Count > 0)
            {
                var shown = result.Examples.Count < result.ExampleTotal ? $"{result.Examples.Count:N0} of {result.ExampleTotal:N0}" : $"{result.ExampleTotal:N0}";
                var by = result.ExampleKeyColumns.Count switch
                {
                    0 => "",
                    1 => $" by {result.ExampleKeyColumns[0]}",
                    _ => $" by ({string.Join(", ", result.ExampleKeyColumns)})"
                };
                sb.AppendLine($"      Examples{by} ({shown}): {string.Join(", ", result.Examples)}");
            }
            if (result.Earliest != null || result.Latest != null)
            {
                sb.AppendLine($"      Earliest: {result.Earliest ?? "-"}, latest: {result.Latest ?? "-"}");
            }
            foreach (var grid in result.Grids)
            {
                var shown = grid.CountText ?? (grid.Rows.Count < grid.Total ? $"{grid.Rows.Count:N0} of {grid.Total:N0}" : $"{grid.Total:N0}");
                sb.AppendLine($"      {grid.Title} ({shown}){(grid.Note != null ? $"; {grid.Note}" : "")}:");
                foreach (var line in GridLines(grid))
                {
                    sb.AppendLine($"        {line}");
                }
            }
            foreach (var detail in result.Details)
            {
                sb.AppendLine($"      {detail}");
            }
        }

        /// <summary>
        /// Zero counts are left out when anything else was counted, since they bury the counts to act on.
        /// When everything is zero, a passing check still says what it counted: every plain count, so
        /// "Rows below 0" is not shown without "Rows above 100", or the first of the "category: measure"
        /// counts when there are no plain ones. results.json keeps every count, for comparing runs.
        /// </summary>
        private static IEnumerable<ResultCount> ShownCounts(List<ResultCount> counts)
        {
            if (counts.Exists(c => c.Value != 0))
            {
                return counts.Where(c => c.Value != 0);
            }
            var plain = counts.Where(c => !c.Label.Contains(": ", StringComparison.Ordinal)).ToList();
            return plain.Count > 0 ? plain : counts.Take(1);
        }

        /// <summary>
        /// Counts labeled "category: measure", such as "Not found: IDs" and "Not found: rows", go in one
        /// table with a row per category, when any category has more than one measure; other counts stay
        /// one per line, above it.
        /// </summary>
        private static void WriteCounts(StringBuilder sb, List<ResultCount> counts)
        {
            var split = counts.Select(c =>
            {
                var at = c.Label.LastIndexOf(": ", StringComparison.Ordinal);
                return (Count: c, Category: at > 0 ? c.Label[..at] : null, Measure: at > 0 ? c.Label[(at + 2)..] : null);
            }).ToList();
            var paired = split.Where(s => s.Category != null).ToList();
            if (!paired.GroupBy(s => s.Category).Any(g => g.Count() > 1))
            {
                foreach (var count in counts)
                {
                    sb.AppendLine($"      {count.Label}: {count.Value:N0}");
                }
                return;
            }

            foreach (var single in split.Where(s => s.Category == null))
            {
                sb.AppendLine($"      {single.Count.Label}: {single.Count.Value:N0}");
            }
            // Column order comes from the first category with the most measures, so a category that has
            // only some of them (NULL rows have no IDs) does not reorder the table by coming first.
            var categories = paired.GroupBy(s => s.Category!, StringComparer.Ordinal).ToList();
            var fullest = categories.MaxBy(g => g.Select(s => s.Measure).Distinct(StringComparer.Ordinal).Count())!;
            var measures = fullest.Select(s => s.Measure!).Concat(paired.Select(s => s.Measure!)).Distinct(StringComparer.Ordinal).ToList();
            var rows = categories
                .Select(g => measures.Select(m => g.FirstOrDefault(s => s.Measure == m).Count?.Value.ToString("N0") ?? "-")
                    .Prepend(g.Key).ToList())
                .ToList();
            var grid = new ResultGrid("Counts", ["Category"], measures.Select(m => char.ToUpperInvariant(m[0]) + m[1..]).ToList(), rows, rows.Count);
            foreach (var line in GridLines(grid))
            {
                sb.AppendLine($"      {line}");
            }
        }

        /// <summary>
        /// Keys alone are grouped by all but their last column when that at least halves the lines, such as
        /// "sectionID 3: unitID 5, 7, 10", or written on one line for a single-column key. Anything else is
        /// an aligned table with a header row.
        /// </summary>
        private static List<string> GridLines(ResultGrid grid)
        {
            var keyCount = grid.KeyColumns.Count;
            if (grid.ValueColumns.Count == 0 && keyCount == 1)
            {
                return [$"{grid.KeyColumns[0]}: {string.Join(", ", grid.Rows.Select(r => r[0]))}"];
            }
            if (grid.ValueColumns.Count == 0 && keyCount > 1)
            {
                var groups = grid.Rows
                    .GroupBy(r => string.Join(", ", grid.KeyColumns.Take(keyCount - 1).Select((c, i) => $"{c} {r[i]}")))
                    .ToList();
                if (groups.Count * 2 <= grid.Rows.Count)
                {
                    return groups.Select(g => $"{g.Key}: {grid.KeyColumns[^1]} {string.Join(", ", g.Select(r => r[keyCount - 1]))}").ToList();
                }
            }

            var header = grid.KeyColumns.Concat(grid.ValueColumns).ToList();
            var widths = header.Select((h, i) => grid.Rows.Select(r => r[i].Length).Append(h.Length).Max()).ToList();
            string Line(IEnumerable<string> cells) => string.Join("  ", cells.Select((c, i) => c.PadRight(widths[i]))).TrimEnd();
            return grid.Rows.Select(Line).Prepend(Line(header)).ToList();
        }

        /// <summary>
        /// The subject after a check's name in a heading. A table name's brackets set it off from the check
        /// name; a label such as "columns named gradyear" needs parentheses.
        /// </summary>
        internal static string SubjectText(string? table) =>
            table == null ? "" : table.StartsWith('[') ? $" {table}" : $" ({table})";

        /// <summary>
        /// Context and design observations are not problems, so they read as found or not found.
        /// </summary>
        internal static string StatusText(ResultKind kind, CheckStatus status) => status switch
        {
            CheckStatus.NoProblemsFound => kind == ResultKind.Problem ? "no problems found" : "none found",
            CheckStatus.ProblemsFound => kind == ResultKind.Problem ? "problems found" : "found",
            CheckStatus.Skipped => "skipped",
            CheckStatus.FailedToRun => "failed to run",
            _ => status.ToString()
        };
    }
}
