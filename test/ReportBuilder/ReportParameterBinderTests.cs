using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>
/// Binding checks every value a user sends against the report's parameters before any of it
/// is used, and fills current-user parameters on the server.
/// </summary>
public sealed class ReportParameterBinderTests
{
    private static readonly ReportCurrentUser User = new() { LoginId = "jdoe", MothraId = "01234567" };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> NoChoices =
        new Dictionary<string, IReadOnlyList<ReportChoice>>();

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> DeptChoices =
        new Dictionary<string, IReadOnlyList<ReportChoice>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dept"] =
            [
                new ReportChoice { Value = "VME", Label = "Medicine & Epidemiology" },
                new ReportChoice { Value = "PMI", Label = "Pathology" },
            ],
        };

    private static ReportParameter Parameter(string name, ReportParameterType? type = ReportParameterType.Text,
        bool? required = null, string? defaultValue = null, ReportParameterBinding? binding = null, string? label = null,
        int? order = null)
    {
        return new ReportParameter
        {
            Name = name,
            Label = label,
            Type = type,
            Required = required,
            DefaultValue = defaultValue,
            Binding = binding,
            DisplayOrder = order,
            Operator = ReportFilterOperator.Equals,
        };
    }

    private static Dictionary<string, IReadOnlyList<string>> Input(params (string Name, string[] Values)[] values)
    {
        return values.ToDictionary(v => v.Name, v => (IReadOnlyList<string>)v.Values);
    }

    private static ReportBindResult Bind(ReportParameter definition, Dictionary<string, IReadOnlyList<string>> input,
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>>? choices = null)
    {
        return ReportParameterBinder.Bind([definition], input, User, choices ?? NoChoices);
    }

    private static object BoundValue(ReportBindResult result)
    {
        Assert.Empty(result.Errors);
        return Assert.Single(result.Parameters).Value;
    }

    [Fact]
    public void Text_IsTrimmedAndMatchedIgnoringCase()
    {
        ReportBindResult result = Bind(Parameter("Name"), Input(("name", ["  Smith "])));

        BoundReportParameter parameter = Assert.Single(result.Parameters);
        Assert.Equal("Name", parameter.Name);
        Assert.Equal("Smith", parameter.Value);
        Assert.Equal(ReportFilterOperator.Equals, parameter.Operator);
        Assert.True(parameter.FromUser);
    }

    [Fact]
    public void Text_TooLong_IsAnError()
    {
        ReportBindResult result = Bind(Parameter("Name", label: "Last name"), Input(("Name", [new string('x', ReportLimits.MaxTextLength + 1)])));

        Assert.Equal(["Last name must be 500 characters or fewer."], result.Errors);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void NoType_IsText()
    {
        Assert.Equal("x", BoundValue(Bind(Parameter("Name", type: null), Input(("Name", ["x"])))));
    }

    [Theory]
    [InlineData("12.5", "12.5")]
    [InlineData("-3", "-3")]
    [InlineData("1,000", "1000")]
    public void Number_IsParsedInvariantly(string given, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            BoundValue(Bind(Parameter("Salary", ReportParameterType.Number), Input(("Salary", [given])))));
    }

    [Fact]
    public void Number_NotANumber_IsAnError()
    {
        Assert.Equal(["Salary must be a number."], Bind(Parameter("Salary", ReportParameterType.Number), Input(("Salary", ["ten"]))).Errors);
    }

    [Fact]
    public void Date_IsIsoDate()
    {
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified),
            BoundValue(Bind(Parameter("Hired", ReportParameterType.Date), Input(("Hired", ["2026-03-01"])))));
    }

    [Theory]
    [InlineData("03/01/2026")]
    [InlineData("2026-3-1")]
    [InlineData("2026-02-30")]
    public void Date_OtherFormats_AreAnError(string given)
    {
        Assert.Equal(["Hired must be a date (YYYY-MM-DD)."], Bind(Parameter("Hired", ReportParameterType.Date), Input(("Hired", [given]))).Errors);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("False", false)]
    public void Boolean_IsParsed(string given, bool expected)
    {
        Assert.Equal(expected, BoundValue(Bind(Parameter("Active", ReportParameterType.Boolean), Input(("Active", [given])))));
    }

    [Fact]
    public void Boolean_Other_IsAnError()
    {
        Assert.Equal(["Active must be true or false."], Bind(Parameter("Active", ReportParameterType.Boolean), Input(("Active", ["yes"]))).Errors);
    }

    [Fact]
    public void Choice_MustBeOneOfTheChoices()
    {
        Assert.Equal("vme", BoundValue(Bind(Parameter("Dept", ReportParameterType.Choice), Input(("Dept", ["vme"])), DeptChoices)));

        Assert.Equal(["Dept has a value that isn't one of its choices."],
            Bind(Parameter("Dept", ReportParameterType.Choice), Input(("Dept", ["XYZ"])), DeptChoices).Errors);
    }

    [Fact]
    public void Choice_UsesOnlyTheFirstValue()
    {
        Assert.Equal("PMI", BoundValue(Bind(Parameter("Dept", ReportParameterType.Choice), Input(("Dept", ["PMI", "XYZ"])), DeptChoices)));
    }

    [Fact]
    public void MultiChoice_KeepsDistinctAllowedValues()
    {
        object value = BoundValue(Bind(Parameter("Dept", ReportParameterType.MultiChoice), Input(("Dept", ["VME", "PMI", "vme", " "])), DeptChoices));

        Assert.Equal(["VME", "PMI"], Assert.IsAssignableFrom<IReadOnlyList<string>>(value));
    }

    [Fact]
    public void MultiChoice_AnyValueNotAllowed_IsAnError()
    {
        Assert.Equal(["Dept has a value that isn't one of its choices."],
            Bind(Parameter("Dept", ReportParameterType.MultiChoice), Input(("Dept", ["VME", "XYZ"])), DeptChoices).Errors);
    }

    [Fact]
    public void Choice_WithoutChoices_IsAnError()
    {
        Assert.Equal(["Dept has no choices to pick from."], Bind(Parameter("Dept", ReportParameterType.Choice), Input(("Dept", ["VME"]))).Errors);
    }

    [Fact]
    public void Missing_UsesTheDefault()
    {
        BoundReportParameter parameter = Assert.Single(Bind(Parameter("Dept", defaultValue: " VME "), Input()).Parameters);

        Assert.Equal("VME", parameter.Value);
        Assert.True(parameter.FromUser);
    }

    [Fact]
    public void BlankValues_UseTheDefault()
    {
        Assert.Equal("VME", BoundValue(Bind(Parameter("Dept", defaultValue: "VME"), Input(("Dept", ["", "  "])))));
    }

    [Fact]
    public void MissingOptional_IsLeftOut()
    {
        ReportBindResult result = Bind(Parameter("Dept"), Input());

        Assert.Empty(result.Errors);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void MissingRequired_IsAnErrorNamedByLabel()
    {
        Assert.Equal(["Department is required."], Bind(Parameter("Dept", required: true, label: "Department"), Input()).Errors);
    }

    [Theory]
    [InlineData(ReportParameterBinding.CurrentUserLoginId, "jdoe")]
    [InlineData(ReportParameterBinding.CurrentUserMothraId, "01234567")]
    public void CurrentUser_IsFilledOnTheServer(ReportParameterBinding binding, string expected)
    {
        BoundReportParameter parameter = Assert.Single(Bind(Parameter("Who", binding: binding), Input()).Parameters);

        Assert.Equal(expected, parameter.Value);
        Assert.Equal(ReportParameterType.Text, parameter.Type);
        Assert.False(parameter.FromUser);
    }

    [Fact]
    public void CurrentUser_SuppliedByTheClient_IsRefused()
    {
        ReportBindResult result = Bind(Parameter("Who", binding: ReportParameterBinding.CurrentUserLoginId), Input(("who", ["someoneelse"])));

        Assert.Equal(["Who is set automatically and can't be supplied."], result.Errors);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void CurrentUser_WithoutTheIdentifier_FailsClosed()
    {
        ReportBindResult result = ReportParameterBinder.Bind(
            [Parameter("Who", binding: ReportParameterBinding.CurrentUserMothraId)], Input(), new ReportCurrentUser { LoginId = "jdoe" }, NoChoices);

        Assert.Equal(["Who can't be filled in for your account."], result.Errors);
    }

    [Fact]
    public void UnknownNames_AreErrorsWithTheNameShortened()
    {
        string longName = new('n', 60);
        ReportBindResult result = Bind(Parameter("Dept"), Input(("Other", ["x"]), (longName, ["x"])));

        Assert.Equal(["This report has no parameter named 'Other'.", $"This report has no parameter named '{new string('n', 50)}…'."], result.Errors);
    }

    [Fact]
    public void NamesDifferingOnlyByCase_AreFlaggedAsDuplicatesInsteadOfCrashing()
    {
        ReportBindResult result = Bind(Parameter("Dept"), Input(("Dept", ["PMI"]), ("dept", ["VME"])));

        Assert.Equal(["The parameter 'dept' was sent more than once."], result.Errors);
        Assert.Equal("PMI", Assert.Single(result.Parameters).Value);
    }

    [Fact]
    public void Parameters_AreBoundInDisplayOrderAndUnnamedOnesSkipped()
    {
        ReportBindResult result = ReportParameterBinder.Bind(
            [Parameter("B", defaultValue: "b", order: 2), Parameter(" ", defaultValue: "x"), Parameter("A", defaultValue: "a", order: 1)],
            Input(),
            User,
            NoChoices);

        Assert.Equal(["A", "B"], result.Parameters.Select(p => p.Name));
    }
}
