using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// A problem check counted in the shared profile read (TableProfile): one result per table, listing
    /// each column with something found. Only tables with findings are read again, once per kind of
    /// problem in each flagged column, to list the rows by key with their dates. No values.
    /// </summary>
    public sealed class ProfileProblemCheck(string name, string description, List<string> countLabels,
        Func<ColumnExtras, long?[]> counts, Func<ColumnRef, List<(string Label, string? Where)>> rowConditions,
        Func<SqlParameter[]>? parameters = null, bool showsType = false, string? decision = null,
        Func<AnalysisContext, TableRef, ColumnRef, List<string>>? columnNotes = null) : IAutomaticCheck
    {
        // The type is shown, not acted on: it cannot tell a matched column from free text (an email and a
        // note can both be varchar(250)), so every column raises the same choice for the migration.
        public static readonly ProfileProblemCheck LeadingOrTrailingSpaces = new("Leading or trailing spaces",
            "Values that start or end with spaces. SQL Server ignores trailing spaces when comparing with =, but C# does not, " +
            "so these break lookups in migration code wherever the values are matched (IDs, codes, names, emails). In free text " +
            "they break nothing, but the migration still has to choose whether to keep or trim them; leading spaces can be " +
            "deliberate, such as indentation. Trailing spaces in char and nchar columns are padding and not counted.",
            ["Leading", "Trailing"], e => [e.LeadingSpaces, e.TrailingSpaces], SpacesRows,
            showsType: true, decision: "Keep or trim leading and trailing spaces");

        public static readonly ProfileProblemCheck ImplausibleDates = new("Implausible dates",
            $"Dates before {TableProfile.EarliestPlausibleYear} or more than {TableProfile.FutureYears} years from now, " +
            "which often means placeholder dates such as 1900-01-01.",
            [$"Before {TableProfile.EarliestPlausibleYear}", $"Over {TableProfile.FutureYears} years ahead"], e => [e.DatesTooEarly, e.DatesTooLate],
            c => [($"Before {TableProfile.EarliestPlausibleYear}", TableProfile.DatesTooEarlyWhere(c)),
                  ($"Over {TableProfile.FutureYears} years ahead", TableProfile.DatesTooLateWhere(c))]);

        // The profile counts all three kinds together; the rows are listed by kind, since each needs a different fix.
        public static readonly ProfileProblemCheck UnusualCharacters = new("Unusual characters",
            "Values holding control characters (other than tab and line breaks), signs of encoding damage (UTF-8 text read as " +
            "Windows-1252, such as Ã© in place of é), or the replacement character �. A value can be listed under more than one kind. " +
            "Control characters are listed by code.",
            ["Values"], e => [e.UnusualCharacters],
            c => TableProfile.UnusualCharacterKinds(c).Select((string Label, string? Where) (k) => (k.Label, k.Where)).ToList(),
            TableProfile.UnusualCharacterPatterns, columnNotes: (context, table, column) =>
                TableProfile.UnusualCharacterKinds(column).Count > 0 && context.Profile(table).Extras[column.Name].UnusualCharacters > 0
                    ? TableProfile.ControlCharacterCodes(context, table, column)
                    : []);

        private static List<(string Label, string? Where)> SpacesRows(ColumnRef c) =>
            [("Leading spaces", TableProfile.LeadingSpacesWhere(c)), ("Trailing spaces", TableProfile.TrailingSpacesWhere(c))];

        public string Name => name;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var results = tables.Select(t => RunTable(context, t)).ToList();
            return AutomaticResult.OnlyFindings(results, Name, description, Severity.Warning,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found");
        }

        private CheckResult RunTable(AnalysisContext context, TableRef table)
        {
            var result = AutomaticResult.For(context, Name, description, ResultKind.Problem, Severity.Warning, table.Name);
            if (result.Status == CheckStatus.Skipped)
            {
                return result;
            }

            var profile = context.Profile(table);
            if (profile.Error != null)
            {
                result.Status = CheckStatus.FailedToRun;
                result.Message = $"Failed to run: {profile.Error}";
                return result;
            }

            var found = table.Columns
                .Select(c => (Column: c, Counts: counts(profile.Extras[c.Name])))
                .Where(c => c.Counts.Any(n => n > 0))
                .ToList();
            if (found.Count == 0)
            {
                return result;
            }

            // A dash marks a count the column's type does not have, such as trailing spaces in char.
            result.Status = CheckStatus.ProblemsFound;
            result.Grids.Add(new ResultGrid("Columns", ["Column"], showsType ? countLabels.Prepend("Type").ToList() : countLabels,
                found.Select(f => f.Counts.Select(n => n?.ToString("N0", CultureInfo.InvariantCulture) ?? "-")
                    .Prepend(showsType ? f.Column.Summary.Type : null).OfType<string>().Prepend(f.Column.Name).ToList()).ToList(),
                found.Count));
            if (decision != null)
            {
                result.Decision = new ResultDecision(decision, found.Count, 1);
            }

            foreach (var (column, _) in found)
            {
                foreach (var (label, where) in rowConditions(column).Where(c => c.Where != null))
                {
                    AddRows(context, result, table, $"{column.Name}, {label}", where!);
                }
                if (columnNotes != null)
                {
                    result.Details.AddRange(columnNotes(context, table, column));
                }
            }
            return result;
        }

        /// <summary>
        /// One line per column and kind of problem, with the rows by key so each can be looked up directly.
        /// Kinds with no rows are left out. A failed listing keeps the profile's counts.
        /// </summary>
        private void AddRows(AnalysisContext context, CheckResult result, TableRef table, string heading, string where)
        {
            ResultSql.AffectedRows rows;
            try
            {
                rows = ResultSql.FindAffectedRows(context, table, where, parameters?.Invoke() ?? []);
            }
            catch (SqlException ex)
            {
                result.Details.Add($"{heading}: rows could not be listed: {context.ErrorText(ex)}");
                return;
            }
            if (rows.Total == 0)
            {
                return;
            }

            var dated = ResultSql.RangeText(rows.Earliest, rows.Latest) is { } range ? $"; dated {range}" : "";
            if (table.KeyColumns.Count == 0)
            {
                result.Details.Add($"{heading}: {ResultSql.RowCount(rows.Total)}{dated} ({table.Name} has no key to list them by)");
                return;
            }
            var shown = rows.Keys.Count < rows.Total ? $"{rows.Keys.Count:N0} of {rows.Total:N0}" : $"{rows.Total:N0}";
            result.Details.Add($"{heading}: rows by {ResultSql.KeyNames(table.KeyColumns)} ({shown}): {string.Join(", ", rows.Keys)}{dated}");
        }
    }
}
