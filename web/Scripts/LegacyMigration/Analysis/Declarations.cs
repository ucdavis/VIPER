using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public enum Severity { Critical, Warning, Info }

    /// <summary>
    /// The per-database analysis.json, as written. Names in it are checked against the schema export
    /// by AnalysisPlan before anything runs. Every part is optional.
    /// </summary>
    public sealed class Declarations
    {
        /// <summary>
        /// Tables, views, and whole schemas ([schema]) the automatic checks visit; null visits everything.
        /// Declared checks name their own tables and may reach outside it.
        /// </summary>
        public List<string>? Include { get; init; }

        public List<string> Exclude { get; init; } = [];
        public Dictionary<string, string> DateColumns { get; init; } = new();
        // Nullable entries: see ReadOptions.
        public List<AutomaticCheckOverride?> AutomaticChecks { get; init; } = [];
        public List<SafeColumnEntry?> SafeColumns { get; init; } = [];

        // Kept as raw JSON so each entry can be read as the type its "check" field names, with an error
        // that lists the known checks rather than a serializer message.
        public List<JsonElement> Checks { get; init; } = [];

        /// <summary>
        /// The folder holding analysis.json, where customSql files are read from.
        /// </summary>
        [JsonIgnore]
        public string? Folder { get; private set; }

        /// <summary>
        /// Hand-edited, so comments and trailing commas are allowed, while an unknown field (usually a
        /// typo) is an error rather than silently ignored. So is a null in a field that cannot be null; a
        /// null inside a list is not covered, so those lists' entries are typed nullable and binding
        /// reports each null entry.
        /// </summary>
        internal static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };

        /// <summary>
        /// Returns empty declarations when the file does not exist, so a new migration runs the
        /// automatic checks with no setup.
        /// </summary>
        public static Declarations Load(string path)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!File.Exists(path))
            {
                return new Declarations { Folder = folder };
            }

            try
            {
                var declarations = JsonSerializer.Deserialize<Declarations>(File.ReadAllText(path), ReadOptions) ?? new Declarations();
                declarations.Folder = folder;
                return declarations;
            }
            catch (JsonException ex)
            {
                throw new DeclarationException([$"{path}: {ex.Message}"]);
            }
        }
    }

    public sealed class AutomaticCheckOverride
    {
        public required string AutomaticCheck { get; init; }
        public required string Table { get; init; }
        public Severity? Severity { get; init; }
        public bool Skip { get; init; }
        public required string Reason { get; init; }
    }

    /// <summary>
    /// A column whose values the report may show wherever they appear (row keys, values found by checks,
    /// the profile's range), because they are codes or other values that say nothing about a person. The
    /// reason is printed beside the column's range. A column whose name looks personal also needs
    /// LooksPersonal, saying why its values are safe anyway.
    /// </summary>
    public sealed class SafeColumnEntry
    {
        public required string Table { get; init; }
        public required string Column { get; init; }
        public required string Reason { get; init; }
        public string? LooksPersonal { get; init; }
    }

    /// <summary>
    /// Every problem found in analysis.json, reported together so they can all be fixed in one pass.
    /// </summary>
    public sealed class DeclarationException(IReadOnlyList<string> errors)
        : Exception($"{errors.Count} problem(s) in the declarations")
    {
        public IReadOnlyList<string> Errors { get; } = errors;
    }
}
