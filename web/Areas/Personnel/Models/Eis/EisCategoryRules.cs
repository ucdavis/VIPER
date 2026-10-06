namespace Viper.Areas.Personnel.Models.Eis;

/// <summary>Which of a person's jobs a title-code or job-group category counts.</summary>
public enum EisSalaryRule
{
    /// <summary>A paid job (annual rate above zero).</summary>
    Paid,

    /// <summary>A without-salary job, but only when the person has no paid one in the same group.</summary>
    WithoutSalaryOnly,

    /// <summary>A without-salary job, whether or not a paid one exists.</summary>
    WithoutSalary,

    /// <summary>Any paid or without-salary job.</summary>
    Either,
}

/// <summary>
/// The Appointment Category page's rules, in the legacy page's order. The legacy page called a
/// procedure per category (about 70 calls); these rules reproduce those procedures' tests over one
/// query of the person's jobs, one of their rate codes and one of their manual flags.
/// </summary>
public static class EisCategoryRules
{
    /// <summary>The manual categories (AcademicPersonnel dvtFlags), which EIS admins set per academic year.</summary>
    public static readonly IReadOnlyDictionary<int, string> Flags = new Dictionary<int, string>
    {
        [1] = "Branch Chief",
        [2] = "Director (Internal Program)",
        [3] = "Director (Center/Unit)",
        [4] = "Director",
        [5] = "Service Chief",
        [6] = "Executive Associate Dean",
        [7] = "Graduate Group Advisor",
        [8] = "Graduate Group Chair",
        [9] = "Section Head",
    };

    private const string OffScaleRate = "UCOFF1";
    private static readonly string[] AboveScaleRates = ["UCABVE", "UCABVS"];
    private const string EmeritusText = "EMERI";
    private const string RecallText = "RECAL";
    private const string MspProgram = "MSP";
    private const string SenateProgram = "SENATE";

    // Title codes are listed as four digits; UCPath job codes carry two leading zeros.
    private const string JobCodePrefix = "00";

    private static readonly IReadOnlyList<Rule> Rules =
    [
        Titles("___ in E.S.", EisSalaryRule.Paid, "3001", "3011", "3012"),
        Titles("___ in E.S. (WOS)", EisSalaryRule.WithoutSalaryOnly, "3001", "3011", "3012"),
        Special("Above Scale", facts => facts.RateCodes.Overlaps(AboveScaleRates)),
        Groups("Academic Administrator", EisSalaryRule.Paid, "S56"),
        Groups("Academic Administrator (WOS)", EisSalaryRule.WithoutSalaryOnly, "S56"),
        Groups("Academic Coordinator", EisSalaryRule.Paid, "S44", "S46"),
        Groups("Academic Coordinator (WOS)", EisSalaryRule.WithoutSalaryOnly, "S44", "S46"),
        Groups("Acting Academic Coordinator", EisSalaryRule.Paid, "S44"),
        Groups("Acting Dean/Provost", EisSalaryRule.Paid, "S24"),
        Groups("Acting Department Chair", EisSalaryRule.Paid, "S64"),
        Groups("Acting Director", EisSalaryRule.Paid, "S34"),
        Groups("Acting Professor (Non-Senate)", EisSalaryRule.Paid, "124"),
        Groups("Acting Professor (Senate)", EisSalaryRule.Paid, "114"),
        Groups("Adjunct Professor", EisSalaryRule.Paid, "335"),
        Groups("Adjunct Professor (WOS)", EisSalaryRule.WithoutSalaryOnly, "335"),
        Titles("Assistant Dean (Academic)", EisSalaryRule.Paid, "1020"),
        Titles("Assistant Dean (Staff)", EisSalaryRule.Paid, "0384"),
        Titles("Associate Dean", EisSalaryRule.Paid, "1010"),
        Flag(1),
        Titles("Clinical Professor - Volunteer", EisSalaryRule.WithoutSalary, "2057", "2037", "2017"),
        Titles("HS Clinical Professor", EisSalaryRule.Paid, "2050", "2030", "2010"),
        Titles("HS Clinical Professor (WOS)", EisSalaryRule.WithoutSalaryOnly, "2050", "2030", "2010"),
        Titles("Dean", EisSalaryRule.Paid, "1000"),
        Titles("Department Chair", EisSalaryRule.Either, "1096"),
        Titles("Department Vice-Chair", EisSalaryRule.Either, "1094"),
        Flag(4),
        Flag(3),
        Flag(2),
        Flag(6),
        Special("Emeritus (Senate)", facts => facts.HasActiveJobLike(EmeritusText) && facts.IsSenate),
        Special("Emeritus (Non-Senate)", facts => facts.HasActiveJobLike(EmeritusText) && !facts.IsSenate),
        Groups("Faculty Assistant", EisSalaryRule.Paid, "928"),
        Flag(7),
        Flag(8),
        Groups("Lecturer (SOE)", EisSalaryRule.Paid, "210"),
        Groups("Lecturer (SOE) - Emeritus", EisSalaryRule.WithoutSalary, "216"),
        Groups("Lecturer (SOE) - Recall", EisSalaryRule.Paid, "212"),
        Groups("Lecturer W/O SOE", EisSalaryRule.Paid, "225"),
        Special("MSP", facts => facts.IsMsp),
        Special("Off-Scale", facts => facts.RateCodes.Contains(OffScaleRate)),
        Titles("Professional Researcher", EisSalaryRule.Paid, "3220", "3210", "3200"),
        Titles("Professional Researcher (WOS)", EisSalaryRule.WithoutSalaryOnly, "3220", "3210", "3200"),
        Groups("Professor - Tenured", EisSalaryRule.Paid, "010"),
        Groups("Professor - Tenured (WOS)", EisSalaryRule.WithoutSalaryOnly, "010"),
        Groups("Professor - Non-Tenured", EisSalaryRule.Paid, "011"),
        Groups("Professor - Non-Tenured (WOS)", EisSalaryRule.WithoutSalaryOnly, "011"),
        Groups("Professor in Residence", EisSalaryRule.Paid, "311"),
        Groups("Professor in Residence (WOS)", EisSalaryRule.WithoutSalaryOnly, "311"),
        Groups("Professor of Clinical ___", EisSalaryRule.Paid, "317"),
        Groups("Professor of Clinical ___ (WOS)", EisSalaryRule.WithoutSalaryOnly, "317"),
        Titles("Project Scientist", EisSalaryRule.Paid, "3394", "3392", "3390"),
        Titles("Project Scientist (WOS)", EisSalaryRule.WithoutSalaryOnly, "3394", "3392", "3390"),
        Special("Recall (Senate)", facts => facts.HasActiveJobLike(RecallText) && facts.IsSenate),
        Special("Recall (Non-Senate)", facts => facts.HasActiveJobLike(RecallText) && !facts.IsSenate),
        Titles("Resident", EisSalaryRule.Paid, "2730"),
        Flag(9),
        Flag(5),
        Titles("Specialist in Coop. Extension", EisSalaryRule.Paid, "3475", "3477", "3479"),
        Titles("Specialist", EisSalaryRule.Paid, "3330", "3320", "3310", "3300"),
        Titles("Veterinarian", EisSalaryRule.Paid, "9532", "0503", "9533"),
        Groups("Visiting Professor", EisSalaryRule.Paid, "323"),
        Groups("Visiting Professor (WOS)", EisSalaryRule.WithoutSalaryOnly, "323"),
    ];

    /// <summary>Every category in the legacy page's order, with whether it applies to this person.</summary>
    public static IReadOnlyList<EisCategory> Evaluate(
        IReadOnlyList<EisJobRow> jobs,
        IReadOnlyCollection<string> rateCodes,
        IReadOnlyCollection<int> flags,
        EisPrograms programs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(rateCodes);
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(programs);
        var facts = new Facts(
            jobs,
            new HashSet<string>(rateCodes.Select(code => code.Trim()), StringComparer.OrdinalIgnoreCase),
            new HashSet<int>(flags),
            IsProgram(programs.StaffProgram, MspProgram),
            IsProgram(programs.FacultyProgram, SenateProgram));
        return [.. Rules.Select(rule => new EisCategory(rule.Label, rule.Applies(facts), rule.FlagCode))];
    }

    /// <summary>
    /// The academic year the flags table uses, such as "2026-2027": July through June, as
    /// usp_eis_isPersonFlag and usp_eis_putPersonModFlags compute it.
    /// </summary>
    public static string AcademicYear(DateOnly today)
    {
        const int lastMonthOfYear = 6;
        int start = today.Month <= lastMonthOfYear ? today.Year - 1 : today.Year;
        return $"{start}-{start + 1}";
    }

    private static bool IsProgram(string? program, string expected)
    {
        return string.Equals(program?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static Rule Titles(string label, EisSalaryRule salary, params string[] titleCodes)
    {
        HashSet<string> jobCodes = new(titleCodes.Select(code => JobCodePrefix + code), StringComparer.OrdinalIgnoreCase);
        return Jobs(label, salary, job => job.JobCode is { } code && jobCodes.Contains(code.Trim()));
    }

    private static Rule Groups(string label, EisSalaryRule salary, params string[] jobGroups)
    {
        HashSet<string> groups = new(jobGroups, StringComparer.OrdinalIgnoreCase);
        return Jobs(label, salary, job => job.JobGroup is { } group && groups.Contains(group.Trim()));
    }

    private static Rule Jobs(string label, EisSalaryRule salary, Func<EisJobRow, bool> matches)
    {
        return new Rule(label, null, facts =>
        {
            bool paid = facts.Jobs.Any(job => job.IsCurrent && job.IsPaid && matches(job));
            bool withoutSalary = facts.Jobs.Any(job => job.IsCurrent && job.IsWithoutSalary && matches(job));
            return salary switch
            {
                EisSalaryRule.Paid => paid,
                EisSalaryRule.WithoutSalaryOnly => withoutSalary && !paid,
                EisSalaryRule.WithoutSalary => withoutSalary,
                _ => paid || withoutSalary,
            };
        });
    }

    private static Rule Flag(int code)
    {
        return new Rule(Flags[code], code, facts => facts.Flags.Contains(code));
    }

    private static Rule Special(string label, Func<Facts, bool> applies)
    {
        return new Rule(label, null, applies);
    }

    private sealed record Rule(string Label, int? FlagCode, Func<Facts, bool> Applies);

    private sealed record Facts(
        IReadOnlyList<EisJobRow> Jobs, HashSet<string> RateCodes, HashSet<int> Flags, bool IsMsp, bool IsSenate)
    {
        public bool HasActiveJobLike(string text)
        {
            return Jobs.Any(job => job.IsActive && job.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}
