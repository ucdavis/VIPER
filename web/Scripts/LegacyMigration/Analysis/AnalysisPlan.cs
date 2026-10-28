using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The declarations with every name bound to the schema export. Building it checks the whole file
    /// and reports every problem at once, before the database is touched.
    /// </summary>
    public sealed class AnalysisPlan
    {
        private readonly Dictionary<(string Check, string Table), AutomaticCheckOverride> _overrides;

        private AnalysisPlan(SchemaSummary summary, SchemaIndex schema, Dictionary<TableRef, ColumnRef> dateColumns,
            Dictionary<(string Check, string Table), AutomaticCheckOverride> overrides, List<DeclaredCheck> declaredChecks)
        {
            Summary = summary;
            Schema = schema;
            DateColumns = dateColumns;
            _overrides = overrides;
            DeclaredChecks = declaredChecks;
        }

        /// <summary>
        /// The schema export as read, for automatic checks driven by its facts rather than by table data.
        /// </summary>
        public SchemaSummary Summary { get; }

        public SchemaIndex Schema { get; }

        public IReadOnlyDictionary<TableRef, ColumnRef> DateColumns { get; }

        public IReadOnlyList<DeclaredCheck> DeclaredChecks { get; }

        /// <summary>
        /// The override for one automatic check on one table or view, by its [schema].[name].
        /// </summary>
        public AutomaticCheckOverride? FindOverride(string automaticCheck, string objectName) =>
            _overrides.GetValueOrDefault((automaticCheck, objectName));

        public static AnalysisPlan Bind(SchemaSummary summary, Declarations declarations)
        {
            var excluded = declarations.Exclude.ToHashSet(StringComparer.Ordinal);
            var schema = new SchemaIndex(summary, excluded, declarations.Include);
            var binder = new SchemaBinder(schema)
            {
                DeclarationsFolder = declarations.Folder,
                Database = summary.Database,
                CompatibilityLevel = summary.CompatibilityLevel
            };

            BindInclude(declarations, schema, binder);
            for (var i = 0; i < declarations.Exclude.Count; i++)
            {
                binder.Object($"exclude[{i}]", "table", declarations.Exclude[i]);
            }

            var dateColumns = BindDateColumns(declarations, binder);
            var overrides = BindOverrides(declarations, binder);
            var declaredChecks = BindChecks(declarations, binder);

            // A declared lookup counts as a foreign key: its values are shown when the column it looks up was
            // made by the database. Found before safeColumns apply, so a declared column passes nothing on.
            // Unlike a foreign key, nothing makes its type match, so a text column shows only its values of
            // digits: a value matching no ID may be a LoginId or a name.
            var lookups = declaredChecks.OfType<ValuesMustExistInCheck>()
                .SelectMany(l => l.Columns.Zip(l.OtherColumns, (column, other) => (Column: column, Other: other, l.Name)))
                .Where(p => p.Other.Visibility == ValueVisibility.Shown)
                .ToList();
            BindSafeColumns(declarations, binder);

            if (binder.Errors.Count > 0)
            {
                throw new DeclarationException(binder.Errors);
            }

            foreach (var (column, _, name) in lookups)
            {
                column.Show(column.IsNumber ? ValueVisibility.Shown : ValueVisibility.DigitsOnly, $"used in the declared lookup {name}");
            }

            return new AnalysisPlan(summary, schema, dateColumns, overrides, declaredChecks);
        }

        /// <summary>
        /// An empty list is an error rather than "everything", so a file meant to narrow the run never widens it.
        /// </summary>
        private static void BindInclude(Declarations declarations, SchemaIndex schema, SchemaBinder binder)
        {
            if (declarations.Include is not { } include)
            {
                return;
            }
            if (include.Count == 0)
            {
                binder.Error("include", "list at least one table, view, or schema, or leave \"include\" out to check everything.");
            }

            var schemas = schema.SchemaNames.ToHashSet(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < include.Count; i++)
            {
                var entry = include[i];
                var where = $"include[{i}]";
                if (string.IsNullOrWhiteSpace(entry) || !SchemaIndex.IsSchemaEntry(entry))
                {
                    binder.Object(where, "table", entry);
                }
                else if (!schemas.Contains(entry))
                {
                    var suggestion = schemas.FirstOrDefault(s => s.Equals(entry, StringComparison.OrdinalIgnoreCase));
                    binder.Error(where, $"{entry} is not a schema holding a table or view in the schema export. " +
                                        (suggestion != null ? $"Did you mean {suggestion}?" : "Schemas are written [schema], tables and views [schema].[name]."));
                }
                if (!string.IsNullOrWhiteSpace(entry) && !seen.Add(entry))
                {
                    binder.Error(where, $"{entry} is already listed.");
                }
            }
        }

        /// <summary>
        /// An automatic-check setting on an object the automatic checks do not visit would silently do nothing.
        /// </summary>
        private static void RequireIncluded(SchemaBinder binder, string where, TableRef? table)
        {
            if (table is { IsIncluded: false })
            {
                binder.Error(where, $"{table.Name} is outside \"include\", so the automatic checks do not visit it.");
            }
        }

        private static void BindSafeColumns(Declarations declarations, SchemaBinder binder)
        {
            var listed = new HashSet<ColumnRef>();
            for (var i = 0; i < declarations.SafeColumns.Count; i++)
            {
                var entry = declarations.SafeColumns[i];
                var where = $"safeColumns[{i}]";
                if (entry == null)
                {
                    binder.Error(where, NullEntry);
                    continue;
                }
                var column = binder.Column(where, "column", binder.Object(where, "table", entry.Table), entry.Column);
                if (string.IsNullOrWhiteSpace(entry.Reason))
                {
                    binder.Error(where, "\"reason\" must say why the values are not personal, in one sentence.");
                }
                if (column == null)
                {
                    continue;
                }
                if (!listed.Add(column))
                {
                    binder.Error(where, $"{column.Name} of {column.Table.Name} is already listed.");
                    continue;
                }
                binder.RequireNotPersonal(where, column, entry.LooksPersonal);
                column.Show(ValueVisibility.Shown, entry.Reason);
            }
        }

        private static Dictionary<TableRef, ColumnRef> BindDateColumns(Declarations declarations, SchemaBinder binder)
        {
            var dateColumns = new Dictionary<TableRef, ColumnRef>();
            foreach (var (tableName, columnName) in declarations.DateColumns)
            {
                var where = $"dateColumns {tableName}";
                var column = binder.Column(where, "column", binder.Object(where, "table", tableName), columnName);
                if (column == null)
                {
                    continue;
                }
                if (!column.IsDate)
                {
                    binder.Error(where, $"{column.Name} is {column.Summary.Type}, not a date column.");
                    continue;
                }
                dateColumns[column.Table] = column;
            }
            return dateColumns;
        }

        private static Dictionary<(string Check, string Table), AutomaticCheckOverride> BindOverrides(
            Declarations declarations, SchemaBinder binder)
        {
            var known = CheckRegistry.Automatic.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
            var acrossTables = CheckRegistry.Automatic.Where(c => !c.AcceptsTableOverrides).Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
            var overrides = new Dictionary<(string Check, string Table), AutomaticCheckOverride>();

            for (var i = 0; i < declarations.AutomaticChecks.Count; i++)
            {
                var entry = declarations.AutomaticChecks[i];
                var where = $"automaticChecks[{i}]";
                if (entry == null)
                {
                    binder.Error(where, NullEntry);
                    continue;
                }

                if (!known.Contains(entry.AutomaticCheck))
                {
                    binder.Error(where, $"\"{entry.AutomaticCheck}\" is not an automatic check. Known checks: {KnownList(known)}.");
                }
                else if (acrossTables.Contains(entry.AutomaticCheck))
                {
                    binder.Error(where, $"\"{entry.AutomaticCheck}\" reports one result across tables, so it cannot be skipped or changed for one table.");
                }
                var table = binder.Object(where, "table", entry.Table);
                RequireIncluded(binder, where, table);
                var objectName = table?.Name;
                if (entry.Skip == (entry.Severity != null))
                {
                    binder.Error(where, "give exactly one of \"severity\" or \"skip\": true.");
                }
                if (string.IsNullOrWhiteSpace(entry.Reason))
                {
                    binder.Error(where, "\"reason\" must say why, in one sentence.");
                }
                if (objectName != null && !overrides.TryAdd((entry.AutomaticCheck, objectName), entry))
                {
                    binder.Error(where, $"\"{entry.AutomaticCheck}\" is already overridden for {objectName}.");
                }
            }
            return overrides;
        }

        private static List<DeclaredCheck> BindChecks(Declarations declarations, SchemaBinder binder)
        {
            var checks = new List<DeclaredCheck>();

            for (var i = 0; i < declarations.Checks.Count; i++)
            {
                var element = declarations.Checks[i];
                var name = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("check", out var c)
                    && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                var where = $"checks[{i}]{(name != null ? $" ({name})" : "")}";

                if (name == null)
                {
                    binder.Error(where, "each check needs a \"check\" field naming which check to run.");
                    continue;
                }
                if (!CheckRegistry.Declared.TryGetValue(name, out var type))
                {
                    binder.Error(where, $"\"{name}\" is not a known check. Known checks: {KnownList(CheckRegistry.Declared.Keys)}.");
                    continue;
                }

                CheckDeclaration? declaration;
                try
                {
                    declaration = (CheckDeclaration?)element.Deserialize(type, Declarations.ReadOptions);
                }
                catch (JsonException ex)
                {
                    binder.Error(where, ex.Message);
                    continue;
                }

                var check = declaration?.Bind(binder, where);
                if (check != null)
                {
                    checks.Add(check);
                }
            }
            return checks;
        }

        private const string NullEntry = "is null; write the entry as an object, or remove it.";

        private static string KnownList(IEnumerable<string> names)
        {
            var list = names.Order(StringComparer.Ordinal).ToList();
            return list.Count > 0 ? string.Join(", ", list) : "(none yet)";
        }
    }

    /// <summary>
    /// Looks declared names up in the schema export and collects an error for each one it cannot find.
    /// </summary>
    public sealed class SchemaBinder(SchemaIndex schema)
    {
        public List<string> Errors { get; } = [];

        /// <summary>
        /// Where analysis.json is, its database's name, and its compatibility level, for customSql, which
        /// reads its query from a file there and parses it as that level of T-SQL.
        /// </summary>
        public string? DeclarationsFolder { get; init; }
        public string Database { get; init; } = "";
        public int CompatibilityLevel { get; init; }

        public void Error(string where, string message) => Errors.Add($"{where}: {message}");

        /// <summary>
        /// A table or view as SQL Server would find it from a query: ignoring case, as in a
        /// case-insensitive database. Null without an error, for the caller to report.
        /// </summary>
        public TableRef? FindObject(string name) =>
            schema.FindTableOrView(name) ?? (schema.SuggestTableOrView(name) is { } match ? schema.FindTableOrView(match) : null);

        /// <summary>
        /// A table or view: everywhere a declaration names an object, either is accepted.
        /// </summary>
        public TableRef? Object(string where, string field, string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Error(where, $"\"{field}\" is required.");
                return null;
            }

            var found = schema.FindTableOrView(name);
            if (found == null)
            {
                var suggestion = schema.SuggestTableOrView(name);
                Error(where, $"{name} is not a table or view in the schema export. " +
                             (suggestion != null ? $"Did you mean {suggestion}?" : "Names are written [schema].[name], as in summary.json."));
            }
            return found;
        }

        /// <summary>
        /// One or more columns of the same table; null when the list is empty or any name did not bind.
        /// </summary>
        public List<ColumnRef>? Columns(string where, string field, TableRef? table, List<string>? names)
        {
            if (table == null)
            {
                return null;
            }
            if (names is not { Count: > 0 })
            {
                Error(where, $"\"{field}\" needs at least one column.");
                return null;
            }

            var columns = names.Select(n => Column(where, field, table, n)).ToList();
            if (columns.Exists(c => c == null))
            {
                return null;
            }
            if (columns.Select(c => c!.Name).Distinct(StringComparer.Ordinal).Count() < columns.Count)
            {
                Error(where, $"\"{field}\" names a column more than once.");
                return null;
            }
            return columns.Select(c => c!).ToList();
        }

        /// <summary>
        /// Returns null without a further error when the table itself did not bind.
        /// </summary>
        public ColumnRef? Column(string where, string field, TableRef? table, string? name)
        {
            if (table == null)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                Error(where, $"\"{field}\" is required.");
                return null;
            }

            var column = table.FindColumn(name);
            if (column == null)
            {
                var suggestion = table.SuggestColumn(name);
                Error(where, $"{name} is not a column of {table.Name}." + (suggestion != null ? $" Did you mean {suggestion}?" : ""));
            }
            return column;
        }

        // Name words, or parts of a word, that suggest a column holds something personal. Parts catch run-together
        // names (username, lastname); short or common words must match whole (age, not page; grade, not upgrade).
        // Salary and pay are left out: UC salaries are public.
        private static readonly string[] PersonalWordParts =
        [
            "mail", "name", "login", "phone", "ssn", "birth", "address", "ethnic", "religio", "disab", "medical", "health"
        ];
        private static readonly string[] PersonalWords =
        [
            "dob", "user", "first", "last", "grade", "grades", "score", "scores", "gpa", "balance", "balances",
            "gender", "sex", "race", "age", "comment", "comments", "note", "notes"
        ];

        /// <summary>
        /// The word of a column name that suggests it holds something personal, or null.
        /// </summary>
        internal static string? PersonalWord(string columnName) =>
            PersonIdRules.Words(columnName).Find(w => Array.Exists(PersonalWordParts, p => w.Contains(p, StringComparison.Ordinal)) || Array.IndexOf(PersonalWords, w) >= 0);

        /// <summary>
        /// A declaration that shows a column's values: an error when the column's name looks personal,
        /// unless looksPersonal says why its values are safe to show anyway.
        /// </summary>
        public void RequireNotPersonal(string where, ColumnRef column, string? looksPersonal)
        {
            if (!string.IsNullOrWhiteSpace(looksPersonal))
            {
                return;
            }
            var match = column.IsPersonColumn ? "it is flagged as a person column"
                : PersonalWord(column.Name) is { } word ? $"its name has \"{word}\"" : null;
            if (match != null)
            {
                Error(where, $"{column.Name} may hold personal values: {match}. To show its values anyway, add \"looksPersonal\" saying why they are safe, in one sentence.");
            }
        }
    }
}
