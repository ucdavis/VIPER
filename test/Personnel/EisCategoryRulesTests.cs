using Viper.Areas.Personnel.Models.Eis;
using static Viper.test.Personnel.EisTestData;

namespace Viper.test.Personnel;

/// <summary>
/// EisCategoryRules reproduces the legacy category procedures' tests. These cover each kind of
/// rule once, plus the order and labels the page shows.
/// </summary>
public sealed class EisCategoryRulesTests
{
    private static readonly EisPrograms NoPrograms = new(null, null);

    private static IReadOnlyList<string> Applying(
        IReadOnlyList<EisJobRow>? jobs = null,
        IReadOnlyCollection<string>? rateCodes = null,
        IReadOnlyCollection<int>? flags = null,
        EisPrograms? programs = null)
    {
        return [.. EisCategoryRules.Evaluate(jobs ?? [], rateCodes ?? [], flags ?? [], programs ?? NoPrograms)
            .Where(category => category.Applies)
            .Select(category => category.Label)];
    }

    [Fact]
    public void Evaluate_ListsEveryCategoryInTheLegacyOrder()
    {
        IReadOnlyList<EisCategory> categories = EisCategoryRules.Evaluate([], [], [], NoPrograms);

        Assert.Equal(62, categories.Count);
        Assert.Equal("___ in E.S.", categories[0].Label);
        Assert.Equal("Visiting Professor (WOS)", categories[^1].Label);
        Assert.DoesNotContain(categories, category => category.Applies);
    }

    [Fact]
    public void Evaluate_MarksOnlyTheManualCategoriesWithTheirFlagCode()
    {
        IReadOnlyList<EisCategory> categories = EisCategoryRules.Evaluate([], [], [], NoPrograms);

        List<int> codes = [.. categories.Select(category => category.FlagCode).OfType<int>().Order()];
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], codes);
        Assert.Equal(4, categories.Single(category => category.Label == "Director").FlagCode);
    }

    [Fact]
    public void Evaluate_MatchesTitleCodesWithTheirLeadingZeros()
    {
        Assert.Equal(["Dean"], Applying(jobs: [Job(jobCode: "001000 ")]));
        Assert.Empty(Applying(jobs: [Job(jobCode: "1000")]));
    }

    [Fact]
    public void Evaluate_PaidJobCountsForThePaidCategoryOnly()
    {
        Assert.Equal(["Adjunct Professor"], Applying(jobs: [Job(jobGroup: "335")]));
    }

    [Fact]
    public void Evaluate_WithoutSalaryCategoryNeedsNoPaidJobInTheGroup()
    {
        Assert.Equal(["Adjunct Professor (WOS)"], Applying(jobs: [Job(jobGroup: "335", isPaid: false)]));
        Assert.Equal(
            ["Adjunct Professor"],
            Applying(jobs: [Job(jobGroup: "335", isPaid: false), Job(jobGroup: "335")]));
    }

    [Fact]
    public void Evaluate_WithoutSalaryOnlyAndEitherCategories()
    {
        Assert.Equal(["Clinical Professor - Volunteer"], Applying(jobs: [Job(jobCode: "002057", isPaid: false)]));
        Assert.Empty(Applying(jobs: [Job(jobCode: "002057")]));
        Assert.Equal(["Department Chair"], Applying(jobs: [Job(jobCode: "001096", isPaid: false)]));
        Assert.Equal(["Department Chair"], Applying(jobs: [Job(jobCode: "001096")]));
    }

    [Fact]
    public void Evaluate_IgnoresJobsThatAreNotCurrent()
    {
        Assert.Empty(Applying(jobs: [Job(jobGroup: "335", isCurrent: false), Job()]));
    }

    [Fact]
    public void Evaluate_ScaleCategoriesComeFromRateCodes()
    {
        Assert.Equal(["Above Scale"], Applying(rateCodes: ["ucabvs "]));
        Assert.Equal(["Off-Scale"], Applying(rateCodes: ["UCOFF1"]));
    }

    [Fact]
    public void Evaluate_EmeritusAndRecallSplitBySenateMembership()
    {
        EisJobRow emeritus = Job(description: "PROF EMERITUS", isCurrent: false);
        EisJobRow recall = Job(description: "Prof-Recall");
        var senate = new EisPrograms(null, " senate ");

        Assert.Equal(["Emeritus (Senate)"], Applying(jobs: [emeritus], programs: senate));
        Assert.Equal(["Emeritus (Non-Senate)"], Applying(jobs: [emeritus]));
        Assert.Equal(["Recall (Senate)"], Applying(jobs: [recall], programs: senate));
        Assert.Equal(["Recall (Non-Senate)"], Applying(jobs: [recall]));
        Assert.Empty(Applying(jobs: [Job(description: "PROF EMERITUS", isActive: false)]));
    }

    [Fact]
    public void Evaluate_MspComesFromTheStaffProgram()
    {
        Assert.Equal(["MSP"], Applying(programs: new EisPrograms("MSP", null)));
    }

    [Fact]
    public void Evaluate_ManualCategoriesComeFromFlags()
    {
        Assert.Equal(["Branch Chief", "Service Chief"], Applying(flags: [5, 1]));
    }

    [Fact]
    public void Evaluate_RejectsMissingArguments()
    {
        Assert.Throws<ArgumentNullException>(() => EisCategoryRules.Evaluate(null!, [], [], NoPrograms));
        Assert.Throws<ArgumentNullException>(() => EisCategoryRules.Evaluate([], null!, [], NoPrograms));
        Assert.Throws<ArgumentNullException>(() => EisCategoryRules.Evaluate([], [], null!, NoPrograms));
        Assert.Throws<ArgumentNullException>(() => EisCategoryRules.Evaluate([], [], [], null!));
    }

    [Theory]
    [InlineData(2026, 6, 30, "2025-2026")]
    [InlineData(2026, 7, 1, "2026-2027")]
    [InlineData(2027, 1, 15, "2026-2027")]
    public void AcademicYear_RunsJulyThroughJune(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, EisCategoryRules.AcademicYear(new DateOnly(year, month, day)));
    }
}
