using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// A report is checked before it's saved or previewed: its source, columns and parameters must
/// exist in the [report] catalog, settings must fit together, and permissions must be RAPS's.
/// </summary>
public sealed class ReportDefinitionValidatorTests
{
    private readonly FakeReportSourceCatalog _catalog = new();
    private readonly FakeReportPermissionLookup _permissions = new();
    private readonly ReportDefinitionValidator _validator;

    public ReportDefinitionValidatorTests()
    {
        _validator = new ReportDefinitionValidator(_catalog, _permissions);
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "FacultyRoster",
            Kind = ReportSourceKind.View,
            Columns =
            [
                FakeReportSourceCatalog.Column("Name"),
                FakeReportSourceCatalog.Column("Dept"),
                FakeReportSourceCatalog.Column("Hired", ReportValueKind.Date),
                FakeReportSourceCatalog.Column("Salary", ReportValueKind.FractionalNumber),
                new ReportSourceColumn { Name = "Photo", SqlType = "varbinary", Kind = ReportValueKind.Binary, IsNullable = true, IsSupported = false },
            ],
            Parameters = [],
            PagesInSql = true,
        });
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "usp_Leave",
            Kind = ReportSourceKind.Procedure,
            Columns = [],
            ColumnsError = "Uses a temp table.",
            Parameters = [FakeReportSourceCatalog.Parameter("LoginId"), FakeReportSourceCatalog.Parameter("Out", supported: false)],
            PagesInSql = false,
        });
        _catalog.Sources.Add(new ReportSourceDetail
        {
            Name = "DeptChoices",
            Kind = ReportSourceKind.View,
            Columns = [FakeReportSourceCatalog.Column("Value"), FakeReportSourceCatalog.Column("Label")],
            Parameters = [],
            PagesInSql = true,
        });
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ReportColumnEdit Column(string name, bool visible = true)
    {
        return new ReportColumnEdit { SourceColumn = name, Format = ReportColumnFormat.Text, Sortable = true, Visible = visible };
    }

    private static ReportParameterEdit Parameter(string name, ReportParameterType type = ReportParameterType.Text,
        ReportFilterOperator? op = null, string? defaultValue = null)
    {
        return new ReportParameterEdit { Name = name, Type = type, Operator = op, DefaultValue = defaultValue, Binding = ReportParameterBinding.UserInput };
    }

    private static ReportEditRequest Roster(params ReportParameterEdit[] parameters)
    {
        return new ReportEditRequest
        {
            Title = "Faculty roster",
            Source = "facultyroster",
            Columns = [Column("name"), Column("Dept")],
            Parameters = parameters,
            Permissions = ["svmsecure.personnel", " SVMSecure.Personnel "],
        };
    }

    private Task<ReportValidation> Validate(ReportEditRequest request)
    {
        return _validator.ValidateAsync(request, Ct);
    }

    [Fact]
    public async Task ValidReport_HasNoErrorsAndTheCatalogsSpellings()
    {
        ReportValidation result = await Validate(Roster());

        Assert.Empty(result.Errors);
        Assert.Equal("FacultyRoster", result.Source?.Name);
        Assert.Equal("Name", result.SourceNames["name"]);
        Assert.Equal(["SVMSecure.Personnel"], result.Permissions);
    }

    [Fact]
    public async Task ReportFields_AreChecked()
    {
        ReportValidation result = await Validate(Roster() with
        {
            Title = " ",
            Area = new string('a', 501),
            SensitiveNotice = new string('n', 501),
            PageSize = 0,
        });

        Assert.Equal(
            [
                "Enter a title.",
                "The area must be 500 characters or fewer.",
                "The confidentiality notice must be 500 characters or fewer.",
                "Rows per page must be between 1 and 500.",
            ],
            result.Errors);
    }

    [Theory]
    [InlineData("", "Choose a data source.")]
    [InlineData("Dropped", "'Dropped' isn't a view or procedure in the [report] schema.")]
    public async Task UnknownSource_IsAnErrorAndNothingElseIsChecked(string source, string error)
    {
        ReportValidation result = await Validate(Roster() with { Source = source, Columns = [] });

        Assert.Equal([error], result.Errors);
        Assert.Null(result.Source);
    }

    [Fact]
    public async Task Columns_MustExistBeShowableAndListedOnce()
    {
        ReportValidation result = await Validate(Roster() with
        {
            Columns = [Column("Name"), Column("NAME"), Column("Salary2"), Column("Photo"), Column(" ")],
            OrderBy = "Nope",
        });

        Assert.Equal(
            [
                "The column 'NAME' is listed more than once.",
                "The source has no column named 'Salary2'.",
                "The column 'Photo' holds varbinary values, which a report can't show.",
                "Every column needs a source column.",
                "The source has no column named 'Nope' to sort by.",
            ],
            result.Errors);
    }

    [Fact]
    public async Task Columns_AtLeastOneMustShow()
    {
        ReportValidation result = await Validate(Roster() with { Columns = [Column("Name", visible: false)] });

        Assert.Equal(["Show at least one column."], result.Errors);
    }

    [Fact]
    public async Task ColumnHeadings_HaveALimit()
    {
        ReportValidation result = await Validate(Roster() with { Columns = [Column("Name") with { Label = new string('x', 501) }] });

        Assert.Equal(["The heading of 'Name' must be 500 characters or fewer."], result.Errors);
    }

    [Fact]
    public async Task ProcedureWithUnknownColumns_TakesItsColumnsOnTrust()
    {
        ReportValidation result = await Validate(new ReportEditRequest
        {
            Title = "Leave",
            Source = "usp_Leave",
            Columns = [Column("Anything")],
            OrderBy = "Anything",
            Parameters = [Parameter("LoginId") with { Binding = ReportParameterBinding.CurrentUserLoginId }],
        });

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcedureParameters_MustBeOnesItTakes()
    {
        ReportValidation result = await Validate(new ReportEditRequest
        {
            Title = "Leave",
            Source = "usp_Leave",
            Columns = [Column("Name")],
            Parameters = [Parameter("Out"), Parameter("Year")],
        });

        Assert.Equal(
            [
                "The parameter 'Out' doesn't match a parameter the procedure takes.",
                "The parameter 'Year' doesn't match a parameter the procedure takes.",
            ],
            result.Errors);
    }

    [Fact]
    public async Task ViewParameters_MustBeColumnsAndListedOnce()
    {
        ReportValidation result = await Validate(Roster(Parameter("Building"), Parameter("Dept"), Parameter("dept"), Parameter(" ")));

        Assert.Equal(
            [
                "The parameter 'Building' doesn't match a column of the view.",
                "The parameter 'dept' is listed more than once.",
                "Every parameter needs a name.",
            ],
            result.Errors);
    }

    [Theory]
    [InlineData(ReportParameterType.MultiChoice, ReportFilterOperator.In, true)]
    [InlineData(ReportParameterType.MultiChoice, ReportFilterOperator.Equals, false)]
    [InlineData(ReportParameterType.Choice, ReportFilterOperator.In, false)]
    [InlineData(ReportParameterType.Text, ReportFilterOperator.Contains, true)]
    [InlineData(ReportParameterType.Number, ReportFilterOperator.Contains, false)]
    [InlineData(ReportParameterType.Date, ReportFilterOperator.From, true)]
    [InlineData(ReportParameterType.Number, ReportFilterOperator.To, true)]
    [InlineData(ReportParameterType.Text, ReportFilterOperator.From, false)]
    [InlineData(ReportParameterType.Boolean, ReportFilterOperator.Equals, true)]
    public async Task Operators_MustSuitTheType(ReportParameterType type, ReportFilterOperator op, bool fits)
    {
        ReportParameterEdit parameter = Parameter("Dept", type, op) with { ChoicesList = [new ReportChoice { Value = "VME", Label = "Medicine" }] };

        ReportValidation result = await Validate(Roster(parameter));

        Assert.Equal(fits, result.Errors.Count == 0);
    }

    [Fact]
    public async Task MultiChoiceWithoutAnOperator_NeedsIn()
    {
        ReportValidation result = await Validate(Roster(Parameter("Dept", ReportParameterType.MultiChoice) with
        {
            ChoicesSource = "DeptChoices",
        }));

        Assert.Equal(["The parameter 'Dept' can't use 'Equals' with a MultiChoice value."], result.Errors);
    }

    [Fact]
    public async Task Choices_NeedAListOrACatalogView()
    {
        ReportValidation result = await Validate(Roster(
            Parameter("Name", ReportParameterType.Choice),
            Parameter("Dept", ReportParameterType.Choice) with { ChoicesSource = "usp_Leave" },
            Parameter("Hired", ReportParameterType.Choice) with { ChoicesSource = " deptchoices " }));

        Assert.Equal(
            [
                "The parameter 'Name' needs a list of choices or a view to read them from.",
                "The parameter 'Dept''s choices view 'usp_Leave' isn't a view in the [report] schema.",
            ],
            result.Errors);
    }

    [Fact]
    public async Task ChoiceLists_NeedDistinctValues()
    {
        ReportValidation result = await Validate(Roster(Parameter("Dept", ReportParameterType.Choice) with
        {
            ChoicesList = [new ReportChoice { Value = "VME", Label = "A" }, new ReportChoice { Value = "vme", Label = "B" }, new ReportChoice { Value = " ", Label = "C" }],
        }));

        Assert.Equal(
            ["The parameter 'Dept' has a choice without a value.", "The parameter 'Dept' has the same choice more than once."],
            result.Errors);
    }

    [Theory]
    [InlineData(ReportParameterType.Number, "1,000.5", true)]
    [InlineData(ReportParameterType.Number, "ten", false)]
    [InlineData(ReportParameterType.Date, "2026-03-01", true)]
    [InlineData(ReportParameterType.Date, "03/01/2026", false)]
    [InlineData(ReportParameterType.Boolean, "true", true)]
    [InlineData(ReportParameterType.Boolean, "yes", false)]
    [InlineData(ReportParameterType.Choice, "vme", true)]
    [InlineData(ReportParameterType.Choice, "XYZ", false)]
    [InlineData(ReportParameterType.Text, "anything", true)]
    public async Task Defaults_MustFitTheType(ReportParameterType type, string value, bool valid)
    {
        ReportFilterOperator? op = type is ReportParameterType.Number or ReportParameterType.Date ? ReportFilterOperator.From : null;
        ReportParameterEdit parameter = Parameter("Dept", type, op, value) with { ChoicesList = [new ReportChoice { Value = "VME", Label = "Medicine" }] };

        string[] expected = valid ? [] : [$"The parameter 'Dept''s default '{value}' isn't a valid {type} value."];

        ReportValidation result = await Validate(Roster(parameter));

        Assert.Equal(expected, result.Errors);
    }

    [Fact]
    public async Task DefaultForAChoiceFromAView_IsCheckedWhenRun()
    {
        ReportValidation result = await Validate(Roster(Parameter("Dept", ReportParameterType.Choice, defaultValue: "Anything") with { ChoicesSource = "DeptChoices" }));

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CurrentUserParameters_SkipTypeChoiceAndDefaultChecks()
    {
        ReportValidation result = await Validate(Roster(Parameter("Name", ReportParameterType.MultiChoice, ReportFilterOperator.Contains, "x") with
        {
            Binding = ReportParameterBinding.CurrentUserLoginId,
        }));

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ParameterText_HasLimits()
    {
        ReportValidation result = await Validate(Roster(Parameter("Name") with { Label = new string('l', 501), DefaultValue = new string('d', 501) }));

        Assert.Equal(
            [
                "The parameter 'Name''s label must be 500 characters or fewer.",
                "The parameter 'Name''s default must be 500 characters or fewer.",
            ],
            result.Errors);
    }

    [Fact]
    public async Task Permissions_MustBeRapsPermissions()
    {
        string tooLong = "SVMSecure." + new string('x', 100);

        ReportValidation result = await Validate(Roster() with { Permissions = ["SVMSecure.Personnel", "SVMSecure.Typo", tooLong, " "] });

        Assert.Equal(
            [
                $"The permission '{tooLong[..50]}…' is longer than 100 characters.",
                "RAPS has no permission named 'SVMSecure.Typo'.",
                $"RAPS has no permission named '{tooLong[..50]}…'.",
            ],
            result.Errors);
        Assert.Equal(["SVMSecure.Personnel"], result.Permissions);
    }
}
