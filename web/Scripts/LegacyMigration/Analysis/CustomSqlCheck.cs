using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class CustomSqlDeclaration : CheckDeclaration
    {
        public required string Name { get; init; }
        public required string File { get; init; }
        public required List<string> KeyColumns { get; init; }
        public string? OccurredAtColumn { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var errors = binder.Errors.Count;
            if (string.IsNullOrWhiteSpace(Name))
            {
                binder.Error(where, "\"name\" is required.");
            }
            if (Table != null)
            {
                binder.Error(where, "\"table\" is not used: the tables come from the query.");
            }
            if (KeyColumns is not { Count: > 0 } || KeyColumns.Exists(string.IsNullOrWhiteSpace))
            {
                binder.Error(where, "\"keyColumns\" needs at least one column of the query.");
            }
            else if (KeyColumns.Distinct(StringComparer.OrdinalIgnoreCase).Count() < KeyColumns.Count)
            {
                binder.Error(where, "\"keyColumns\" names a column more than once.");
            }

            var sql = ReadQuery(binder, where);
            var tables = new List<TableRef>();
            var keySources = new List<ColumnRef?>();
            if (sql != null)
            {
                var outcome = CustomSqlValidator.Validate(sql, binder.CompatibilityLevel);
                foreach (var error in outcome.Errors)
                {
                    binder.Error(where, $"{File}, {error}");
                }
                var bound = new Dictionary<CustomSqlValidator.TableName, TableRef>();
                foreach (var name in outcome.Tables)
                {
                    var table = BindTable(binder, where, name);
                    if (table != null)
                    {
                        bound[name] = table;
                        if (!tables.Contains(table))
                        {
                            tables.Add(table);
                        }
                    }
                }
                keySources = KeyColumns.Select(k => KeySource(outcome, bound, k)).ToList();
                if (outcome.Errors.Count == 0 && outcome.Tables.Count == 0)
                {
                    binder.Error(where, $"{File} reads no table or view.");
                }
            }

            return binder.Errors.Count > errors ? null : new CustomSqlCheck
            {
                Name = $"customSql: {Name}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = tables,
                Sql = sql!,
                KeyColumns = KeyColumns,
                KeySources = keySources,
                OccurredAtColumn = OccurredAtColumn
            };
        }

        /// <summary>
        /// The table column a key column of the query was traced to, or null when it cannot be.
        /// </summary>
        private static ColumnRef? KeySource(CustomSqlValidator.Outcome outcome,
            Dictionary<CustomSqlValidator.TableName, TableRef> bound, string key) =>
            outcome.Sources?.GetValueOrDefault(key) is { } source && bound.TryGetValue(source.Table, out var table)
                ? table.Columns.FirstOrDefault(c => c.Name.Equals(source.Column, StringComparison.OrdinalIgnoreCase))
                : null;

        /// <summary>
        /// The query file, which must sit beside analysis.json, so the declarations and the queries they
        /// run are reviewed together.
        /// </summary>
        private string? ReadQuery(SchemaBinder binder, string where)
        {
            if (string.IsNullOrWhiteSpace(File) || binder.DeclarationsFolder == null)
            {
                binder.Error(where, "\"file\" is required: a .sql file in the same folder as analysis.json.");
                return null;
            }
            // Both separators are refused on every OS: Linux reads a backslash as part of the file name, so
            // "sub\query.sql", written on Windows, would otherwise pass there as a file in this folder.
            var path = Path.GetFullPath(Path.Join(binder.DeclarationsFolder, File));
            if (File.IndexOfAny(['/', '\\']) >= 0
                || !string.Equals(Path.GetDirectoryName(path), binder.DeclarationsFolder, StringComparison.OrdinalIgnoreCase)
                || !path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                binder.Error(where, $"\"file\" must be a .sql file in the same folder as analysis.json, named without a folder; {File} is not.");
                return null;
            }

            try
            {
                return System.IO.File.ReadAllText(path);
            }
            catch (IOException ex)
            {
                binder.Error(where, $"{File} could not be read: {ex.Message}");
                return null;
            }
        }

        private static TableRef? BindTable(SchemaBinder binder, string where, CustomSqlValidator.TableName name)
        {
            if (name.Database != null && !name.Database.Equals(binder.Database, StringComparison.OrdinalIgnoreCase))
            {
                binder.Error(where, $"the query reads {name.Database}, another database. Only this database's tables can be read.");
                return null;
            }

            // An unqualified name is in dbo, the default schema in the legacy databases.
            var quoted = $"{SchemaExport.Quote(name.Schema ?? "dbo")}.{SchemaExport.Quote(name.Name)}";
            var table = binder.FindObject(quoted);
            if (table == null)
            {
                binder.Error(where, $"the query reads {quoted}, which is not a table or view in the schema export.");
            }
            return table;
        }
    }

    /// <summary>
    /// A developer's own query, run as written once CustomSqlValidator has accepted it, returning one row
    /// per problem. Only the key columns (as examples) and the date column (as a range) are read from its
    /// rows; any other column stays out of the report. A key traced to a table column is shown as that
    /// column's values are (ResultSql.CanShow); one that is not is described by shape.
    /// </summary>
    public sealed class CustomSqlCheck : DeclaredCheck
    {
        public required string Sql { get; init; }
        public required List<string> KeyColumns { get; init; }

        /// <summary>
        /// The table column each key column was traced to, in KeyColumns' order; null for one that was not.
        /// </summary>
        public required List<ColumnRef?> KeySources { get; init; }

        public string? OccurredAtColumn { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            // A query of several tables names them all, rather than one "vs" another.
            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.Problem,
                Severity = Severity,
                Table = string.Join(", ", Tables.Select(t => t.Name)),
                Status = CheckStatus.NoProblemsFound
            };
            context.Database.Read(Sql, reader =>
            {
                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                var missing = KeyColumns.Append(OccurredAtColumn).OfType<string>()
                    .Where(c => !columns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
                if (missing.Count > 0)
                {
                    result.Status = CheckStatus.FailedToRun;
                    result.Message = $"Failed to run: the query returns no column named {ResultSql.JoinAnd(missing)}.";
                    return;
                }

                var keys = KeyColumns.Select(reader.GetOrdinal).ToList();
                var date = OccurredAtColumn != null ? reader.GetOrdinal(OccurredAtColumn) : -1;
                if (date >= 0 && reader.GetFieldType(date) != typeof(DateTime) && reader.GetFieldType(date) != typeof(DateTimeOffset))
                {
                    result.Status = CheckStatus.FailedToRun;
                    result.Message = $"Failed to run: {OccurredAtColumn} is not a date or time, so it cannot date the rows.";
                    return;
                }

                long total = 0;
                DateTime? earliest = null, latest = null;
                while (reader.Read())
                {
                    total++;
                    if (result.Examples.Count < AnalysisContext.ExampleLimit)
                    {
                        var parts = keys.Select((i, j) => KeySources[j] is { } source
                            ? ResultSql.KeyCellText(source, AnalysisDatabase.ValueOf(reader, i))
                            : ResultSql.ShapeText(AnalysisDatabase.ValueOf(reader, i))).ToList();
                        result.Examples.Add(parts.Count == 1 ? parts[0] : $"({string.Join(", ", parts)})");
                    }
                    if (date >= 0 && ResultSql.AsDate(AnalysisDatabase.ValueOf(reader, date)) is { } when)
                    {
                        earliest = earliest == null || when < earliest ? when : earliest;
                        latest = latest == null || when > latest ? when : latest;
                    }
                }

                result.Counts.Add(new ResultCount("Rows found", total));
                result.ExampleKeyColumns.AddRange(KeyColumns);
                var untraced = KeyColumns.Where((_, j) => KeySources[j] == null).ToList();
                if (total > 0 && untraced.Count > 0)
                {
                    result.Details.Add($"{ResultSql.JoinAnd(untraced)} {(untraced.Count == 1 ? "is" : "are")} described by shape: not selected straight from a table column, " +
                                       "so where the values come from cannot be checked. Select the table's column itself (t.column) to show it.");
                }
                result.ExampleTotal = total;
                ResultSql.SetDates(result, earliest, latest);
                if (total > 0)
                {
                    result.Status = CheckStatus.ProblemsFound;
                }
            });
            return [result];
        }
    }
}
