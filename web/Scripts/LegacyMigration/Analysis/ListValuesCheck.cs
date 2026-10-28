using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class ListValuesDeclaration : CheckDeclaration
    {
        public required string Column { get; init; }

        // Entries are nullable because the reader lets a null list entry through (Declarations.ReadOptions).
        public List<string?>? AllowedValues { get; init; }
        public bool IgnoreCase { get; init; }
        public string? LooksPersonal { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var column = binder.Column(where, "column", table, Column);
            if (column == null)
            {
                return null;
            }
            if (!column.IsText && !column.IsNumber && column.BaseType != "bit")
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type}; listValues needs a text, number, or bit column.");
                return null;
            }
            // Declaring listValues shows the column's values in this check, so it is held to safeColumns' rule.
            binder.RequireNotPersonal(where, column, LooksPersonal);

            // A number or bit column compares by value, so each entry must be one; an entry that is not could never match.
            HashSet<decimal>? allowedNumbers = null;
            if (AllowedValues != null && !column.IsText)
            {
                var isBit = column.BaseType == "bit";
                allowedNumbers = [];
                foreach (var entry in AllowedValues)
                {
                    if (ParseAllowed(entry, isBit) is { } number)
                    {
                        allowedNumbers.Add(number);
                    }
                    else
                    {
                        binder.Error(where, $"\"allowedValues\" has {(entry != null ? $"\"{entry}\"" : "null")}, but {column.Name} is {column.Summary.Type}; " +
                                            (isBit ? "give 0, 1, true, or false." : "give numbers."));
                    }
                }
            }

            return new ListValuesCheck
            {
                Name = $"listValues: {column.Name}",
                Description = Description,

                // Without allowedValues nothing can fail, so the listing is context, not a warning.
                Severity = AllowedValues != null ? SeverityOrDefault : Severity ?? Analysis.Severity.Info,
                Tables = [column.Table],
                Column = column,
                // A null entry in a text column's list could never match, so it is dropped.
                AllowedValues = AllowedValues?.OfType<string>().ToList(),
                AllowedNumbers = allowedNumbers,
                IgnoreCase = IgnoreCase
            };
        }

        private static decimal? ParseAllowed(string? entry, bool isBit)
        {
            var text = entry?.Trim();
            if (isBit)
            {
                return text?.ToLowerInvariant() switch
                {
                    "0" or "false" => 0,
                    "1" or "true" => 1,
                    _ => null
                };
            }
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
        }
    }

    /// <summary>
    /// Lists each distinct value and how many rows hold it. It copies the column's values from the
    /// database into the report (it never changes the database), so it is meant for code columns only,
    /// and lists at most ValueLimit values whatever it is pointed at. The declaration is the approval to
    /// show them, here only; elsewhere the column follows ResultSql.CanShow. Text compares with
    /// allowedValues as written; a number or bit column compares by value (1, 1.0, and 1.00 are equal),
    /// and its values are shown as SQL Server writes them, a bit as 0 or 1.
    /// </summary>
    public sealed class ListValuesCheck : DeclaredCheck
    {
        private const int ValueLimit = 100;

        public required ColumnRef Column { get; init; }
        public List<string>? AllowedValues { get; init; }

        /// <summary>
        /// For a number or bit column, AllowedValues as numbers (a bit's true and false as 1 and 0).
        /// </summary>
        public HashSet<decimal>? AllowedNumbers { get; init; }

        public bool IgnoreCase { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            // Grouped exactly: a binary collation keeps case variants apart, and the length keeps values
            // that differ only by trailing spaces apart, since SQL Server ignores those when comparing.
            var value = Column.ComparableSql("t");
            var group = Column.IsText ? $"{value} COLLATE Latin1_General_BIN2, DATALENGTH({value})" : value;
            var select = Column.IsText ? $"{value} COLLATE Latin1_General_BIN2" : value;
            var dates = ResultSql.DateRange(context, Column.Table, (aggregate, date) => $"{aggregate}(t.{date})");
            var rows = context.Database.QueryRows(
                $"SELECT {select}, COUNT_BIG(*), {dates}\nFROM {Column.Table.Name} AS t\nGROUP BY {group}\nORDER BY COUNT_BIG(*) DESC");

            var allowedText = Column.IsText ? AllowedValues?.ToHashSet(IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) : null;
            var values = rows.Select(r => (Value: Shown(r[0]), Rows: Convert.ToInt64(r[1], CultureInfo.InvariantCulture),
                Earliest: ResultSql.AsDate(r[2]), Latest: ResultSql.AsDate(r[3]))).ToList();

            // NULL and blank are listed but not checked against allowedValues; notBlank covers those.
            bool IsAllowed(object? v) => v == null || (v is string s && s.Trim().Length == 0) || (AllowedNumbers, allowedText) switch
            {
                ({ } numbers, _) => ToNumber(v) is { } number && numbers.Contains(number),
                (_, { } texts) => texts.Contains(Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""),
                _ => true
            };

            var result = NewResult(Name, AllowedValues != null ? ResultKind.Problem : ResultKind.Context);
            result.Counts.Add(new ResultCount("Distinct values, not counting NULL", values.Count(v => v.Value != null)));
            if (AllowedValues != null)
            {
                var notAllowed = values.Where(v => !IsAllowed(v.Value)).ToList();
                result.Counts.Add(new ResultCount("Rows with a value not in allowedValues", notAllowed.Sum(v => v.Rows)));
                ResultSql.SetDates(result, notAllowed.Min(v => v.Earliest), notAllowed.Max(v => v.Latest));
                result.Status = notAllowed.Count > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            }

            // Blank values of different lengths are one entry in the list. Each value's own date range shows
            // whether it is still being written, even when nothing is checked against allowedValues.
            var listed = values
                .GroupBy(v => ResultSql.ShowValue(v.Value))
                .Select(g => (Label: g.Key, Rows: g.Sum(v => v.Rows), Allowed: g.All(v => IsAllowed(v.Value)),
                    Range: ResultSql.RangeText(g.Min(v => v.Earliest), g.Max(v => v.Latest))))
                .OrderByDescending(v => v.Rows)
                .ToList();
            result.Details.AddRange(listed.Take(ValueLimit)
                .Select(v => $"{v.Label}: {v.Rows:N0}{(v.Range != null ? $", {v.Range}" : "")}{(v.Allowed ? "" : "  (not allowed)")}"));
            if (listed.Count > ValueLimit)
            {
                result.Details.Add($"... and {listed.Count - ValueLimit:N0} more values, not listed. Is this a code column?");
            }

            return [result];
        }

        /// <summary>
        /// A value as compared and listed: char padding dropped, a bit as 0 or 1 rather than .NET's True and
        /// False, and a decimal without the trailing zeros of its column's scale (1.00 as 1).
        /// </summary>
        private object? Shown(object? value) => value switch
        {
            bool b => b ? 1 : 0,
            decimal d => decimal.Parse(d.ToString("G29", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => ResultSql.Unpadded(Column, value)
        };

        // A decimal(38) value too large for .NET's decimal arrives as text and cannot match.
        private static decimal? ToNumber(object value)
        {
            try
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }
    }
}
