using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The tables, views, and columns of a schema export. Checks build SQL only from the TableRef and
    /// ColumnRef objects found here, so a name that is not in the export can never reach a query.
    /// </summary>
    public sealed class SchemaIndex
    {
        private readonly Dictionary<string, TableRef> _tables;
        private readonly Dictionary<string, TableRef> _views;
        private HashSet<string>? _names;

        public SchemaIndex(SchemaSummary summary, ISet<string> excluded, IReadOnlyList<string>? include = null)
        {
            var personColumns = summary.PersonIdColumns.Select(p => (p.Object, p.Column)).ToHashSet();
            _tables = summary.Tables.ToDictionary(t => t.Name,
                t => new TableRef(t.Name, t.Columns, t, excluded.Contains(t.Name), IsIncluded(include, t.Name), personColumns), StringComparer.Ordinal);
            _views = summary.Views.ToDictionary(v => v.Name,
                v => new TableRef(v.Name, v.Columns, null, excluded.Contains(v.Name), IsIncluded(include, v.Name), personColumns), StringComparer.Ordinal);
            ShowBySchema(summary);
        }

        /// <summary>
        /// The columns whose values the schema alone shows to be safe to report: values the database made
        /// (identity, rowversion), columns whose foreign key points at one of those (followed along a chain),
        /// and person ID columns (MothraId, IAM ID, PIDM), whose values are shown only when all digits.
        /// </summary>
        private void ShowBySchema(SchemaSummary summary)
        {
            var columns = _tables.Values.Concat(_views.Values).SelectMany(t => t.Columns).ToList();
            foreach (var column in columns.Where(c => c.Summary.Identity != null || c.BaseType is "timestamp" or "rowversion"))
            {
                column.Show(ValueVisibility.Shown);
            }

            bool changed;
            do
            {
                changed = false;
                foreach (var fk in summary.ForeignKeys)
                {
                    for (var i = 0; i < fk.Columns.Count; i++)
                    {
                        var child = FindTable(fk.Table)?.FindColumn(fk.Columns[i]);
                        var parent = FindTable(fk.ReferencedTable)?.FindColumn(fk.ReferencedColumns[i]);
                        if (child is { Visibility: ValueVisibility.Masked } && parent is { Visibility: ValueVisibility.Shown })
                        {
                            child.Show(ValueVisibility.Shown);
                            changed = true;
                        }
                    }
                }
            } while (changed);

            var personIds = summary.PersonIdColumns.Where(p => p.Category == PersonIdCategory.IdType).Select(p => (p.Object, p.Column)).ToHashSet();
            foreach (var column in columns.Where(c => personIds.Contains((c.Table.Name, c.Name))))
            {
                column.IsPersonId = true;
                column.Show(ValueVisibility.DigitsOnly);
            }
        }

        /// <summary>
        /// The columns whose values a check described by shape rather than showing, for the report to list.
        /// </summary>
        public IEnumerable<ColumnRef> MaskedColumns =>
            _tables.Values.Concat(_views.Values).SelectMany(t => t.Columns).Where(c => c.WasMasked);

        /// <summary>
        /// Whether an include list names the object or its schema; true when there is no list. A schema
        /// entry ([cts]) matches by prefix, which bracket quoting keeps exact: [cts]].[x] does not start with "[cts].".
        /// </summary>
        public static bool IsIncluded(IReadOnlyList<string>? include, string objectName) =>
            include == null || include.Any(entry => entry == objectName || objectName.StartsWith(entry + ".", StringComparison.Ordinal));

        /// <summary>
        /// An include entry naming a schema rather than a table or view: one bracket-quoted part.
        /// </summary>
        public static bool IsSchemaEntry(string entry) => !entry.Contains("].[", StringComparison.Ordinal);

        /// <summary>
        /// The schemas holding a table or view, as [schema], for binding and suggesting include entries.
        /// </summary>
        public IEnumerable<string> SchemaNames => _tables.Keys.Concat(_views.Keys).Select(SchemaOf).Distinct(StringComparer.Ordinal);

        /// <summary>
        /// The [schema] part of a [schema].[name].
        /// </summary>
        public static string SchemaOf(string objectName) => objectName[..(objectName.IndexOf("].[", StringComparison.Ordinal) + 1)];

        public IEnumerable<TableRef> Tables => _tables.Values;

        public IEnumerable<TableRef> Views => _views.Values;

        public TableRef? FindTable(string name) => _tables.GetValueOrDefault(name);

        public TableRef? FindTableOrView(string name) => _tables.GetValueOrDefault(name) ?? _views.GetValueOrDefault(name);

        /// <summary>
        /// Whether a table or view is listed under "exclude".
        /// </summary>
        public bool IsExcluded(string name) => FindTableOrView(name)?.IsExcluded == true;

        /// <summary>
        /// A table or view whose name differs only in case, to suggest when a declared name is not found.
        /// </summary>
        public string? SuggestTableOrView(string name) =>
            _tables.Keys.Concat(_views.Keys).FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Whether text is the name of a schema, table, view, or column in the export, without brackets and
        /// ignoring case, as SQL Server writes names in its error messages.
        /// </summary>
        public bool IsName(string text) => (_names ??= Names()).Contains(text);

        private HashSet<string> Names()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in _tables.Values.Concat(_views.Values))
            {
                names.UnionWith(table.Name[1..^1].Split("].[").Select(p => p.Replace("]]", "]", StringComparison.Ordinal)));
                names.UnionWith(table.Columns.Select(c => c.Name));
            }
            return names;
        }
    }

    /// <summary>
    /// A table or view from the schema export. Declared checks accept either; automatic checks that read
    /// table data use only tables.
    /// </summary>
    public sealed class TableRef
    {
        internal TableRef(string name, List<ColumnSummary> columns, TableSummary? tableSummary, bool isExcluded, bool isIncluded,
            ISet<(string Object, string Column)> personColumns)
        {
            Name = name;
            TableSummary = tableSummary;
            IsExcluded = isExcluded;
            IsIncluded = isIncluded;
            Columns = columns.Select(c => new ColumnRef(this, c, personColumns.Contains((name, c.Name)))).ToList();
            KeyColumns = ChooseKeyColumns();
        }

        /// <summary>
        /// Bracket-quoted by the export ([schema].[name]), so it goes into SQL as is.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The table's keys and row count; null for a view.
        /// </summary>
        public TableSummary? TableSummary { get; }

        public bool IsView => TableSummary == null;

        /// <summary>
        /// Left out of every check, automatic and declared, except Tables that look like copies, which
        /// compares its columns without reading it.
        /// </summary>
        public bool IsExcluded { get; }

        /// <summary>
        /// Named by "include", or there is no include list.
        /// </summary>
        public bool IsIncluded { get; }

        /// <summary>
        /// Visited by the automatic checks. Declared checks need only that the object is not excluded.
        /// </summary>
        public bool GetsAutomaticChecks => IsIncluded && !IsExcluded;

        public IReadOnlyList<ColumnRef> Columns { get; }

        /// <summary>
        /// The columns whose values the report shows as examples of affected rows: the identity column (always
        /// a surrogate key), else the primary key, else the narrowest unique key. A table with none of
        /// these, and every view, gets counts only.
        /// </summary>
        public IReadOnlyList<ColumnRef> KeyColumns { get; }

        public ColumnRef? FindColumn(string name) => Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));

        public string? SuggestColumn(string name) =>
            Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Name;

        private List<ColumnRef> ChooseKeyColumns()
        {
            if (TableSummary == null)
            {
                return [];
            }

            var identity = Columns.FirstOrDefault(c => c.Summary.Identity != null);
            if (identity != null)
            {
                return [identity];
            }

            var key = TableSummary.PrimaryKeyColumns.Count > 0
                ? TableSummary.PrimaryKeyColumns
                : TableSummary.UniqueKeys.OrderBy(k => k.Count).FirstOrDefault() ?? [];
            return key.Select(name => FindColumn(name)!).ToList();
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Which of a column's values the report may show. A value is masked unless its origin makes it
    /// safe, whatever it looks like: an SSN is all digits too.
    /// </summary>
    public enum ValueVisibility
    {
        /// <summary>
        /// Described by shape only, the default.
        /// </summary>
        Masked,

        /// <summary>
        /// A person ID column, or a text column in a declared lookup: values that are all digits are
        /// shown, anything else (a LoginId typed into a MothraId column) by shape.
        /// </summary>
        DigitsOnly,

        /// <summary>
        /// Made by the database, a key into such a column, or declared under safeColumns.
        /// </summary>
        Shown
    }

    public sealed class ColumnRef
    {
        private static readonly HashSet<string> TextTypes = new(StringComparer.Ordinal)
        {
            "char", "varchar", "text", "nchar", "nvarchar", "ntext"
        };

        private static readonly HashSet<string> DateTypes = new(StringComparer.Ordinal)
        {
            "date", "datetime", "datetime2", "smalldatetime", "datetimeoffset"
        };

        private static readonly HashSet<string> NumberTypes = new(StringComparer.Ordinal)
        {
            "tinyint", "smallint", "int", "bigint", "decimal", "numeric", "float", "real", "money", "smallmoney"
        };

        internal ColumnRef(TableRef table, ColumnSummary summary, bool isPersonColumn)
        {
            Table = table;
            Summary = summary;
            BaseType = summary.Type.Split('(')[0];
            IsPersonColumn = isPersonColumn;
        }

        /// <summary>
        /// Flagged in the export's personIdColumns, as an ID type or as recording who did something.
        /// </summary>
        public bool IsPersonColumn { get; }

        /// <summary>
        /// Flagged in the export's personIdColumns as an ID type (MothraId, IAM ID, PIDM).
        /// </summary>
        public bool IsPersonId { get; internal set; }

        /// <summary>
        /// Whether the report may show this column's values (see ResultSql.CanShow).
        /// </summary>
        public ValueVisibility Visibility { get; private set; }

        /// <summary>
        /// Why a declaration made the values visible, printed beside the column's range in its profile;
        /// null when the schema did.
        /// </summary>
        public string? ShownReason { get; private set; }

        /// <summary>
        /// Some value of this column was described by shape rather than shown.
        /// </summary>
        public bool WasMasked { get; private set; }

        /// <summary>
        /// The distinct values described by shape, trimmed, where the check had them, for the report to
        /// summarize by shape. They are never written themselves.
        /// </summary>
        public IReadOnlyCollection<string> MaskedValues => _maskedValues;

        private readonly HashSet<string> _maskedValues = new(StringComparer.Ordinal);

        /// <summary>
        /// Raises the visibility, never lowers it.
        /// </summary>
        internal void Show(ValueVisibility visibility, string? reason = null)
        {
            if (visibility > Visibility)
            {
                Visibility = visibility;
                ShownReason = reason;
            }
        }

        internal void NoteMasked(object? value = null)
        {
            WasMasked = true;
            if (value != null)
            {
                _maskedValues.Add(Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "");
            }
        }

        public TableRef Table { get; }

        public ColumnSummary Summary { get; }

        public string Name => Summary.Name;

        public string Sql => SchemaExport.Quote(Name);

        /// <summary>
        /// The column as an expression that can be compared, grouped, and counted. The legacy text and
        /// ntext types cannot be, so they are read as their max equivalents.
        /// </summary>
        public string ComparableSql(string alias) => BaseType switch
        {
            "text" => $"CAST({alias}.{Sql} AS varchar(max))",
            "ntext" => $"CAST({alias}.{Sql} AS nvarchar(max))",
            _ => $"{alias}.{Sql}"
        };

        /// <summary>
        /// The built-in type without its length or precision (varchar for varchar(50)).
        /// </summary>
        public string BaseType { get; }

        public bool IsText => TextTypes.Contains(BaseType);

        public bool IsDate => DateTypes.Contains(BaseType);

        public bool IsNumber => NumberTypes.Contains(BaseType);

        /// <summary>
        /// char and nchar, which pad every value shorter than the column with trailing spaces. SQL
        /// comparisons ignore the padding; C# comparisons do not.
        /// </summary>
        public bool IsPadded => BaseType is "char" or "nchar";

        public override string ToString() => $"{Table.Name}.{Sql}";
    }
}
