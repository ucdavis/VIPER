using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// For each foreign key the export marks untrusted or disabled, the child rows whose values have no
    /// parent row even once spaces are trimmed and case is ignored; with afterCleanup, the rows that match
    /// a parent only that way. The two do not overlap (ResultSql.MissingFrom, as valuesMustExistIn).
    /// Trusted keys are skipped, since SQL Server already guarantees them. Rows with a NULL in
    /// any key column are skipped, as SQL Server does. Rows are reported by the child table's key, never
    /// by the key values, which can be anything the parent is keyed on.
    /// </summary>
    public sealed class UnenforcedForeignKeyCheck(bool afterCleanup) : IAutomaticCheck
    {
        public string Name => afterCleanup ? "Unenforced foreign keys, after cleanup" : "Unenforced foreign keys";

        private string Description => afterCleanup
            ? "Rows with no parent that would match one once spaces are trimmed and case is ignored: the stored values need cleaning. " +
              "They are not counted under Unenforced foreign keys."
            : "Rows whose values are missing from the table an untrusted or disabled foreign key points to, even once spaces are trimmed " +
              "and case is ignored. Rows that match only that way are under Unenforced foreign keys, after cleanup.";

        private Severity DefaultSeverity => afterCleanup ? Severity.Warning : Severity.Critical;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var schema = context.Plan.Schema;
            // The child decides: a key into a shared table outside "include" is still checked.
            var inScope = context.Plan.Summary.ForeignKeys
                .Select(fk => (Key: fk, Child: schema.FindTable(fk.Table), Parent: schema.FindTable(fk.ReferencedTable)))
                .Where(k => k.Child is { GetsAutomaticChecks: true } && k.Parent is { IsExcluded: false })
                .ToList();
            var unenforced = inScope.Where(k => k.Key.IsNotTrusted || k.Key.IsDisabled).ToList();
            var keys = unenforced
                .Select(k => (k.Key, Child: k.Child!, Parent: k.Parent!,
                    Columns: k.Key.Columns.Select(c => k.Child!.FindColumn(c)!).ToList(),
                    ParentColumns: k.Key.ReferencedColumns.Select(c => k.Parent!.FindColumn(c)!).ToList()))
                .Where(k => !afterCleanup || k.Columns.Exists(c => c.IsText))
                .OrderBy(k => k.Child.Name, StringComparer.Ordinal).ThenBy(k => k.Key.Name, StringComparer.Ordinal)
                .ToList();

            var results = new List<CheckResult>();
            var clean = new List<string>();
            var cleanupOnly = new List<string>();
            foreach (var (key, child, parent, columns, parentColumns) in keys)
            {
                Console.WriteLine($"  {key.Name}");
                var state = key.IsDisabled ? "disabled" : "untrusted";
                var description = $"{key.Name} ({state}): {ResultSql.KeyNames(columns)} -> {parent.Name} {ResultSql.KeyNames(parentColumns)}. {Description}";
                var result = AutomaticResult.For(context, Name, description, ResultKind.Problem, DefaultSeverity, child.Name);
                var blocked = false;
                if (result.Status != CheckStatus.Skipped)
                {
                    // One key failing, such as a large table timing out, must not cost the others their results.
                    try
                    {
                        blocked = Check(context, result, child, parent, columns, parentColumns);
                    }
                    catch (SqlException ex)
                    {
                        result.Status = CheckStatus.FailedToRun;
                        result.Message = $"Failed to run: {context.ErrorText(ex)}";
                    }
                }
                if (result.Status == CheckStatus.NoProblemsFound)
                {
                    (blocked ? cleanupOnly : clean).Add(key.Name);
                }
                results.Add(result);
            }

            var findings = results.Where(r => r.Status != CheckStatus.NoProblemsFound).ToList();
            if (clean.Count > 0 || cleanupOnly.Count > 0 || findings.Count == 0)
            {
                var summary = new CheckResult
                {
                    Name = Name,
                    Description = Description,
                    Kind = ResultKind.Problem,
                    Severity = DefaultSeverity,
                    Status = CheckStatus.NoProblemsFound
                };
                summary.Details.Add(keys.Count > 0
                    ? $"Checked {keys.Count.ToString("N0", CultureInfo.InvariantCulture)} untrusted or disabled foreign keys."
                    : NothingToCheck(inScope.Count, unenforced.Count));
                if (clean.Count > 0 && !afterCleanup)
                {
                    // Re-enabling these WITH CHECK would succeed, making them trusted again.
                    summary.Details.Add($"No missing parents ({clean.Count:N0}): {string.Join(", ", clean)}");
                }
                if (cleanupOnly.Count > 0)
                {
                    summary.Details.Add($"Missing parents only until cleanup ({cleanupOnly.Count:N0}): {string.Join(", ", cleanupOnly)}. " +
                                        "Their values match a parent only once spaces are trimmed and case is ignored (see Unenforced foreign keys, " +
                                        "after cleanup), so re-enabling them WITH CHECK fails until those values are cleaned.");
                }
                findings.Add(summary);
            }
            return findings;
        }

        /// <summary>
        /// Why no key was checked, so a zero does not read like a check that failed to look.
        /// </summary>
        private string NothingToCheck(int foreignKeys, int unenforced) =>
            foreignKeys == 0 ? "Nothing to check: the tables checked have no foreign keys."
            : unenforced == 0 ? $"Nothing to check: all {foreignKeys:N0} foreign keys are trusted and enabled, so SQL Server already guarantees them."
            : afterCleanup ? $"Nothing to check: none of the {unenforced:N0} untrusted or disabled foreign keys is on text columns, and only text can match once spaces are trimmed and case is ignored."
            : $"Checked {unenforced:N0} untrusted or disabled foreign keys.";

        /// <summary>
        /// The key's rows in this check's category. Returns whether, with none in it, the key still has rows
        /// that match a parent only after cleanup, which the other check reports and which keep it from being
        /// re-enabled WITH CHECK, since SQL Server neither trims nor ignores case it would not ignore anyway.
        /// </summary>
        private bool Check(AnalysisContext context, CheckResult result, TableRef child, TableRef parent,
            List<ColumnRef> columns, List<ColumnRef> parentColumns)
        {
            var (noMatch, cleanupOnly) = ResultSql.MissingFrom(columns, parent, parentColumns);
            var where = afterCleanup ? cleanupOnly ?? "1 = 0" : noMatch;

            var rows = ResultSql.AddAffectedRows(context, result, child, where);
            if (rows == 0)
            {
                return !afterCleanup && cleanupOnly != null && context.Database.QueryRows(
                    $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {child.Name} AS t WHERE {cleanupOnly}) THEN 1 ELSE 0 END").Single()[0] is 1;
            }

            var values = context.Database.QueryRows(
                $"SELECT COUNT_BIG(*) FROM (SELECT DISTINCT {ResultSql.List("t", columns)} FROM {child.Name} AS t\nWHERE {where}) AS x").Single();
            result.Counts.Add(new ResultCount(afterCleanup ? "Rows that match only after cleanup" : "Rows with no parent", rows));
            result.Counts.Add(new ResultCount("Distinct values", Convert.ToInt64(values[0], CultureInfo.InvariantCulture)));
            result.Status = CheckStatus.ProblemsFound;
            return false;
        }
    }
}
