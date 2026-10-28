using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Exports a legacy database's schema and object definitions: one .sql file per object plus a
    /// summary.json of keys, constraint trust, and dependencies. Reads only catalog views, never table
    /// rows; row counts come from partition metadata.
    ///
    /// Output: Output/{connection}/{environment}/, cleared on each run so a dropped object cannot
    /// linger as a stale file.
    /// </summary>
    public sealed class SchemaExport
    {
        private static readonly HashSet<string> ModuleTypes = new(StringComparer.Ordinal) { "V", "P", "FN", "IF", "TF", "TR" };
        private static readonly HashSet<string> ClrModuleTypes = new(StringComparer.Ordinal) { "PC", "FS", "FT", "TA" };

        // Constraints are scripted with their table; TT is the internal object behind a table type.
        private static readonly HashSet<string> HandledTypes = new(StringComparer.Ordinal)
        {
            "U", "TT", "SN", "PK", "UQ", "F", "C", "V", "P", "FN", "IF", "TF", "TR", "PC", "FS", "FT", "TA"
        };

        private static readonly Regex LinkedServerPattern = new(
            @"\b(OPENQUERY|OPENROWSET|OPENDATASOURCE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        // EXEC with parentheses runs a string; EXEC followed by a name calls a procedure, which SQL
        // Server records as a dependency.
        private static readonly Regex DynamicSqlPattern = new(
            @"\bsp_executesql\b|\bEXEC(UTE)?\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

        // SchemaCompare looks for this line to tell a hidden definition from a changed one.
        internal const string MissingDefinitionMarker = "-- Definition not available:";

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            NewLine = "\r\n",
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _connectionString;
        private readonly string _connectionName;
        private readonly string _environment;
        private readonly string _outputDir;

        private SchemaExport(string connectionString, string connectionName, string environment, string outputDir)
        {
            _connectionString = connectionString;
            _connectionName = connectionName;
            _environment = environment;
            _outputDir = outputDir;
        }

        public static int Run(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("LEGACY DATABASE SCHEMA EXPORT");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();

            try
            {
                var connectionName = LegacyMigrationScriptHelper.ValidateConnectionName(
                    LegacyMigrationScriptHelper.GetRequiredArg(args, "connection"));
                var environment = LegacyMigrationScriptHelper.RequireEnvironment();
                var configuration = LegacyMigrationScriptHelper.LoadConfiguration(environment);
                var connectionString = LegacyMigrationScriptHelper.GetConnectionString(configuration, connectionName, environment);
                var outputDir = OutputDir(connectionName, environment);

                Console.WriteLine();
                Console.WriteLine($"  Connection: {connectionName} ({environment})");
                Console.WriteLine($"  Database:   {LegacyMigrationScriptHelper.GetServerAndDatabase(connectionString)}");
                Console.WriteLine($"  Output:     {outputDir}");
                Console.WriteLine();

                new SchemaExport(connectionString, connectionName, environment, outputDir).Export();
                LegacyMigrationScriptHelper.WriteFinished(timer);
                return 0;
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

        internal static string OutputDir(string connectionName, string environment) =>
            Path.Join(LegacyMigrationScriptHelper.GetToolRoot(), "Output", connectionName, environment);

        /// <summary>
        /// Exports and returns the summary it wrote. The analysis calls this before every run, so its
        /// checks always match the database as it is now.
        /// </summary>
        internal static SchemaSummary Export(string connectionString, string connectionName, string environment) =>
            new SchemaExport(connectionString, connectionName, environment, OutputDir(connectionName, environment)).Export();

        private SchemaSummary Export()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            Console.WriteLine("Reading catalog...");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var catalog = Catalog.Load(conn);
            var catalogTime = timer.Elapsed;

            var summary = WriteFiles(catalog);
            Console.WriteLine($"  Catalog read in {LegacyMigrationScriptHelper.Duration(catalogTime)}; " +
                              $"files written in {LegacyMigrationScriptHelper.Duration(timer.Elapsed - catalogTime)}.");
            return summary;
        }

        /// <summary>
        /// Writes the export of a catalog already read, for tests: everything Export does after Catalog.Load.
        /// </summary>
        internal static SchemaSummary Write(Catalog catalog, string connectionName, string environment, string outputDir) =>
            new SchemaExport("", connectionName, environment, outputDir).WriteFiles(catalog);

        private SchemaSummary WriteFiles(Catalog catalog)
        {
            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, recursive: true);
            }
            Directory.CreateDirectory(_outputDir);

            var summary = new SchemaSummary
            {
                Connection = _connectionName,
                Environment = _environment,
                Server = catalog.Info.Server,
                Database = catalog.Info.Database,
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ProductVersion = catalog.Info.ProductVersion,
                CompatibilityLevel = catalog.Info.CompatibilityLevel,
                DatabaseCollation = catalog.Info.Collation,
                HasViewDefinition = catalog.Info.HasViewDefinition,
                ReferencesError = catalog.ReferencesError,
                ExcludedToolObjects = catalog.Objects.Where(o => o.IsToolObject)
                    .Select(o => $"{Qualified(o)} ({o.TypeDesc})").ToList(),
                UnexportedObjects = catalog.Objects
                    .Where(o => !o.IsToolObject && !HandledTypes.Contains(o.Type) && !(o.Type == "D" && o.ParentObjectId != 0))
                    .Select(o => $"{Qualified(o)} ({o.TypeDesc})").ToList()
            };

            ExportTables(catalog, summary);
            ExportTableTypesAndAliases(catalog, summary);
            ExportModules(catalog, summary);
            ExportSynonyms(catalog, summary);
            AddDependencies(catalog, summary);
            AddViewColumns(catalog, summary);
            summary.PersonIdColumns.AddRange(PersonIdRules.Find(summary));

            LegacyMigrationScriptHelper.WriteCrlfFile(
                Path.Join(_outputDir, "summary.json"), JsonSerializer.Serialize(summary, JsonOptions));

            DisplaySummary(summary);
            return summary;
        }

        #region Tables

        private void ExportTables(Catalog catalog, SchemaSummary summary)
        {
            Console.WriteLine("Exporting tables...");
            var folder = new OutputFolder(_outputDir, "Tables");

            foreach (var table in catalog.Objects.Where(o => o.Type == "U" && !o.IsToolObject))
            {
                var indexes = OrderedIndexes(catalog.IndexesByObject.GetValueOrDefault(table.ObjectId, []), catalog);
                var foreignKeys = InStableOrder(catalog.ForeignKeysByParent.GetValueOrDefault(table.ObjectId, []),
                    fk => fk.IsSystemNamed, fk => ForeignKeyContent(fk, catalog));
                var checks = InStableOrder(catalog.ChecksByParent.GetValueOrDefault(table.ObjectId, []),
                    c => c.IsSystemNamed, c => c.Definition);
                var hasPrimaryKey = indexes.Any(i => i.IsPrimaryKey);
                // As for UniqueKeys: a filtered unique index guarantees uniqueness only within its filter.
                var hasUniqueKey = indexes.Any(i => i.IsUnique && !i.IsDisabled && i.FilterDefinition == null);
                var qualified = Qualified(table);

                folder.Write(table.Schema, table.Name, ScriptTable(table, catalog, indexes, foreignKeys, checks, hasPrimaryKey));

                summary.Tables.Add(new TableSummary(
                    qualified,
                    catalog.RowCounts.GetValueOrDefault(table.ObjectId),
                    hasPrimaryKey,
                    hasUniqueKey,
                    catalog.TriggerCountByParent.GetValueOrDefault(table.ObjectId),
                    indexes.Where(i => i.IsPrimaryKey).Select(i => KeyColumnNames(i, catalog)).FirstOrDefault() ?? [],
                    indexes.Where(i => i.IsUnique && !i.IsPrimaryKey && !i.IsDisabled && i.FilterDefinition == null)
                        .Select(i => KeyColumnNames(i, catalog)).ToList(),
                    catalog.ColumnsByObject.GetValueOrDefault(table.ObjectId, []).Select(SummarizeColumn).ToList()));

                if (!hasPrimaryKey)
                {
                    summary.TablesWithoutPrimaryKey.Add(qualified);
                }
                if (!hasUniqueKey)
                {
                    summary.TablesWithoutUniqueKey.Add(qualified);
                }

                summary.ForeignKeys.AddRange(foreignKeys.Select(fk => SummarizeForeignKey(fk, qualified, catalog)));
                summary.CheckConstraints.AddRange(checks.Select(c => new CheckConstraintSummary(
                    c.Name, qualified, c.Definition, c.IsNotTrusted, c.IsDisabled, c.IsSystemNamed)));
            }

            summary.ObjectCounts["Tables"] = folder.Count;
        }

        /// <summary>
        /// A table's constraints in an order every environment shares. The catalog lists them by name, but a
        /// system-generated name ends in a suffix that differs per database, so the same unnamed constraints
        /// would come out in a different order in Test and Production and compare as changed. Named ones keep
        /// the catalog's order; system-named ones follow, ordered by what they enforce.
        /// </summary>
        private static List<T> InStableOrder<T>(IEnumerable<T> items, Func<T, bool> isSystemNamed, Func<T, string> content) =>
            items.OrderBy(isSystemNamed).ThenBy(i => isSystemNamed(i) ? content(i) : "", StringComparer.Ordinal).ToList();

        /// <summary>
        /// Indexes with the primary key first, then as InStableOrder: only unique constraints can be
        /// system-named, since CREATE INDEX requires a name.
        /// </summary>
        private static List<IndexInfo> OrderedIndexes(IEnumerable<IndexInfo> indexes, Catalog catalog) =>
            InStableOrder(indexes, i => i.IsSystemNamed, i => KeyColumnList(i, catalog))
                .OrderByDescending(i => i.IsPrimaryKey).ToList();

        private static string ForeignKeyContent(ForeignKeyInfo fk, Catalog catalog)
        {
            var columns = catalog.ForeignKeyColumns.GetValueOrDefault(fk.ObjectId, []);
            return $"{string.Join(",", columns.Select(c => c.ParentColumn))} {fk.RefSchema}.{fk.RefTable} " +
                   string.Join(",", columns.Select(c => c.RefColumn));
        }

        private static ForeignKeySummary SummarizeForeignKey(ForeignKeyInfo fk, string qualified, Catalog catalog)
        {
            var columns = catalog.ForeignKeyColumns.GetValueOrDefault(fk.ObjectId, []);
            return new ForeignKeySummary(
                fk.Name, qualified, columns.Select(c => c.ParentColumn).ToList(),
                $"{Quote(fk.RefSchema)}.{Quote(fk.RefTable)}", columns.Select(c => c.RefColumn).ToList(),
                fk.IsNotTrusted, fk.IsDisabled, fk.IsSystemNamed, fk.DeleteAction, fk.UpdateAction);
        }

        private static ColumnSummary SummarizeColumn(ColumnInfo col) => new(
            col.Name,
            FormatType(col.SystemTypeName, "sys", false, col.MaxLength, col.Precision, col.Scale),
            col.IsUserDefinedType ? $"{Quote(col.TypeSchema)}.{Quote(col.TypeName)}" : null,
            TextLength(col.SystemTypeName, col.MaxLength),
            col.IsNullable,
            col.ComputedDefinition != null,
            col.CollationName,
            col.IsIdentity ? new IdentitySummary(col.IdentitySeed ?? "", col.IdentityIncrement ?? "", col.IdentityLastValue) : null);

        private static int? TextLength(string systemTypeName, short maxLength) => systemTypeName switch
        {
            "varchar" or "char" => maxLength,
            "nvarchar" or "nchar" => maxLength == -1 ? -1 : maxLength / 2,
            "text" or "ntext" => -1,
            _ => null
        };

        private static List<string> KeyColumnNames(IndexInfo index, Catalog catalog) =>
            catalog.IndexColumns.GetValueOrDefault((index.ObjectId, index.IndexId), [])
                .Where(c => !c.IsIncluded).Select(c => c.Name).ToList();

        private static string ScriptTable(DbObject table, Catalog catalog, List<IndexInfo> indexes,
            List<ForeignKeyInfo> foreignKeys, List<CheckInfo> checks, bool hasPrimaryKey)
        {
            var qualified = Qualified(table);
            var sb = new StringBuilder();

            sb.AppendLine($"-- Table: {qualified}");
            if (!hasPrimaryKey)
            {
                sb.AppendLine("-- No primary key");
            }
            sb.AppendLine($"CREATE TABLE {qualified} (");
            sb.AppendLine(string.Join(",\n", ColumnAndKeyLines(table.ObjectId, catalog, indexes).Select(l => "    " + l)));
            sb.AppendLine(");");
            sb.AppendLine("GO");

            foreach (var check in checks)
            {
                sb.AppendLine();
                AppendTrustComment(sb, check.IsNotTrusted, check.IsDisabled);
                var name = check.IsSystemNamed ? "" : $"CONSTRAINT {Quote(check.Name)} ";
                var nfr = check.IsNotForReplication ? " NOT FOR REPLICATION" : "";
                sb.AppendLine($"ALTER TABLE {qualified} {CheckClause(check.IsNotTrusted)} ADD {name}CHECK{nfr} {check.Definition};");
                AppendDisable(sb, qualified, check.Name, check.IsDisabled, check.IsSystemNamed);
                sb.AppendLine("GO");
            }

            foreach (var fk in foreignKeys)
            {
                var columns = catalog.ForeignKeyColumns.GetValueOrDefault(fk.ObjectId, []);
                var name = fk.IsSystemNamed ? "" : $"CONSTRAINT {Quote(fk.Name)} ";
                var parentColumns = string.Join(", ", columns.Select(c => Quote(c.ParentColumn)));
                var refColumns = string.Join(", ", columns.Select(c => Quote(c.RefColumn)));

                sb.AppendLine();
                AppendTrustComment(sb, fk.IsNotTrusted, fk.IsDisabled);
                sb.Append($"ALTER TABLE {qualified} {CheckClause(fk.IsNotTrusted)} ADD {name}FOREIGN KEY ({parentColumns}) ");
                sb.Append($"REFERENCES {Quote(fk.RefSchema)}.{Quote(fk.RefTable)} ({refColumns})");
                if (fk.DeleteAction != "NO_ACTION")
                {
                    sb.Append($" ON DELETE {fk.DeleteAction.Replace('_', ' ')}");
                }
                if (fk.UpdateAction != "NO_ACTION")
                {
                    sb.Append($" ON UPDATE {fk.UpdateAction.Replace('_', ' ')}");
                }
                if (fk.IsNotForReplication)
                {
                    sb.Append(" NOT FOR REPLICATION");
                }
                sb.AppendLine(";");
                AppendDisable(sb, qualified, fk.Name, fk.IsDisabled, fk.IsSystemNamed);
                sb.AppendLine("GO");
            }

            foreach (var index in indexes.Where(i => !i.IsPrimaryKey && !i.IsUniqueConstraint))
            {
                sb.AppendLine();
                sb.AppendLine(ScriptIndex(qualified, index, catalog));
                if (index.IsDisabled)
                {
                    sb.AppendLine($"ALTER INDEX {Quote(index.Name)} ON {qualified} DISABLE;");
                }
                sb.AppendLine("GO");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Column definitions followed by PRIMARY KEY / UNIQUE constraints, for both tables and table types.
        /// </summary>
        private static List<string> ColumnAndKeyLines(int objectId, Catalog catalog, List<IndexInfo> indexes)
        {
            var lines = catalog.ColumnsByObject.GetValueOrDefault(objectId, [])
                .Select(c => ScriptColumn(c, catalog.Info.Collation))
                .ToList();
            lines.AddRange(indexes.Where(i => i.IsPrimaryKey || i.IsUniqueConstraint).Select(k => KeyConstraintLine(k, catalog)));
            return lines;
        }

        private static string KeyConstraintLine(IndexInfo key, Catalog catalog)
        {
            var name = key.IsSystemNamed ? "" : $"CONSTRAINT {Quote(key.Name)} ";
            var kind = key.IsPrimaryKey ? "PRIMARY KEY" : "UNIQUE";
            var options = key.IgnoreDupKey ? " WITH (IGNORE_DUP_KEY = ON)" : "";
            // Key indexes are not in the ALTER INDEX ... DISABLE loop, and a system-named one has no stable name
            // to disable by, so the state is marked here.
            var disabled = key.IsDisabled ? "/* DISABLED index */ " : "";
            return $"{disabled}{name}{kind} {ClusterKeyword(key.Type)} ({KeyColumnList(key, catalog)}){options}";
        }

        private static string ScriptColumn(ColumnInfo col, string databaseCollation)
        {
            if (col.ComputedDefinition != null)
            {
                return $"{Quote(col.Name)} AS {col.ComputedDefinition}{(col.IsPersisted ? " PERSISTED" : "")}";
            }

            var sb = new StringBuilder($"{Quote(col.Name)} {FormatType(col.TypeName, col.TypeSchema, col.IsUserDefinedType, col.MaxLength, col.Precision, col.Scale)}");

            // Only a collation that differs from the database default is worth calling out: it changes
            // how the column compares and joins.
            if (col.CollationName != null && !col.IsUserDefinedType && col.CollationName != databaseCollation)
            {
                sb.Append($" COLLATE {col.CollationName}");
            }
            if (col.IsSparse)
            {
                sb.Append(" SPARSE");
            }
            if (col.IsIdentity)
            {
                sb.Append($" IDENTITY({col.IdentitySeed},{col.IdentityIncrement})");
                if (col.IdentityNotForReplication)
                {
                    sb.Append(" NOT FOR REPLICATION");
                }
            }
            if (col.IsRowGuidCol)
            {
                sb.Append(" ROWGUIDCOL");
            }
            sb.Append(col.IsNullable ? " NULL" : " NOT NULL");
            if (col.DefaultDefinition != null)
            {
                var name = col.DefaultIsSystemNamed ? "" : $" CONSTRAINT {Quote(col.DefaultName!)}";
                sb.Append($"{name} DEFAULT {col.DefaultDefinition}");
            }

            return sb.ToString();
        }

        private static string ScriptIndex(string qualified, IndexInfo index, Catalog catalog)
        {
            // Only rowstore indexes are scripted; XML, spatial, and columnstore indexes are noted so the
            // export still shows they exist.
            if (index.Type is not (1 or 2))
            {
                return $"-- {index.TypeDesc} index {Quote(index.Name)} on ({KeyColumnList(index, catalog)}) is not scripted";
            }

            var sb = new StringBuilder("CREATE ");
            if (index.IsUnique)
            {
                sb.Append("UNIQUE ");
            }
            sb.Append($"{ClusterKeyword(index.Type)} INDEX {Quote(index.Name)} ON {qualified} ({KeyColumnList(index, catalog)})");

            var included = catalog.IndexColumns.GetValueOrDefault((index.ObjectId, index.IndexId), [])
                .Where(c => c.IsIncluded).Select(c => Quote(c.Name)).ToList();
            if (included.Count > 0)
            {
                sb.Append($" INCLUDE ({string.Join(", ", included)})");
            }
            if (index.FilterDefinition != null)
            {
                sb.Append($" WHERE {index.FilterDefinition}");
            }
            if (index.IgnoreDupKey)
            {
                sb.Append(" WITH (IGNORE_DUP_KEY = ON)");
            }
            sb.Append(';');

            return sb.ToString();
        }

        private static string KeyColumnList(IndexInfo index, Catalog catalog)
        {
            return string.Join(", ", catalog.IndexColumns.GetValueOrDefault((index.ObjectId, index.IndexId), [])
                .Where(c => !c.IsIncluded)
                .Select(c => $"{Quote(c.Name)} {(c.IsDescending ? "DESC" : "ASC")}"));
        }

        private static string ClusterKeyword(byte indexType) => indexType == 1 ? "CLUSTERED" : "NONCLUSTERED";

        private static string CheckClause(bool isNotTrusted) => isNotTrusted ? "WITH NOCHECK" : "WITH CHECK";

        private static void AppendTrustComment(StringBuilder sb, bool isNotTrusted, bool isDisabled)
        {
            if (isDisabled)
            {
                sb.AppendLine("-- DISABLED: not enforced for new rows, and existing rows were never validated");
            }
            else if (isNotTrusted)
            {
                sb.AppendLine("-- UNTRUSTED: enforced for new rows, but existing rows were never validated");
            }
        }

        private static void AppendDisable(StringBuilder sb, string qualified, string name, bool isDisabled, bool isSystemNamed)
        {
            if (!isDisabled)
            {
                return;
            }

            // A system-generated name differs between environments, so it is left out of the DDL to
            // keep exports comparable; summary.json still records it.
            sb.AppendLine(isSystemNamed
                ? "-- (system-named constraint; NOCHECK CONSTRAINT omitted)"
                : $"ALTER TABLE {qualified} NOCHECK CONSTRAINT {Quote(name)};");
        }

        private static string FormatType(string typeName, string typeSchema, bool isUserDefined, short maxLength, byte precision, byte scale)
        {
            if (isUserDefined)
            {
                return $"{Quote(typeSchema)}.{Quote(typeName)}";
            }

            return typeName switch
            {
                "varchar" or "char" or "varbinary" or "binary" => $"{typeName}({(maxLength == -1 ? "max" : maxLength.ToString())})",
                "nvarchar" or "nchar" => $"{typeName}({(maxLength == -1 ? "max" : (maxLength / 2).ToString())})",
                "decimal" or "numeric" => $"{typeName}({precision},{scale})",
                "datetime2" or "time" or "datetimeoffset" => $"{typeName}({scale})",
                _ => typeName
            };
        }

        #endregion

        #region Types, modules, synonyms

        private void ExportTableTypesAndAliases(Catalog catalog, SchemaSummary summary)
        {
            Console.WriteLine("Exporting user-defined types...");
            var folder = new OutputFolder(_outputDir, "Types");

            foreach (var type in catalog.Types)
            {
                var qualified = $"{Quote(type.Schema)}.{Quote(type.Name)}";
                string script;

                if (type.IsTableType && type.TableObjectId is { } tableObjectId)
                {
                    var lines = ColumnAndKeyLines(tableObjectId, catalog,
                        OrderedIndexes(catalog.IndexesByObject.GetValueOrDefault(tableObjectId, []), catalog));
                    lines.AddRange(InStableOrder(catalog.ChecksByParent.GetValueOrDefault(tableObjectId, []), c => c.IsSystemNamed, c => c.Definition)
                        .Select(c => $"CHECK {c.Definition}"));
                    script = $"CREATE TYPE {qualified} AS TABLE (\n{string.Join(",\n", lines.Select(l => "    " + l))}\n);\nGO\n";
                }
                else if (type.IsAssemblyType)
                {
                    script = $"-- CLR type {qualified} is not scripted\n";
                }
                else
                {
                    var baseType = FormatType(type.BaseTypeName ?? "", "sys", false, type.MaxLength, type.Precision, type.Scale);
                    script = $"CREATE TYPE {qualified} FROM {baseType} {(type.IsNullable ? "NULL" : "NOT NULL")};\nGO\n";
                }

                folder.Write(type.Schema, type.Name, script);
            }

            summary.ObjectCounts["Types"] = folder.Count;
        }

        private void ExportModules(Catalog catalog, SchemaSummary summary)
        {
            Console.WriteLine("Exporting views, procedures, functions, and triggers...");
            var folders = new Dictionary<string, OutputFolder>
            {
                ["Views"] = new(_outputDir, "Views"),
                ["StoredProcedures"] = new(_outputDir, "StoredProcedures"),
                ["Functions"] = new(_outputDir, "Functions"),
                ["Triggers"] = new(_outputDir, "Triggers")
            };
            var objectsById = catalog.Objects.ToDictionary(o => o.ObjectId);

            foreach (var obj in catalog.Objects.Where(o => !o.IsToolObject && (ModuleTypes.Contains(o.Type) || ClrModuleTypes.Contains(o.Type))))
            {
                var folder = folders[ModuleFolder(obj.Type)];
                catalog.Modules.TryGetValue(obj.ObjectId, out var module);

                var onTable = obj.Type is "TR" or "TA" && objectsById.TryGetValue(obj.ParentObjectId, out var parent)
                    ? $"-- On table: {Qualified(parent)}\n"
                    : "";
                var disabled = catalog.DisabledTriggers.Contains(obj.ObjectId) ? "-- DISABLED trigger\n" : "";
                var header = $"-- {obj.TypeDesc}: {Qualified(obj)}\n{onTable}{disabled}";

                folder.Write(obj.Schema, obj.Name, ScriptModule(header, Qualified(obj), obj.TypeDesc, module, ClrModuleTypes.Contains(obj.Type), summary));
                RecordLinkedServerMentions(Qualified(obj), module?.Definition, summary);
            }

            foreach (var trigger in catalog.DatabaseTriggers)
            {
                var header = $"-- DATABASE DDL TRIGGER: {Quote(trigger.Name)}\n{(trigger.IsDisabled ? "-- DISABLED trigger\n" : "")}";
                folders["Triggers"].Write("Database", trigger.Name, ScriptModule(header, Quote(trigger.Name), "DATABASE_DDL_TRIGGER", trigger.Module, false, summary));
                RecordLinkedServerMentions(Quote(trigger.Name), trigger.Module.Definition, summary);
            }

            foreach (var (name, folder) in folders)
            {
                summary.ObjectCounts[name] = folder.Count;
            }
        }

        private static string ModuleFolder(string type) => type switch
        {
            "V" => "Views",
            "P" or "PC" => "StoredProcedures",
            "TR" or "TA" => "Triggers",
            _ => "Functions"
        };

        private static string ScriptModule(string header, string qualified, string typeDesc, ModuleInfo? module, bool isClr, SchemaSummary summary)
        {
            if (module?.Definition == null)
            {
                var reason = isClr ? "CLR object; no T-SQL definition"
                    : module?.IsEncrypted == true ? "created WITH ENCRYPTION"
                    : "definition not visible; the login may lack VIEW DEFINITION";
                summary.MissingDefinitions.Add(new MissingDefinition(qualified, typeDesc, reason));
                return $"{header}{MissingDefinitionMarker} {reason}\n";
            }

            // SSMS scripts these settings too: they are captured at create time and change how the
            // module behaves, and old databases often have modules created with them OFF.
            return $"{header}SET ANSI_NULLS {(module.UsesAnsiNulls ? "ON" : "OFF")}\nGO\n" +
                   $"SET QUOTED_IDENTIFIER {(module.UsesQuotedIdentifier ? "ON" : "OFF")}\nGO\n" +
                   $"{module.Definition.Trim()}\nGO\n";
        }

        private static void RecordLinkedServerMentions(string qualified, string? definition, SchemaSummary summary)
        {
            if (definition == null)
            {
                return;
            }

            var keywords = LinkedServerPattern.Matches(definition)
                .Select(m => m.Value.ToUpperInvariant()).Distinct().OrderBy(k => k).ToList();
            if (keywords.Count > 0)
            {
                summary.LinkedServerMentions.Add(new LinkedServerMention(qualified, keywords));
            }
        }

        private void ExportSynonyms(Catalog catalog, SchemaSummary summary)
        {
            Console.WriteLine("Exporting synonyms...");
            var folder = new OutputFolder(_outputDir, "Synonyms");

            foreach (var synonym in catalog.Synonyms)
            {
                var qualified = $"{Quote(synonym.Schema)}.{Quote(synonym.Name)}";
                folder.Write(synonym.Schema, synonym.Name, $"CREATE SYNONYM {qualified} FOR {synonym.BaseObjectName};\nGO\n");

                if (synonym.Server != null || (synonym.Database != null && !synonym.Database.Equals(catalog.Info.Database, StringComparison.OrdinalIgnoreCase)))
                {
                    summary.ExternalReferences.Add(new ExternalReference(
                        qualified, synonym.Server, synonym.Database, synonym.ReferencedSchema, synonym.ReferencedName ?? synonym.BaseObjectName));
                }
            }

            summary.ObjectCounts["Synonyms"] = folder.Count;
        }

        private static void AddViewColumns(Catalog catalog, SchemaSummary summary)
        {
            summary.Views.AddRange(catalog.Objects.Where(o => o.Type == "V" && !o.IsToolObject)
                .Select(view => new ViewSummary(Qualified(view),
                    catalog.ColumnsByObject.GetValueOrDefault(view.ObjectId, []).Select(SummarizeColumn).ToList())));
        }

        private static void AddDependencies(Catalog catalog, SchemaSummary summary)
        {
            var toolObjectIds = catalog.Objects.Where(o => o.IsToolObject).Select(o => o.ObjectId).ToHashSet();

            summary.ExternalReferences.AddRange(catalog.ExternalReferences
                .Where(r => !toolObjectIds.Contains(r.ReferencingId)).Select(r => r.Reference));
            summary.UnresolvedReferences.AddRange(catalog.UnresolvedReferences
                .Where(r => !toolObjectIds.Contains(r.ReferencingId)).Select(r => r.Reference));

            summary.ModuleDependencies.AddRange(catalog.Objects
                .Where(o => !o.IsToolObject && catalog.ModuleReferences.ContainsKey(o.ObjectId))
                .Select(m => SummarizeModule(Qualified(m), m.TypeDesc, catalog.ModuleReferences[m.ObjectId],
                    catalog.Modules.GetValueOrDefault(m.ObjectId)?.Definition, catalog)));
            summary.ModuleDependencies.AddRange(catalog.DatabaseTriggers
                .Select(t => SummarizeModule(Quote(t.Name), "DATABASE_DDL_TRIGGER", t.References, t.Module.Definition, catalog)));
        }

        private static ModuleDependency SummarizeModule(string qualified, string typeDesc, ModuleReferences read,
            string? definition, Catalog catalog) =>
            new(qualified, typeDesc, GroupReferences(read.References, catalog.Info.Database),
                definition != null && DynamicSqlPattern.IsMatch(definition), read.Error);

        /// <summary>
        /// One entry per referenced object, combining the rows SQL Server returns for the object itself
        /// and for each of its columns.
        /// </summary>
        private static List<ModuleReference> GroupReferences(List<EntityReference> references, string database) =>
            references
                .GroupBy(r => ReferenceName(r, database), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new ModuleReference(g.Key,
                    g.Any(r => r.IsSelected || r.IsSelectAll),
                    g.Any(r => r.IsUpdated || r.IsInsertAll),
                    g.Where(r => r.Column != null && r.IsUpdated).Select(r => r.Column!)
                        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.Ordinal).ToList()))
                .ToList();

        // A three-part name pointing back at this database is local, so it gets the local name.
        private static string ReferenceName(EntityReference reference, string database)
        {
            var name = reference.Schema != null ? $"{Quote(reference.Schema)}.{Quote(reference.Entity)}" : Quote(reference.Entity);
            if (reference.Server == null && (reference.Database == null || reference.Database.Equals(database, StringComparison.OrdinalIgnoreCase)))
            {
                return name;
            }
            var qualified = $"{Quote(reference.Database ?? "")}.{(reference.Schema != null ? name : $".{name}")}";
            return reference.Server != null ? $"{Quote(reference.Server)}.{qualified}" : qualified;
        }

        #endregion

        private static void DisplaySummary(SchemaSummary summary)
        {
            int Count(string key) => summary.ObjectCounts.GetValueOrDefault(key);

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine($"EXPORT COMPLETE: {summary.Server}/{summary.Database} ({summary.Environment})");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"  Tables:             {Count("Tables")} ({summary.TablesWithoutPrimaryKey.Count} without a primary key, {summary.TablesWithoutUniqueKey.Count} without any unique key)");
            Console.WriteLine($"  Views:              {Count("Views")}");
            Console.WriteLine($"  Stored procedures:  {Count("StoredProcedures")}");
            Console.WriteLine($"  Functions:          {Count("Functions")}");
            Console.WriteLine($"  Triggers:           {Count("Triggers")}");
            Console.WriteLine($"  Types:              {Count("Types")}");
            Console.WriteLine($"  Synonyms:           {Count("Synonyms")}");
            Console.WriteLine($"  Foreign keys:       {summary.ForeignKeys.Count} ({summary.ForeignKeys.Count(f => f.IsNotTrusted)} untrusted, {summary.ForeignKeys.Count(f => f.IsDisabled)} disabled)");
            Console.WriteLine($"  Check constraints:  {summary.CheckConstraints.Count} ({summary.CheckConstraints.Count(c => c.IsNotTrusted)} untrusted, {summary.CheckConstraints.Count(c => c.IsDisabled)} disabled)");

            var externalDatabases = summary.ExternalReferences
                .Select(r => r.Server != null ? $"{r.Server}.{r.Database}" : r.Database)
                .Where(d => d != null).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d).ToList();
            Console.WriteLine($"  External databases: {externalDatabases.Count}{(externalDatabases.Count > 0 ? $" ({string.Join(", ", externalDatabases)})" : "")}");
            Console.WriteLine($"  Linked-server calls (OPENQUERY etc.): {summary.LinkedServerMentions.Count} objects");
            Console.WriteLine($"  References to objects that may not exist: {summary.UnresolvedReferences.Count}");
            Console.WriteLine($"  Objects that change data: {summary.ModuleDependencies.Count(m => m.References.Exists(r => r.Writes))}; " +
                              $"using dynamic SQL: {summary.ModuleDependencies.Count(m => m.UsesDynamicSql)}; " +
                              $"dependencies not readable: {summary.ModuleDependencies.Count(m => m.Error != null)}");
            Console.WriteLine($"  Possible person ID columns: {summary.PersonIdColumns.Count(c => c.Category == PersonIdCategory.IdType)} " +
                              $"by ID type, {summary.PersonIdColumns.Count(c => c.Category == PersonIdCategory.Review)} for review");
            Console.WriteLine($"  Excluded tool objects (dt_*, diagram support): {summary.ExcludedToolObjects.Count}");

            var warnings = new List<string>();
            if (!summary.HasViewDefinition)
            {
                warnings.Add("The login lacks VIEW DEFINITION. Objects it has no permission on are invisible to the catalog, so they are missing from this export entirely and cannot be listed.");
            }
            if (summary.ReferencesError != null)
            {
                warnings.Add("External databases and references to objects that may not exist could not be read, so both are empty: " +
                             $"{summary.ReferencesError} A permission error means the login lacks VIEW DEFINITION on the database " +
                             "or SELECT on sys.sql_expression_dependencies.");
            }
            if (summary.MissingDefinitions.Count > 0)
            {
                warnings.Add($"{summary.MissingDefinitions.Count} objects were exported without a definition (see missingDefinitions in summary.json).");
            }
            if (summary.UnexportedObjects.Count > 0)
            {
                warnings.Add($"{summary.UnexportedObjects.Count} objects are of a type this export does not script (see unexportedObjects in summary.json).");
            }

            if (warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine("WARNINGS:");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"  - {warning}");
                }
                Console.ResetColor();
            }
        }

        internal static string Quote(string identifier) => $"[{identifier.Replace("]", "]]")}]";

        private static string Qualified(DbObject obj) => $"{Quote(obj.Schema)}.{Quote(obj.Name)}";

        // Characters Windows forbids in a file name, plus the separator and the escape itself. A fixed list rather
        // than Path.GetInvalidFileNameChars, which differs on Linux, so every platform names a file the same way.
        private const string FileNameEscapes = "%.<>:\"/\\|?*";

        /// <summary>
        /// One part of an object's file name, schema or name: each character a file name cannot hold, and the
        /// dot that separates the parts, written as %XX, so [a].[b.c] and [a.b].[c] get different files and an
        /// object's file name never depends on which other objects exist. A trailing space, which Windows
        /// drops, is escaped too.
        /// </summary>
        internal static string FileNamePart(string part)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < part.Length; i++)
            {
                var ch = part[i];
                var escape = ch < ' ' || FileNameEscapes.Contains(ch) || (ch == ' ' && i == part.Length - 1);
                sb.Append(escape ? $"%{(int)ch:X2}" : ch.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// One folder of per-object files, named schema.name (see FileNamePart). Names differing only in case
        /// cannot share a folder on a case-insensitive file system, so the later one gets a numeric suffix.
        /// </summary>
        private sealed class OutputFolder
        {
            private readonly string _path;
            private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

            public OutputFolder(string root, string name)
            {
                _path = Path.Join(root, name);
            }

            public int Count => _used.Count;

            public void Write(string schema, string name, string content)
            {
                var baseName = $"{FileNamePart(schema)}.{FileNamePart(name)}";
                var fileName = baseName;
                for (var n = 2; !_used.Add(fileName); n++)
                {
                    fileName = $"{baseName}~{n}";
                }

                Directory.CreateDirectory(_path);
                LegacyMigrationScriptHelper.WriteCrlfFile(Path.Join(_path, fileName + ".sql"), content);
            }
        }
    }
}
