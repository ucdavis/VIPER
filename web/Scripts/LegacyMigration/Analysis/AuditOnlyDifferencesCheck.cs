using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Groups of rows identical in every column but their surrogate key and their audit columns, whose
    /// audit columns differ: usually one record saved again. Exact copies, identical in the audit columns
    /// too, stay with DuplicateRowsCheck. In a table named like a log, repeated rows are usually separate
    /// events, so its result is only info, with a note saying why.
    /// </summary>
    public sealed class AuditOnlyDifferencesCheck : IAutomaticCheck
    {
        private const int GroupLimit = 10;

        // A difference in who approved, reviewed, or signed a record is a real one, so those are compared.
        private static readonly string[] ComparedVerbs = ["approv", "review", "sign"];

        // Whole name words that mark a log table, such as RequestAudit, ChangeLog, or StatusHistory. Whole words
        // keep names like Histology, Login, and Logistics out.
        private static readonly string[] LogWords = ["audit", "audits", "log", "logs", "hist", "history", "histories"];

        private const string Description =
            "Rows that match except for their key and their audit columns (when or by whom the row was created or changed), " +
            "where the audit columns differ: usually one record saved more than once. Exact copies are under Duplicate rows. " +
            "Tables named like a log (audit, log, history) are info rather than warning, since their rows are usually separate events.";

        public string Name => "Rows differing only in audit columns";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var reviewColumns = context.Plan.Summary.PersonIdColumns
                .Where(p => p.Category == PersonIdCategory.Review)
                .Select(p => (Table: p.Object, p.Column))
                .ToHashSet();
            var tables = context.Plan.Schema.Tables
                .Where(t => t.GetsAutomaticChecks)
                .Select(t => (Table: t, Key: DuplicateRowsCheck.SurrogateKey(t)))
                .Where(t => t.Key != null)
                .OrderBy(t => t.Table.Name, StringComparer.Ordinal)
                .ToList();

            var results = new List<CheckResult>();
            foreach (var (table, key) in tables)
            {
                var isLog = IsLogTable(table);
                var result = AutomaticResult.For(context, Name, Description, ResultKind.Problem, isLog ? Severity.Info : Severity.Warning, table.Name);
                if (result.Status != CheckStatus.Skipped)
                {
                    var compared = DuplicateRowsCheck.Compared(table, key);
                    var dated = context.Plan.DateColumns.GetValueOrDefault(table);
                    var audit = compared.Where(c => (c == dated && RecordsWhenSaved(c)) || IsAuditColumn(table, c, reviewColumns)).ToList();
                    var core = compared.Except(audit).ToList();
                    if (audit.Count > 0 && core.Count > 0)
                    {
                        RunTable(context, table, key!, core, audit, result);
                    }
                    if (isLog && result.Status == CheckStatus.ProblemsFound)
                    {
                        result.Details.Insert(0, "Info, not warning: named like a log, so these are likely separate events.");
                    }
                }
                results.Add(result);
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, Severity.Warning,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found");
        }

        private static bool IsLogTable(TableRef table) =>
            IsLogName(table.Name[(table.Name.IndexOf("].[", StringComparison.Ordinal) + 3)..^1]);

        internal static bool IsLogName(string name) => PersonIdRules.Words(name).Exists(w => Array.IndexOf(LogWords, w) >= 0);

        private static bool IsAuditColumn(TableRef table, ColumnRef column, HashSet<(string Table, string Column)> reviewColumns)
        {
            var words = PersonIdRules.Words(column.Name);
            if (words.Exists(w => Array.Exists(ComparedVerbs, v => w.StartsWith(v, StringComparison.Ordinal))))
            {
                return false;
            }
            return reviewColumns.Contains((table.Name, column.Name))
                || (column.IsDate && words.Exists(Suggestions.IsCreatedOrModifiedWord));
        }

        // Name words that say a date column records when its row was saved, beyond created and modified.
        private static readonly string[] RowTimeWords = ["timestamp", "stamp", "logged", "generated", "snapshot"];

        /// <summary>
        /// The declared date column is set aside only when its name says it records when the row was saved.
        /// A date column may instead record when something happened (an appointment's start), and two rows
        /// differing in it are then two events, not one record saved again.
        /// </summary>
        private static bool RecordsWhenSaved(ColumnRef column) =>
            PersonIdRules.Words(column.Name).Exists(w => Suggestions.IsCreatedOrModifiedWord(w) || Array.IndexOf(RowTimeWords, w) >= 0);

        private static void RunTable(AnalysisContext context, TableRef table, ColumnRef key, List<ColumnRef> core, List<ColumnRef> audit,
            CheckResult result)
        {
            var group = string.Join(", ", core.Select(DuplicateRowsCheck.Comparable));

            // An audit column differs within a group when its values differ, or when some rows are NULL and others not.
            var differs = string.Join(" OR ", audit.Select(c =>
            {
                var e = DuplicateRowsCheck.Comparable(c);
                return $"MIN({e}) <> MAX({e}) OR COUNT_BIG({e}) BETWEEN 1 AND COUNT_BIG(*) - 1";
            }));
            var having = $"COUNT_BIG(*) > 1 AND ({differs})";
            try
            {
                var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}(t.{date})");
                var totals = context.Database.QueryRows(
                    $"SELECT COUNT_BIG(*), SUM(x.n), MIN(x.Earliest), MAX(x.Latest), SUM(x.loose)\n" +
                    $"FROM (SELECT COUNT_BIG(*) AS n, {DuplicateRowsCheck.AnyDiffersOnlyLoosely(core)} AS loose, {dates} FROM {table.Name} AS t\n" +
                    $"      GROUP BY {group}\n      HAVING {having}) AS x").Single();
                var groups = Convert.ToInt64(totals[0], CultureInfo.InvariantCulture);
                if (groups == 0)
                {
                    return;
                }

                result.Status = CheckStatus.ProblemsFound;
                result.Counts.Add(new ResultCount("Groups", groups));
                result.Counts.Add(new ResultCount("Rows", Convert.ToInt64(totals[1], CultureInfo.InvariantCulture)));
                var looseGroups = Convert.ToInt64(totals[4], CultureInfo.InvariantCulture);
                if (looseGroups > 0)
                {
                    result.Counts.Add(new ResultCount($"Groups whose other columns also differ in {DuplicateRowsCheck.LooseDifference}", looseGroups));
                }
                ResultSql.SetDates(result, totals[2], totals[3]);
                result.Details.Add($"Audit columns ignored: {string.Join(", ", audit.Select(c => c.Name))}");

                var rows = context.Database.QueryRows(
                    $"SELECT x.g, x.k\nFROM (SELECT t.{key.Sql} AS k, MIN(t.{key.Sql}) OVER (PARTITION BY {group}) AS g FROM {table.Name} AS t) AS x\n" +
                    $"WHERE x.g IN (SELECT TOP ({GroupLimit}) MIN(t.{key.Sql}) FROM {table.Name} AS t GROUP BY {group}\n" +
                    $"      HAVING {having} ORDER BY MIN(t.{key.Sql}))\nORDER BY x.g, x.k");
                var listed = rows.GroupBy(r => ResultSql.CellText(r[0])).ToList();
                var loose = looseGroups > 0
                    ? DuplicateRowsCheck.LooseColumnsByGroup(context, table, key, core, group, having, GroupLimit)
                    : [];
                var shown = listed.Count < groups ? $"{listed.Count:N0} of {groups:N0}" : $"{groups:N0}";
                result.Details.Add($"Groups, by {key.Name} ({shown}):");
                result.Details.AddRange(listed.Select(g =>
                    $"  {string.Join(", ", g.Select(r => ResultSql.KeyCellText(key, r[1])))}{DuplicateRowsCheck.LooseNote(loose.GetValueOrDefault(g.Key))}"));
            }
            catch (SqlException ex)
            {
                result.Status = CheckStatus.FailedToRun;
                result.Message = $"Failed to run: {context.ErrorText(ex)}";
            }
        }
    }
}
