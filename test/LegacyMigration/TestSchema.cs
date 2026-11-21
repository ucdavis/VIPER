using System.Globalization;
using System.Text.Json;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// A small schema export shaped like the legacy databases the toolkit has been run on, with its person
/// columns found by PersonIdRules as the export finds them, and helpers that bind an analysis.json to it.
/// </summary>
internal static class TestSchema
{
    public const string Visitor = "[dbo].[visitor]";
    public const string Status = "[dbo].[Status]";
    public const string Person = "[dbo].[tblPerson]";
    public const string ChangeLog = "[audit].[ChangeLog]";
    public const string Students = "[dbo].[vStudents]";

    // MaxLength from the type, in characters, as the export records it: -1 for max, null for other types.
    private static ColumnSummary Column(string name, string type, bool identity = false)
    {
        var length = type.Contains('(') && !type.StartsWith("decimal", StringComparison.Ordinal) ? type[(type.IndexOf('(') + 1)..^1] : null;
        int? maxLength = null;
        if (length == "max")
        {
            maxLength = -1;
        }
        else if (int.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            maxLength = n;
        }
        return new(name, type, null, maxLength, true, false, null, identity ? new IdentitySummary("1", "1", "100") : null);
    }

    public static SchemaSummary Summary()
    {
        List<TableSummary> tables =
        [
            new(Visitor, 277, true, true, 0, ["id"], [],
            [
                Column("id", "int", identity: true), Column("mothraid", "varchar(8)"), Column("gradYear", "int"),
                Column("startWeekID", "int"), Column("endWeekID", "int"), Column("addUser", "varchar(8)"),
                Column("addDate", "datetime"), Column("notes", "varchar(max)")
            ]),
            new(Status, 18, true, true, 0, ["GradYear"], [], [Column("GradYear", "int"), Column("OpenDate", "datetime"), Column("Label", "varchar(20)"), Column("Abbrev", "char(5)")]),
            new(Person, 300, true, true, 0, ["person_MothraID", "person_TermCode"], [],
            [
                Column("person_MothraID", "char(8)"), Column("person_TermCode", "int"), Column("person_LastName", "varchar(50)"),
                Column("person_PercentAdmin", "float")
            ]),
            new(ChangeLog, 1000, true, true, 0, ["log_ID"], [],
                [Column("log_ID", "int", identity: true), Column("ModBy", "varchar(8)"), Column("detail", "varchar(200)")])
        ];
        List<ViewSummary> views = [new(Students, [Column("mothraId", "varchar(8)"), Column("name", "varchar(50)")])];
        List<ForeignKeySummary> foreignKeys =
            [new("FK_visitor_Status", Visitor, ["gradYear"], Status, ["GradYear"], false, false, false, "NO_ACTION", "NO_ACTION")];

        var withoutPeople = new SchemaSummary { Tables = tables, Views = views, ForeignKeys = foreignKeys };
        return new SchemaSummary
        {
            Database = "Legacy",
            CompatibilityLevel = 130,
            HasViewDefinition = true,
            Tables = tables,
            Views = views,
            ForeignKeys = foreignKeys,
            PersonIdColumns = PersonIdRules.Find(withoutPeople)
        };
    }

    public static SchemaIndex Index() => new(Summary(), new HashSet<string>());

    public static ColumnRef ColumnOf(string table, string column) =>
        Index().FindTableOrView(table)!.FindColumn(column)!;

    /// <summary>
    /// analysis.json read and bound as a run does it, to this schema or another.
    /// </summary>
    public static AnalysisPlan Plan(string json, SchemaSummary? summary = null) =>
        AnalysisPlan.Bind(summary ?? Summary(), JsonSerializer.Deserialize<Declarations>(json, Declarations.ReadOptions)!);

    /// <summary>
    /// What checks run with: the plan and the fake database, with lookups (such as VIPER) by connection name.
    /// </summary>
    public static AnalysisContext Context(AnalysisPlan plan, FakeDatabase database, Dictionary<string, FakeDatabase>? lookups = null) =>
        new(plan, database, name => lookups?.GetValueOrDefault(name) ?? throw new InvalidOperationException($"No lookup database {name}."));

    /// <summary>
    /// This schema with one foreign key from visitor.gradYear to Status, marked untrusted.
    /// </summary>
    public static SchemaSummary WithUntrustedForeignKey()
    {
        var summary = Summary();
        return new SchemaSummary
        {
            Database = summary.Database,
            CompatibilityLevel = summary.CompatibilityLevel,
            HasViewDefinition = summary.HasViewDefinition,
            Tables = summary.Tables,
            Views = summary.Views,
            PersonIdColumns = summary.PersonIdColumns,
            ForeignKeys = [summary.ForeignKeys[0] with { IsNotTrusted = true }]
        };
    }

    /// <summary>
    /// The errors binding reports, or none when it binds.
    /// </summary>
    public static IReadOnlyList<string> Errors(string json)
    {
        try
        {
            Plan(json);
            return [];
        }
        catch (DeclarationException ex)
        {
            return ex.Errors;
        }
    }

    /// <summary>
    /// The errors for one declared check, written as its JSON object.
    /// </summary>
    public static IReadOnlyList<string> CheckErrors(string check) => Errors($$"""{ "checks": [{{check}}] }""");
}
