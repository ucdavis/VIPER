using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Compares two schema exports of the same connection (e.g. Test vs Production) and reports objects
    /// present in only one, objects whose definitions differ, and likely permission gaps. Works on the
    /// exported files only; it never connects to a database. Line-by-line detail is left to
    /// git diff --no-index, whose commands the report prints.
    ///
    /// Output: the report is printed and saved as Output/{connection}/compare-{left}-{right}.txt.
    /// </summary>
    public sealed class SchemaCompare
    {
        private const string MissingDefinitionMarker = SchemaExport.MissingDefinitionMarker;

        private static readonly Regex QuotedTwoPartName = new(
            @"^\[((?:[^\]]|\]\])*)\]\.\[((?:[^\]]|\]\])*)\]$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        private readonly string _connectionName;
        private readonly string _left;
        private readonly string _right;
        private readonly string _leftDir;
        private readonly string _rightDir;
        private readonly string _reportPath;
        private readonly string? _repoRoot;
        private readonly StringBuilder _report = new();

        private SchemaCompare(string connectionName, string left, string right)
            : this(connectionName, left, right, Path.Join(LegacyMigrationScriptHelper.GetToolRoot(), "Output", connectionName))
        {
        }

        /// <summary>
        /// Compares the exports in connectionDir's left and right folders, saving the report beside them.
        /// Tests pass a folder of their own.
        /// </summary>
        internal SchemaCompare(string connectionName, string left, string right, string connectionDir)
        {
            _connectionName = connectionName;
            _left = left;
            _right = right;
            _leftDir = Path.Join(connectionDir, left);
            _rightDir = Path.Join(connectionDir, right);
            _reportPath = Path.Join(connectionDir, $"compare-{left}-{right}.txt");
            _repoRoot = FindRepoRoot(connectionDir);
        }

        public static int Run(string[] args)
        {
            try
            {
                var connectionName = LegacyMigrationScriptHelper.ValidateConnectionName(
                    LegacyMigrationScriptHelper.GetRequiredArg(args, "connection"));
                var left = LegacyMigrationScriptHelper.ParseEnvironment(LegacyMigrationScriptHelper.GetRequiredArg(args, "left"));
                var right = LegacyMigrationScriptHelper.ParseEnvironment(LegacyMigrationScriptHelper.GetRequiredArg(args, "right"));
                if (left == right)
                {
                    throw new ArgumentException($"Both environments are {left}; compare two different environments.");
                }

                new SchemaCompare(connectionName, left, right).Compare();
                return 0;
            }
            catch (ArgumentException ex)
            {
                LegacyMigrationScriptHelper.WriteError("ARGUMENT ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LegacyMigrationScriptHelper.WriteError("EXPORT NOT FOUND", ex.Message);
            }
            catch (JsonException ex)
            {
                LegacyMigrationScriptHelper.WriteError("UNREADABLE SUMMARY", ex.Message);
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

        private void Compare()
        {
            var report = Report();
            Console.Write(report);
            LegacyMigrationScriptHelper.WriteCrlfFile(_reportPath, report);
            Console.WriteLine();
            Console.WriteLine($"Report saved to: {_reportPath}");
        }

        /// <summary>
        /// The comparison report, from the two exports' files. Call once: it builds the report as it goes.
        /// </summary>
        internal string Report()
        {
            var leftSummary = LoadSummary(_leftDir, _left);
            var rightSummary = LoadSummary(_rightDir, _right);
            var leftFiles = ObjectFiles(_leftDir);
            var rightFiles = ObjectFiles(_rightDir);

            var onlyLeft = leftFiles.Keys.Where(k => !rightFiles.ContainsKey(k)).Order().ToList();
            var onlyRight = rightFiles.Keys.Where(k => !leftFiles.ContainsKey(k)).Order().ToList();
            var different = new List<string>();
            var whitespaceOnly = new List<string>();
            var permissionGaps = new List<string>();
            var viewColumns = ViewColumnDifferences(leftSummary, rightSummary);
            var columnsOnly = 0;
            var identical = 0;

            foreach (var key in leftFiles.Keys.Where(rightFiles.ContainsKey).Order())
            {
                var leftText = File.ReadAllText(leftFiles[key]);
                var rightText = File.ReadAllText(rightFiles[key]);
                var leftMissing = leftText.Contains(MissingDefinitionMarker);
                var rightMissing = rightText.Contains(MissingDefinitionMarker);

                if (leftText == rightText)
                {
                    // A view's columns are fixed when it is created or refreshed, so the same definition can
                    // still return different columns; such a view is not identical.
                    if (viewColumns.ContainsKey(key))
                    {
                        columnsOnly++;
                    }
                    else
                    {
                        identical++;
                    }
                }
                else if (leftMissing != rightMissing)
                {
                    // The difference is that one environment would not show the definition, not that the
                    // definitions differ, so it is reported as a gap rather than a difference.
                    var hidden = leftMissing ? _left : _right;
                    permissionGaps.Add($"{key}: definition not visible in {hidden} ({MissingReason(leftMissing ? leftText : rightText)})");
                }
                else if (Normalize(leftText) == Normalize(rightText))
                {
                    whitespaceOnly.Add(key);
                }
                else
                {
                    different.Add(key);
                }
            }

            WriteHeader(leftSummary, rightSummary);
            WriteEnvironment(leftSummary, rightSummary);
            WritePermissionGaps(leftSummary, rightSummary, permissionGaps);
            WriteList($"OBJECTS ONLY IN {_left.ToUpperInvariant()}", onlyLeft);
            WriteList($"OBJECTS ONLY IN {_right.ToUpperInvariant()}", onlyRight);
            WriteDifferences(different, leftFiles, rightFiles);
            WriteList("WHITESPACE-ONLY DIFFERENCES", whitespaceOnly);
            WriteList("VIEW COLUMNS THAT DIFFER", viewColumns.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => v.Value).ToList());
            WriteListDifference("UNSCRIPTED OBJECT TYPES", leftSummary.UnexportedObjects, rightSummary.UnexportedObjects);
            WriteListDifference("EXCLUDED TOOL OBJECTS", leftSummary.ExcludedToolObjects, rightSummary.ExcludedToolObjects);

            Line();
            Line("TOTALS");
            WriteAligned([
                ("Identical", identical.ToString()),
                ("Different", different.Count.ToString()),
                ("Whitespace-only", whitespaceOnly.Count.ToString()),
                ("Same definition, different view columns", columnsOnly.ToString()),
                ("Possible permission gaps", permissionGaps.Count.ToString()),
                ($"Only in {_left}", onlyLeft.Count.ToString()),
                ($"Only in {_right}", onlyRight.Count.ToString())
            ]);

            return _report.ToString();
        }

        private static SchemaSummary LoadSummary(string dir, string environment)
        {
            var path = Path.Join(dir, "summary.json");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"No export found for {environment} at {dir}. Run RunSchemaExport.bat for that environment first.");
            }

            return JsonSerializer.Deserialize<SchemaSummary>(File.ReadAllText(path), SchemaExport.JsonOptions)
                ?? throw new JsonException($"{path} is empty.");
        }

        /// <summary>
        /// Exported object files keyed by "Folder/name" (e.g. "Tables/dbo.Rotation"), so the same object
        /// lines up across environments. summary.json is compared separately.
        /// </summary>
        private static Dictionary<string, string> ObjectFiles(string dir)
        {
            return Directory.EnumerateFiles(dir, "*.sql", SearchOption.AllDirectories)
                .ToDictionary(
                    f => Path.GetRelativePath(dir, f).Replace('\\', '/')[..^".sql".Length],
                    f => f,
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The script with each run of whitespace between T-SQL tokens collapsed to one space, so indentation,
        /// trailing spaces, and blank lines stop mattering. Whitespace inside a string, a quoted name, or a
        /// comment is part of that token and kept, since it can change what the code does. Text the tokenizer
        /// cannot read is kept as is, so it compares exactly. The line break that ends a -- comment stays a line
        /// break: as a space, the code after it would read as part of the comment.
        /// </summary>
        internal static string Normalize(string text)
        {
            using var reader = new StringReader(text);
            var tokens = new TSql160Parser(initialQuotedIdentifiers: true).GetTokenStream(reader, out var errors);
            if (errors.Count > 0)
            {
                return text;
            }

            var sb = new StringBuilder();
            var separator = "";
            var afterLineComment = false;
            foreach (var token in tokens.Where(t => t.TokenType != TSqlTokenType.EndOfFile))
            {
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                {
                    separator = sb.Length == 0 ? "" : afterLineComment ? "\n" : " ";
                    continue;
                }
                sb.Append(separator).Append(token.Text);
                separator = "";
                afterLineComment = token.TokenType == TSqlTokenType.SingleLineComment;
            }
            return sb.ToString();
        }

        /// <summary>
        /// The views in both exports whose columns differ, keyed by their object file ("Views/dbo.vName"), each
        /// with a line saying how. A SELECT * view keeps the columns it had when last created or refreshed, so
        /// one refreshed in only one environment differs here even when its definition does not.
        /// Views whose names differ only in case (possible under a case-sensitive collation) are left out: the
        /// export gives the second a ~2 file suffix that its name alone cannot tell.
        /// </summary>
        private Dictionary<string, string> ViewColumnDifferences(SchemaSummary left, SchemaSummary right)
        {
            var caseClashes = left.Views.Concat(right.Views).Select(v => v.Name).Distinct(StringComparer.Ordinal)
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)
                .SelectMany(g => g).ToHashSet(StringComparer.Ordinal);
            var rightViews = right.Views.Where(v => !caseClashes.Contains(v.Name)).ToDictionary(v => v.Name, StringComparer.Ordinal);

            // Assigned rather than added, so two views landing on one key can never end the comparison.
            var differences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, detail) in left.Views
                .Where(v => rightViews.ContainsKey(v.Name))
                .Select(v => (Key: ViewFileKey(v.Name), Detail: ColumnDifference(v.Columns, rightViews[v.Name].Columns)))
                .Where(v => v.Detail != null))
            {
                differences[key] = $"{key}: {detail}";
            }
            return differences;
        }

        private string? ColumnDifference(List<ColumnSummary> left, List<ColumnSummary> right)
        {
            var leftColumns = left.Select(c => (c.Name, c.Type)).ToList();
            var rightColumns = right.Select(c => (c.Name, c.Type)).ToList();
            if (leftColumns.SequenceEqual(rightColumns))
            {
                return null;
            }

            var leftTypes = leftColumns.ToDictionary(c => c.Name, c => c.Type, StringComparer.Ordinal);
            var rightTypes = rightColumns.ToDictionary(c => c.Name, c => c.Type, StringComparer.Ordinal);
            var parts = new List<string>();
            var onlyLeft = leftColumns.Where(c => !rightTypes.ContainsKey(c.Name)).Select(c => c.Name).ToList();
            var onlyRight = rightColumns.Where(c => !leftTypes.ContainsKey(c.Name)).Select(c => c.Name).ToList();
            var types = leftColumns.Where(c => rightTypes.TryGetValue(c.Name, out var type) && type != c.Type)
                .Select(c => $"{c.Name} ({c.Type} in {_left}, {rightTypes[c.Name]} in {_right})").ToList();
            if (onlyLeft.Count > 0)
            {
                parts.Add($"only in {_left}: {string.Join(", ", onlyLeft)}");
            }
            if (onlyRight.Count > 0)
            {
                parts.Add($"only in {_right}: {string.Join(", ", onlyRight)}");
            }
            if (types.Count > 0)
            {
                parts.Add($"types differ: {string.Join(", ", types)}");
            }
            return parts.Count > 0 ? string.Join("; ", parts) : "column order differs";
        }

        /// <summary>
        /// The object file key of a view named [schema].[name] in summary.json, named as the export names files.
        /// </summary>
        internal static string ViewFileKey(string quotedName) =>
            SplitQuotedName(quotedName) is var (schema, name)
                ? $"Views/{SchemaExport.FileNamePart(schema)}.{SchemaExport.FileNamePart(name)}"
                : $"Views/{quotedName}";

        /// <summary>
        /// [schema].[name] back into its two names, with ]] read as ]; null when it is not in that form.
        /// </summary>
        internal static (string Schema, string Name)? SplitQuotedName(string quoted) =>
            QuotedTwoPartName.Match(quoted) is { Success: true } m
                ? (m.Groups[1].Value.Replace("]]", "]"), m.Groups[2].Value.Replace("]]", "]"))
                : null;

        private static string MissingReason(string text)
        {
            var start = text.IndexOf(MissingDefinitionMarker, StringComparison.Ordinal) + MissingDefinitionMarker.Length;
            var end = text.IndexOfAny(['\r', '\n'], start);
            return (end < 0 ? text[start..] : text[start..end]).Trim();
        }

        private void WriteHeader(SchemaSummary left, SchemaSummary right)
        {
            Line("================================================================================");
            Line($"SCHEMA COMPARE: {_connectionName}, {_left} vs {_right}");
            Line("================================================================================");
            WriteAligned([($"{_left} export", left.GeneratedAt), ($"{_right} export", right.GeneratedAt)]);
            Line();
            Line("Full line-by-line comparison:");
            Line($"  {GitDiff("--stat", _leftDir, _rightDir)}");
        }

        private void WriteEnvironment(SchemaSummary left, SchemaSummary right)
        {
            Line();
            Line("ENVIRONMENT (* = differs)");
            var rows = new (string Label, string Left, string Right)[]
            {
                ("Server", left.Server, right.Server),
                ("Database", left.Database, right.Database),
                ("SQL Server version", left.ProductVersion, right.ProductVersion),
                ("Compatibility level", left.CompatibilityLevel.ToString(), right.CompatibilityLevel.ToString()),
                ("Database collation", left.DatabaseCollation, right.DatabaseCollation),
                ("VIEW DEFINITION", left.HasViewDefinition ? "yes" : "NO", right.HasViewDefinition ? "yes" : "NO")
            };

            var labelWidth = rows.Max(r => r.Label.Length) + 2;
            var width = Math.Max(rows.Max(r => r.Left.Length), _left.Length) + 2;
            Line($"    {"".PadRight(labelWidth)}{_left.PadRight(width)}{_right}");
            foreach (var (label, l, r) in rows)
            {
                var marker = string.Equals(l, r, StringComparison.OrdinalIgnoreCase) ? " " : "*";
                Line($"  {marker} {label.PadRight(labelWidth)}{l.PadRight(width)}{r}");
            }
        }

        private void WriteAligned((string Label, string Value)[] rows)
        {
            var width = rows.Max(r => r.Label.Length) + 2;
            foreach (var (label, value) in rows)
            {
                Line($"  {(label + ":").PadRight(width)}{value}");
            }
        }

        private void WritePermissionGaps(SchemaSummary left, SchemaSummary right, List<string> gaps)
        {
            var lines = new List<string>();
            if (left.HasViewDefinition != right.HasViewDefinition)
            {
                var lacking = left.HasViewDefinition ? _right : _left;
                lines.Add($"The {lacking} login lacks VIEW DEFINITION. Objects it cannot see at all are missing from that " +
                          "export and show up below as \"only in\" the other environment.");
            }
            lines.AddRange(gaps);

            WriteList("POSSIBLE PERMISSION GAPS", lines);
        }

        private void WriteDifferences(List<string> keys, Dictionary<string, string> leftFiles, Dictionary<string, string> rightFiles)
        {
            Line();
            Line($"DEFINITIONS THAT DIFFER ({keys.Count})");
            if (keys.Count == 0)
            {
                Line("  (none)");
                return;
            }

            foreach (var key in keys)
            {
                Line($"  {key}");
                Line($"    {GitDiff(null, leftFiles[key], rightFiles[key])}");
            }
        }

        private void WriteListDifference(string title, List<string> left, List<string> right)
        {
            var lines = left.Except(right).Select(o => $"{o}: only in {_left}")
                .Concat(right.Except(left).Select(o => $"{o}: only in {_right}"))
                .Order().ToList();

            // These lists are informational; only a difference between environments is worth a section.
            if (lines.Count > 0)
            {
                WriteList(title, lines);
            }
        }

        private void WriteList(string title, List<string> items)
        {
            Line();
            Line($"{title} ({items.Count})");
            if (items.Count == 0)
            {
                Line("  (none)");
            }
            foreach (var item in items)
            {
                Line($"  {item}");
            }
        }

        /// <summary>
        /// A git diff --no-index command for two paths. Paths are relative to the repository root when
        /// there is one, so the command is short and runs from there; otherwise they are absolute.
        /// </summary>
        private string GitDiff(string? option, string leftPath, string rightPath)
        {
            string Display(string p) =>
                (_repoRoot != null ? Path.GetRelativePath(_repoRoot, p) : p).Replace('\\', '/');

            var flag = option != null ? $" {option}" : "";
            return $"git diff --no-index{flag} -- \"{Display(leftPath)}\" \"{Display(rightPath)}\"";
        }

        private static string? FindRepoRoot(string start)
        {
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            {
                if (Directory.Exists(Path.Join(dir.FullName, ".git")) || File.Exists(Path.Join(dir.FullName, ".git")))
                {
                    return dir.FullName;
                }
            }
            return null;
        }

        private void Line(string text = "") => _report.Append(text).Append("\r\n");
    }
}
