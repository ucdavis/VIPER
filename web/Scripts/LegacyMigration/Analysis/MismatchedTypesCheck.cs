using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Columns that hold the same thing in different tables but with different types, such as a MothraId
    /// that is varchar(8) in one table and nchar(10) in another. Joins between them need implicit
    /// conversions, and padded or longer values can miss rows. Limited to the columns that get joined:
    /// person ID columns, grouped by ID type whatever their names, and columns named like a key (primary,
    /// unique, identity, or either side of a foreign key) in some table, grouped by name ignoring case and
    /// underscores. Generic names such as ID are left out, since they name unrelated keys. Then the column
    /// pairs that declarations compare or join, which catches related columns with different names.
    /// Metadata only.
    /// </summary>
    public sealed class MismatchedTypesCheck : IAutomaticCheck
    {
        private const string Description = "Columns that hold the same thing in different tables, but with different types.";

        private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal) { "id", "key", "pk", "row", "rec" };
        public string Name => "Related columns with different types";

        public bool AcceptsTableOverrides => false;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var summary = context.Plan.Summary;
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).ToList();
            var tableNames = tables.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

            var personIds = summary.PersonIdColumns
                .Where(p => p.Category == PersonIdCategory.IdType && p.IdType != null && tableNames.Contains(p.Object))
                .ToDictionary(p => (p.Object, p.Column), p => p.IdType!);
            var keyNames = KeyColumns(summary, tables).Select(Normalized).Where(n => n.Length > 0).ToHashSet(StringComparer.Ordinal);

            var members = tables
                .SelectMany(t => t.Columns.Select(c => (Table: t.Name, Column: c)))
                .Select(c => (c.Table, c.Column, Group: personIds.TryGetValue((c.Table, c.Column.Name), out var idType)
                    ? $"{idType} columns"
                    : keyNames.Contains(Normalized(c.Column.Name)) ? $"columns named {Normalized(c.Column.Name)}" : null))
                .Where(c => c.Group != null)
                .ToList();
            var groups = members
                .GroupBy(c => c.Group!, StringComparer.Ordinal)
                .Where(g => g.Select(c => TypeOf(c.Column)).Distinct().Count() > 1)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();
            var groupOf = members.ToDictionary(c => c.Column, c => c.Group!);
            var pairs = DeclaredPairs(context, groupOf);

            foreach (var group in groups)
            {
                var result = new CheckResult
                {
                    Name = Name,
                    Description = Description,
                    Kind = ResultKind.DesignObservation,
                    Severity = Severity.Info,
                    Table = group.Key,
                    Status = CheckStatus.ProblemsFound
                };
                var byType = group
                    .GroupBy(c => c.Column.Summary.Type.ToLowerInvariant())
                    .OrderByDescending(t => t.Count())
                    .Select(t => new List<string>
                    {
                        t.Key, t.Count().ToString("N0", CultureInfo.InvariantCulture),
                        string.Join(", ", t.Select(c => $"{c.Table}.{c.Column.Name}").Order(StringComparer.Ordinal))
                    })
                    .ToList();
                result.Grids.Add(new ResultGrid("Types", ["Type"], ["Columns", "Where"], byType, byType.Count));

                AddPadding(result, group.Select(c => c.Column).ToList());
                yield return result;
            }

            if (pairs.Count > 0)
            {
                yield return PairsResult(pairs, groups.SelectMany(g => g.Select(c => c.Column)).ToHashSet());
            }

            if (groups.Count == 0 && pairs.Count == 0)
            {
                var none = new CheckResult
                {
                    Name = Name,
                    Description = Description,
                    Kind = ResultKind.DesignObservation,
                    Severity = Severity.Info,
                    Status = CheckStatus.NoProblemsFound
                };
                none.Details.Add($"Checked the key and person ID columns of {tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables, " +
                    "and the column pairs that declarations compare or join; none found.");
                yield return none;
            }
        }

        private static string TypeOf(ColumnRef column) => column.Summary.Type.ToLowerInvariant();

        /// <summary>
        /// Column pairs that declarations compare or join (valuesMustExistIn's columns and otherColumns,
        /// matchesOtherTable's joinOn and its compared columns), which catches related columns with
        /// different names, such as potentialScheduleID and Schedule_ID. Only pairs whose types differ,
        /// leaving out pairs whose columns already share a name or person ID group, which reports them,
        /// and pairs on an excluded table. Each pair once, with the checks that name it.
        /// </summary>
        private static List<(ColumnRef Column, ColumnRef Other, List<string> Checks)> DeclaredPairs(
            AnalysisContext context, Dictionary<ColumnRef, string> groupOf)
        {
            var named = context.Plan.DeclaredChecks.SelectMany(check => check switch
            {
                ValuesMustExistInCheck v => v.Columns.Zip(v.OtherColumns, (a, b) => (A: a, B: b, Check: "valuesMustExistIn")),
                MatchesOtherTableCheck m => m.JoinColumns.Zip(m.OtherJoinColumns, (a, b) => (A: a, B: b, Check: "matchesOtherTable joinOn"))
                    .Append((A: m.Column, B: m.OtherColumn, Check: "matchesOtherTable")),
                _ => []
            });
            return named
                .Where(p => !p.A.Table.IsExcluded && !p.B.Table.IsExcluded && TypeOf(p.A) != TypeOf(p.B))
                .Where(p => !(groupOf.TryGetValue(p.A, out var a) && groupOf.TryGetValue(p.B, out var b) && a == b))
                .GroupBy(p => (p.A, p.B))
                .Select(g => (Column: g.Key.A, Other: g.Key.B, Checks: g.Select(p => p.Check).Distinct(StringComparer.Ordinal).ToList()))
                .OrderBy(p => p.Column.Table.Name, StringComparer.Ordinal).ThenBy(p => p.Column.Name, StringComparer.Ordinal)
                .ToList();
        }

        private CheckResult PairsResult(List<(ColumnRef Column, ColumnRef Other, List<string> Checks)> pairs, HashSet<ColumnRef> inGroups)
        {
            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.DesignObservation,
                Severity = Severity.Info,
                Table = "declared column pairs",
                Status = CheckStatus.ProblemsFound
            };
            result.Details.Add("Column pairs that declarations compare or join, whose types differ.");
            var rows = pairs.Select(p => new List<string>
            {
                $"{p.Column.Table.Name}.{p.Column.Name}", TypeOf(p.Column),
                $"{p.Other.Table.Name}.{p.Other.Name}", TypeOf(p.Other), string.Join(", ", p.Checks)
            }).ToList();
            result.Grids.Add(new ResultGrid("Pairs", ["Column"], ["Type", "Other column", "Other type", "Declared by"], rows, rows.Count));
            // A padded column already in a group has its note and decision there.
            AddPadding(result, pairs.SelectMany(p => new[] { p.Column, p.Other }).Distinct().Where(c => !inGroups.Contains(c)).ToList());
            return result;
        }

        /// <summary>
        /// Padding is invisible in SQL, so name what it breaks for whoever writes the migration.
        /// </summary>
        private static void AddPadding(CheckResult result, List<ColumnRef> columns)
        {
            var padded = columns.Where(c => c.IsPadded).ToList();
            if (padded.Count == 0)
            {
                return;
            }
            var types = padded.Select(TypeOf).Distinct().Order(StringComparer.Ordinal);
            result.Details.Add($"{string.Join(", ", types)} pads values shorter than the column with spaces. SQL comparisons " +
                "ignore the padding, but C# comparisons after loading do not, and copying the values into a varchar or " +
                "nvarchar column keeps the spaces: trim these values when reading them.");
            result.Decision = new ResultDecision("Trim char and nchar padding when reading", padded.Count,
                padded.Select(c => c.Table.Name).Distinct(StringComparer.Ordinal).Count());
        }

        /// <summary>
        /// The names of every column that is a key somewhere: primary, unique, identity, or either side of
        /// a foreign key.
        /// </summary>
        private static IEnumerable<string> KeyColumns(SchemaSummary summary, List<TableRef> tables) =>
            tables.SelectMany(t => (t.TableSummary?.PrimaryKeyColumns ?? [])
                    .Concat(t.TableSummary?.UniqueKeys.SelectMany(k => k) ?? [])
                    .Concat(t.Columns.Where(c => c.Summary.Identity != null).Select(c => c.Name)))
                .Concat(summary.ForeignKeys.SelectMany(fk => fk.Columns.Concat(fk.ReferencedColumns)));

        /// <summary>
        /// Lowercase without underscores, so Rot_ID, rot_ID, and RotID match; empty for a generic name.
        /// </summary>
        private static string Normalized(string name) =>
            PersonIdRules.Words(name).TrueForAll(GenericWords.Contains) ? "" : name.Replace("_", "").ToLowerInvariant();
    }
}
