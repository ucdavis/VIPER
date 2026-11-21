using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Suggests what Areas/{connection}/analysis.json does not declare yet, so declarations never start from
    /// a blank page and new tables are noticed after schema changes: tables that look like backups to exclude,
    /// a guessed date column per table, likely relationships as valuesMustExistIn checks, and person ID columns
    /// as identifiesOnePerson checks. Guesses cover only the tables the file's include names, and leave out
    /// what it already declares. A file with no include and no checks also gets the database's tables to
    /// choose an include from, since scoping is the first decision on a database holding more than the
    /// migration moves. Every entry is a guess from names and keys; no table data is read.
    ///
    /// Refreshes the schema export first, as the analysis does. Output:
    /// Output/{connection}/analysis/{environment}/analysis.suggestions.json; analysis.json is read, never
    /// changed. The suggestions are checked against the export before they are reported.
    /// </summary>
    public static class Suggestions
    {
        private static readonly JsonSerializerOptions StringOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        // Name words that mark a table as a backup or copy.
        private static readonly HashSet<string> BackupWords = new(StringComparer.Ordinal) { "bk", "bak", "bkup", "backup", "old", "copy" };

        // A date written into a name, such as _2022_12_30 or 20221230.
        private static readonly Regex DateInName = new(@"(?<!\d)(19|20)\d{2}[_-]?(0[1-9]|1[0-2])[_-]?(0[1-9]|[12]\d|3[01])(?!\d)",
            RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        // Name words that say a date column records when a row was created, or last changed, in order of
        // preference (createdOn beats approvalSubmittedOn), each as the forms it is written in. Whole words
        // only, so Module, Additional, and Address are not taken for mod and add.
        internal static readonly string[][] CreatedWords =
        [
            ["creat", "create", "created", "creation"], ["insert", "inserted"], ["add", "added"], ["enter", "entered"], ["submit", "submitted"]
        ];
        internal static readonly string[][] ModifiedWords =
        [
            ["mod", "modified", "modify", "modification"], ["upd", "updt", "update", "updated"], ["change", "changed"], ["edit", "edited"]
        ];

        // A form run together with what it dates, in a name the word splitter cannot break up (moddate, addtime).
        private static readonly string[] RunTogetherEndings = ["", "date", "dt", "time", "on", "stamp"];

        /// <summary>
        /// Whether a name word (lowercase, from PersonIdRules.Words) is one of the forms, alone or run together
        /// with a date word.
        /// </summary>
        internal static bool IsWordFor(string word, string[] forms) =>
            Array.Exists(forms, f => Array.Exists(RunTogetherEndings, e => word == f + e));

        /// <summary>
        /// Whether a name word says a column records when its row was created or changed.
        /// </summary>
        internal static bool IsCreatedOrModifiedWord(string word) =>
            CreatedWords.Concat(ModifiedWords).Any(forms => IsWordFor(word, forms));

        // Key names too generic to say which table a column refers to.
        private static readonly HashSet<string> GenericKeyWords = new(StringComparer.Ordinal) { "id", "key", "pk", "row", "rec" };

        private static readonly HashSet<string> DateTypes = new(StringComparer.Ordinal) { "date", "datetime", "datetime2", "smalldatetime", "datetimeoffset" };

        public static int Run(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("LEGACY DATABASE ANALYSIS SUGGESTIONS");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();

            try
            {
                var connectionName = LegacyMigrationScriptHelper.ValidateConnectionName(
                    LegacyMigrationScriptHelper.GetRequiredArg(args, "connection"));
                var project = LegacyMigrationScriptHelper.ValidateProjectName(LegacyMigrationScriptHelper.GetOptionalArg(args, "project"));
                var environment = LegacyMigrationScriptHelper.RequireEnvironment();
                var toolRoot = LegacyMigrationScriptHelper.GetToolRoot();
                var (declarationsPath, outputDir) = LegacyMigrationScriptHelper.AnalysisPaths(toolRoot, connectionName, environment, project);
                var declarationsName = Path.GetRelativePath(toolRoot, declarationsPath);
                var configuration = LegacyMigrationScriptHelper.LoadConfiguration(environment);
                var connectionString = LegacyMigrationScriptHelper.GetConnectionString(configuration, connectionName, environment);
                var path = Path.Join(outputDir, "analysis.suggestions.json");

                Declarations declared;
                try
                {
                    declared = Declarations.Load(declarationsPath);
                }
                catch (DeclarationException ex)
                {
                    throw new InvalidOperationException($"cannot read the declarations: {string.Join("; ", ex.Errors)}", ex);
                }

                Console.WriteLine($"  Connection:   {connectionName} ({environment})");
                if (project != null)
                {
                    Console.WriteLine($"  Project:      {project}");
                }
                Console.WriteLine($"  Database:     {LegacyMigrationScriptHelper.GetServerAndDatabase(connectionString)}");
                Console.WriteLine($"  Declarations: {(File.Exists(declarationsPath) ? declarationsPath : $"(none yet; would be {declarationsName})")}");
                Console.WriteLine($"  Include:      {(declared.Include != null ? string.Join(", ", declared.Include) : "(none: every table)")}");
                Console.WriteLine($"  Output:       {path}");
                Console.WriteLine();

                Console.WriteLine("Refreshing the schema export...");
                var summary = SchemaExport.Export(connectionString, connectionName, environment);
                Console.WriteLine();

                Directory.CreateDirectory(outputDir);
                LegacyMigrationScriptHelper.WriteCrlfFile(path, Build(summary, connectionName, environment, declarationsName, declared));

                // Read it back as the analysis would, so a guess that cannot bind is caught here.
                AnalysisPlan.Bind(summary, Declarations.Load(path));
                Console.WriteLine($"Suggestions saved to: {path}");
                Console.WriteLine("They read and bind against the schema export as written. Copy what you keep into");
                Console.WriteLine($"{declarationsName}.");
                LegacyMigrationScriptHelper.WriteFinished(timer);
                return 0;
            }
            catch (DeclarationException ex)
            {
                LegacyMigrationScriptHelper.WriteError("SUGGESTIONS ERROR", "the file was written but does not bind against the export:");
                foreach (var error in ex.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            catch (ArgumentException ex)
            {
                LegacyMigrationScriptHelper.WriteError("ARGUMENT ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LegacyMigrationScriptHelper.WriteError("CONFIGURATION ERROR", ex.Message);
            }
            catch (SqlException ex)
            {
                LegacyMigrationScriptHelper.WriteError("DATABASE ERROR", $"{ex.Message} (error {ex.Number})");
            }
            catch (UnauthorizedAccessException ex)
            {
                LegacyMigrationScriptHelper.WriteError("ACCESS DENIED", ex.Message);
            }
            catch (IOException ex)
            {
                LegacyMigrationScriptHelper.WriteError("FILE I/O ERROR", ex.Message);
            }

            return 1;
        }

        internal static string Build(SchemaSummary summary, string connectionName, string environment, string declarationsName, Declarations declared)
        {
            var include = declared.Include;
            var declaredExcluded = declared.Exclude.ToHashSet(StringComparer.Ordinal);
            var backups = summary.Tables
                .Select(t => (t.Name, Reason: BackupReason(t.Name)))
                .Where(t => t.Reason != null)
                .ToList();
            var skipped = backups.Select(t => t.Name).Concat(declaredExcluded).ToHashSet(StringComparer.Ordinal);
            var excluded = backups
                .Where(t => SchemaIndex.IsIncluded(include, t.Name) && !declaredExcluded.Contains(t.Name))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
            var tables = summary.Tables
                .Where(t => !skipped.Contains(t.Name) && SchemaIndex.IsIncluded(include, t.Name))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();
            // A guessed lookup may point outside include, such as into a shared table.
            var targets = summary.Tables.Where(t => !skipped.Contains(t.Name)).ToList();
            var declaredColumns = DeclaredColumns(declared);

            var sb = new StringBuilder();
            sb.AppendLine($"// Suggestions for {connectionName} ({environment}), generated {DateTime.Now:yyyy-MM-dd HH:mm} from the schema export and");
            sb.AppendLine($"// {declarationsName}, leaving out what that file already declares. Every entry is a guess from");
            sb.AppendLine("// table and column names and keys; no table data was read. Keep, edit, or delete each one, then copy what");
            sb.AppendLine("// you keep into analysis.json. This file itself is never read by the analysis.");
            sb.AppendLine("{");

            if (include == null && declared.Checks.Count == 0)
            {
                AppendInventory(sb, summary);
            }

            sb.AppendLine("  // Tables whose names look like backups or dated copies.");
            sb.AppendLine("  \"exclude\": [");
            AppendItems(sb, excluded.Select(t => $"    {Json(t.Name)}"), excluded.Select(t => t.Reason));
            sb.AppendLine("  ],");
            sb.AppendLine();

            sb.AppendLine("  // For each table, the column that dates its rows, guessed from the column names. Confirm each one.");
            sb.AppendLine("  \"dateColumns\": {");
            AppendDateColumns(sb, tables.Where(t => !declared.DateColumns.ContainsKey(t.Name)).ToList());
            sb.AppendLine("  },");
            sb.AppendLine();

            sb.AppendLine("  \"checks\": [");
            var checks = RelationshipChecks(summary, tables, targets, declaredColumns)
                .Concat(PersonIdChecks(summary, tables.Select(t => t.Name).ToHashSet(StringComparer.Ordinal), declaredColumns)).ToList();
            AppendItems(sb, checks, checks.Select(_ => (string?)null));
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Every table and view, as a commented include to copy from: the schemas first when there are
        /// several, then each object with its approximate row count.
        /// </summary>
        private static void AppendInventory(StringBuilder sb, SchemaSummary summary)
        {
            var objects = summary.Tables.Select(t => (t.Name, Note: ResultSql.RowCount(t.ApproximateRowCount), IsView: false))
                .Concat(summary.Views.Select(v => (v.Name, Note: "view", IsView: true)))
                .OrderBy(o => o.Name, StringComparer.Ordinal)
                .ToList();
            var schemas = objects.GroupBy(o => SchemaIndex.SchemaOf(o.Name), StringComparer.Ordinal).ToList();

            sb.AppendLine("  // No include yet. If this database holds more than the migration moves (other apps' tables, or sensitive");
            sb.AppendLine("  // records it does not need), copy an include into analysis.json naming the schemas, tables, and views to");
            sb.AppendLine("  // check, then run this again so the guesses below cover only those. Row counts are approximate.");
            sb.AppendLine("  // \"include\": [");
            if (schemas.Count > 1)
            {
                foreach (var schema in schemas)
                {
                    var tables = schema.Count(o => !o.IsView);
                    var views = schema.Count(o => o.IsView);
                    sb.AppendLine($"  //   {Json(schema.Key)}, // {tables:N0} {(tables == 1 ? "table" : "tables")}, {views:N0} {(views == 1 ? "view" : "views")}");
                }
            }
            foreach (var o in objects)
            {
                sb.AppendLine($"  //   {Json(o.Name)}, // {o.Note}");
            }
            sb.AppendLine("  // ],");
            sb.AppendLine();
        }

        /// <summary>
        /// The (check, table, column) of every column the declarations already check, so a guess they cover
        /// is not suggested again. Read from the raw entries, which have been checked only as JSON.
        /// </summary>
        private static HashSet<(string Check, string Table, string Column)> DeclaredColumns(Declarations declared)
        {
            static string? Text(JsonElement element, string name) =>
                element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            var found = new HashSet<(string, string, string)>();
            foreach (var entry in declared.Checks.Where(e => e.ValueKind == JsonValueKind.Object))
            {
                if (Text(entry, "check") is not { } check || Text(entry, "table") is not { } table)
                {
                    continue;
                }
                var columns = entry.TryGetProperty("columns", out var list) && list.ValueKind == JsonValueKind.Array
                    ? list.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString()!)
                    : Text(entry, "column") is { } single ? [single] : [];
                found.UnionWith(columns.Select(column => (check, table, column)));
            }
            return found;
        }

        /// <summary>
        /// Writes items separated by commas, each followed by its comment, if any.
        /// </summary>
        private static void AppendItems(StringBuilder sb, IEnumerable<string> items, IEnumerable<string?> comments)
        {
            var list = items.Zip(comments).ToList();
            if (list.Count == 0)
            {
                sb.AppendLine("    // Nothing new.");
            }
            for (var i = 0; i < list.Count; i++)
            {
                var (item, comment) = list[i];
                sb.AppendLine($"{item}{(i < list.Count - 1 ? "," : "")}{(comment != null ? $" // {comment}" : "")}");
            }
        }

        private static string? BackupReason(string tableName)
        {
            var name = ObjectName(tableName);
            var word = PersonIdRules.Words(name).Find(BackupWords.Contains);
            var date = DateInName.IsMatch(name);
            return (word, date) switch
            {
                (not null, true) => $"name word \"{word}\" and a date",
                (not null, false) => $"name word \"{word}\"",
                (null, true) => "a date in the name",
                _ => null
            };
        }

        /// <summary>
        /// One entry per table whose date columns include one named for creation, else for modification,
        /// else its only date column. Tables with several and no such name get a comment listing them.
        /// </summary>
        private static void AppendDateColumns(StringBuilder sb, List<TableSummary> tables)
        {
            var entries = new List<(string Line, string? Comment)>();
            var undecided = new List<string>();
            foreach (var table in tables)
            {
                var dates = table.Columns.Where(c => !c.IsComputed && DateTypes.Contains(c.Type.Split('(')[0])).Select(c => c.Name).ToList();
                if (dates.Count == 0)
                {
                    continue;
                }

                var created = FirstByPreference(dates, CreatedWords);
                var modified = FirstByPreference(dates, ModifiedWords);
                var (pick, reason) = created != null ? (created, "named for creation")
                    : modified != null ? (modified, "named for modification")
                    : dates.Count == 1 ? (dates[0], "the only date column")
                    : (null, null);
                if (pick == null)
                {
                    undecided.Add($"{table.Name}: {string.Join(", ", dates)}");
                    continue;
                }

                var others = dates.Where(d => d != pick).ToList();
                entries.Add(($"    {Json(table.Name)}: {Json(pick)}", others.Count > 0 ? $"{reason}; others: {string.Join(", ", others)}" : reason));
            }

            AppendItems(sb, entries.Select(e => e.Line), entries.Select(e => e.Comment));
            if (undecided.Count > 0)
            {
                sb.AppendLine("    // Several date columns, none named for creation or modification; pick one if any dates the rows:");
                foreach (var line in undecided)
                {
                    sb.AppendLine($"    //   {line}");
                }
            }
        }

        /// <summary>
        /// The first column with a name word in the most preferred group of forms that any column has.
        /// </summary>
        private static string? FirstByPreference(List<string> columns, string[][] groups) =>
            groups
                .Select(forms => columns.Find(c => PersonIdRules.Words(c).Exists(w => IsWordFor(w, forms))))
                .FirstOrDefault(c => c != null);

        /// <summary>
        /// A column named exactly like another table's single-column primary key, with no foreign key
        /// already on it, as a valuesMustExistIn check. Generic key names (ID) and ambiguous matches are
        /// left out.
        /// </summary>
        private static IEnumerable<string> RelationshipChecks(SchemaSummary summary, List<TableSummary> tables, List<TableSummary> targetTables,
            HashSet<(string Check, string Table, string Column)> declared)
        {
            var keyed = targetTables
                .Where(t => t.PrimaryKeyColumns.Count == 1 && !PersonIdRules.Words(t.PrimaryKeyColumns[0]).TrueForAll(GenericKeyWords.Contains))
                .GroupBy(t => t.PrimaryKeyColumns[0], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var foreignKeyed = summary.ForeignKeys.SelectMany(fk => fk.Columns.Select(c => (fk.Table, Column: c))).ToHashSet();

            foreach (var table in tables)
            {
                foreach (var column in table.Columns.Where(c => !table.PrimaryKeyColumns.Contains(c.Name) && !foreignKeyed.Contains((table.Name, c.Name))
                                                                && !declared.Contains(("valuesMustExistIn", table.Name, c.Name))))
                {
                    if (!keyed.TryGetValue(column.Name, out var targets))
                    {
                        continue;
                    }
                    var matches = targets
                        .Where(t => t.Name != table.Name)
                        .Select(t => (Table: t, Key: t.Columns.First(c => c.Name == t.PrimaryKeyColumns[0])))
                        .Where(t => SameKind(column, t.Key))
                        .ToList();
                    if (matches.Count != 1)
                    {
                        continue;
                    }

                    var (target, key) = matches[0];
                    yield return Check(
                        ("check", "valuesMustExistIn"),
                        ("description", $"Guessed: {column.Name} is named like the primary key of {target.Name}. Confirm the relationship before keeping."),
                        ("table", table.Name),
                        ("columns", new[] { column.Name }),
                        ("otherTable", target.Name),
                        ("otherColumns", new[] { key.Name }));
                }
            }
        }

        private static bool SameKind(ColumnSummary a, ColumnSummary b) => Kind(a) != null && Kind(a) == Kind(b);

        private static string? Kind(ColumnSummary column) => column.Type.Split('(')[0] switch
        {
            "char" or "varchar" or "nchar" or "nvarchar" => "text",
            "tinyint" or "smallint" or "int" or "bigint" or "decimal" or "numeric" => "number",
            _ => null
        };

        /// <summary>
        /// The schema export's person ID columns in the given tables (views repeat their tables' columns),
        /// as identifiesOnePerson checks against VIPER's users.Person, or AAUD's aaudUser for an ID type
        /// users.Person does not hold (PIDM).
        /// </summary>
        private static IEnumerable<string> PersonIdChecks(SchemaSummary summary, HashSet<string> tableNames,
            HashSet<(string Check, string Table, string Column)> declared)
        {
            const string viper = "VIPER.users.Person";
            var inViper = IdentifiesOnePersonCheck.LookupTables[viper].Columns;
            return summary.PersonIdColumns
                .Where(p => p.Category == PersonIdCategory.IdType && p.IdType != null && PersonIdType.Declarable.ContainsKey(p.IdType)
                            && tableNames.Contains(p.Object) && !declared.Contains(("identifiesOnePerson", p.Object, p.Column)))
                .OrderBy(p => p.Object, StringComparer.Ordinal).ThenBy(p => p.Column, StringComparer.Ordinal)
                .Select(p => Check(
                    ("check", "identifiesOnePerson"),
                    ("description", $"Guessed from the {p.Reason}. Every {p.Column} should be a person the migration can link to."),
                    ("table", p.Object),
                    ("column", p.Column),
                    ("idType", p.IdType!),
                    ("lookIn", inViper.ContainsKey(p.IdType!) ? viper : "AAUD.dbo.aaudUser")));
        }

        private static string Check(params (string Name, object Value)[] fields)
        {
            var lines = fields.Select(f => $"      {Json(f.Name)}: {(f.Value is string s ? Json(s) : JsonSerializer.Serialize(f.Value, StringOptions))}");
            return $"    {{\n{string.Join(",\n", lines)}\n    }}";
        }

        private static string Json(string value) => JsonSerializer.Serialize(value, StringOptions);

        /// <summary>
        /// The object's own name from [schema].[name], with ]] unescaped.
        /// </summary>
        private static string ObjectName(string quoted)
        {
            var start = quoted.IndexOf("].[", StringComparison.Ordinal);
            var name = start >= 0 ? quoted[(start + 3)..] : quoted.TrimStart('[');
            return name.TrimEnd(']').Replace("]]", "]");
        }
    }
}
