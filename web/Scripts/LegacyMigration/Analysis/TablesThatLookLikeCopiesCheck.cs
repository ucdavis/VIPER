using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Tables whose columns are all in another table with at most a couple more: backups, snapshots, and
    /// leftover copies, whatever their names say. Metadata only, plus the latest year from the shared
    /// profile read. One result across tables, a block of rows per group.
    /// </summary>
    public sealed class TablesThatLookLikeCopiesCheck : IAutomaticCheck
    {
        /// <summary>
        /// A table matches another when all its columns are in the other and the other has at most this many more.
        /// </summary>
        internal const int MaxExtraColumns = 2;

        /// <summary>
        /// Smaller tables, such as two-column link tables, match each other by chance.
        /// </summary>
        internal const int MinColumns = 3;

        private const string Description =
            "Tables whose columns (name, ignoring case, and type) are all in another table that has at most 2 more: backups, snapshots, " +
            "and leftover copies, whatever their names say. Matching tables are grouped, and each table lists the columns it has beyond " +
            "those the whole group shares. Tables of fewer than 3 columns are left out, since small link tables match by chance; excluded " +
            "tables are compared too. Copies with renamed columns, copies missing more than 2 columns, and copies in other databases are " +
            "not found, and the rows themselves are not compared.";

        public string Name => "Tables that look like copies";

        public bool AcceptsTableOverrides => false;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.DesignObservation,
                Severity = Severity.Info,
                Status = CheckStatus.NoProblemsFound
            };

            // Excluded tables cost nothing here, and a match confirms why they were excluded. Tables outside
            // include are left alone, since include is how a database holding sensitive records is narrowed.
            var tables = context.Plan.Schema.Tables.Where(t => t.IsIncluded).ToDictionary(t => t.Name, StringComparer.Ordinal);
            var groups = Groups(tables.Values.Select((string Name, IReadOnlyList<ColumnSummary> Columns) (t) => (t.Name, t.TableSummary!.Columns)));
            if (groups.Count == 0)
            {
                result.Details.Add($"Checked {Count(tables.Count)} tables; none found.");
                return [result];
            }

            result.Status = CheckStatus.ProblemsFound;
            var rows = new List<List<string>>();
            foreach (var members in groups.Select(g => g.Select(name => tables[name]).ToList()))
            {
                var shared = members.Select(t => Signatures(t.TableSummary!.Columns))
                    .Aggregate((a, b) => a.Intersect(b).ToHashSet());
                rows.AddRange(members.Select((t, i) => new List<string>
                {
                    i == 0 ? $"{Count(shared.Count)} shared columns" : "",
                    t.Name,
                    Count(t.TableSummary!.ApproximateRowCount),
                    LatestYear(context, t),
                    OnlySome(t, shared),
                    t.IsExcluded ? "excluded" : ""
                }));
            }
            result.Grids.Add(new ResultGrid("Groups", ["Group", "Table"], ["Rows", "Latest year", "Columns only some have", "Note"],
                rows, rows.Count, "rows are the export's approximate counts",
                $"{Count(groups.Count)} groups, {Count(rows.Count)} tables"));
            return [result];
        }

        /// <summary>
        /// Tables that match, joined into groups when matches chain (A in B, B in C), each group's names
        /// ordered, groups ordered by their first name.
        /// </summary>
        internal static List<List<string>> Groups(IEnumerable<(string Name, IReadOnlyList<ColumnSummary> Columns)> tables)
        {
            var candidates = tables
                .Where(t => t.Columns.Count >= MinColumns)
                .Select(t => (t.Name, Columns: Signatures(t.Columns)))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var parent = candidates.ToDictionary(t => t.Name, t => t.Name, StringComparer.Ordinal);
            string Root(string name) => parent[name] == name ? name : parent[name] = Root(parent[name]);

            for (var i = 0; i < candidates.Count; i++)
            {
                for (var j = i + 1; j < candidates.Count; j++)
                {
                    var (small, large) = candidates[i].Columns.Count <= candidates[j].Columns.Count
                        ? (candidates[i].Columns, candidates[j].Columns)
                        : (candidates[j].Columns, candidates[i].Columns);
                    if (large.Count - small.Count <= MaxExtraColumns && small.IsSubsetOf(large))
                    {
                        parent[Root(candidates[j].Name)] = Root(candidates[i].Name);
                    }
                }
            }

            return candidates
                .GroupBy(t => Root(t.Name), StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(t => t.Name).ToList())
                .OrderBy(g => g[0], StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Name ignoring case, plus type with its length or precision. Nullability, identity, defaults, and
        /// keys are left out, since SELECT INTO and hand-made backups drop or change them.
        /// </summary>
        private static HashSet<(string Name, string Type)> Signatures(IEnumerable<ColumnSummary> columns) =>
            columns.Select(c => (c.Name.ToLowerInvariant(), c.Type.ToLowerInvariant())).ToHashSet();

        private static string OnlySome(TableRef table, HashSet<(string Name, string Type)> shared)
        {
            var extra = table.Columns.Where(c => !shared.Contains((c.Name.ToLowerInvariant(), c.Summary.Type.ToLowerInvariant())))
                .Select(c => c.Name).ToList();
            return extra.Count > 0 ? string.Join(", ", extra) : "-";
        }

        /// <summary>
        /// The latest year in the table's declared date column, from the shared profile read; excluded
        /// tables are never read.
        /// </summary>
        private static string LatestYear(AnalysisContext context, TableRef table)
        {
            if (table.IsExcluded)
            {
                return "not read";
            }
            if (!context.Plan.DateColumns.TryGetValue(table, out var column))
            {
                return "-";
            }
            var profile = context.Profile(table);
            return profile.Error == null
                ? profile.Columns.Find(c => c.Column == column.Name)?.LatestYear?.ToString(CultureInfo.InvariantCulture) ?? "-"
                : "not read";
        }

        private static string Count(long n) => n.ToString("N0", CultureInfo.InvariantCulture);
    }
}
