using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

internal sealed record SampleRow(
    string Name,
    string? Department,
    int Age,
    decimal? Salary,
    DateOnly HireDate,
    IReadOnlyList<string>? Ethnicities = null);

internal sealed record SampleParams
{
    public string? FacultyType { get; init; }

    public IReadOnlyList<string>? Departments { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Search { get; init; }

    public int? MinimumAge { get; init; }

    public bool? IncludeEmeriti { get; init; }

    public string? Computed => Search;

    public SampleInner Inner { get; init; } = new();
}

internal sealed record SampleInner
{
    public string? Value { get; init; }
}

/// <summary>
/// A configurable report for engine tests. <see cref="Standard"/> declares one parameter of
/// every type and one column of every format.
/// </summary>
internal sealed class TestReport : ReportDefinition<SampleRow, SampleParams>
{
    public const string RunPermission = "SVMSecure.Test.Run";
    public const string SalaryPermission = "SVMSecure.Test.Salary";

    public static readonly DateOnly DefaultStart = new(2026, 7, 1);

    public static readonly IReadOnlyList<ReportOption> FacultyTypes =
    [
        new("S", "Senate"),
        new("F", "Federation"),
        new("B", "Both"),
    ];

    public static readonly IReadOnlyList<ReportOption> DepartmentOptions =
    [
        new("APC", "Anatomy, Physiology and Cell Biology"),
        new("VME", "Medicine and Epidemiology"),
    ];

    private readonly Action<ReportBuilder<SampleRow, SampleParams>> _configure;
    private readonly string _key;
    private readonly string _title;
    private readonly string _area;
    private readonly string _description;
    private readonly IReadOnlyList<string> _permissions;
    private readonly Func<SampleParams, IReadOnlyList<SampleRow>> _fetch;

    public TestReport(
        Action<ReportBuilder<SampleRow, SampleParams>>? configure = null,
        string key = "test.sample",
        string title = "Sample report",
        string area = "Test",
        string description = "A report used by tests.",
        IReadOnlyList<string>? permissions = null,
        Func<SampleParams, IReadOnlyList<SampleRow>>? fetch = null)
    {
        _configure = configure ?? Standard;
        _key = key;
        _title = title;
        _area = area;
        _description = description;
        _permissions = permissions ?? [RunPermission];
        _fetch = fetch ?? (_ => Rows());
    }

    public int ConfigureCalls { get; private set; }

    public SampleParams? FetchedWith { get; private set; }

    public ReportContext? FetchedFor { get; private set; }

    public override string Key => _key;

    public override string Title => _title;

    public override string Area => _area;

    public override string Description => _description;

    public override IReadOnlyList<string> Permissions => _permissions;

    public static void Standard(ReportBuilder<SampleRow, SampleParams> report)
    {
        report.Choice(p => p.FacultyType, "Faculty type", FacultyTypes).Required().Default(() => "S");
        report.MultiChoice(p => p.Departments, "Departments", DepartmentOptions);
        report.Date(p => p.StartDate, "Start date").Required().Default(() => DefaultStart);
        report.Date(p => p.EndDate, "End date");
        report.Text(p => p.Search, "Search");
        report.Number(p => p.MinimumAge, "Minimum age");
        report.Boolean(p => p.IncludeEmeriti, "Include emeriti");
        report.Validate(
            p => p.EndDate is null || p.EndDate >= p.StartDate,
            "End date must be on or after the start date.",
            "endDate");

        report.Column(r => r.Name, "Name");
        report.Column(r => r.Department, "Department");
        report.Column(r => r.Age, "Age").AsNumber();
        report.Column(r => r.Salary, "Salary").AsCurrency()
            .VisibleWhen(context => context.HasPermission(SalaryPermission));
        report.Column(r => r.HireDate, "Hire date").AsDate();
    }

    public static ReportContext ContextWith(params string[] permissions)
    {
        var granted = new HashSet<string>(permissions, StringComparer.Ordinal);
        return new ReportContext("tester", granted.Contains);
    }

    public static readonly DateTimeOffset GeneratedAt = new(2026, 9, 29, 8, 0, 0, TimeSpan.FromHours(-7));

    /// <summary>
    /// Five people: three in VME (one key padded with a space), one in "apc", and one with a
    /// blank department.
    /// </summary>
    public static IReadOnlyList<SampleRow> Rows()
    {
        return
        [
            new("Ada", "VME", 61, 120_000m, new DateOnly(1995, 7, 1), ["Asian", "White"]),
            new("Ben", "apc", 45, 90_000m, new DateOnly(2010, 1, 15), ["White"]),
            new("Cy", "VME ", 38, null, new DateOnly(2018, 9, 1), ["Hispanic"]),
            new("Di", "VME", 66, 150_000m, new DateOnly(1990, 3, 1), []),
            new("Ed", "  ", 29, 70_000m, new DateOnly(2022, 8, 15), ["White"]),
        ];
    }

    public static ReportResult Run(TestReport report, ReportContext? context = null)
    {
        return report.CreateResult(Rows(), context ?? ContextWith(RunPermission, SalaryPermission), GeneratedAt);
    }

    protected override void Configure(ReportBuilder<SampleRow, SampleParams> report)
    {
        ConfigureCalls++;
        _configure(report);
    }

    protected override Task<IReadOnlyList<SampleRow>> FetchAsync(
        SampleParams parameters, ReportContext context, CancellationToken ct)
    {
        FetchedWith = parameters;
        FetchedFor = context;
        return Task.FromResult(_fetch(parameters));
    }
}
