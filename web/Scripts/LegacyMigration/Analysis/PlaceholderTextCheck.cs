using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Text columns holding words that stand in for a missing value ("Unknown", "N/A"), from the shared
    /// profile read, with each word as stored and its row count. One result per table, combined into one
    /// table in the report.
    /// </summary>
    public sealed class PlaceholderTextCheck : IAutomaticCheck
    {
        /// <summary>
        /// Words that stand in for a missing value, matched against a whole value after trimming spaces,
        /// ignoring case. A fixed list that reveals nothing about anyone, so matches are shown as stored.
        /// identifiesOnePerson counts the same words as placeholders.
        /// </summary>
        internal static readonly IReadOnlyList<string> Words = ["None", "N/A", "NA", "NULL", "Unknown", "TBD", "TBA", "test", "?", "-"];

        private static readonly HashSet<string> WordSet = new(Words, StringComparer.OrdinalIgnoreCase);

        // After Words, which it lists: static fields are set in order.
        private static readonly string Description =
            $"Text columns holding a word that stands in for a missing value as the whole value ({string.Join(", ", Words)}; " +
            "ignoring case and surrounding spaces), with each word as stored and how many rows hold it. The new schema " +
            "may use NULL or a lookup row instead. Some may be real values, such as a \"None\" option a person chose, so this is for review.";

        public string Name => "Placeholder text";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var results = new List<CheckResult>();
            foreach (var table in tables)
            {
                var result = AutomaticResult.For(context, Name, Description, ResultKind.DesignObservation, Severity.Info, table.Name);
                if (result.Status != CheckStatus.Skipped)
                {
                    RunTable(context, table, result);
                }
                results.Add(result);
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, Severity.Info,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found", ResultKind.DesignObservation);
        }

        private static void RunTable(AnalysisContext context, TableRef table, CheckResult result)
        {
            var profile = context.Profile(table);
            if (profile.Error != null)
            {
                result.Status = CheckStatus.FailedToRun;
                result.Message = $"Failed to run: {profile.Error}";
                return;
            }

            var profiles = profile.Columns.ToDictionary(p => p.Column, StringComparer.Ordinal);
            var found = table.Columns
                .Select(c => (Column: c, Count: profile.Extras[c.Name].Placeholders ?? 0, Profile: profiles[c.Name]))
                .Where(c => c.Count > 0)
                .ToList();
            if (found.Count == 0)
            {
                return;
            }

            result.Status = CheckStatus.ProblemsFound;
            result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type", "Placeholders (rows)", "Share"],
                found.Select(f =>
                {
                    var nonBlank = profile.Rows - (f.Profile.Nulls ?? 0) - (f.Profile.Blanks ?? 0);
                    return new List<string>
                    {
                        f.Column.Name, f.Column.Summary.Type, WordsFound(context, table, f.Column),
                        nonBlank > 0 ? ResultSql.Share(f.Count, nonBlank) : "-"
                    };
                }).ToList(), found.Count));
        }

        /// <summary>
        /// Each placeholder as stored, under a binary collation so "Unknown" and "unknown" are listed apart,
        /// most rows first.
        /// </summary>
        private static string WordsFound(AnalysisContext context, TableRef table, ColumnRef column)
        {
            var trimmed = $"LTRIM(RTRIM({column.ComparableSql("t")}))";
            try
            {
                var rows = context.Database.QueryRows(
                    $"SELECT {trimmed} COLLATE Latin1_General_BIN2, COUNT_BIG(*)\nFROM {table.Name} AS t\n" +
                    $"WHERE {Where(column)}\nGROUP BY {trimmed} COLLATE Latin1_General_BIN2\nORDER BY COUNT_BIG(*) DESC");
                return string.Join("; ", rows.Select(r =>
                    $"{Convert.ToString(r[0], CultureInfo.InvariantCulture)} {Convert.ToInt64(r[1], CultureInfo.InvariantCulture):N0}"));
            }
            catch (SqlException ex)
            {
                return $"could not be listed: {context.ErrorText(ex)}";
            }
        }

        /// <summary>
        /// The condition the profile counts, on alias t: the trimmed value is one of the words, ignoring case
        /// even in a case-sensitive column. The words are this class's own constants, quoted as literals.
        /// </summary>
        internal static string Where(ColumnRef column) =>
            $"LTRIM(RTRIM({column.ComparableSql("t")})) COLLATE Latin1_General_CI_AS IN ({string.Join(", ", Words.Select(w => $"'{w.Replace("'", "''", StringComparison.Ordinal)}'"))})";

        /// <summary>
        /// Whether a value, trimmed, is one of the words, ignoring case.
        /// </summary>
        internal static bool IsWord(string text) => WordSet.Contains(text.Trim());
    }
}
