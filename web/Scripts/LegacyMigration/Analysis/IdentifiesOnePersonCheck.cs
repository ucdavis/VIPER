using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class IdentifiesOnePersonDeclaration : CheckDeclaration
    {
        public required string Column { get; init; }
        public required string IdType { get; init; }
        public required string LookIn { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var column = binder.Column(where, "column", table, Column);
            if (!PersonIdType.Declarable.TryGetValue(IdType, out var idType))
            {
                binder.Error(where, $"\"idType\" \"{IdType}\" is not known. Known: {string.Join(", ", PersonIdType.Declarable.Keys)}.");
            }
            if (!IdentifiesOnePersonCheck.LookupTables.ContainsKey(LookIn))
            {
                binder.Error(where, $"\"lookIn\" \"{LookIn}\" is not known. Known: {string.Join(", ", IdentifiesOnePersonCheck.LookupTables.Keys.Order())}.");
            }
            if (column != null && !column.IsText && !column.IsNumber)
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type}; person IDs are text or numbers.");
                return null;
            }
            if (column == null || idType == null || !IdentifiesOnePersonCheck.LookupTables.TryGetValue(LookIn, out var lookup))
            {
                return null;
            }
            if (!lookup.Columns.ContainsKey(idType.Name))
            {
                var holders = IdentifiesOnePersonCheck.LookupTables.Where(l => l.Value.Columns.ContainsKey(idType.Name)).Select(l => l.Key).Order();
                binder.Error(where, $"{LookIn} has no {idType.Name} column. Look {idType.Name}s up in {string.Join(" or ", holders)}.");
                return null;
            }

            return new IdentifiesOnePersonCheck
            {
                Name = $"identifiesOnePerson: {column.Name} as {idType.Name} in {LookIn}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [column.Table],
                Column = column,
                IdType = idType,
                Lookup = lookup
            };
        }
    }

    /// <summary>
    /// A kind of person ID. Shape describes its values, and IsShaped (given a trimmed value) decides which
    /// are worth looking up as this type and, when IsShown, which may be copied into the report. Clean is
    /// the looser form tried when a stored value does not match exactly. Number IDs are looked up as int.
    /// LookupKey, when set, turns the lookup column into the value compared, given the column's SQL, and
    /// Accepted turns a stored value into the same form; an ID that matches only through them is still
    /// found exactly, since both are ways the ID is properly written.
    /// </summary>
    public sealed record PersonIdType(string Name, string Shape, Func<string, bool> IsShaped, Func<string, string> Clean, bool IsNumber,
        Func<string, string>? LookupKey = null, bool IsShown = true, Func<string, string>? Accepted = null)
    {
        /// <summary>
        /// A stored value as looked up exactly.
        /// </summary>
        public string Exact(string text) => Accepted?.Invoke(text) ?? text;

        /// <summary>
        /// A stored value as looked up after cleanup: trimmed, cleaned, and in the accepted form. Case is
        /// ignored when matching it.
        /// </summary>
        public string Loose(string text) => Exact(Clean(text.Trim()));

        // The domain of a MailId stored without one.
        public const string MailDomain = "ucdavis.edu";

        private const int MothraIdLength = 8;
        private const int IamIdLength = 10;
        private const int PidmLength = 8;

        // A MothraId stored as a number loses its leading zeros, so shorter digit strings are padded back.
        public static readonly PersonIdType MothraId = new("MothraId", $"up to {MothraIdLength} digits",
            t => IsDigits(t) && t.Length <= MothraIdLength,
            t => IsDigits(t) && t.Length < MothraIdLength ? t.PadLeft(MothraIdLength, '0') : t, false);

        public static readonly PersonIdType IamId = new("IamId", $"{IamIdLength} digits",
            t => IsDigits(t) && t.Length == IamIdLength, t => t, false);

        // VIPER's users.Person primary key (AAUD's aaudUserID). Any positive whole number has its shape.
        public static readonly PersonIdType PersonId = new("PersonId", "a whole number",
            t => IsDigits(t) && int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0,
            t => IsDigits(t) && int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n.ToString(CultureInfo.InvariantCulture) : t, true);

        // Banner's person number, stored as text in aaudUser. Stored as a number elsewhere it has no leading
        // zeros, so cleanup drops them.
        public static readonly PersonIdType Pidm = new("Pidm", $"up to {PidmLength} digits",
            t => IsDigits(t) && t.Length <= PidmLength,
            t => IsDigits(t) && t.TrimStart('0').Length > 0 ? t.TrimStart('0') : t, false);

        // LoginIds and MailIds are often derived from names, so they are never copied into the report, even
        // in a column declared to hold them.
        public static readonly PersonIdType LoginId = new("LoginId", "any text", t => t.Length > 0, t => t, false, IsShown: false);

        // A MailId may be stored as the mailbox alone or as the full address (aaudUser.mailID can be), so
        // both sides are compared as full addresses, a mailbox alone taking the UC Davis domain. The domain
        // must match: an address elsewhere names someone else's mailbox, even with the same name.
        public static readonly PersonIdType MailId = new("MailId", "any text", t => t.Length > 0, t => t, false,
            c => $"CASE WHEN CHARINDEX('@', {c}) > 0 THEN {c} ELSE {c} + '@{MailDomain}' END", IsShown: false,
            Accepted: t => t.Contains('@') ? t : $"{t}@{MailDomain}");

        public static readonly IReadOnlyDictionary<string, PersonIdType> Declarable =
            new[] { MothraId, IamId, PersonId, Pidm, LoginId, MailId }.ToDictionary(t => t.Name, StringComparer.Ordinal);

        public static bool IsDigits(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);
    }

    /// <summary>
    /// Puts each distinct ID in a column in exactly one category by what a lookup finds. Nothing is
    /// changed or chosen. IDs are copied into the report only when they have the shape of the declared ID
    /// type or are a known placeholder word, so a column that holds something else, such as LoginIds,
    /// names, or phone numbers, cannot leak it; other IDs are described by shape and their rows reported
    /// by key.
    /// </summary>
    public sealed class IdentifiesOnePersonCheck : DeclaredCheck
    {
        // SQL Server allows 2,100 parameters per command.
        private const int BatchSize = 1000;

        /// <summary>
        /// Connection names the connection that reaches the table, for the analysis's environment.
        /// Columns holds the column for each PersonIdType name.
        /// </summary>
        public sealed record LookupTable(string Connection, string Table, IReadOnlyDictionary<string, string> Columns);

        /// <summary>
        /// Where people can be looked up, each over its own database's connection, since legacy logins
        /// cannot be assumed to read VIPER or AAUD. users.Person is what migrations into VIPER link people
        /// to. It has no PIDM, so PIDMs are looked up in aaudUser only.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, LookupTable> LookupTables = new Dictionary<string, LookupTable>(StringComparer.Ordinal)
        {
            ["AAUD.dbo.aaudUser"] = new("AAUD", "[dbo].[aaudUser]", new Dictionary<string, string>
            {
                ["MothraId"] = "[mothraID]",
                ["IamId"] = "[iam_id]",
                ["PersonId"] = "[aaudUserID]",
                ["Pidm"] = "[PIDM]",
                ["LoginId"] = "[loginID]",
                ["MailId"] = "[mailID]"
            }),
            ["VIPER.users.Person"] = new("VIPER", "[users].[Person]", new Dictionary<string, string>
            {
                ["MothraId"] = "[MothraId]",
                ["IamId"] = "[IamId]",
                ["PersonId"] = "[PersonId]",
                ["LoginId"] = "[LoginId]",
                ["MailId"] = "[MailId]"
            })
        };

        private enum Category
        {
            FoundOne, NotFound, FoundSeveral, FoundAfterCleanup, FoundAfterCleanupSeveral, Placeholder, DifferentIdType, DifferentIdTypeSeveral
        }

        private static readonly (Category Category, string Label, bool IsProblem)[] Categories =
        [
            (Category.FoundOne, "Found one person", false),
            (Category.NotFound, "Not found", true),
            (Category.FoundSeveral, "Found several people", true),
            (Category.FoundAfterCleanup, "Found only after cleanup", true),
            (Category.FoundAfterCleanupSeveral, "Found only after cleanup, several people", true),
            (Category.Placeholder, "Placeholder", false),
            (Category.DifferentIdType, "Found as a different ID type", true),
            (Category.DifferentIdTypeSeveral, "Found as a different ID type, several people", true)
        ];

        public required ColumnRef Column { get; init; }
        public required PersonIdType IdType { get; init; }
        public required LookupTable Lookup { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var result = NewResult(Name);
            var ids = ReadIds(context, out var nullRows);
            result.Details.Add($"Looked up in {Lookup.Table} over the {Lookup.Connection} connection.");
            if (Column.IsPadded)
            {
                result.Details.Add($"{Column.Summary.Type} pads values shorter than the column with spaces, so IDs were matched without " +
                    "the padding. SQL comparisons ignore it, but C# comparisons after loading do not, and copying the values into a " +
                    "varchar or nvarchar column keeps the spaces: trim these IDs when reading them.");
            }

            var candidates = ids.Where(i => !IsPlaceholder(i.Text)).ToList();
            var found = FindPeople(context, IdType,
                candidates.SelectMany(i => new[] { IdType.Exact(i.Text), IdType.Loose(i.Text) }).Distinct(StringComparer.Ordinal).ToList());
            // After cleanup, the people of every stored form that cleans to the same value, each person once.
            var foundLoose = found
                .GroupBy(f => f.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.SelectMany(f => f.Value).ToHashSet(StringComparer.Ordinal).Count, StringComparer.OrdinalIgnoreCase);
            if (IdType == PersonIdType.MailId)
            {
                AddFullAddressNote(result, candidates);
            }

            var categorized = new List<(object Raw, string Text, long Rows, Category Category)>();
            var unmatched = new List<(object Raw, string Text, long Rows)>();
            foreach (var id in ids)
            {
                if (Categorize(id.Text, found, foundLoose) is { } category)
                {
                    categorized.Add((id.Raw, id.Text, id.Rows, category));
                }
                else
                {
                    unmatched.Add(id);
                }
            }

            var foundAs = FindAsOtherTypes(context, unmatched.Select(u => u.Text.Trim()).Distinct(StringComparer.Ordinal).ToList());
            foreach (var id in unmatched)
            {
                var category = !foundAs.TryGetValue(id.Text.Trim(), out var match) ? Category.NotFound
                    : match.People == 1 ? Category.DifferentIdType
                    : Category.DifferentIdTypeSeveral;
                categorized.Add((id.Raw, id.Text, id.Rows, category));
            }

            if (nullRows > 0)
            {
                result.Counts.Add(new ResultCount("NULL (not checked): rows", nullRows));
            }
            if (ids.Count == 0)
            {
                result.Details.Add($"No IDs to check: {(nullRows > 0 ? "every row is NULL" : $"{Column.Table.Name} is empty")}.");
            }
            foreach (var (category, label, isProblem) in Categories)
            {
                // Empty categories are left out so the result reads at a glance; Found one person shows
                // whenever there were IDs to look up.
                var inCategory = categorized.Where(c => c.Category == category).ToList();
                if (inCategory.Count == 0 && (category != Category.FoundOne || ids.Count == 0))
                {
                    continue;
                }
                result.Counts.Add(new ResultCount($"{label}: IDs", inCategory.Count));
                result.Counts.Add(new ResultCount($"{label}: rows", inCategory.Sum(c => c.Rows)));
                if (category == Category.Placeholder)
                {
                    AddPlaceholderList(result, inCategory.Select(c => (c.Raw, c.Rows)).ToList());
                }
                else if (isProblem && inCategory.Count > 0 && category is not (Category.DifferentIdType or Category.DifferentIdTypeSeveral))
                {
                    AddIdList(context, result, label, inCategory.Select(c => (c.Raw, c.Text, c.Rows)).ToList());
                }
            }

            AddDifferentTypes(context, result, categorized, foundAs);

            result.Status = categorized.Exists(c => Categories.Any(k => k.Category == c.Category && k.IsProblem))
                ? CheckStatus.ProblemsFound
                : CheckStatus.NoProblemsFound;
            return [result];
        }

        /// <summary>
        /// The details for values found as other ID types. These may be LoginIds or MailIds, which never go
        /// in the report, so only how many were found as each type is given, and their rows by key.
        /// </summary>
        private void AddDifferentTypes(AnalysisContext context, CheckResult result,
            List<(object Raw, string Text, long Rows, Category Category)> categorized, Dictionary<string, (List<string> Types, int People)> foundAs)
        {
            var different = categorized.Where(c => c.Category == Category.DifferentIdType).ToList();
            if (different.Count > 0)
            {
                result.Grids.Add(MatchedAs("Found as a different ID type", different, foundAs));
                AddDifferentTypeRows(context, result, different.Select(d => d.Raw).ToList());
            }
            var several = categorized.Where(c => c.Category == Category.DifferentIdTypeSeveral).ToList();
            if (several.Count > 0)
            {
                result.Grids.Add(MatchedAs("Found as a different ID type, several people", several, foundAs));
                result.Details.Add("Found as a different ID type, several people: each value names more than one person, by one ID type " +
                    "or across them (such as one person's LoginId and another's MailId), so the migration cannot tell which person is meant.");
                AddRowsByKey(context, result, several.Select(s => s.Raw).ToList());
            }
        }

        /// <summary>
        /// How many IDs and rows matched each combination of other ID types ("LoginId and MailId", "MailId
        /// only"), which add up to the whole, then, when any value matched more than one type, each type's
        /// own coverage ("Any MailId"), so the one type that explains the most values stands out.
        /// </summary>
        private static ResultGrid MatchedAs(string title, List<(object Raw, string Text, long Rows, Category Category)> values,
            Dictionary<string, (List<string> Types, int People)> foundAs)
        {
            var matched = values.Select(v => (foundAs[v.Text.Trim()].Types, v.Rows)).ToList();
            var combinations = matched
                .GroupBy(m => m.Types.Count == 1 ? $"{m.Types[0]} only" : ResultSql.JoinAnd(m.Types), StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Ids: g.Count(), Rows: g.Sum(m => m.Rows)))
                .OrderByDescending(c => c.Ids);
            var coverage = matched.Exists(m => m.Types.Count > 1)
                ? matched.SelectMany(m => m.Types.Select(t => (Type: t, m.Rows)))
                    .GroupBy(t => t.Type, StringComparer.Ordinal)
                    .Select(g => (Label: $"Any {g.Key}", Ids: g.Count(), Rows: g.Sum(t => t.Rows)))
                    .OrderByDescending(c => c.Ids)
                : Enumerable.Empty<(string Label, int Ids, long Rows)>();
            var rows = combinations.Concat(coverage)
                .Select(c => new List<string> { c.Label, c.Ids.ToString("N0", CultureInfo.InvariantCulture), c.Rows.ToString("N0", CultureInfo.InvariantCulture) })
                .ToList();
            // The rows are combinations and coverage, not IDs, so the heading counts the IDs instead.
            var ids = values.Count == 1 ? "1 ID" : $"{values.Count.ToString("N0", CultureInfo.InvariantCulture)} IDs";
            return new ResultGrid(title, ["Matched as"], ["IDs", "Rows"], rows, rows.Count, CountText: ids);
        }

        /// <summary>
        /// The column's distinct values, grouped exactly (a binary collation and the length), so values that
        /// differ only by case or trailing spaces stay apart and can be told from exact matches. A char or
        /// nchar column's padding is trimmed first: every shorter value has it, so it says nothing about the
        /// data, and SQL's = ignores it when the rows are looked up again by value.
        /// </summary>
        private List<(object Raw, string Text, long Rows)> ReadIds(AnalysisContext context, out long nullRows)
        {
            var value = Column.IsPadded ? $"RTRIM({Column.ComparableSql("t")})" : Column.ComparableSql("t");
            var select = Column.IsText ? $"{value} COLLATE Latin1_General_BIN2" : value;
            var group = Column.IsText ? $"{select}, DATALENGTH({value})" : value;
            var rows = context.Database.QueryRows($"SELECT {select}, COUNT_BIG(*)\nFROM {Column.Table.Name} AS t\nGROUP BY {group}");

            nullRows = rows.Where(r => r[0] == null).Sum(r => Convert.ToInt64(r[1], CultureInfo.InvariantCulture));
            return rows.Where(r => r[0] != null)
                .Select(r => (Raw: r[0]!, Text: Convert.ToString(r[0], CultureInfo.InvariantCulture) ?? "", Rows: Convert.ToInt64(r[1], CultureInfo.InvariantCulture)))
                .ToList();
        }

        /// <summary>
        /// A stored ID's category by what the lookup found as written (in the type's accepted form), then
        /// after cleanup; null when neither found anyone, for the other ID types to be tried.
        /// </summary>
        private Category? Categorize(string text, Dictionary<string, HashSet<string>> found, Dictionary<string, int> foundLoose) =>
            IsPlaceholder(text) ? Category.Placeholder
            : found.TryGetValue(IdType.Exact(text), out var people) ? (people.Count == 1 ? Category.FoundOne : Category.FoundSeveral)
            : foundLoose.TryGetValue(IdType.Loose(text), out var count) ? (count == 1 ? Category.FoundAfterCleanup : Category.FoundAfterCleanupSeveral)
            : null;

        /// <summary>
        /// For a MailId column, how many IDs are full addresses, which are matched with their domain, so the
        /// reader knows an address at another domain is not found rather than matched by its mailbox.
        /// </summary>
        private static void AddFullAddressNote(CheckResult result, List<(object Raw, string Text, long Rows)> ids)
        {
            var full = ids.Count(i => i.Text.Contains('@'));
            if (full > 0)
            {
                result.Details.Add($"{(full == 1 ? "1 ID is" : $"{full:N0} IDs are")} stored as a full address, matched with its domain: " +
                    $"an address at another domain is not found, even when its mailbox is. IDs with no domain are matched as @{PersonIdType.MailDomain} mailboxes.");
            }
        }

        /// <summary>
        /// The people (by the lookup table's PersonId column) holding each value in one ID type's column,
        /// keyed by the value as stored there (a number ID as its digits). A number ID is only looked up
        /// for values written as a plain int. Person keys never leave this class.
        /// </summary>
        private Dictionary<string, HashSet<string>> FindPeople(AnalysisContext context, PersonIdType type, List<string> values)
        {
            var column = $"l.{Lookup.Columns[type.Name]}";
            var key = type.LookupKey?.Invoke(column) ?? column;
            var person = $"l.{Lookup.Columns[PersonIdType.PersonId.Name]}";
            var usable = type.IsNumber
                ? values.Where(v => int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n.ToString(CultureInfo.InvariantCulture) == v)
                : values.Where(v => v.Trim().Length > 0);

            var found = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var database = context.LookupDatabase(Lookup.Connection);
            // Typed to match the lookup column, so the comparison can use its index (except through a
            // LookupKey expression, which scans).
            var batches = usable.Chunk(BatchSize).Select(batch => batch.Select((v, i) => type.IsNumber
                ? new SqlParameter($"@v{i}", SqlDbType.Int) { Value = int.Parse(v, CultureInfo.InvariantCulture) }
                : new SqlParameter($"@v{i}", SqlDbType.VarChar, 100) { Value = v }).ToArray());
            foreach (var parameters in batches)
            {
                var rows = database.QueryRows(
                    $"SELECT DISTINCT {key}, {person}\nFROM {Lookup.Table} AS l\n" +
                    $"WHERE {key} IN ({string.Join(", ", parameters.Select(p => p.ParameterName))})", parameters);
                foreach (var row in rows.Where(r => r[0] != null))
                {
                    var value = Convert.ToString(row[0], CultureInfo.InvariantCulture)!;
                    if (!found.TryGetValue(value, out var people))
                    {
                        found[value] = people = [];
                    }
                    people.Add(Convert.ToString(row[1], CultureInfo.InvariantCulture)!);
                }
            }
            return found;
        }

        /// <summary>
        /// For each unmatched value (trimmed), the other ID types it is found as and how many different
        /// people those matches name. Each type is tried for every value shaped like it, not only until one
        /// matches: a value can be one person's LoginId and another's MailId, and the migration must not
        /// settle that by the order it tries them. PersonId and Pidm are never tried: both are dense
        /// sequences of numbers that MothraIds and other numbers share the shape of, so a match would
        /// usually be a coincidence rather than a sign of what the column holds.
        /// </summary>
        private Dictionary<string, (List<string> Types, int People)> FindAsOtherTypes(AnalysisContext context, List<string> values)
        {
            var matches = new Dictionary<string, (List<string> Types, HashSet<string> People)>(StringComparer.Ordinal);
            foreach (var type in new[] { PersonIdType.MothraId, PersonIdType.IamId, PersonIdType.LoginId, PersonIdType.MailId }.Where(t => t != IdType))
            {
                var shaped = values.Where(type.IsShaped).ToList();
                var people = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (value, found) in FindPeople(context, type, shaped.Select(type.Loose).Distinct(StringComparer.Ordinal).ToList()))
                {
                    if (!people.TryGetValue(value.Trim(), out var all))
                    {
                        people[value.Trim()] = all = [];
                    }
                    all.UnionWith(found);
                }
                foreach (var value in shaped.Where(v => people.ContainsKey(type.Loose(v))))
                {
                    if (!matches.TryGetValue(value, out var match))
                    {
                        matches[value] = match = ([], []);
                    }
                    match.Types.Add(type.Name);
                    match.People.UnionWith(people[type.Loose(value)]);
                }
            }
            return matches.ToDictionary(m => m.Key, m => (m.Value.Types, m.Value.People.Count), StringComparer.Ordinal);
        }

        /// <summary>
        /// Blank, zeros only, or a word that stands in for a missing value (PlaceholderTextCheck.Words). A
        /// fixed list that reveals nothing about anyone, so these are shown as stored.
        /// </summary>
        private static bool IsPlaceholder(string text) =>
            text.Trim().TrimStart('0').Length == 0 || PlaceholderTextCheck.IsWord(text);

        /// <summary>
        /// Each placeholder value as stored (blank, zeros, or a placeholder word) with its row count.
        /// </summary>
        private static void AddPlaceholderList(CheckResult result, List<(object Raw, long Rows)> ids)
        {
            var shown = ids
                .GroupBy(i => ResultSql.ShowValue(i.Raw))
                .Select(g => (Label: g.Key, Rows: g.Sum(i => i.Rows)))
                .OrderByDescending(v => v.Rows)
                .Select(v => $"{v.Label} ({ResultSql.RowCount(v.Rows)})");
            result.Details.Add($"Placeholder: {string.Join(", ", shown)}");
        }

        /// <summary>
        /// Lists the IDs shaped like the declared ID type (after trimming), quoted so stray spaces show,
        /// unless the type is never shown (LoginId, MailId). Anything else may be a LoginId, a name, a phone
        /// number, or free text, so it is described by its shape and its rows are listed by key instead.
        /// </summary>
        private void AddIdList(AnalysisContext context, CheckResult result, string label, List<(object Raw, string Text, long Rows)> ids)
        {
            bool Shown(string text) => IdType.IsShown && IdType.IsShaped(text.Trim());
            var shaped = ids.Where(i => Shown(i.Text)).Select(i => i.Text).Order(StringComparer.Ordinal).ToList();
            var unshaped = ids.Where(i => !Shown(i.Text)).ToList();
            var listed = shaped.Take(AnalysisContext.ExampleLimit).Select(i => ResultSql.ShowValue(i)).ToList();
            // The counts are in the result's table above, so the line holds only what it adds.
            var line = $"{label}:";
            if (listed.Count > 0)
            {
                line += $" {string.Join(", ", listed)}";
            }
            if (shaped.Count > listed.Count)
            {
                line += $", and {shaped.Count - listed.Count:N0} more";
            }
            if (unshaped.Count > 0)
            {
                var why = IdType.IsShown ? $"not shaped like {IdType.Name} ({IdType.Shape})" : $"{IdType.Name}s are never shown";
                var notShown = $"{unshaped.Count:N0} not shown, {why}: {ResultSql.ShapeSummary(unshaped.Select(u => u.Text.Trim()).ToList())}";
                line += listed.Count > 0 ? $" ({notShown})" : $" {notShown}";
            }
            result.Details.Add(line);

            if (unshaped.Count > 0)
            {
                AddRowsByKey(context, result, unshaped.Select(u => u.Raw).ToList());
            }
        }

        /// <summary>
        /// The condition for rows holding any of the values ReadIds returned, compared exactly as they were
        /// grouped, so a value's rows do not include those of a variant counted in another category: under a
        /// binary collation, and, since even that ignores trailing spaces, by length too. The values go in as
        /// parameters of the column's own kind of text, so the lengths count the same bytes. A char or nchar
        /// column's values were read without their padding, which every stored value has, so its lengths are
        /// not compared.
        /// </summary>
        private (string Sql, SqlParameter[] Parameters) ExactlyAny(List<object> values)
        {
            if (!Column.IsText)
            {
                var (inList, numbers) = ResultSql.InList(values);
                return ($"t.{Column.Sql} IN ({inList})", numbers);
            }
            var e = Column.ComparableSql("t");
            var type = Column.BaseType is "nchar" or "nvarchar" or "ntext" ? SqlDbType.NVarChar : SqlDbType.VarChar;
            var parameters = values.Select((v, i) => new SqlParameter($"@p{i}", type, -1) { Value = v }).ToArray();
            var matches = parameters.Select(p => Column.IsPadded
                ? $"{e} COLLATE Latin1_General_BIN2 = {p.ParameterName}"
                : $"({e} COLLATE Latin1_General_BIN2 = {p.ParameterName} AND DATALENGTH({e}) = DATALENGTH({p.ParameterName}))");
            return ($"({string.Join("\n   OR ", matches)})", parameters);
        }

        /// <summary>
        /// The rows holding IDs that are not shown, by key, so each can be looked up directly.
        /// </summary>
        private void AddRowsByKey(AnalysisContext context, CheckResult result, List<object> values)
        {
            if (values.Count > BatchSize)
            {
                result.Details.Add($"  Too many values ({values.Count:N0}) to list their rows.");
                return;
            }

            var (exactly, parameters) = ExactlyAny(values);
            var rows = ResultSql.FindAffectedRows(context, Column.Table, exactly, parameters);
            var dated = ResultSql.RangeText(rows.Earliest, rows.Latest) is { } range ? $"; dated {range}" : "";
            if (Column.Table.KeyColumns.Count == 0)
            {
                result.Details.Add($"  Rows: {rows.Total:N0}{dated} ({Column.Table.Name} has no key to list them by)");
                return;
            }
            var shown = rows.Keys.Count < rows.Total ? $"{rows.Keys.Count:N0} of {rows.Total:N0}" : $"{rows.Total:N0}";
            result.Details.Add($"  Rows by {ResultSql.KeyNames(Column.Table.KeyColumns)} ({shown}): {string.Join(", ", rows.Keys)}{dated}");
        }

        /// <summary>
        /// Values found as another ID type may be LoginIds or MailIds, which never go in the report, so their
        /// rows are reported by key instead.
        /// </summary>
        private void AddDifferentTypeRows(AnalysisContext context, CheckResult result, List<object> values)
        {
            if (values.Count > BatchSize)
            {
                result.Details.Add($"Found as a different ID type: too many values ({values.Count:N0}) to list their rows.");
                return;
            }

            var (exactly, parameters) = ExactlyAny(values);
            var rows = ResultSql.AddAffectedRows(context, result, Column.Table, exactly, parameters);
            result.Details.Add(Column.Table.KeyColumns.Count > 0
                ? $"Found as a different ID type: {ResultSql.RowCount(rows)}, listed by key under Examples."
                : $"Found as a different ID type: {ResultSql.RowCount(rows)} ({Column.Table.Name} has no key to list them by).");
        }
    }
}
