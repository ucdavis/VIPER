using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Person columns are the ones whose key values the report masks, since they may hold LoginIds. A column
/// these rules miss has its values shown as written, so the rules are tested name by name.
/// </summary>
[InvariantCulture]
public class PersonIdRulesTests
{
    private static List<PersonIdColumn> Find(string type, params string[] columns) =>
        PersonIdRules.Find(new SchemaSummary
        {
            Tables = [new TableSummary("[dbo].[t]", 1, false, false, 0, [], [],
                columns.Select(c => new ColumnSummary(c, type, null, null, true, false, null, null)).ToList())]
        });

    [Theory]
    [InlineData("Mothra_ID", "MothraId")]
    [InlineData("vmdoPeople_mothraID", "MothraId")]
    [InlineData("MOTHRAID", "MothraId")]
    [InlineData("mothraid", "MothraId")]
    [InlineData("PersonIam", "IamId")]
    [InlineData("IAMID", "IamId")]
    [InlineData("iam_id", "IamId")]
    [InlineData("PIDM", "Pidm")]
    [InlineData("termEnrollment_pidm", "Pidm")]
    public void Find_NameWordForAnIdType_IsThatIdType(string column, string idType)
    {
        var found = Assert.Single(Find("varchar(10)", column));

        Assert.Equal((PersonIdCategory.IdType, idType), (found.Category, found.IdType));
    }

    [Theory]
    [InlineData("ModBy")]
    [InlineData("Modified_By")]
    [InlineData("CreatedBy")]
    [InlineData("updtBy")]
    [InlineData("approvedby")]
    [InlineData("addUser")]
    [InlineData("modifyUser")]
    [InlineData("Who_Mod")]
    [InlineData("WhoCreated")]
    public void Find_NameSayingWhoDidSomething_IsForReview(string column)
    {
        var found = Assert.Single(Find("varchar(8)", column));

        Assert.Equal((PersonIdCategory.Review, null), (found.Category, found.IdType));
    }

    [Theory]
    [InlineData("William")]
    [InlineData("SortBy")]
    [InlineData("GroupBy")]
    [InlineData("Standby")]
    [InlineData("UserName")]
    [InlineData("WhoCares")]
    [InlineData("Hobby")]
    public void Find_OtherNames_AreNotFlagged(string column)
    {
        Assert.Empty(Find("varchar(50)", column));
    }

    [Theory]
    [InlineData("smallint")]
    [InlineData("datetime")]
    [InlineData("bit")]
    public void Find_TypeTooSmallOrNotAnId_IsNotFlagged(string type)
    {
        Assert.Empty(Find(type, "Mothra_ID", "ModBy"));
    }

    [Fact]
    public void Find_ForeignKeyChainToAnIdTypeColumn_CarriesTheIdType()
    {
        var found = PersonIdRules.Find(new SchemaSummary
        {
            Tables =
            [
                new TableSummary("[dbo].[Person]", 1, true, true, 0, ["MothraID"], [], [new("MothraID", "char(8)", null, 8, false, false, null, null)]),
                new TableSummary("[dbo].[Instructor]", 1, true, true, 0, ["Who"], [], [new("Who", "char(8)", null, 8, false, false, null, null)]),
                new TableSummary("[dbo].[Teaching]", 1, false, false, 0, [], [], [new("Teacher", "char(8)", null, 8, false, false, null, null)])
            ],
            ForeignKeys =
            [
                new("FK_Instructor_Person", "[dbo].[Instructor]", ["Who"], "[dbo].[Person]", ["MothraID"], false, false, false, "NO_ACTION", "NO_ACTION"),
                new("FK_Teaching_Instructor", "[dbo].[Teaching]", ["Teacher"], "[dbo].[Instructor]", ["Who"], false, false, false, "NO_ACTION", "NO_ACTION")
            ]
        });

        Assert.Equal(["[dbo].[Instructor].Who", "[dbo].[Person].MothraID", "[dbo].[Teaching].Teacher"],
            found.Select(f => $"{f.Object}.{f.Column}").Order(StringComparer.Ordinal).ToList());
        Assert.All(found, f => Assert.Equal("MothraId", f.IdType));
        Assert.Contains("FK_Teaching_Instructor", found.Single(f => f.Column == "Teacher").Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Find_ViewColumns_AreFlaggedToo()
    {
        var found = PersonIdRules.Find(new SchemaSummary
        {
            Views = [new ViewSummary("[dbo].[vStudents]", [new("mothraId", "varchar(8)", null, 8, true, false, null, null)])]
        });

        Assert.Equal("[dbo].[vStudents]", Assert.Single(found).Object);
    }
}
