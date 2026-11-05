using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// For each table checked, the views, procedures, functions, and triggers that write to it, from the
    /// schema export's moduleDependencies: no table reads. Also names where that map is blind (dynamic SQL,
    /// linked server calls, objects SQL Server could not bind) and the tables no stored object uses.
    /// </summary>
    public sealed class ObjectsThatChangeDataCheck : IAutomaticCheck
    {
        private const string Description =
            "The views, procedures, functions, and triggers stored in this database that insert, update, or delete rows in each table, " +
            "as SQL Server records them, with how many stored objects use the table at all. Changes made by application code are not seen, " +
            "nor are those made through dynamic SQL or a linked server; the objects that use either are named below.";

        public string Name => "Objects that change data";

        public bool AcceptsTableOverrides => false;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.Context,
                Severity = Severity.Info,
                Status = CheckStatus.NoProblemsFound
            };
            var summary = context.Plan.Summary;
            var modules = summary.ModuleDependencies;
            // Without VIEW DEFINITION, the catalog shows only the objects the login has some permission on.
            if (modules.Count == 0)
            {
                result.Message = summary.HasViewDefinition
                    ? "This database stores no views, procedures, functions, or triggers."
                    : "No views, procedures, functions, or triggers are visible to this login, which lacks VIEW DEFINITION; " +
                      "the database may still store some.";
                return [result];
            }
            if (!summary.HasViewDefinition)
            {
                result.Details.Add("Incomplete: the login lacks VIEW DEFINITION, so objects it has no permission on are left out.");
            }

            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var names = tables.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uses = modules
                .SelectMany(m => m.References.Where(r => names.Contains(r.Name)).Select(r => (Table: r.Name, m.Object, r.Reads, r.Writes)))
                .ToLookup(u => u.Table, StringComparer.OrdinalIgnoreCase);

            // A statement that also uses a temporary table cannot be bound until it runs, so SQL Server
            // records its tables as referenced but neither read nor written: listed as a possible write.
            var written = tables
                .Select(t => (t.Name, Writers: uses[t.Name].Where(u => u.Writes).Select(u => u.Object)
                        .Concat(uses[t.Name].Where(u => !u.Reads && !u.Writes).Select(u => $"{u.Object} {UnrecordedUse}"))
                        .Distinct().OrderBy(o => o, StringComparer.Ordinal).ToList(),
                    Users: uses[t.Name].Select(u => u.Object).Distinct().Count()))
                .Where(t => t.Writers.Count > 0)
                .ToList();
            // One writer per row, so a table with many stays readable; the table and its count head its first row.
            result.Grids.Add(new ResultGrid("Tables written by stored objects", ["Table"], ["Written by", "Used by"],
                written.SelectMany(t => t.Writers.Select((w, i) => i == 0
                    ? new List<string> { t.Name, w, Count(t.Users) }
                    : ["", w, ""])).ToList(), written.Count));

            // Where the app's queries live in its own code, most tables land here, so each shows the latest
            // year of its declared date column: a stronger sign of whether the table is still in use.
            var unused = tables.Where(t => !t.IsView && !uses[t.Name].Any()).ToList();
            if (unused.Count > 0)
            {
                result.Grids.Add(new ResultGrid("Used by no stored object", ["Table"], ["Date column", "Latest year"],
                    unused.Select(t => LatestDate(context, t)).ToList(), unused.Count,
                    "application code is not seen, so this alone does not mean a table is unused; the latest year of its rows is the stronger sign"));
            }

            // With only part of the database checked, an object is named when it uses one of the tables
            // checked; the others are counted.
            var wholeDatabase = context.Plan.Schema.Tables.All(t => t.IsIncluded);
            bool UsesCheckedTable(ModuleDependency m) => wholeDatabase || m.References.Exists(r => names.Contains(r.Name));
            var linked = summary.LinkedServerMentions.Select(l => l.Object).ToHashSet(StringComparer.Ordinal);
            AddBlindSpot(result, "Using dynamic SQL", modules.Where(m => m.UsesDynamicSql).ToList(), UsesCheckedTable);
            AddBlindSpot(result, "Calling a linked server", modules.Where(m => linked.Contains(m.Object)).ToList(), UsesCheckedTable);

            // Both lists above come from reading definitions, so an object without one is in neither,
            // whatever it runs.
            if (summary.MissingDefinitions.Count > 0)
            {
                result.Grids.Add(new ResultGrid("No definition to read, so dynamic SQL and linked server calls in them were not looked for",
                    ["Object"], ["Why"],
                    summary.MissingDefinitions.Select(d => new List<string> { d.Object, d.Reason }).ToList(), summary.MissingDefinitions.Count));
            }

            // These objects have no references, so whether they use a table checked is unknown; all are named.
            var unreadable = modules.Where(m => m.Error != null).ToList();
            if (unreadable.Count > 0)
            {
                result.Grids.Add(new ResultGrid("SQL Server could not resolve their references, so what they read and write is unknown", ["Object"], ["Why"],
                    unreadable.Select(m => new List<string> { m.Object, Why(m.Error!) }).ToList(), unreadable.Count));
            }
            var silent = modules.Where(m => m.References.Count == 0 && m.Error == null && !m.UsesDynamicSql && !linked.Contains(m.Object))
                .Select(m => m.Object).ToList();
            if (silent.Count > 0)
            {
                result.Details.Add($"No references recorded, so what they use is unknown ({Count(silent.Count)}): {string.Join(", ", silent)}");
            }
            return [result];
        }

        /// <summary>
        /// The table's declared date column and the latest year in it, from the shared profile read.
        /// </summary>
        private static List<string> LatestDate(AnalysisContext context, TableRef table)
        {
            if (!context.Plan.DateColumns.TryGetValue(table, out var column))
            {
                return [table.Name, "none declared", "-"];
            }
            var profile = context.Profile(table);
            var latest = profile.Error == null
                ? profile.Columns.Find(c => c.Column == column.Name)?.LatestYear?.ToString(CultureInfo.InvariantCulture) ?? "-"
                : "not read";
            return [table.Name, column.Name, latest];
        }

        private const string UnrecordedUse = "(use not recorded, often a temporary table)";

        // SQL Server's binding error, whose own text says only that the references "might not include" every
        // column, followed by general advice.
        private const string BindingError = " (error 2020)";

        /// <summary>
        /// Why an object's references are unknown: for SQL Server's binding error, its usual causes in plain
        /// words; for any other error, the message as SQL Server gave it.
        /// </summary>
        internal static string Why(string error) =>
            error.EndsWith(BindingError, StringComparison.Ordinal)
                ? $"It references something that does not exist now, a temporary table, or another database or server{BindingError}"
                : error;

        private static void AddBlindSpot(CheckResult result, string label, List<ModuleDependency> modules, Func<ModuleDependency, bool> named)
        {
            if (modules.Count == 0)
            {
                return;
            }
            var shown = modules.Where(named).Select(m => m.Object).ToList();
            var others = modules.Count - shown.Count;
            var list = shown.Count > 0 ? $": {string.Join(", ", shown)}" : "";
            var rest = others > 0 ? $"{(shown.Count > 0 ? "; " : ": ")}{Count(others)} more not using the tables checked" : "";
            result.Details.Add($"{label} ({Count(modules.Count)}){list}{rest}");
        }

        private static string Count(long n) => n.ToString("N0", CultureInfo.InvariantCulture);
    }
}
