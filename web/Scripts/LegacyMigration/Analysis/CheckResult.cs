using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public enum CheckStatus { NoProblemsFound, ProblemsFound, Skipped, FailedToRun }

    /// <summary>
    /// Which part of the report a result belongs in. Context and design observations are kept apart
    /// from problems so they do not bury them.
    /// </summary>
    public enum ResultKind { Problem, Context, DesignObservation }

    public sealed record ResultCount(string Label, long Value);

    /// <summary>
    /// A choice the migration has to make, raised by a result: the question, and how many columns and
    /// tables of the result it covers. The report gathers these at the top, so they are not lost in a long
    /// section.
    /// </summary>
    public sealed record ResultDecision(string Question, int Columns, int Tables);

    /// <summary>
    /// A list of issues, one per row: each row holds its key values, then its value columns. Rows holds up
    /// to AnalysisContext.ExampleLimit of them and Total how many there were in all. Note follows the
    /// title in the report, such as the date range of the issues' rows. CountText replaces the row count
    /// after the title when the rows are not the things counted, such as a summary table of IDs.
    /// </summary>
    public sealed record ResultGrid(string Title, List<string> KeyColumns, List<string> ValueColumns, List<List<string>> Rows,
        long Total, string? Note = null, string? CountText = null);

    /// <summary>
    /// One check's outcome. Everything here is written to report.txt and results.json, so a check may
    /// only put in counts, row keys (TableRef.KeyColumns), and dates, plus column values for the few
    /// checks the design allows to write them.
    /// </summary>
    public sealed class CheckResult
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required ResultKind Kind { get; init; }
        public required Severity Severity { get; init; }
        public string? Table { get; init; }
        public CheckStatus Status { get; set; }

        /// <summary>
        /// Why the check was skipped or failed, or why its severity was changed.
        /// </summary>
        public string? Message { get; set; }

        public List<ResultCount> Counts { get; } = [];

        /// <summary>
        /// Up to AnalysisContext.ExampleLimit examples; ExampleTotal is how many there were in all.
        /// </summary>
        public List<string> Examples { get; } = [];
        public long ExampleTotal { get; set; }

        /// <summary>
        /// The columns each example key is made of, so the report can name them.
        /// </summary>
        public List<string> ExampleKeyColumns { get; } = [];

        /// <summary>
        /// Issues identified by key, laid out in the report as a table, or grouped when they are keys alone.
        /// </summary>
        public List<ResultGrid> Grids { get; } = [];

        /// <summary>
        /// The date range of the affected rows, when the table has a declared date column.
        /// </summary>
        public string? Earliest { get; set; }
        public string? Latest { get; set; }

        /// <summary>
        /// Lines printed under the result in report.txt, such as lists of column names. Metadata only.
        /// </summary>
        public List<string> Details { get; } = [];

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResultDecision? Decision { get; set; }

        /// <summary>
        /// The column profile's per-column counts. Written to results.json only, since the full profile
        /// of every table would bury report.txt.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ColumnProfile>? ColumnProfiles { get; set; }
    }

    /// <summary>
    /// One column's counts from the column profile. Text values are never included: only counts, the
    /// lowest and highest number (for columns whose values can be shown only), the earliest and latest year, and
    /// the longest text length. Fields a type does not support are left out of results.json, as is a
    /// field with nothing to report (the lowest of a column that is always NULL, which Nulls shows).
    /// NotProfiled says why a column was skipped entirely, and RangeWithheld why a number column's lowest
    /// and highest are not given, so a withheld range is not mistaken for a missing one. RangeShownBecause
    /// says why a declaration made a column's range shown (a safeColumns reason, or a declared lookup).
    /// </summary>
    public sealed record ColumnProfile(string Column, string Type,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Nulls,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Blanks,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Distinct,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Lowest,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Highest,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? EarliestYear,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? LatestYear,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? LongestText,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NotProfiled,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RangeWithheld = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RangeShownBecause = null);
}
