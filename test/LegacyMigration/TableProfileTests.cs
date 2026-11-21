using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The profile read is one query per table that most automatic checks share. It must never read a lowest
/// or highest value it is not allowed to report, and must map its one wide row back to the right columns.
/// </summary>
[InvariantCulture]
public class TableProfileTests
{
    private static (AnalysisContext Context, FakeDatabase Database, TableRef Table) Visitor(string json = "{}")
    {
        var plan = TestSchema.Plan(json);
        var table = plan.Schema.FindTable(TestSchema.Visitor)!;
        var database = new FakeDatabase().AnswerProfile(TestSchema.Visitor, new ProfileRow(table, 277)
            .Number("id", distinct: 277, lowest: 1, highest: 300)
            .Text("mothraid", nulls: 2, blanks: 1, distinct: 250, longest: 8, numbers: 270, leadingZeros: 260, placeholders: 3,
                lengths: [0, 0, 0, 0, 0, 0, 1, 273])
            .Number("gradYear", distinct: 9, lowest: 2018, highest: 2026)
            .Date("addDate", distinct: 270, earliest: 2019, latest: 2026, timed: 12, timedEarliest: 2019, timedLatest: 2020));
        return (TestSchema.Context(plan, database), database, table);
    }

    [Fact]
    public void Read_LowestAndHighest_OnlyForColumnsWhoseValuesCanBeShown()
    {
        var (context, database, table) = Visitor();

        context.Profile(table);

        var sql = Assert.Single(database.Queries).Sql;
        Assert.Contains("MIN(t.[id])", sql, StringComparison.Ordinal);
        // A foreign key into a column the database did not make is not enough.
        Assert.DoesNotContain("MIN(t.[gradYear])", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MIN(t.[startWeekID])", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MAX(t.[endWeekID])", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MIN(t.[notes])", sql, StringComparison.Ordinal);
        Assert.Contains("MIN(YEAR(t.[addDate]))", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MIN(t.[addDate])", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_SafeColumn_IsReadWithItsReason()
    {
        var (context, database, table) = Visitor("""
            { "safeColumns": [{ "table": "[dbo].[visitor]", "column": "startWeekID", "reason": "Week numbers." }] }
            """);

        var profile = context.Profile(table);

        Assert.Contains("MIN(t.[startWeekID])", database.Queries[0].Sql, StringComparison.Ordinal);
        Assert.Equal("Week numbers.", profile.Columns.Single(c => c.Column == "startWeekID").RangeShownBecause);
    }

    [Fact]
    public void Read_FloatColumnInATable_IsNeverRangeRead()
    {
        var plan = TestSchema.Plan("{}");
        var table = plan.Schema.FindTable(TestSchema.Person)!;
        var database = new FakeDatabase().AnswerProfile(TestSchema.Person, new ProfileRow(table, 300));

        TestSchema.Context(plan, database).Profile(table);

        Assert.DoesNotContain("MIN(t.[person_PercentAdmin])", database.Queries[0].Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MIN(t.[person_TermCode])", database.Queries[0].Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_MapsEachAggregateBackToItsColumn()
    {
        var (context, _, table) = Visitor();

        var profile = context.Profile(table);

        Assert.Equal(277, profile.Rows);
        var id = profile.Columns.Single(c => c.Column == "id");
        Assert.Equal(("1", "300", null), (id.Lowest, id.Highest, id.RangeWithheld));
        var weeks = profile.Columns.Single(c => c.Column == "startWeekID");
        Assert.Null(weeks.Lowest);
        Assert.NotNull(weeks.RangeWithheld);
        var mothraId = profile.Columns.Single(c => c.Column == "mothraid");
        Assert.Equal((2L, 1L, 250L, 8L), (mothraId.Nulls, mothraId.Blanks, mothraId.Distinct, mothraId.LongestText));
        var extras = profile.Extras["mothraid"];
        Assert.Equal((270L, 260L, 3L), (extras.Numbers, extras.LeadingZeros, extras.Placeholders));
        Assert.Equal([0L, 0L, 0L, 0L, 0L, 0L, 1L, 273L], Assert.IsType<long[]>(extras.Lengths));
        var added = profile.Columns.Single(c => c.Column == "addDate");
        Assert.Equal((2019, 2026), (added.EarliestYear, added.LatestYear));
        Assert.Equal((12L, 2019, 2020), (profile.Extras["addDate"].TimesOfDay, profile.Extras["addDate"].TimesOfDayEarliestYear,
            profile.Extras["addDate"].TimesOfDayLatestYear));
    }

    [Fact]
    public void Read_WideTable_IsSplitIntoQueriesWhoseRowsMapBackToEveryColumn()
    {
        // 40 short varchar columns take 32 select-list items each, too many for one query.
        var summary = TestSchema.Summary();
        summary.Tables.Add(new TableSummary("[dbo].[Wide]", 5, false, false, 0, [], [],
            Enumerable.Range(1, 40).Select(i => new ColumnSummary($"c{i}", "varchar(20)", null, 20, true, false, null, null)).ToList()));
        var plan = TestSchema.Plan("{}", summary);
        var table = plan.Schema.FindTable("[dbo].[Wide]")!;
        var full = new ProfileRow(table, 5).Text("c1", distinct: 11).Text("c40", distinct: 440).Build();
        // Each query is answered with its slice of the one row, in order.
        var offset = 0;
        var asked = "";
        var database = new FakeDatabase().AnswerWhen(
            sql =>
            {
                asked = sql;
                return sql.EndsWith("FROM [dbo].[Wide] AS t", StringComparison.Ordinal);
            },
            _ =>
            {
                var items = asked.Split(",\n    ").Length;
                var slice = full[offset..(offset + items)];
                offset += items;
                return [slice];
            });

        var profile = TestSchema.Context(plan, database).Profile(table);

        Assert.Null(profile.Error);
        Assert.Equal(2, database.Queries.Count);
        Assert.All(database.Queries, q => Assert.True(q.Sql.Split(",\n    ").Length <= 1000));
        Assert.Equal((5L, 11L, 440L), (profile.Rows, profile.Columns.Single(c => c.Column == "c1").Distinct, profile.Columns.Single(c => c.Column == "c40").Distinct));
        Assert.Equal(full.Length, offset);
    }

    [Fact]
    public void Profile_IsReadOncePerTable()
    {
        var (context, database, table) = Visitor();

        context.Profile(table);
        context.Profile(table);

        Assert.Single(database.Queries);
    }
}
