using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Counts for one column beyond its ColumnProfile, used by the checks that share the profile read.
    /// Null where the column's type does not have them. Numbers, LeadingZeros, Dates, and Lists count
    /// non-blank text values (trimmed) that look like a number, a number starting with 0, a date, or a
    /// list; Lengths counts values of each length from 1, for short columns only. TimesOfDay counts date
    /// values whose time is not midnight, for date types that hold a time, with the earliest and latest
    /// year among them. Zeros and Negatives count whole-number values of 0 and below 0. Placeholders counts
    /// text values that are a placeholder word (PlaceholderTextCheck.Words).
    /// </summary>
    public sealed record ColumnExtras(long? LeadingSpaces, long? TrailingSpaces, long? UnusualCharacters,
        long? DatesTooEarly, long? DatesTooLate, long? Numbers = null, long? LeadingZeros = null, long? Dates = null,
        long? Lists = null, long[]? Lengths = null, long? TimesOfDay = null, int? TimesOfDayEarliestYear = null,
        int? TimesOfDayLatestYear = null, long? Zeros = null, long? Negatives = null, long? Placeholders = null);

    /// <summary>
    /// A table's profile, which every check built on it shares: AnalysisContext.Profile reads each table
    /// once (one query, or a few for a wide table) and keeps the result. Error is set instead when the
    /// read failed.
    /// </summary>
    public sealed class TableProfile
    {
        /// <summary>
        /// Dates before this year, or more than FutureYears ahead, are implausible: usually placeholders
        /// such as 1900-01-01.
        /// </summary>
        public const int EarliestPlausibleYear = 1950;
        public const int FutureYears = 10;

        private enum ColumnKind { Text, Number, Date, DistinctOnly, NullsOnly, Computed }

        // Compared and counted as distinct values, but with no meaningful lowest or highest.
        private static readonly HashSet<string> DistinctOnlyTypes = new(StringComparer.Ordinal) { "bit", "uniqueidentifier", "time" };

        /// <summary>
        /// Text columns this short or shorter get a count of values of each length.
        /// </summary>
        public const int ShortColumnLength = 20;

        // ISDATE takes at most 4,000 characters, and longer values are never dates.
        private const int DateTestLength = 4000;

        public long Rows { get; private init; }
        public List<ColumnProfile> Columns { get; private init; } = [];
        public Dictionary<string, ColumnExtras> Extras { get; private init; } = new(StringComparer.Ordinal);
        public string? Error { get; private init; }
        public TimeSpan Elapsed { get; private init; }

        public static TableProfile Read(AnalysisContext context, TableRef table)
        {
            // Every aggregate goes into one select list, run as one query, or a few for a wide table (Batches),
            // whose rows put together are that query's row. Each column records where its aggregates start in
            // it. SQL Server computes each COUNT(DISTINCT) in a pass of its own, so a query may read the table,
            // or a spool of it, several times; Elapsed (printed by ColumnProfileCheck) shows what each table cost.
            var selects = new List<string> { "COUNT_BIG(*)" };
            var plans = new List<(ColumnRef Column, ColumnKind Kind, int Start)>();
            var rangeColumns = RangeColumns(table);
            foreach (var column in table.Columns)
            {
                var kind = KindOf(column);
                plans.Add((column, kind, selects.Count));
                selects.AddRange(Aggregates(column, kind, rangeColumns.ContainsKey(column.Name)));
            }

            var timer = Stopwatch.StartNew();
            try
            {
                var row = Batches(plans, selects.Count)
                    .SelectMany(b => context.Database.QueryRows(
                        $"SELECT {string.Join(",\n    ", selects.Skip(b.From).Take(b.To - b.From))}\nFROM {table.Name} AS t",
                        b.HasText ? UnusualCharacterPatterns() : []).Single())
                    .ToArray();
                return new TableProfile
                {
                    Rows = Convert.ToInt64(row[0], CultureInfo.InvariantCulture),
                    Columns = plans.Select(p => ReadProfile(p.Column, p.Kind, rangeColumns.TryGetValue(p.Column.Name, out var reason), reason, row, p.Start)).ToList(),
                    Extras = plans.ToDictionary(p => p.Column.Name, p => ReadExtras(p.Column, p.Kind, row, p.Start), StringComparer.Ordinal),
                    Elapsed = timer.Elapsed
                };
            }
            catch (SqlException ex)
            {
                return new TableProfile { Error = context.ErrorText(ex), Elapsed = timer.Elapsed };
            }
        }

        // Select-list items per profile query. SQL Server allows 4,096; a wide table of short text columns
        // (up to 32 items each) would pass that, and staying well under keeps each query's work bounded.
        private const int SelectLimit = 1000;

        /// <summary>
        /// The select list split at column boundaries into queries of at most SelectLimit items, as [From, To)
        /// ranges of it, each saying whether it holds a text column (which needs the character patterns). A
        /// narrow table is one query; a column with more items than the limit alone gets a query of its own.
        /// </summary>
        private static List<(int From, int To, bool HasText)> Batches(List<(ColumnRef Column, ColumnKind Kind, int Start)> plans, int total)
        {
            var batches = new List<(int From, int To, bool HasText)>();
            var from = 0;
            var hasText = false;
            for (var i = 0; i < plans.Count; i++)
            {
                var end = i + 1 < plans.Count ? plans[i + 1].Start : total;
                if (end - from > SelectLimit && plans[i].Start > from)
                {
                    batches.Add((from, plans[i].Start, hasText));
                    from = plans[i].Start;
                    hasText = false;
                }
                hasText |= plans[i].Kind == ColumnKind.Text;
            }
            batches.Add((from, total, hasText));
            return batches;
        }

        private static ColumnKind KindOf(ColumnRef column) => column switch
        {
            // A computed column is derived from the others, and a non-persisted one is evaluated on
            // read, where bad data can make it fail and take the whole table's query with it.
            { Summary.IsComputed: true } => ColumnKind.Computed,
            { IsText: true } => ColumnKind.Text,
            { IsNumber: true } => ColumnKind.Number,
            { IsDate: true } => ColumnKind.Date,
            _ when DistinctOnlyTypes.Contains(column.BaseType) => ColumnKind.DistinctOnly,
            _ => ColumnKind.NullsOnly
        };

        // Recorded in place of the range for every other number column, so it is not read as missing.
        private const string RangeWithheldReason =
            "not made by the database, a key into such a column, a person ID, a declared lookup, or listed under " +
            "safeColumns; the lowest or highest value of other number columns is often one person's";

        /// <summary>
        /// The columns whose lowest and highest values the profile reports: those whose values can be shown
        /// (ColumnRef.Visibility), with the reason a declaration gave, if any. For any other number column,
        /// such as a score or a balance, the lowest or highest value is often one person's.
        /// </summary>
        private static Dictionary<string, string?> RangeColumns(TableRef table) =>
            table.Columns.Where(c => c.Visibility != ValueVisibility.Masked)
                .ToDictionary(c => c.Name, c => c.ShownReason, StringComparer.Ordinal);

        private static List<string> Aggregates(ColumnRef column, ColumnKind kind, bool showsRange)
        {
            if (kind == ColumnKind.Computed)
            {
                return [];
            }

            var c = $"t.{column.Sql}";
            var e = column.ComparableSql("t");
            // IS NULL rather than COUNT_BIG(column), which text, ntext, and image do not allow.
            var nulls = $"COUNT_BIG(CASE WHEN {c} IS NULL THEN 1 END)";
            static string Count(string? where) => where != null ? $"COUNT_BIG(CASE WHEN {where} THEN 1 END)" : "NULL";

            return kind switch
            {
                ColumnKind.Text =>
                [
                    nulls, $"COUNT_BIG(CASE WHEN {e} = '' THEN 1 END)", $"COUNT_BIG(DISTINCT {e})", $"MAX(LEN({e}))",
                    Count(LeadingSpacesWhere(column)), Count(TrailingSpacesWhere(column)), Count(UnusualCharactersWhere(column)),
                    .. ShapeAggregates(column, e)
                ],
                ColumnKind.Number =>
                [
                    nulls, $"COUNT_BIG(DISTINCT {c})", showsRange ? $"MIN({c})" : "NULL", showsRange ? $"MAX({c})" : "NULL",
                    IsWholeNumber(column) ? Count($"{c} = 0") : "NULL", IsWholeNumber(column) ? Count($"{c} < 0") : "NULL"
                ],
                // Earliest and latest year only: in a column such as a birth date, the earliest or latest
                // date is often one person's.
                ColumnKind.Date =>
                [
                    nulls, $"COUNT_BIG(DISTINCT {c})", $"MIN(YEAR({c}))", $"MAX(YEAR({c}))",
                    Count(DatesTooEarlyWhere(column)), Count(DatesTooLateWhere(column)),
                    .. TimeOfDayAggregates(column, c)
                ],
                ColumnKind.DistinctOnly => [nulls, $"COUNT_BIG(DISTINCT {c})"],
                _ => [nulls]
            };
        }

        // The conditions behind the profile's problem counts, on alias t, shared with the checks that list
        // the rows behind a count so the two never disagree. Null where the column's type has no such count.

        public static string? LeadingSpacesWhere(ColumnRef column)
        {
            if (KindOf(column) != ColumnKind.Text)
            {
                return null;
            }
            var e = column.ComparableSql("t");
            return $"{e} <> '' AND {e} LIKE ' %'";
        }

        public static string? TrailingSpacesWhere(ColumnRef column)
        {
            // char and nchar pad every value with trailing spaces, so only their leading spaces are a problem.
            if (KindOf(column) != ColumnKind.Text || column.IsPadded)
            {
                return null;
            }

            // = '' is also true for values of only spaces, since SQL Server ignores trailing spaces when
            // comparing; those are blanks, not values with stray spaces. DATALENGTH counts trailing spaces.
            var e = column.ComparableSql("t");
            return $"{e} <> '' AND DATALENGTH({e}) > DATALENGTH(RTRIM({e}))";
        }

        /// <summary>
        /// Each kind of unusual character on its own, using the UnusualCharacterPatterns parameters. Empty
        /// for columns that are not text.
        /// </summary>
        public static List<(string Label, string Where)> UnusualCharacterKinds(ColumnRef column)
        {
            if (KindOf(column) != ColumnKind.Text)
            {
                return [];
            }
            var binary = $"{column.ComparableSql("t")} COLLATE Latin1_General_BIN2";
            return
            [
                ("Control characters", $"{binary} LIKE @control"),
                ("Encoding damage", $"({binary} LIKE @mojibake1 OR {binary} LIKE @mojibake2 OR {binary} LIKE @mojibake3)"),
                ("Replacement character", $"{binary} LIKE @replacement")
            ];
        }

        // Control characters ordinary text never holds: all below 32 but tab, line feed, and carriage return.
        private static IEnumerable<int> ControlCodes => Enumerable.Range(1, 31).Where(c => c is not (9 or 10 or 13));

        /// <summary>
        /// The low bytes of Unicode's curly quotes and dashes, which is what they become when text is
        /// converted to one byte per character by keeping each character's low byte instead of mapping it.
        /// Seen in ClinicalScheduler's Service.Description (0x19, 0x1C, 0x1D where smart quotes belong).
        /// </summary>
        private static readonly Dictionary<int, string> CutToLowByte = new()
        {
            [0x13] = "–",
            [0x14] = "—",
            [0x18] = "‘",
            [0x19] = "’",
            [0x1C] = "“",
            [0x1D] = "”"
        };

        /// <summary>
        /// Which control characters a column holds, by code, with how many rows hold each, so a reader
        /// can recognize a pattern without seeing any text. Codes that are a Unicode quote or dash cut to
        /// its low byte name the character they likely were. Empty for columns that are not text.
        /// </summary>
        public static List<string> ControlCharacterCodes(AnalysisContext context, TableRef table, ColumnRef column)
        {
            if (KindOf(column) != ColumnKind.Text)
            {
                return [];
            }
            var binary = $"{column.ComparableSql("t")} COLLATE Latin1_General_BIN2";
            var codes = ControlCodes.ToList();
            object?[] row;
            try
            {
                row = context.Database.QueryRows(
                    $"SELECT {string.Join(",\n    ", codes.Select(c => $"COUNT_BIG(CASE WHEN {binary} LIKE N'%' + NCHAR({c}) + N'%' THEN 1 END)"))}\n" +
                    $"FROM {table.Name} AS t").Single();
            }
            catch (SqlException ex)
            {
                return [$"{column.Name}, control characters could not be listed: {context.ErrorText(ex)}"];
            }

            var found = codes.Select((c, i) => (Code: c, Rows: Convert.ToInt64(row[i], CultureInfo.InvariantCulture))).Where(f => f.Rows > 0).ToList();
            if (found.Count == 0)
            {
                return [];
            }
            var listed = found.Select(f => $"0x{f.Code:X2} ({ResultSql.RowCount(f.Rows)}" +
                (CutToLowByte.TryGetValue(f.Code, out var original) ? $", likely {original} cut to one byte)" : ")"));
            return [$"{column.Name}, control characters: {string.Join(", ", listed)}"];
        }

        /// <summary>
        /// How many date values have a time other than midnight, and the earliest and latest year among
        /// them, which shows whether such values are still being written. NULLs for date, which holds no time.
        /// </summary>
        private static List<string> TimeOfDayAggregates(ColumnRef column, string c)
        {
            if (column.BaseType == "date")
            {
                return ["NULL", "NULL", "NULL"];
            }
            var timed = $"CAST({c} AS time) <> '00:00'";
            return [$"COUNT_BIG(CASE WHEN {timed} THEN 1 END)", $"MIN(CASE WHEN {timed} THEN YEAR({c}) END)", $"MAX(CASE WHEN {timed} THEN YEAR({c}) END)"];
        }

        /// <summary>
        /// Integer types, and decimal or numeric with no digits after the point: the types keys are made of.
        /// </summary>
        public static bool IsWholeNumber(ColumnRef column) =>
            column.BaseType is "tinyint" or "smallint" or "int" or "bigint"
            || (column.BaseType is "decimal" or "numeric" && column.Summary.Type.EndsWith(",0)", StringComparison.Ordinal));

        public static string? UnusualCharactersWhere(ColumnRef column) =>
            UnusualCharacterKinds(column) is { Count: > 0 } kinds ? string.Join(" OR ", kinds.Select(k => k.Where)) : null;

        public static string? DatesTooEarlyWhere(ColumnRef column) =>
            KindOf(column) == ColumnKind.Date ? $"t.{column.Sql} < '{EarliestPlausibleYear}0101'" : null;

        public static string? DatesTooLateWhere(ColumnRef column) =>
            KindOf(column) == ColumnKind.Date ? $"t.{column.Sql} > DATEADD(YEAR, {FutureYears}, SYSDATETIME())" : null;

        /// <summary>
        /// What text values look like, for the design observations: how many are numbers (digits with at
        /// most one point and an optional leading minus, so it works under any compatibility level, unlike
        /// TRY_CONVERT), how many of those start with 0, how many ISDATE accepts, how many contain a comma
        /// or semicolon, and for short columns, how many have each length. Patterns use a binary collation
        /// so [0-9] means only the ASCII digits. Long and legacy text columns hold free text, so they skip
        /// the date and list counts.
        /// </summary>
        private static List<string> ShapeAggregates(ColumnRef column, string e)
        {
            var x = $"LTRIM(RTRIM({e})) COLLATE Latin1_General_BIN2";
            var number = $"({x} NOT LIKE '%[^0-9.]%' OR ({x} LIKE '-%' AND SUBSTRING({x}, 2, LEN({x})) NOT LIKE '%[^0-9.]%')) " +
                         $"AND {x} LIKE '%[0-9]%' AND {x} NOT LIKE '%.%.%'";
            var maxLength = column.Summary.MaxLength ?? 0;
            var bounded = !IsFreeText(column);

            return
            [
                $"COUNT_BIG(CASE WHEN {e} <> '' AND {number} THEN 1 END)",
                $"COUNT_BIG(CASE WHEN {e} <> '' AND {number} AND {x} LIKE '0[0-9]%' THEN 1 END)",
                bounded && maxLength <= DateTestLength ? $"COUNT_BIG(CASE WHEN {e} <> '' AND ISDATE({x}) = 1 THEN 1 END)" : "NULL",
                bounded ? $"COUNT_BIG(CASE WHEN {e} LIKE '%[,;]%' THEN 1 END)" : "NULL",
                $"COUNT_BIG(CASE WHEN {PlaceholderTextCheck.Where(column)} THEN 1 END)",
                .. Enumerable.Range(1, LengthBuckets(column)).Select(n => $"COUNT_BIG(CASE WHEN LEN({e}) = {n} THEN 1 END)")
            ];
        }

        /// <summary>
        /// Text columns with no length limit (max, text, ntext): notes and comments, which hold commas as a
        /// matter of course.
        /// </summary>
        public static bool IsFreeText(ColumnRef column) =>
            column.IsText && (column.Summary.MaxLength is not > 0 || column.BaseType is "text" or "ntext");

        private static int LengthBuckets(ColumnRef column) =>
            column.Summary.MaxLength is > 0 and <= ShortColumnLength && column.BaseType is not ("text" or "ntext")
                ? column.Summary.MaxLength.Value
                : 0;

        /// <summary>
        /// LIKE patterns, compared under a binary collation so accents and case count. Control characters
        /// leave out tab, line feed, and carriage return, which ordinary text holds. Encoding damage is
        /// UTF-8 text read as Windows-1252: a lead character (Ã, Â, or â) followed by what a UTF-8
        /// continuation byte (0x80-0xBF) reads as, which is how é becomes Ã©. U+FFFD replaced a character
        /// that could not be read at all. New parameters on every call, since a parameter can belong to
        /// only one command.
        /// </summary>
        public static SqlParameter[] UnusualCharacterPatterns()
        {
            var control = string.Concat(ControlCodes.Select(c => (char)c));

            // 0x80-0x9F as Windows-1252 shows them, then the same bytes as Latin-1, then 0xA0-0xBF.
            const string windows1252 = "€‚ƒ„…†‡ˆ‰Š‹ŒŽ‘’“”•–—˜™š›œžŸ";
            var continuation = windows1252
                + string.Concat(Enumerable.Range(0x80, 0x20).Select(c => (char)c))
                + string.Concat(Enumerable.Range(0xA0, 0x20).Select(c => (char)c));

            return
            [
                new SqlParameter("@control", $"%[{control}]%"),
                new SqlParameter("@mojibake1", $"%Ã[{continuation}]%"),
                new SqlParameter("@mojibake2", $"%Â[{continuation}]%"),
                new SqlParameter("@mojibake3", $"%â[{continuation}]%"),
                new SqlParameter("@replacement", "%�%")
            ];
        }

        private static ColumnProfile ReadProfile(ColumnRef column, ColumnKind kind, bool showsRange, string? rangeReason,
            object?[] row, int start)
        {
            long? Long(int offset) => row[start + offset] is { } v ? Convert.ToInt64(v, CultureInfo.InvariantCulture) : null;
            int? Int(int offset) => row[start + offset] is { } v ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : null;
            string? Text(int offset) => row[start + offset] is { } v ? Convert.ToString(v, CultureInfo.InvariantCulture) : null;

            var type = column.Summary.Type;
            return kind switch
            {
                ColumnKind.Computed => new ColumnProfile(column.Name, type, null, null, null, null, null, null, null, null, "computed column"),
                ColumnKind.Text => new ColumnProfile(column.Name, type, Long(0), Long(1), Long(2), null, null, null, null, Long(3), null),
                ColumnKind.Number => new ColumnProfile(column.Name, type, Long(0), null, Long(1), Text(2), Text(3), null, null, null, null,
                    showsRange ? null : RangeWithheldReason, rangeReason),
                ColumnKind.Date => new ColumnProfile(column.Name, type, Long(0), null, Long(1), null, null, Int(2), Int(3), null, null),
                ColumnKind.DistinctOnly => new ColumnProfile(column.Name, type, Long(0), null, Long(1), null, null, null, null, null, null),
                _ => new ColumnProfile(column.Name, type, Long(0), null, null, null, null, null, null, null, $"{column.BaseType}: NULLs counted only")
            };
        }

        private static ColumnExtras ReadExtras(ColumnRef column, ColumnKind kind, object?[] row, int start)
        {
            long? Long(int offset) => row[start + offset] is { } v ? Convert.ToInt64(v, CultureInfo.InvariantCulture) : null;
            int? Int(int offset) => row[start + offset] is { } v ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : null;

            return kind switch
            {
                ColumnKind.Text => new ColumnExtras(Long(4), Long(5), Long(6), null, null, Long(7), Long(8), Long(9), Long(10),
                    Enumerable.Range(0, LengthBuckets(column)).Select(i => Long(12 + i) ?? 0).ToArray(), Placeholders: Long(11)),
                ColumnKind.Date => new ColumnExtras(null, null, null, Long(4), Long(5), TimesOfDay: Long(6),
                    TimesOfDayEarliestYear: Int(7), TimesOfDayLatestYear: Int(8)),
                ColumnKind.Number => new ColumnExtras(null, null, null, null, null, Zeros: Long(4), Negatives: Long(5)),
                _ => new ColumnExtras(null, null, null, null, null)
            };
        }
    }
}
