using System.Collections.Generic;
using System.Text.Json.Serialization;

// These records are written to summary.json, where people read every property; the code reads only some.
// ReSharper disable NotAccessedPositionalProperty.Global

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// The summary.json written beside the per-object files: the facts later steps (schema
    /// comparison, data analysis) read without re-parsing DDL. Object names are bracket-quoted
    /// ([schema].[name]) so they can be pasted straight into SQL.
    /// </summary>
    public sealed class SchemaSummary
    {
        public string Connection { get; init; } = "";
        public string Environment { get; init; } = "";
        public string Server { get; init; } = "";
        public string Database { get; init; } = "";
        public string GeneratedAt { get; init; } = "";
        public string ProductVersion { get; init; } = "";
        public int CompatibilityLevel { get; init; }
        public string DatabaseCollation { get; init; } = "";
        public bool HasViewDefinition { get; init; }
        // Collections are init rather than get-only so deserializing summary.json can set them.
        public Dictionary<string, int> ObjectCounts { get; init; } = new();
        public List<TableSummary> Tables { get; init; } = [];
        public List<ViewSummary> Views { get; init; } = [];
        public List<PersonIdColumn> PersonIdColumns { get; init; } = [];
        public List<string> TablesWithoutPrimaryKey { get; init; } = [];
        public List<string> TablesWithoutUniqueKey { get; init; } = [];
        public List<ForeignKeySummary> ForeignKeys { get; init; } = [];
        public List<CheckConstraintSummary> CheckConstraints { get; init; } = [];
        public List<ExternalReference> ExternalReferences { get; init; } = [];
        public List<LinkedServerMention> LinkedServerMentions { get; init; } = [];
        public List<UnresolvedReference> UnresolvedReferences { get; init; } = [];
        // Set when the login could not read the dependencies behind ExternalReferences and UnresolvedReferences,
        // so empty lists are not taken to mean there are none.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReferencesError { get; init; }
        public List<ModuleDependency> ModuleDependencies { get; init; } = [];
        public List<MissingDefinition> MissingDefinitions { get; init; } = [];
        public List<string> UnexportedObjects { get; init; } = [];
        public List<string> ExcludedToolObjects { get; init; } = [];
    }

    /// <summary>
    /// ApproximateRowCount comes from partition metadata, not COUNT(*). UniqueKeys are the enforced
    /// unique indexes and constraints other than the primary key; a filtered one only guarantees
    /// uniqueness within its filter, so it is left out.
    /// </summary>
    public sealed record TableSummary(string Name, long ApproximateRowCount, bool HasPrimaryKey, bool HasUniqueKey,
        int TriggerCount, List<string> PrimaryKeyColumns, List<List<string>> UniqueKeys, List<ColumnSummary> Columns);

    /// <summary>
    /// Type is the built-in type as scripted (varchar(50)); UserType names the alias type when the
    /// column uses one. MaxLength is in characters for text columns, -1 for max, and null otherwise.
    /// </summary>
    public sealed record ColumnSummary(string Name, string Type, string? UserType, int? MaxLength, bool IsNullable,
        bool IsComputed, string? Collation, IdentitySummary? Identity);

    /// <summary>
    /// LastValue is null until a row has been inserted.
    /// </summary>
    public sealed record IdentitySummary(string Seed, string Increment, string? LastValue);

    /// <summary>
    /// A view's columns as SQL Server recorded them when the view was created or last refreshed. A view
    /// written as SELECT * keeps that list even after its tables change, so it can differ from them.
    /// </summary>
    public sealed record ViewSummary(string Name, List<ColumnSummary> Columns);

    public enum PersonIdCategory
    {
        /// <summary>
        /// The name (or a foreign key to such a column) says which ID type the column holds.
        /// </summary>
        [JsonStringEnumMemberName("idType")] IdType,

        /// <summary>
        /// The name says the column records who did something, but not with which ID.
        /// </summary>
        [JsonStringEnumMemberName("review")] Review
    }

    /// <summary>
    /// A column that may hold a person's ID, found from metadata only (see PersonIdRules). IdType is
    /// null in the Review category; Reason says what matched.
    /// </summary>
    public sealed record PersonIdColumn(string Object, string Column, string Type,
        [property: JsonConverter(typeof(JsonStringEnumConverter<PersonIdCategory>))] PersonIdCategory Category,
        string? IdType, string Reason);

    public sealed record ForeignKeySummary(string Name, string Table, List<string> Columns, string ReferencedTable,
        List<string> ReferencedColumns, bool IsNotTrusted, bool IsDisabled, bool IsSystemNamed, string DeleteAction,
        string UpdateAction);

    public sealed record CheckConstraintSummary(string Name, string Table, string Definition, bool IsNotTrusted,
        bool IsDisabled, bool IsSystemNamed);

    public sealed record ExternalReference(string ReferencingObject, string? Server, string? Database, string? Schema,
        string Entity);

    /// <summary>
    /// A module whose text calls OPENQUERY, OPENROWSET, or OPENDATASOURCE. These pass-through calls
    /// are invisible to sys.sql_expression_dependencies, so they are found by scanning definitions;
    /// a mention inside a comment is reported too.
    /// </summary>
    public sealed record LinkedServerMention(string Object, List<string> Keywords);

    public sealed record UnresolvedReference(string ReferencingObject, string? Schema, string Entity);

    /// <summary>
    /// What one view, procedure, function, or trigger uses, as SQL Server binds it. UsesDynamicSql: its
    /// definition runs SQL built as text (EXEC with parentheses, sp_executesql), whose references SQL
    /// Server cannot see; a mention inside a comment counts too. Error is set instead of References when
    /// SQL Server could not bind the module, such as one naming a column that no longer exists. A missing
    /// table is not an error (SQL Server resolves table names when the module runs); it is listed under
    /// UnresolvedReferences.
    /// </summary>
    public sealed record ModuleDependency(string Object, string Type, List<ModuleReference> References, bool UsesDynamicSql,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error);

    /// <summary>
    /// Name is bracket-quoted, with the database (and server) in front when it is elsewhere. Reads and
    /// Writes are SQL Server's record of whether the module selects from or modifies it (insert, update,
    /// delete); neither means the module only calls it, as with a procedure or function. WrittenColumns
    /// are the columns SQL Server records as modified.
    /// </summary>
    public sealed record ModuleReference(string Name, bool Reads, bool Writes, List<string> WrittenColumns);

    public sealed record MissingDefinition(string Object, string Type, string Reason);
}
