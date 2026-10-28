using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Zero and negative values in whole-number columns that identify or reference rows, from the shared
    /// profile read: a reference holding 0 for "none", or a placeholder row keyed 0 or -1. One result per
    /// table, combined into one table in the report.
    /// </summary>
    public sealed class ZeroOrNegativeIdsCheck : IAutomaticCheck
    {
        private const string Description =
            "Whole-number columns that identify or reference rows, holding 0 or negative values, which usually stand for " +
            "\"none\" or \"unknown\" rather than a real row: a reference to row 0, or a placeholder row in the table it points at. " +
            "Columns are chosen by name (ending in ID), foreign key, declared valuesMustExistIn lookup, or primary key. " +
            "Person columns are left out (see Person columns), as are identity columns seeded at 0 or below, whose 0 is an ordinary row.";

        // CourseID, course_id, and Course_Id end in ID as a word; Paid and Valid do not.
        private static readonly Regex IdName = new(@"(ID|_[Ii][Dd]|[a-z]Id)$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public string Name => "Zero or negative IDs";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var summary = context.Plan.Summary;
            var foreignKeys = summary.ForeignKeys.SelectMany(fk => fk.Columns.Select(c => (fk.Table, Column: c))).ToHashSet();
            var lookups = context.Plan.DeclaredChecks.OfType<ValuesMustExistInCheck>().SelectMany(l => l.Columns).ToHashSet();
            var personColumns = summary.PersonIdColumns.Select(p => (Table: p.Object, p.Column)).ToHashSet();
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();

            var results = new List<CheckResult>();
            foreach (var table in tables)
            {
                var result = AutomaticResult.For(context, Name, Description, ResultKind.DesignObservation, Severity.Info, table.Name);
                if (result.Status != CheckStatus.Skipped)
                {
                    var candidates = table.Columns
                        .Where(c => TableProfile.IsWholeNumber(c) && !personColumns.Contains((table.Name, c.Name)) && !IsSeededAtZeroOrBelow(c))
                        .Select(c => (Column: c, Why: Reasons(table, c, foreignKeys, lookups)))
                        .Where(c => c.Why.Count > 0)
                        .ToList();
                    if (candidates.Count > 0)
                    {
                        RunTable(context, table, result, candidates);
                    }
                }
                results.Add(result);
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, Severity.Info,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found", ResultKind.DesignObservation);
        }

        private void RunTable(AnalysisContext context, TableRef table, CheckResult result, List<(ColumnRef Column, List<string> Why)> candidates)
        {
            var profile = context.Profile(table);
            if (profile.Error != null)
            {
                result.Status = CheckStatus.FailedToRun;
                result.Message = $"Failed to run: {profile.Error}";
                return;
            }

            var found = candidates
                .Select(c => (c.Column, c.Why, Extras: profile.Extras[c.Column.Name]))
                .Where(c => (c.Extras.Zeros ?? 0) + (c.Extras.Negatives ?? 0) > 0)
                .ToList();
            if (found.Count == 0)
            {
                return;
            }

            result.Status = CheckStatus.ProblemsFound;
            result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type", "Why", "Zero rows", "Negative rows"],
                found.Select(f => new List<string>
                {
                    f.Column.Name, f.Column.Summary.Type, string.Join(", ", f.Why), Count(f.Extras.Zeros ?? 0), Count(f.Extras.Negatives ?? 0)
                }).ToList(), found.Count));
            result.Decision = new ResultDecision("Decide what 0 or a negative value means in an ID or reference column", found.Count, 1);
        }

        private static List<string> Reasons(TableRef table, ColumnRef column, HashSet<(string Table, string Column)> foreignKeys,
            HashSet<ColumnRef> lookups)
        {
            var reasons = new List<string>();
            if (table.TableSummary?.PrimaryKeyColumns.Contains(column.Name) == true || column.Summary.Identity != null)
            {
                reasons.Add("primary key");
            }
            if (foreignKeys.Contains((table.Name, column.Name)))
            {
                reasons.Add("foreign key");
            }
            if (lookups.Contains(column))
            {
                reasons.Add("declared lookup");
            }
            if (IsIdName(column.Name))
            {
                reasons.Add("named like an ID");
            }
            return reasons;
        }

        internal static bool IsIdName(string name) => IdName.IsMatch(name);

        private static bool IsSeededAtZeroOrBelow(ColumnRef column) =>
            column.Summary.Identity is { } identity
            && decimal.TryParse(identity.Seed, NumberStyles.Number, CultureInfo.InvariantCulture, out var seed)
            && seed <= 0;

        private static string Count(long n) => n.ToString("N0", CultureInfo.InvariantCulture);
    }
}
