using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Flags columns that may hold a person's ID, from metadata only, so they can be found before person
    /// identifiers change. Matches words that name an ID type or say who made a change, never specific
    /// column names, so it works on a schema nobody has looked at yet.
    ///
    /// A column name is split into words at underscores, case changes, and digits ("vmdoPeople_mothraID"
    /// becomes vmdo, people, mothra, id), and only whole words match, so "iam" does not flag "William".
    /// </summary>
    public static class PersonIdRules
    {
        // A word names the ID type as is, or with "id" attached (an all-capitals name like IAMID cannot be split).
        private static readonly (string IdType, string[] Words)[] IdTypeWords =
        [
            ("MothraId", ["mothra"]),
            ("IamId", ["iam"]),
            ("Pidm", ["pidm"])
        ];

        // Followed by "by" or "user", or following "who", these say who did something (ModBy, addUser,
        // Who_Mod). Matched as word beginnings so abbreviations (mod, upd, updt) match too. A verb is
        // required so SortBy and GroupBy are not flagged.
        private static readonly string[] WhoDidItVerbs =
        [
            "creat", "mod", "upd", "chang", "chg", "edit", "delet", "enter", "add", "insert", "approv", "submit", "review", "sign"
        ];

        // Types that can hold a MothraId, IAM ID, or PIDM; smallint and tinyint are too small.
        private static readonly HashSet<string> CandidateTypes = new(StringComparer.Ordinal)
        {
            "char", "varchar", "text", "nchar", "nvarchar", "ntext", "int", "bigint", "numeric", "decimal"
        };

        private static readonly Regex WordPattern = new(
            "[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public static List<PersonIdColumn> Find(SchemaSummary summary)
        {
            var found = new Dictionary<(string Object, string Column), PersonIdColumn>();
            var objects = summary.Tables.Select(t => (t.Name, t.Columns)).Concat(summary.Views.Select(v => (v.Name, v.Columns)));

            foreach (var (objectName, columns) in objects)
            {
                foreach (var column in columns.Where(c => CandidateTypes.Contains(c.Type.Split('(')[0])))
                {
                    var match = MatchName(column.Name);
                    if (match != null)
                    {
                        found[(objectName, column.Name)] = new PersonIdColumn(
                            objectName, column.Name, column.Type, match.Value.Category, match.Value.IdType, match.Value.Reason);
                    }
                }
            }

            AddForeignKeyMatches(summary, found);

            return found.Values
                .OrderBy(c => c.Category).ThenBy(c => c.Object, StringComparer.Ordinal).ThenBy(c => c.Column, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// A name's words, lowercase: split at underscores, case changes, and digits.
        /// </summary>
        internal static List<string> Words(string name) =>
            WordPattern.Matches(name).Select(m => m.Value.ToLowerInvariant()).ToList();

        private static (PersonIdCategory Category, string? IdType, string Reason)? MatchName(string columnName)
        {
            var words = Words(columnName);

            foreach (var (idType, idWords) in IdTypeWords)
            {
                var word = words.Find(w => idWords.Any(i => w == i || w == i + "id"));
                if (word != null)
                {
                    return (PersonIdCategory.IdType, idType, $"name word \"{word}\"");
                }
            }

            for (var i = 0; i < words.Count; i++)
            {
                if (i + 1 < words.Count && words[i + 1] is "by" or "user" && IsWhoDidItVerb(words[i]))
                {
                    return (PersonIdCategory.Review, null, $"name words \"{words[i]} {words[i + 1]}\"");
                }
                if (words[i].Length > 2 && words[i].EndsWith("by", StringComparison.Ordinal) && IsWhoDidItVerb(words[i][..^2]))
                {
                    return (PersonIdCategory.Review, null, $"name word \"{words[i]}\"");
                }
                if (words[i] == "who" && i + 1 < words.Count && IsWhoDidItVerb(words[i + 1]))
                {
                    return (PersonIdCategory.Review, null, $"name words \"who {words[i + 1]}\"");
                }
            }

            return null;
        }

        private static bool IsWhoDidItVerb(string word) => Array.Exists(WhoDidItVerbs, v => word.StartsWith(v, StringComparison.Ordinal));

        /// <summary>
        /// A column whose foreign key references an ID-type column holds that ID type too, whatever its
        /// name. Repeated until nothing changes, so a chain of foreign keys is followed to its end.
        /// </summary>
        private static void AddForeignKeyMatches(SchemaSummary summary, Dictionary<(string Object, string Column), PersonIdColumn> found)
        {
            var columnTypes = summary.Tables
                .SelectMany(t => t.Columns.Select(c => (Key: (t.Name, c.Name), c.Type)))
                .ToDictionary(c => c.Key, c => c.Type);

            bool changed;
            do
            {
                changed = false;
                foreach (var fk in summary.ForeignKeys)
                {
                    for (var i = 0; i < fk.Columns.Count; i++)
                    {
                        var child = (fk.Table, fk.Columns[i]);
                        if (found.TryGetValue((fk.ReferencedTable, fk.ReferencedColumns[i]), out var parent)
                            && parent.Category == PersonIdCategory.IdType
                            && !(found.TryGetValue(child, out var existing) && existing.Category == PersonIdCategory.IdType))
                        {
                            found[child] = new PersonIdColumn(fk.Table, fk.Columns[i], columnTypes.GetValueOrDefault(child, ""),
                                PersonIdCategory.IdType, parent.IdType,
                                $"foreign key {fk.Name} references {fk.ReferencedTable}.{SchemaExport.Quote(fk.ReferencedColumns[i])}");
                            changed = true;
                        }
                    }
                }
            } while (changed);
        }
    }
}
