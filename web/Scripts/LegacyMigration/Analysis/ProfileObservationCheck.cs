using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// A design observation read from the shared profile read (TableProfile): candidates for a different
    /// design in the new schema, not defects. One result per table, listing each column it applies to,
    /// with counts only. Evaluate returns a column's cells, or null when the observation does not apply.
    /// </summary>
    public sealed class ProfileObservationCheck(string name, string description, List<string> cellLabels,
        Func<ObservedColumn, string[]?> evaluate, string? decision = null) : IAutomaticCheck
    {
        /// <summary>
        /// Columns with this few distinct values, each held by at least CodeRepeats times as many values
        /// on average. More than 20 distinct values already needs over 200 values, so the cap only adds
        /// larger code sets in large tables. Codes look like this, but so do people (a column changed by a
        /// few staff) and sequences, so the report asks for a review rather than calling them codes.
        /// </summary>
        private const int CodeMaxDistinct = 50;
        private const int CodeRepeats = 10;

        /// <summary>
        /// A short column whose most common length holds at least this share of values, but not all of
        /// them, looks like a fixed-length code with exceptions (8-character IDs with a few of 9).
        /// </summary>
        private const double DominantLengthShare = 0.8;

        /// <summary>
        /// A date column with at least this share of values at midnight stores dates, with any values
        /// that have a time of day as exceptions.
        /// </summary>
        private const double MidnightShare = 0.95;

        public static readonly ProfileObservationCheck AlwaysNumbersOrDates = new("Text that is always numbers or dates",
            "Text columns whose every non-blank value is a number or a date: candidates for a number or date type. " +
            "Values that start with 0 are counted separately. Converting a column to a number type would drop those zeros, " +
            "so columns that have them, such as zero-padded codes, usually need to stay text. " +
            "Person columns are left out (see Person columns): person IDs are numbers kept as text for their leading zeros.",
            ["Type", "Values", "Always", "Starting with 0"], c =>
            {
                var values = c.NonBlank;
                if (values == 0 || c.Extras.Numbers == null || c.IsPersonColumn)
                {
                    return null;
                }
                if (c.Extras.Numbers == values)
                {
                    return [c.Profile.Type, Count(values), "numbers", Count(c.Extras.LeadingZeros ?? 0)];
                }
                return c.Extras.Dates == values ? [c.Profile.Type, Count(values), "dates", "-"] : null;
            });

        public static readonly ProfileObservationCheck ShortCodeLengths = new("Lengths in short code columns",
            $"Short text columns ({TableProfile.ShortColumnLength} characters or fewer) whose values are mostly one length, " +
            "with some of other lengths, such as an ID column mixing 8- and 9-character values.",
            ["Lengths (values)"], c =>
            {
                var lengths = c.Extras.Lengths;
                if (lengths is not { Length: > 0 } || lengths.Count(n => n > 0) < 2)
                {
                    return null;
                }
                var total = lengths.Sum();
                return lengths.Max() >= total * DominantLengthShare
                    ? [string.Join(", ", lengths.Select((n, i) => (Length: i + 1, Count: n)).Where(l => l.Count > 0)
                        .OrderByDescending(l => l.Count).Select(l => $"{l.Length}: {Count(l.Count)}"))]
                    : null;
            });

        public static readonly ProfileObservationCheck FewDistinctValues = new("Text columns with few distinct values",
            $"Text columns with 2 to {CodeMaxDistinct} distinct values, at least {CodeRepeats} values for each distinct value " +
            "on average, and no foreign key. This is a match on counts only: review whether each column holds codes " +
            "(a candidate for a lookup table), references to something else, or a sequence. " +
            "Person columns are left out (see Person columns), since a column changed by a few people matches too.",
            ["Distinct values", "Values"], c =>
            {
                var distinct = c.Profile.Distinct ?? 0;
                return c.Extras.Numbers != null && !c.HasForeignKey && !c.IsPersonColumn && distinct is >= 2 and <= CodeMaxDistinct
                       && c.NonBlank >= distinct * CodeRepeats
                    ? [Count(distinct), Count(c.NonBlank)]
                    : null;
            });

        public static readonly ProfileObservationCheck ListsInOneColumn = new("Lists stored in one column",
            "Text columns with values containing commas or semicolons, which often means several values packed into one. " +
            "Long text columns are left out, since free text holds commas anyway. A small share usually means a few " +
            "exceptions in a single-value column; a share near 100% usually means a consistent format, such as \"Last, First\".",
            ["With , or ;", "Share"], c =>
            {
                var lists = c.Extras.Lists ?? 0;
                return lists > 0 && c.NonBlank > 0
                    ? [Count(lists), ResultSql.Share(lists, c.NonBlank)]
                    : null;
            });

        public static readonly ProfileObservationCheck BitColumnsWithNulls = new("Bit columns holding NULL",
            "Yes/no flags that are NULL on some rows, which makes them three-valued: WHERE Active = 0 misses the NULL rows. " +
            "The migration has to decide what NULL means before the new column can be NOT NULL.",
            ["NULL rows", "Share"], c =>
            {
                var nulls = c.Profile.Nulls ?? 0;
                return c.Profile.Type == "bit" && nulls > 0 && c.Rows > 0
                    ? [Count(nulls), ResultSql.Share(nulls, c.Rows)]
                    : null;
            },
            decision: "Decide what NULL means in yes/no flags");

        public static readonly ProfileObservationCheck DatesAtMidnight = new("Date and time columns at midnight",
            $"Columns of a date type that holds a time, whose values are all, or at least {MidnightShare:0%}, at midnight: " +
            "candidates for the date type. A few values with a time among many at midnight often mean a second writer, " +
            "such as a procedure storing the current time where the application stores a date only. " +
            "A column recording when a row changed that is always at midnight means the time is not being kept. " +
            "The years of the values with a time, beside the column's own, show whether they are still being written.",
            ["Type", "Values", "Years", "With a time of day", "Years with a time"], c =>
            {
                var values = c.Rows - (c.Profile.Nulls ?? 0);
                return c.Extras.TimesOfDay is { } timed && values > 0 && values - timed >= values * MidnightShare
                    ? [c.Profile.Type, Count(values), Years(c.Profile.EarliestYear, c.Profile.LatestYear), Count(timed),
                        Years(c.Extras.TimesOfDayEarliestYear, c.Extras.TimesOfDayLatestYear)]
                    : null;
            });

        public string Name => name;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var foreignKeyColumns = context.Plan.Summary.ForeignKeys
                .SelectMany(fk => fk.Columns.Select(c => (fk.Table, Column: c)))
                .ToHashSet();
            var personColumns = context.Plan.Summary.PersonIdColumns.Select(p => (Table: p.Object, p.Column)).ToHashSet();
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var results = tables.Select(t => RunTable(context, t, foreignKeyColumns, personColumns)).ToList();
            return AutomaticResult.OnlyFindings(results, Name, description, Severity.Info,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found", ResultKind.DesignObservation);
        }

        private CheckResult RunTable(AnalysisContext context, TableRef table, HashSet<(string Table, string Column)> foreignKeyColumns,
            HashSet<(string Table, string Column)> personColumns)
        {
            var result = AutomaticResult.For(context, Name, description, ResultKind.DesignObservation, Severity.Info, table.Name);
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

            var profiles = profile.Columns.ToDictionary(p => p.Column, StringComparer.Ordinal);
            var found = table.Columns
                .Select(c => (Column: c.Name, Cells: evaluate(new ObservedColumn(profiles[c.Name], profile.Extras[c.Name], profile.Rows,
                    foreignKeyColumns.Contains((table.Name, c.Name)), personColumns.Contains((table.Name, c.Name))))))
                .Where(c => c.Cells != null)
                .ToList();
            if (found.Count > 0)
            {
                result.Status = CheckStatus.ProblemsFound;
                result.Grids.Add(new ResultGrid("Columns", ["Column"], cellLabels,
                    found.Select(f => f.Cells!.Prepend(f.Column).ToList()).ToList(), found.Count));
                if (decision != null)
                {
                    result.Decision = new ResultDecision(decision, found.Count, 1);
                }
            }
            return result;
        }

        private static string Count(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

        private static string Years(int? earliest, int? latest) => (earliest, latest) switch
        {
            ({ } e, { } l) when e == l => e.ToString(CultureInfo.InvariantCulture),
            ({ } e, { } l) => FormattableString.Invariant($"{e}-{l}"),
            _ => "-"
        };
    }

    /// <summary>
    /// One column's profile counts, as an observation sees them. NonBlank counts values that are neither
    /// NULL nor blank. IsPersonColumn is true for any column the schema export flagged as a person.
    /// </summary>
    public sealed record ObservedColumn(ColumnProfile Profile, ColumnExtras Extras, long Rows, bool HasForeignKey, bool IsPersonColumn)
    {
        public long NonBlank => Rows - (Profile.Nulls ?? 0) - (Profile.Blanks ?? 0);
    }
}
