using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Shows a customSql query to be one read-only SELECT before it reaches the database, using
    /// Microsoft's T-SQL parser. The tools' read-only promise rests on their own code, since
    /// ApplicationIntent=ReadOnly does not block writes on a standalone server, so anything the parser
    /// cannot show to be a plain SELECT is refused. Also returns the tables the query reads, for binding
    /// to the schema export.
    /// </summary>
    public static class CustomSqlValidator
    {
        /// <summary>
        /// A table or view as the query names it; Schema and Database are null when not written.
        /// </summary>
        public sealed record TableName(string? Database, string? Schema, string Name);

        /// <summary>
        /// The table column an output column of the query is.
        /// </summary>
        public sealed record ColumnSource(TableName Table, string Column);

        /// <summary>
        /// Tables and Sources are empty whenever there are errors. Sources holds the output columns traced to
        /// a table column, by output name, ignoring case (see TraceSources).
        /// </summary>
        public sealed record Outcome(IReadOnlyList<string> Errors, IReadOnlyList<TableName> Tables,
            IReadOnlyDictionary<string, ColumnSource>? Sources = null);

        public static Outcome Validate(string sql, int compatibilityLevel)
        {
            using var reader = new StringReader(sql);
            var fragment = ParserFor(compatibilityLevel).Parse(reader, out var parseErrors);
            if (parseErrors.Count > 0)
            {
                return new Outcome(parseErrors.Select(e => $"line {e.Line}: {e.Message}").ToList(), []);
            }

            var statements = ((TSqlScript)fragment).Batches.SelectMany(b => b.Statements).ToList();
            if (statements.Count != 1)
            {
                return new Outcome([$"must be a single SELECT statement; found {statements.Count} statements."], []);
            }
            if (statements[0] is not SelectStatement select)
            {
                return new Outcome([$"line {statements[0].StartLine}: must be a SELECT statement; found {StatementKind(statements[0])}."], []);
            }

            var errors = new List<string>();
            if (select.Into != null)
            {
                errors.Add($"line {select.Into.StartLine}: SELECT ... INTO creates a table. Remove INTO.");
            }
            var visitor = new ReadOnlyVisitor();
            select.Accept(visitor);
            errors.AddRange(visitor.Errors);
            if (errors.Count > 0)
            {
                return new Outcome(errors, []);
            }

            // A common table expression is named like a table, but is part of the query.
            var expressions = select.WithCtesAndXmlNamespaces?.CommonTableExpressions
                .Select(c => c.ExpressionName.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            var tables = visitor.Tables
                .Where(t => t.Database != null || t.Schema != null || !expressions.Contains(t.Name))
                .Distinct()
                .ToList();
            return new Outcome([], tables, TraceSources(select, expressions));
        }

        /// <summary>
        /// The table column behind each output column that is a plain column reference (t.col, t.col AS
        /// name, or col when the query reads one table) in a single SELECT. Anything else traces to nothing,
        /// so its values are masked: an expression, a column of a common table expression or derived table,
        /// UNION and the like, a name two output columns share, or an alias used twice.
        /// </summary>
        private static Dictionary<string, ColumnSource> TraceSources(SelectStatement select, HashSet<string> expressions)
        {
            if (select.QueryExpression is not QuerySpecification { FromClause: { } from } query)
            {
                return [];
            }
            var aliases = new Dictionary<string, TableName?>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in from.TableReferences)
            {
                AddAliases(reference, aliases, expressions);
            }

            var sources = new Dictionary<string, ColumnSource?>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in query.SelectElements.OfType<SelectScalarExpression>())
            {
                var column = element.Expression as ColumnReferenceExpression;
                var name = element.ColumnName?.Value ?? column?.MultiPartIdentifier?.Identifiers[^1].Value;
                if (name != null)
                {
                    var source = column is { ColumnType: ColumnType.Regular } ? Trace(column, aliases) : null;
                    sources[name] = sources.ContainsKey(name) ? null : source;
                }
            }
            return sources.Where(s => s.Value != null).ToDictionary(s => s.Key, s => s.Value!, StringComparer.OrdinalIgnoreCase);
        }

        // A name added twice traces to nothing. A reference that is not a table, such as a derived table or a
        // function, is added under its alias (or no name) as nothing, so a column naming no alias stays ambiguous.
        private static void AddAliases(TableReference reference, Dictionary<string, TableName?> aliases, HashSet<string> expressions)
        {
            switch (reference)
            {
                case JoinTableReference join:
                    AddAliases(join.FirstTableReference, aliases, expressions);
                    AddAliases(join.SecondTableReference, aliases, expressions);
                    break;
                case NamedTableReference named:
                    var name = named.SchemaObject;
                    var isExpression = name.DatabaseIdentifier == null && name.SchemaIdentifier == null && expressions.Contains(name.BaseIdentifier.Value);
                    Add(named.Alias?.Value ?? name.BaseIdentifier.Value,
                        isExpression ? null : new TableName(name.DatabaseIdentifier?.Value, name.SchemaIdentifier?.Value, name.BaseIdentifier.Value));
                    break;
                case TableReferenceWithAlias other:
                    Add(other.Alias?.Value ?? "", null);
                    break;
                default:
                    Add("", null);
                    break;
            }

            void Add(string key, TableName? table) => aliases[key] = aliases.ContainsKey(key) ? null : table;
        }

        private static ColumnSource? Trace(ColumnReferenceExpression column, Dictionary<string, TableName?> aliases)
        {
            var parts = column.MultiPartIdentifier.Identifiers.Select(i => i.Value).ToList();
            var table = parts.Count switch
            {
                1 when aliases.Count == 1 => aliases.Values.Single(),
                2 => aliases.GetValueOrDefault(parts[0]),
                _ => null
            };
            return table != null ? new ColumnSource(table, parts[^1]) : null;
        }

        private static TSqlParser ParserFor(int compatibilityLevel) => compatibilityLevel switch
        {
            >= 170 => new TSql170Parser(true),
            160 => new TSql160Parser(true),
            150 => new TSql150Parser(true),
            140 => new TSql140Parser(true),
            130 => new TSql130Parser(true),
            120 => new TSql120Parser(true),
            110 => new TSql110Parser(true),
            _ => new TSql100Parser(true)
        };

        // "InsertStatement" reads as "an INSERT statement".
        private static string StatementKind(TSqlStatement statement)
        {
            var kind = statement.GetType().Name.Replace("Statement", "", StringComparison.Ordinal).ToUpperInvariant();
            return $"{("AEIOU".Contains(kind[0]) ? "an" : "a")} {kind} statement";
        }

        /// <summary>
        /// Collects the tables a SELECT reads and refuses what a SELECT can still do beyond reading this
        /// database: reach another server, use a temporary table or a variable (which would need a second
        /// statement to create), advance a sequence, run a user-defined function whose code cannot be checked,
        /// or take or wait for locks through a table hint.
        /// </summary>
        private sealed class ReadOnlyVisitor : TSqlFragmentVisitor
        {
            // The hints that take no locks and wait for none, as the session already reads, and those that
            // only steer the plan. Any other hint, including one SQL Server adds later, is refused.
            private static readonly HashSet<TableHintKind> AllowedHints =
            [
                TableHintKind.NoLock, TableHintKind.ReadUncommitted, TableHintKind.ReadPast, TableHintKind.NoWait,
                TableHintKind.Index, TableHintKind.ForceSeek, TableHintKind.ForceScan, TableHintKind.NoExpand
            ];

            public List<string> Errors { get; } = [];
            public List<TableName> Tables { get; } = [];

            private void Refuse(TSqlFragment node, string message) => Errors.Add($"line {node.StartLine}: {message}");

            public override void ExplicitVisit(NamedTableReference node)
            {
                var name = node.SchemaObject;
                if (name.ServerIdentifier != null)
                {
                    Refuse(node, $"{name.ServerIdentifier.Value} is another server. Only this database's tables can be read.");
                }
                else if (name.BaseIdentifier.Value.StartsWith('#'))
                {
                    Refuse(node, $"{name.BaseIdentifier.Value} is a temporary table, which a single SELECT cannot create. Use a common table expression instead.");
                }
                else
                {
                    Tables.Add(new TableName(name.DatabaseIdentifier?.Value, name.SchemaIdentifier?.Value, name.BaseIdentifier.Value));
                }

                foreach (var hint in node.TableHints.Where(h => !AllowedHints.Contains(h.HintKind)))
                {
                    Refuse(hint, $"the {hint.HintKind.ToString().ToUpperInvariant()} hint is not allowed, since it can take locks or wait for them. " +
                                 "Allowed: NOLOCK, READUNCOMMITTED, READPAST, NOWAIT, INDEX, FORCESEEK, FORCESCAN, NOEXPAND.");
                }
                base.ExplicitVisit(node);
            }

            public override void Visit(OpenQueryTableReference node) =>
                Refuse(node, "OPENQUERY reaches another server. Only this database's tables can be read.");

            public override void Visit(OpenRowsetTableReference node) =>
                Refuse(node, "OPENROWSET reaches another server or a file. Only this database's tables can be read.");

            public override void Visit(BulkOpenRowset node) =>
                Refuse(node, "OPENROWSET reads a file. Only this database's tables can be read.");

            public override void Visit(AdHocTableReference node) =>
                Refuse(node, "OPENDATASOURCE reaches another server. Only this database's tables can be read.");

            // Not visiting the name below it, which would be refused again as a variable.
            public override void ExplicitVisit(VariableTableReference node) =>
                Refuse(node, $"{node.Variable.Name} is a table variable, which a single SELECT cannot declare. Use a common table expression instead.");

            public override void Visit(VariableReference node) =>
                Refuse(node, $"{node.Name} is a variable, which a single SELECT cannot declare. Write the value into the query.");

            public override void Visit(NextValueForExpression node) =>
                Refuse(node, "NEXT VALUE FOR advances a sequence, which changes the database. Remove it.");

            // A user-defined function's code is not part of the query, so what it reads or does cannot be
            // checked, and it may reach another database. Built-in functions have no call target. A
            // method on a column (t.col.value) looks the same as a schema's function (dbo.fn), so it
            // is refused too.
            public override void Visit(FunctionCall node)
            {
                if (node.CallTarget is MultiPartIdentifierCallTarget target)
                {
                    var name = string.Join(".", target.MultiPartIdentifier.Identifiers.Select(i => i.Value).Append(node.FunctionName.Value));
                    Refuse(node, $"{name} is a user-defined function or a method, whose code cannot be checked. Write what it does into the query.");
                }
            }

            public override void Visit(SchemaObjectFunctionTableReference node)
            {
                var name = string.Join(".", node.SchemaObject.Identifiers.Select(i => i.Value));
                Refuse(node, $"{name} is a table-valued function, whose code cannot be checked. Write its query inline or as a common table expression.");
            }
        }
    }
}
