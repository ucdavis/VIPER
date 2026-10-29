using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration
{
    public sealed record DatabaseInfo(string Server, string Database, string ProductVersion, int CompatibilityLevel,
        string Collation, bool HasViewDefinition);

    /// <summary>
    /// A user object from sys.objects. Tool objects (dt_* procedures from SQL Server 2000-era Visual
    /// Studio, and anything SSMS tags as diagram/tools support) are kept so the summary can list what
    /// was excluded, rather than hiding them.
    /// </summary>
    public sealed record DbObject(int ObjectId, string Schema, string Name, string Type, string TypeDesc,
        int ParentObjectId, bool IsToolObject);

    /// <summary>
    /// SystemTypeName is the built-in type underneath an alias type (nvarchar for sysname), so checks
    /// can classify a column without resolving alias types themselves.
    /// </summary>
    public sealed record ColumnInfo(int ObjectId, string Name, string TypeName, string TypeSchema, bool IsUserDefinedType,
        short MaxLength, byte Precision, byte Scale, bool IsNullable, bool IsIdentity, string? IdentitySeed,
        string? IdentityIncrement, bool IdentityNotForReplication, string? ComputedDefinition, bool IsPersisted,
        string? CollationName, bool IsSparse, bool IsRowGuidCol, string? DefaultName, string? DefaultDefinition,
        bool DefaultIsSystemNamed, string SystemTypeName, string? IdentityLastValue);

    public sealed record IndexInfo(int ObjectId, int IndexId, string Name, byte Type, string TypeDesc, bool IsUnique,
        bool IsPrimaryKey, bool IsUniqueConstraint, bool IsSystemNamed, string? FilterDefinition, bool IsDisabled,
        bool IgnoreDupKey);

    public sealed record IndexColumn(string Name, bool IsDescending, bool IsIncluded);

    public sealed record CheckInfo(int ParentObjectId, string Name, string Definition, bool IsNotTrusted, bool IsDisabled,
        bool IsNotForReplication, bool IsSystemNamed);

    public sealed record ForeignKeyInfo(int ObjectId, int ParentObjectId, string Name, string RefSchema, string RefTable,
        bool IsNotTrusted, bool IsDisabled, bool IsNotForReplication, bool IsSystemNamed, string DeleteAction,
        string UpdateAction);

    public sealed record ForeignKeyColumn(string ParentColumn, string RefColumn);

    public sealed record ModuleInfo(string? Definition, bool UsesAnsiNulls, bool UsesQuotedIdentifier, bool IsEncrypted);

    public sealed record DatabaseTrigger(string Name, bool IsDisabled, ModuleInfo Module, ModuleReferences References);

    public sealed record TypeInfo(string Schema, string Name, string? BaseTypeName, short MaxLength, byte Precision,
        byte Scale, bool IsNullable, bool IsTableType, bool IsAssemblyType, int? TableObjectId);

    public sealed record SynonymInfo(string Schema, string Name, string BaseObjectName, string? Server, string? Database,
        string? ReferencedSchema, string? ReferencedName);

    public sealed record ReferenceRow<T>(int ReferencingId, T Reference);

    /// <summary>
    /// One object or column a module references, as SQL Server bound it. Column is null for the object
    /// itself. Names are the referenced object's own when SQL Server resolved it, as written otherwise.
    /// </summary>
    public sealed record EntityReference(string? Server, string? Database, string? Schema, string Entity, string? Column,
        bool IsSelected, bool IsUpdated, bool IsSelectAll, bool IsInsertAll);

    /// <summary>
    /// A module's references, or the error SQL Server raised when it could not bind the module.
    /// </summary>
    public sealed record ModuleReferences(List<EntityReference> References, string? Error);

    /// <summary>
    /// Everything the schema export needs, read from the catalog views in one pass. Each query covers
    /// the whole database so the export runs a fixed number of queries rather than several per table,
    /// except module references, read one module at a time (see ReadModuleReferences).
    /// </summary>
    public sealed class Catalog
    {
        public required DatabaseInfo Info { get; init; }
        public required List<DbObject> Objects { get; init; }
        public required Dictionary<int, List<ColumnInfo>> ColumnsByObject { get; init; }
        public required Dictionary<int, List<IndexInfo>> IndexesByObject { get; init; }
        public required Dictionary<(int ObjectId, int IndexId), List<IndexColumn>> IndexColumns { get; init; }
        public required Dictionary<int, List<CheckInfo>> ChecksByParent { get; init; }
        public required Dictionary<int, List<ForeignKeyInfo>> ForeignKeysByParent { get; init; }
        public required Dictionary<int, List<ForeignKeyColumn>> ForeignKeyColumns { get; init; }
        public required Dictionary<int, long> RowCounts { get; init; }
        public required Dictionary<int, ModuleInfo> Modules { get; init; }
        public required HashSet<int> DisabledTriggers { get; init; }
        public required Dictionary<int, int> TriggerCountByParent { get; init; }
        public required List<DatabaseTrigger> DatabaseTriggers { get; init; }
        public required List<TypeInfo> Types { get; init; }
        public required List<SynonymInfo> Synonyms { get; init; }
        public required List<ReferenceRow<ExternalReference>> ExternalReferences { get; init; }
        public required List<ReferenceRow<UnresolvedReference>> UnresolvedReferences { get; init; }
        public required Dictionary<int, ModuleReferences> ModuleReferences { get; init; }

        /// <summary>
        /// Why ExternalReferences and UnresolvedReferences could not be read, or null when they were.
        /// </summary>
        public string? ReferencesError { get; init; }

        // Catalog views are cheap, but sql_expression_dependencies can be slow on a large database.
        private const int CommandTimeoutSeconds = 300;

        public static Catalog Load(SqlConnection conn)
        {
            var info = Query(conn, @"
                SELECT CAST(@@SERVERNAME AS nvarchar(128)), DB_NAME(),
                    CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)),
                    CAST(d.compatibility_level AS int),
                    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128)),
                    CAST(ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION'), 0) AS bit)
                FROM sys.databases d
                WHERE d.name = DB_NAME()",
                r => new DatabaseInfo(Str(r, 0) ?? "", r.GetString(1), Str(r, 2) ?? "", r.GetInt32(3), Str(r, 4) ?? "", Bool(r, 5)))
                .Single();

            var objects = Query(conn, @"
                SELECT o.object_id, s.name, o.name, RTRIM(o.type), o.type_desc, o.parent_object_id,
                    CAST(CASE
                        WHEN o.type = 'P' AND o.name LIKE 'dt[_]%' THEN 1
                        WHEN EXISTS (
                            SELECT 1 FROM sys.extended_properties ep
                            WHERE ep.class = 1 AND ep.major_id = o.object_id AND ep.minor_id = 0
                                AND ep.name = N'microsoft_database_tools_support') THEN 1
                        ELSE 0 END AS bit)
                FROM sys.objects o
                INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
                WHERE o.is_ms_shipped = 0
                ORDER BY s.name, o.name",
                r => new DbObject(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetInt32(5), Bool(r, 6)));

            var columns = Query(conn, @"
                SELECT c.object_id, c.name, t.name, SCHEMA_NAME(t.schema_id), t.is_user_defined,
                    c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity,
                    CAST(ic.seed_value AS nvarchar(40)), CAST(ic.increment_value AS nvarchar(40)), ic.is_not_for_replication,
                    cc.definition, cc.is_persisted, c.collation_name, c.is_sparse, c.is_rowguidcol,
                    dc.name, dc.definition, dc.is_system_named,
                    CASE WHEN t.is_user_defined = 1 AND t.is_assembly_type = 0 THEN TYPE_NAME(c.system_type_id) ELSE t.name END,
                    CAST(ic.last_value AS nvarchar(40))
                FROM sys.columns c
                INNER JOIN sys.objects o ON o.object_id = c.object_id AND o.type IN ('U', 'TT', 'V') AND o.is_ms_shipped = 0
                INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
                LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                ORDER BY c.object_id, c.column_id",
                r => new ColumnInfo(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), Bool(r, 4),
                    r.GetInt16(5), r.GetByte(6), r.GetByte(7), Bool(r, 8), Bool(r, 9),
                    Str(r, 10), Str(r, 11), Bool(r, 12),
                    Str(r, 13), Bool(r, 14), Str(r, 15), Bool(r, 16), Bool(r, 17),
                    Str(r, 18), Str(r, 19), Bool(r, 20), r.GetString(21), Str(r, 22)));

            var indexes = Query(conn, @"
                SELECT i.object_id, i.index_id, i.name, i.type, i.type_desc, i.is_unique, i.is_primary_key,
                    i.is_unique_constraint, kc.is_system_named, i.filter_definition, i.is_disabled, i.ignore_dup_key
                FROM sys.indexes i
                INNER JOIN sys.objects o ON o.object_id = i.object_id AND o.type IN ('U', 'TT') AND o.is_ms_shipped = 0
                LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
                WHERE i.type > 0 AND i.is_hypothetical = 0
                ORDER BY i.object_id, i.is_primary_key DESC, i.name",
                r => new IndexInfo(r.GetInt32(0), r.GetInt32(1), Str(r, 2) ?? "", r.GetByte(3), r.GetString(4), Bool(r, 5),
                    Bool(r, 6), Bool(r, 7), Bool(r, 8), Str(r, 9), Bool(r, 10), Bool(r, 11)));

            var indexColumns = Query(conn, @"
                SELECT ic.object_id, ic.index_id, c.name, ic.is_descending_key, ic.is_included_column
                FROM sys.index_columns ic
                INNER JOIN sys.objects o ON o.object_id = ic.object_id AND o.type IN ('U', 'TT') AND o.is_ms_shipped = 0
                INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.key_ordinal > 0 OR ic.is_included_column = 1
                ORDER BY ic.object_id, ic.index_id, ic.is_included_column, ic.key_ordinal, ic.index_column_id",
                r => (Key: (r.GetInt32(0), r.GetInt32(1)), Column: new IndexColumn(r.GetString(2), Bool(r, 3), Bool(r, 4))));

            var checks = Query(conn, @"
                SELECT cc.parent_object_id, cc.name, cc.definition, cc.is_not_trusted, cc.is_disabled,
                    cc.is_not_for_replication, cc.is_system_named
                FROM sys.check_constraints cc
                WHERE cc.is_ms_shipped = 0
                ORDER BY cc.parent_object_id, cc.name",
                r => new CheckInfo(r.GetInt32(0), r.GetString(1), r.GetString(2), Bool(r, 3), Bool(r, 4), Bool(r, 5), Bool(r, 6)));

            var foreignKeys = Query(conn, @"
                SELECT fk.object_id, fk.parent_object_id, fk.name, SCHEMA_NAME(ro.schema_id), ro.name,
                    fk.is_not_trusted, fk.is_disabled, fk.is_not_for_replication, fk.is_system_named,
                    fk.delete_referential_action_desc, fk.update_referential_action_desc
                FROM sys.foreign_keys fk
                INNER JOIN sys.objects ro ON ro.object_id = fk.referenced_object_id
                WHERE fk.is_ms_shipped = 0
                ORDER BY fk.parent_object_id, fk.name",
                r => new ForeignKeyInfo(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3), r.GetString(4),
                    Bool(r, 5), Bool(r, 6), Bool(r, 7), Bool(r, 8), r.GetString(9), r.GetString(10)));

            var foreignKeyColumns = Query(conn, @"
                SELECT fkc.constraint_object_id, pc.name, rc.name
                FROM sys.foreign_key_columns fkc
                INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
                INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
                ORDER BY fkc.constraint_object_id, fkc.constraint_column_id",
                r => (Id: r.GetInt32(0), Column: new ForeignKeyColumn(r.GetString(1), r.GetString(2))));

            // sys.partitions is metadata, so this never scans a table. Its row count can lag slightly
            // behind in-flight changes, which is fine for sizing.
            var rowCounts = Query(conn, @"
                SELECT p.object_id, SUM(p.rows)
                FROM sys.partitions p
                INNER JOIN sys.objects o ON o.object_id = p.object_id AND o.type = 'U' AND o.is_ms_shipped = 0
                WHERE p.index_id IN (0, 1)
                GROUP BY p.object_id",
                r => (Id: r.GetInt32(0), Rows: r.GetInt64(1)));

            var modules = Query(conn, @"
                SELECT o.object_id, m.definition, m.uses_ansi_nulls, m.uses_quoted_identifier,
                    CAST(ISNULL(OBJECTPROPERTY(o.object_id, 'IsEncrypted'), 0) AS bit)
                FROM sys.objects o
                LEFT JOIN sys.sql_modules m ON m.object_id = o.object_id
                WHERE o.type IN ('V', 'P', 'FN', 'IF', 'TF', 'TR') AND o.is_ms_shipped = 0",
                r => (Id: r.GetInt32(0), Module: new ModuleInfo(Str(r, 1), Bool(r, 2), Bool(r, 3), Bool(r, 4))));

            var triggers = Query(conn, @"
                SELECT t.object_id, t.parent_id, t.parent_class, t.is_disabled, t.name,
                    m.definition, m.uses_ansi_nulls, m.uses_quoted_identifier,
                    CAST(ISNULL(OBJECTPROPERTY(t.object_id, 'IsEncrypted'), 0) AS bit)
                FROM sys.triggers t
                LEFT JOIN sys.sql_modules m ON m.object_id = t.object_id
                WHERE t.is_ms_shipped = 0",
                r => (Id: r.GetInt32(0), ParentId: r.GetInt32(1), IsDatabase: r.GetByte(2) == 0, IsDisabled: Bool(r, 3),
                    Name: r.GetString(4), Module: new ModuleInfo(Str(r, 5), Bool(r, 6), Bool(r, 7), Bool(r, 8))));

            var types = Query(conn, @"
                SELECT SCHEMA_NAME(t.schema_id), t.name, bt.name, t.max_length, t.precision, t.scale, t.is_nullable,
                    t.is_table_type, t.is_assembly_type, tt.type_table_object_id
                FROM sys.types t
                LEFT JOIN sys.types bt ON bt.user_type_id = t.system_type_id
                LEFT JOIN sys.table_types tt ON tt.user_type_id = t.user_type_id
                WHERE t.is_user_defined = 1
                ORDER BY SCHEMA_NAME(t.schema_id), t.name",
                r => new TypeInfo(r.GetString(0), r.GetString(1), Str(r, 2), r.GetInt16(3), r.GetByte(4), r.GetByte(5),
                    Bool(r, 6), Bool(r, 7), Bool(r, 8), r.IsDBNull(9) ? null : r.GetInt32(9)));

            var synonyms = Query(conn, @"
                SELECT SCHEMA_NAME(sn.schema_id), sn.name, sn.base_object_name,
                    PARSENAME(sn.base_object_name, 4), PARSENAME(sn.base_object_name, 3),
                    PARSENAME(sn.base_object_name, 2), PARSENAME(sn.base_object_name, 1)
                FROM sys.synonyms sn
                WHERE sn.is_ms_shipped = 0
                ORDER BY SCHEMA_NAME(sn.schema_id), sn.name",
                r => new SynonymInfo(r.GetString(0), r.GetString(1), r.GetString(2), Str(r, 3), Str(r, 4), Str(r, 5), Str(r, 6)));

            var (externalReferences, unresolvedReferences, referencesError) = ReadExpressionDependencies(conn);

            var moduleReferences = objects
                .Where(o => o.Type is "V" or "P" or "FN" or "IF" or "TF" or "TR")
                .ToDictionary(o => o.ObjectId, o => ReadModuleReferences(conn, QuotedName(o.Schema, o.Name), "OBJECT"));

            return new Catalog
            {
                Info = info,
                Objects = objects,
                ColumnsByObject = columns.GroupBy(c => c.ObjectId).ToDictionary(g => g.Key, g => g.ToList()),
                IndexesByObject = indexes.GroupBy(i => i.ObjectId).ToDictionary(g => g.Key, g => g.ToList()),
                IndexColumns = indexColumns.GroupBy(c => c.Key).ToDictionary(g => g.Key, g => g.Select(c => c.Column).ToList()),
                ChecksByParent = checks.GroupBy(c => c.ParentObjectId).ToDictionary(g => g.Key, g => g.ToList()),
                ForeignKeysByParent = foreignKeys.GroupBy(f => f.ParentObjectId).ToDictionary(g => g.Key, g => g.ToList()),
                ForeignKeyColumns = foreignKeyColumns.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.Select(c => c.Column).ToList()),
                RowCounts = rowCounts.ToDictionary(r => r.Id, r => r.Rows),
                Modules = modules.ToDictionary(m => m.Id, m => m.Module),
                DisabledTriggers = triggers.Where(t => t.IsDisabled && !t.IsDatabase).Select(t => t.Id).ToHashSet(),
                TriggerCountByParent = triggers.Where(t => !t.IsDatabase).GroupBy(t => t.ParentId).ToDictionary(g => g.Key, g => g.Count()),
                DatabaseTriggers = triggers.Where(t => t.IsDatabase).OrderBy(t => t.Name)
                    .Select(t => new DatabaseTrigger(t.Name, t.IsDisabled, t.Module,
                        ReadModuleReferences(conn, SchemaExport.Quote(t.Name), "DATABASE_DDL_TRIGGER"))).ToList(),
                Types = types,
                Synonyms = synonyms,
                ExternalReferences = externalReferences,
                UnresolvedReferences = unresolvedReferences,
                ReferencesError = referencesError,
                ModuleReferences = moduleReferences
            };
        }

        /// <summary>
        /// External and unresolved references, from sys.sql_expression_dependencies. Reading it needs VIEW
        /// DEFINITION on the database and SELECT on the view, which by default only db_owner has, so a login
        /// without them gets the error back instead, and the rest of the export carries on.
        /// </summary>
        private static (List<ReferenceRow<ExternalReference>> External, List<ReferenceRow<UnresolvedReference>> Unresolved, string? Error)
            ReadExpressionDependencies(SqlConnection conn)
        {
            try
            {
                return (ReadExternalReferences(conn), ReadUnresolvedReferences(conn), null);
            }
            catch (SqlException ex)
            {
                return ([], [], $"{ex.Message} (error {ex.Number})");
            }
        }

        // A three-part name pointing back at this database is not external, so it is filtered out. A
        // referencing object dropped while the export reads has no name left, so it is left out too.
        private static List<ReferenceRow<ExternalReference>> ReadExternalReferences(SqlConnection conn) =>
            Query(conn, @"
                SELECT DISTINCT d.referencing_id, OBJECT_SCHEMA_NAME(d.referencing_id), OBJECT_NAME(d.referencing_id),
                    d.referenced_server_name, d.referenced_database_name, d.referenced_schema_name, d.referenced_entity_name
                FROM sys.sql_expression_dependencies d
                WHERE d.referencing_class = 1 AND OBJECT_NAME(d.referencing_id) IS NOT NULL
                    AND (d.referenced_server_name IS NOT NULL
                        OR (d.referenced_database_name IS NOT NULL AND d.referenced_database_name <> DB_NAME()))
                ORDER BY 2, 3, 4, 5, 6, 7",
                r => new ReferenceRow<ExternalReference>(r.GetInt32(0), new ExternalReference(
                    QuotedName(r.GetString(1), r.GetString(2)), Str(r, 3), Str(r, 4), Str(r, 5), r.GetString(6))));

        // A local, schema-qualified reference that SQL Server could not resolve to an object ID names
        // something that does not exist. Caller-dependent and ambiguous references resolve only at run
        // time, so they are left out rather than reported as missing.
        private static List<ReferenceRow<UnresolvedReference>> ReadUnresolvedReferences(SqlConnection conn) =>
            Query(conn, @"
                SELECT DISTINCT d.referencing_id, OBJECT_SCHEMA_NAME(d.referencing_id), OBJECT_NAME(d.referencing_id),
                    d.referenced_schema_name, d.referenced_entity_name
                FROM sys.sql_expression_dependencies d
                WHERE d.referencing_class = 1 AND d.referenced_class = 1 AND d.referenced_id IS NULL
                    AND OBJECT_NAME(d.referencing_id) IS NOT NULL
                    AND d.referenced_server_name IS NULL
                    AND (d.referenced_database_name IS NULL OR d.referenced_database_name = DB_NAME())
                    AND d.is_caller_dependent = 0 AND d.is_ambiguous = 0
                ORDER BY 2, 3, 4, 5",
                r => new ReferenceRow<UnresolvedReference>(r.GetInt32(0), new UnresolvedReference(
                    QuotedName(r.GetString(1), r.GetString(2)), Str(r, 3), r.GetString(4))));

        /// <summary>
        /// What one module reads and writes, from sys.dm_sql_referenced_entities, which binds the module
        /// the way running it would, without running it. It raises an error for a module it cannot bind
        /// (such as one naming a column that no longer exists), which would end a query covering every
        /// module, so each module is read on its own and its error kept. A database DDL trigger is not in
        /// sys.objects, so it is named on its own, as the DATABASE_DDL_TRIGGER class.
        /// </summary>
        private static ModuleReferences ReadModuleReferences(SqlConnection conn, string name, string referencingClass)
        {
            try
            {
                // The metadata functions look IDs up in this database, so they name only a local reference; a
                // cross-database one keeps the names SQL Server recorded, since its ID belongs to the other database.
                var references = Query(conn, @"
                    SELECT r.referenced_server_name, r.referenced_database_name,
                        COALESCE(CASE WHEN l.IsLocal = 1 THEN OBJECT_SCHEMA_NAME(r.referenced_id) END, r.referenced_schema_name),
                        COALESCE(CASE WHEN l.IsLocal = 1 THEN OBJECT_NAME(r.referenced_id) END, r.referenced_entity_name),
                        COALESCE(CASE WHEN l.IsLocal = 1 THEN COL_NAME(r.referenced_id, NULLIF(r.referenced_minor_id, 0)) END,
                            r.referenced_minor_name),
                        r.is_selected, r.is_updated, r.is_select_all, r.is_insert_all
                    FROM sys.dm_sql_referenced_entities(@name, @class) r
                    CROSS APPLY (SELECT CASE WHEN r.referenced_server_name IS NULL
                        AND (r.referenced_database_name IS NULL OR r.referenced_database_name = DB_NAME())
                        THEN 1 ELSE 0 END AS IsLocal) l
                    WHERE r.referenced_class = 1",
                    r => new EntityReference(Str(r, 0), Str(r, 1), Str(r, 2), r.GetString(3), Str(r, 4),
                        Bool(r, 5), Bool(r, 6), Bool(r, 7), Bool(r, 8)),
                    new SqlParameter("@name", name), new SqlParameter("@class", referencingClass));
                return new ModuleReferences(references, null);
            }
            catch (SqlException ex)
            {
                return new ModuleReferences([], $"{ex.Message} (error {ex.Number})");
            }
        }

        /// <summary>
        /// [schema].[name], quoted as the export names every object, with any ] inside doubled.
        /// </summary>
        internal static string QuotedName(string schema, string name) => $"{SchemaExport.Quote(schema)}.{SchemaExport.Quote(name)}";

        private static List<T> Query<T>(SqlConnection conn, string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            var rows = new List<T>();
            while (reader.Read())
            {
                rows.Add(map(reader));
            }
            return rows;
        }

        private static bool Bool(SqlDataReader r, int i) => !r.IsDBNull(i) && r.GetBoolean(i);

        private static string? Str(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    }
}
