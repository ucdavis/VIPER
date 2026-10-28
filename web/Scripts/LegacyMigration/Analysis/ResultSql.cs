using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// SQL fragments and value formatting shared by the declared checks. Fragments are built only from
    /// bound ColumnRef names; values always go in as parameters.
    /// </summary>
    internal static class ResultSql
    {
        public static string List(string alias, IEnumerable<ColumnRef> columns) =>
            string.Join(", ", columns.Select(c => $"{alias}.{c.Sql}"));

        public static string NotNull(string alias, IEnumerable<ColumnRef> columns) =>
            string.Join(" AND ", columns.Select(c => $"{alias}.{c.Sql} IS NOT NULL"));

        /// <summary>
        /// Column pairs that must all be equal. Text columns with different collations cannot be compared
        /// directly, so those comparisons use the database default.
        /// </summary>
        public static string Equal(string alias, IReadOnlyList<ColumnRef> columns, string otherAlias, IReadOnlyList<ColumnRef> otherColumns) =>
            string.Join(" AND ", columns.Select((c, i) =>
            {
                var other = otherColumns[i];
                var collate = c.IsText && other.IsText && c.Summary.Collation != other.Summary.Collation ? " COLLATE DATABASE_DEFAULT" : "";
                return $"{otherAlias}.{other.Sql}{collate} = {alias}.{c.Sql}{collate}";
            }));

        /// <summary>
        /// Column pairs that must all be equal once text is trimmed and compared ignoring case; other
        /// pairs compare as they are.
        /// </summary>
        public static string EqualAfterCleanup(string alias, IReadOnlyList<ColumnRef> columns, string otherAlias, IReadOnlyList<ColumnRef> otherColumns) =>
            string.Join(" AND ", columns.Select((c, i) =>
            {
                var other = otherColumns[i];
                return c.IsText && other.IsText
                    ? $"LTRIM(RTRIM({other.ComparableSql(otherAlias)})) COLLATE Latin1_General_CI_AS = LTRIM(RTRIM({c.ComparableSql(alias)})) COLLATE Latin1_General_CI_AS"
                    : $"{otherAlias}.{other.Sql} = {alias}.{c.Sql}";
            }));

        /// <summary>
        /// Conditions on the rows of t whose columns have no match in other's columns, in two parts that do
        /// not overlap: NoMatch, no match at all, and CleanupOnly, a match only once text is trimmed and case
        /// is ignored (null when no column pair is text, since only text can match that way). Rows with a
        /// NULL in any of the columns are in neither, as SQL Server skips them for a foreign key.
        /// </summary>
        public static (string NoMatch, string? CleanupOnly) MissingFrom(IReadOnlyList<ColumnRef> columns, TableRef other, IReadOnlyList<ColumnRef> otherColumns)
        {
            var missing = $"{NotNull("t", columns)}\n  AND NOT EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {Equal("t", columns, "p", otherColumns)})";
            if (!columns.Where((c, i) => c.IsText && otherColumns[i].IsText).Any())
            {
                return (missing, null);
            }
            var afterCleanup = $"EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {EqualAfterCleanup("t", columns, "p", otherColumns)})";
            return ($"{missing}\n  AND NOT {afterCleanup}", $"{missing}\n  AND {afterCleanup}");
        }

        /// <summary>
        /// A condition true for rows whose expressions equal any one of the given value lists, with the
        /// parameters it uses: (e1 = @p0_0 AND e2 = @p0_1) OR (e1 = @p1_0 AND ...). columns are the
        /// columns the leading expressions read, which type their parameters (see Parameter); any
        /// expression after them, such as a DATALENGTH, takes its value's own type.
        /// </summary>
        public static (string Sql, SqlParameter[] Parameters) MatchAny(IReadOnlyList<string> expressions, IReadOnlyList<ColumnRef> columns,
            IEnumerable<object?[]> values, string prefix = "m")
        {
            var parameters = new List<SqlParameter>();
            var matches = values.Select((row, i) => "(" + string.Join(" AND ", expressions.Select((e, j) =>
            {
                var name = $"@{prefix}{i}_{j}";
                parameters.Add(Parameter(name, row[j], j < columns.Count ? columns[j] : null));
                return $"{e} = {name}";
            })) + ")").ToList();
            return (matches.Count > 0 ? string.Join("\n   OR ", matches) : "1 = 0", parameters.ToArray());
        }

        /// <summary>
        /// A parameter for a value read from column, typed as the column is when it holds dates or times.
        /// Untyped, a DateTime goes as datetime, which rounds to 3 ms and fails before 1753, so it would
        /// miss its own date and datetime2 rows. A datetime column keeps the untyped datetime, which
        /// matches the value it was read as.
        /// </summary>
        public static SqlParameter Parameter(string name, object? value, ColumnRef? column)
        {
            var parameter = new SqlParameter(name, value ?? DBNull.Value);
            SqlDbType? type = column?.BaseType switch
            {
                "date" => SqlDbType.Date,
                "datetime2" => SqlDbType.DateTime2,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "time" => SqlDbType.Time,
                _ => null
            };
            if (type != null)
            {
                parameter.SqlDbType = type.Value;
                parameter.Scale = FractionalDigits(column!);
            }
            return parameter;
        }

        // The n of datetime2(n), time(n) or datetimeoffset(n), as the export writes them; 0 for date.
        private static byte FractionalDigits(ColumnRef column)
        {
            var type = column.Summary.Type;
            var open = type.IndexOf('(');
            return open > 0 ? byte.Parse(type[(open + 1)..^1], CultureInfo.InvariantCulture) : (byte)0;
        }

        /// <summary>
        /// A row count for the report, such as "1 row" or "1,234 rows".
        /// </summary>
        public static string RowCount(long rows) => rows == 1 ? "1 row" : $"{rows:N0} rows";

        /// <summary>
        /// A share to one decimal place, such as "10.6%". A share that would round to 0% or 100% without
        /// being either prints as "&lt;0.1%" or "&gt;99.9%", so a finding never reads as none or as all.
        /// </summary>
        public static string Share(long part, long whole)
        {
            var share = (double)part / whole;
            return share switch
            {
                > 0 and < 0.0005 => "<0.1%",
                < 1 and >= 0.9995 => ">99.9%",
                _ => share.ToString("0.#%", CultureInfo.InvariantCulture)
            };
        }

        /// <summary>
        /// Whether a column's value can be copied into the report, by where the column's values come from
        /// (ColumnRef.Visibility), never by how the value looks. NULL and blank text say nothing, so they are
        /// always shown. A person ID column shows only values of digits.
        /// </summary>
        public static bool CanShow(ColumnRef column, object? value) => value switch
        {
            null => true,
            string s when s.Trim().Length == 0 => true,
            _ => column.Visibility switch
            {
                ValueVisibility.Shown => true,
                ValueVisibility.DigitsOnly => value is byte or short or int or long or decimal
                                              || (value is string s && s.Trim().All(char.IsAsciiDigit)),
                _ => false
            }
        };

        /// <summary>
        /// A column's value described by shape, "&lt;letters only, 8 chars&gt;", noting the column for the
        /// report's list of masked columns.
        /// </summary>
        public static string Masked(ColumnRef column, object? value)
        {
            column.NoteMasked(value);
            return ShapeText(value);
        }

        /// <summary>
        /// A value described by shape, for a value not traced to a column.
        /// </summary>
        public static string ShapeText(object? value) =>
            value == null ? "NULL" : $"<{ShapeSummary([Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? ""])}>";

        /// <summary>
        /// What kind of characters the values hold and how long they are, grouped, such as
        /// "letters and digits, 5-8 chars (3)". Enough to tell a LoginId from a name or a typo without
        /// showing any value.
        /// </summary>
        /// <param name="values">The values to describe, never shown themselves.</param>
        /// <param name="unit">What the values are, such as "distinct value": when given, every shape names its
        /// count in it ("(1 distinct value)", "(3 distinct values)"), for a reader with nothing else to go on.</param>
        public static string ShapeSummary(List<string> values, string? unit = null)
        {
            var groups = values
                .GroupBy(ShapeOf)
                .OrderByDescending(g => g.Count())
                .Select(g =>
                {
                    var shortest = g.Min(v => v.Length);
                    var longest = g.Max(v => v.Length);
                    var length = shortest == longest ? $"{shortest}" : $"{shortest}-{longest}";
                    var count = unit != null ? $" ({g.Count():N0} {unit}{(g.Count() == 1 ? "" : "s")})"
                        : values.Count > 1 ? $" ({g.Count():N0})" : "";
                    return $"{g.Key}, {length} chars{count}";
                });
            return string.Join("; ", groups);
        }

        private static string ShapeOf(string value)
        {
            if (value.Length == 0)
            {
                return "blank";
            }
            if (value.Contains('@'))
            {
                return EmailShape(value);
            }

            var parts = new List<string>();
            if (value.Any(char.IsLetter))
            {
                parts.Add("letters");
            }
            if (value.Any(char.IsAsciiDigit))
            {
                parts.Add("digits");
            }
            if (value.Any(char.IsWhiteSpace))
            {
                parts.Add("spaces");
            }
            if (value.Any(c => !char.IsLetter(c) && !char.IsAsciiDigit(c) && !char.IsWhiteSpace(c)))
            {
                parts.Add("other characters");
            }
            return parts.Count == 1 ? $"{parts[0]} only" : JoinAnd(parts);
        }

        /// <summary>
        /// A value holding an @: an email address in form (text, @, text with a dot inside it), or what keeps
        /// it from being one, so a value that fails an email format check says why.
        /// </summary>
        private static string EmailShape(string value)
        {
            var problems = new List<string>();
            if (value.Count(c => c == '@') > 1)
            {
                problems.Add("more than one @");
            }
            if (value.StartsWith('@'))
            {
                problems.Add("nothing before the @");
            }
            var domain = value[(value.LastIndexOf('@') + 1)..];
            if (domain.Length == 0)
            {
                problems.Add("nothing after the @");
            }
            else if (!domain.Contains('.'))
            {
                problems.Add("no dot after the @");
            }
            else if (domain.StartsWith('.') || domain.EndsWith('.'))
            {
                problems.Add("a dot right after the @ or at the end");
            }
            if (value.Any(char.IsWhiteSpace))
            {
                problems.Add("spaces");
            }
            if (value.Any(c => c is ',' or ';'))
            {
                problems.Add("commas or semicolons");
            }
            return problems.Count == 0 ? "an email address" : $"contains @, with {JoinAnd(problems)}";
        }

        /// <summary>
        /// A list as written in a sentence: "a", "a and b", or "a, b and c".
        /// </summary>
        public static string JoinAnd(IReadOnlyList<string> items) =>
            items.Count == 1 ? items[0] : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";

        /// <summary>
        /// One row's key, as written in examples: the value alone, or (a, b) for several columns.
        /// </summary>
        public static string KeyText(IReadOnlyList<ColumnRef> columns, object?[] row, int start)
        {
            var parts = columns.Select((c, i) => KeyCellText(c, row[start + i]));
            return columns.Count == 1 ? parts.Single() : $"({string.Join(", ", parts)})";
        }

        /// <summary>
        /// One row's key with its column names, such as "SectionID 2, unitID 2".
        /// </summary>
        public static string NamedKeyText(IReadOnlyList<ColumnRef> columns, object?[] row, int start) =>
            string.Join(", ", columns.Select((c, i) => $"{c.Name} {KeyCellText(c, row[start + i])}"));

        /// <summary>
        /// A key value as written when it can be shown (CanShow), described by shape otherwise.
        /// </summary>
        public static string KeyCellText(ColumnRef column, object? value) =>
            CanShow(column, value) ? CellText(value) : Masked(column, value);

        /// <summary>
        /// The key columns' names as a list heading: "phoneID", or "(SectionID, unitID)".
        /// </summary>
        public static string KeyNames(IReadOnlyList<ColumnRef> columns) =>
            columns.Count == 1 ? columns[0].Name : $"({string.Join(", ", columns.Select(c => c.Name))})";

        // Quoted text in an error message: it opens after a space, bracket, or the start, and closes at the same
        // quote followed by a space, punctuation, or the end, so a value holding an apostrophe (O'Brien) is one
        // match. An opening quote with no close matches to the end of the message.
        private static readonly Regex QuotedText = new(
            """(?<=^|[\s(\[])(?:(['"])(.*?)\1(?=$|[\s.,;:)\]])|['"].*$)""",
            RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));

        /// <summary>
        /// An exception's message as the report shows it. SQL Server and .NET quote the value that failed in
        /// some messages ("converting the varchar value 'jsmith' to data type int"), so quoted text is
        /// described by shape unless it is a name from the export or a .NET type, and an unclosed quote
        /// withholds the rest. A SqlException's number follows the message.
        /// </summary>
        public static string ErrorText(Exception ex, SchemaIndex schema)
        {
            var message = QuotedText.Replace(ex.Message, m =>
                !m.Groups[1].Success ? "<rest of message withheld>"
                : IsNameText(m.Groups[2].Value, schema) || IsTypeName(m.Groups[2].Value) ? m.Value
                : $"<{ShapeSummary([m.Groups[2].Value])}>");
            return ex is SqlException sql ? $"{message} (error {sql.Number})" : message;
        }

        // A type name as .NET messages quote it, such as System.Nullable`1[System.Int32]. No commas or spaces,
        // so an assembly-qualified name never makes the lookup load an assembly.
        private static readonly Regex TypeNamePattern = new(
            @"^[A-Za-z_][\w.`+\[\]]*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Quoted text naming a type loaded in this process, such as 'System.String' in a cast error: it
        /// comes from the code, not the data. Only a real type passes, not text that looks like one.
        /// </summary>
        private static bool IsTypeName(string text)
        {
            if (!TypeNamePattern.IsMatch(text))
            {
                return false;
            }
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(text, throwOnError: false) != null);
            }
            catch (ArgumentException)
            {
                // Brackets that do not make a valid generic type name.
                return false;
            }
        }

        /// <summary>
        /// A name as error messages write it: dbo.visitor, [dbo].[visitor], or t.mothraid, each part a name
        /// from the export, except that a column may follow a query alias such as t.
        /// </summary>
        private static bool IsNameText(string text, SchemaIndex schema)
        {
            var parts = text.Split('.').Select(p => p.Trim('[', ']')).ToList();
            return schema.IsName(parts[^1])
                && parts.SkipLast(1).All(p => schema.IsName(p) || (p.Length <= 2 && p.All(char.IsAsciiLetterLower)));
        }

        public static string CellText(object? value) =>
            value is { } v ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? "" : "NULL";

        /// <summary>
        /// A column value as shown by the checks allowed to show values: quoted, so leading and trailing
        /// spaces are visible, with NULL and blank named.
        /// </summary>
        public static string ShowValue(object? value) => value switch
        {
            null => "<null>",
            string s when s.Trim().Length == 0 => "<blank>",
            string s => $"\"{s}\"",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
        };

        /// <summary>
        /// A column's value as ShowValue shows it, without char or nchar padding, when it can be shown
        /// (CanShow); described by shape otherwise.
        /// </summary>
        public static string ShowValue(ColumnRef column, object? value) =>
            CanShow(column, value) ? ShowValue(Unpadded(column, value)) : Masked(column, value);

        /// <summary>
        /// A column's value without the padding a char or nchar column gives every shorter value. A char
        /// column cannot tell typed trailing spaces from padding, and SQL comparisons ignore both.
        /// </summary>
        public static object? Unpadded(ColumnRef column, object? value) =>
            column.IsPadded && value is string s ? s.TrimEnd(' ') : value;

        /// <summary>
        /// Up to ExampleLimit row keys (none for a keyless table), how many rows there were in all, and
        /// their date range when the table has a declared date column.
        /// </summary>
        public sealed record AffectedRows(List<string> Keys, long Total, object? Earliest, object? Latest);

        /// <summary>
        /// The rows of the table that match the condition (alias t). One query: the window aggregates count
        /// every matching row before TOP picks the examples.
        /// </summary>
        public static AffectedRows FindAffectedRows(AnalysisContext context, TableRef table, string where,
            params SqlParameter[] parameters)
        {
            var keys = table.KeyColumns;
            if (keys.Count > 0)
            {
                var dates = DateRange(context, table, (aggregate, date) => $"{aggregate}(t.{date}) OVER ()");
                var rows = context.Database.QueryRows(
                    $"SELECT TOP ({AnalysisContext.ExampleLimit}) {List("t", keys)}, COUNT_BIG(*) OVER (), {dates}\n" +
                    $"FROM {table.Name} AS t\nWHERE {where}\nORDER BY {List("t", keys)}", parameters);
                return rows.Count > 0
                    ? new AffectedRows(rows.Select(r => KeyText(keys, r, 0)).ToList(),
                        Convert.ToInt64(rows[0][keys.Count], CultureInfo.InvariantCulture), rows[0][keys.Count + 1], rows[0][keys.Count + 2])
                    : new AffectedRows([], 0, null, null);
            }

            var keyless = DateRange(context, table, (aggregate, date) => $"{aggregate}(t.{date})");
            var row = context.Database.QueryRows($"SELECT COUNT_BIG(*), {keyless}\nFROM {table.Name} AS t\nWHERE {where}", parameters).Single();
            return new AffectedRows([], Convert.ToInt64(row[0], CultureInfo.InvariantCulture), row[1], row[2]);
        }

        /// <summary>
        /// Fills a result's examples, total, and date range from FindAffectedRows.
        /// </summary>
        public static long AddAffectedRows(AnalysisContext context, CheckResult result, TableRef table, string where,
            params SqlParameter[] parameters)
        {
            var rows = FindAffectedRows(context, table, where, parameters);
            result.Examples.AddRange(rows.Keys);
            result.ExampleKeyColumns.AddRange(table.KeyColumns.Select(c => c.Name));
            result.ExampleTotal = rows.Total;
            SetDates(result, rows.Earliest, rows.Latest);
            return rows.Total;
        }

        /// <summary>
        /// Two select-list items, Earliest and Latest, from the table's declared date column: format gets
        /// "MIN" or "MAX" and the column, and returns the aggregate. NULL for both when the table has none,
        /// typed so an outer query can still aggregate it (MIN of an untyped NULL is error 8117).
        /// </summary>
        public static string DateRange(AnalysisContext context, TableRef table, Func<string, string, string> format)
        {
            var date = context.Plan.DateColumns.GetValueOrDefault(table);
            return date != null
                ? $"{format("MIN", date.Sql)} AS Earliest, {format("MAX", date.Sql)} AS Latest"
                : "CAST(NULL AS date) AS Earliest, CAST(NULL AS date) AS Latest";
        }

        public static void SetDates(CheckResult result, object? earliest, object? latest)
        {
            result.Earliest = DateText(earliest);
            result.Latest = DateText(latest);
        }

        /// <summary>
        /// A date range for a detail line, such as "2015-03 to 2019-06", one month when both are the
        /// same, or null with no dates.
        /// </summary>
        public static string? RangeText(object? earliest, object? latest)
        {
            var from = DateText(earliest);
            var to = DateText(latest);
            return from == null ? null : to != null && to != from ? $"{from} to {to}" : from;
        }

        public static DateTime? AsDate(object? value) => value switch
        {
            DateTime d => d,
            DateTimeOffset d => d.DateTime,
            _ => null
        };

        /// <summary>
        /// Year and month only: a date column may be personal, such as a birth date, and with one affected
        /// row the earliest and latest are that row's date.
        /// </summary>
        private static string? DateText(object? value) =>
            AsDate(value)?.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        /// <summary>
        /// Parameters named @p0, @p1, ... for a list of values, with the IN list that uses them.
        /// </summary>
        public static (string InList, SqlParameter[] Parameters) InList(IReadOnlyList<object> values, string prefix = "p")
        {
            var parameters = values.Select((v, i) => new SqlParameter($"@{prefix}{i}", v)).ToArray();
            return (string.Join(", ", parameters.Select(p => p.ParameterName)), parameters);
        }
    }
}
