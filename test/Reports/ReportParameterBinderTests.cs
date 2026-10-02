using System.Text.Json;
using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportParameterBinderTests
{
    private static JsonElement Json(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public void TryBind_MissingBody_BindsEmptyParameters()
    {
        bool bound = ReportParameterBinder.TryBind(typeof(SampleParams), null, out object? parameters, out ReportValidationError? error);

        Assert.True(bound);
        Assert.Null(error);
        Assert.Equal(new SampleParams(), parameters);
    }

    [Fact]
    public void TryBind_JsonNull_BindsEmptyParameters()
    {
        bool bound = ReportParameterBinder.TryBind(typeof(SampleParams), Json("null"), out object? parameters, out _);

        Assert.True(bound);
        Assert.IsType<SampleParams>(parameters);
    }

    [Fact]
    public void TryBind_Object_BindsCamelCaseValues()
    {
        JsonElement json = Json("""{ "facultyType": "F", "departments": ["APC"], "startDate": "2025-07-01", "MinimumAge": 60 }""");

        bool bound = ReportParameterBinder.TryBind(typeof(SampleParams), json, out object? parameters, out _);

        Assert.True(bound);
        SampleParams typed = Assert.IsType<SampleParams>(parameters);
        Assert.Equal("F", typed.FacultyType);
        Assert.NotNull(typed.Departments);
        Assert.Equal(["APC"], typed.Departments);
        Assert.Equal(new DateOnly(2025, 7, 1), typed.StartDate);
        Assert.Equal(60, typed.MinimumAge);
    }

    [Fact]
    public void TryBind_NonObject_ReturnsError()
    {
        bool bound = ReportParameterBinder.TryBind(typeof(SampleParams), Json("[1, 2]"), out object? parameters, out ReportValidationError? error);

        Assert.False(bound);
        Assert.Null(parameters);
        Assert.Equal(new ReportValidationError(null, ReportParameterBinder.NotAnObjectMessage), error);
    }

    [Fact]
    public void TryBind_WrongValueType_ReturnsErrorForThatParameter()
    {
        bool bound = ReportParameterBinder.TryBind(
            typeof(SampleParams), Json("""{ "minimumAge": "sixty" }"""), out _, out ReportValidationError? error);

        Assert.False(bound);
        Assert.Equal(new ReportValidationError("minimumAge", ReportParameterBinder.UnreadableMessage), error);
    }

    [Fact]
    public void TryBind_UnknownParameter_ReturnsError()
    {
        bool bound = ReportParameterBinder.TryBind(
            typeof(SampleParams), Json("""{ "facultyTyp": "S" }"""), out _, out ReportValidationError? error);

        Assert.False(bound);
        Assert.NotNull(error);
        Assert.Equal(ReportParameterBinder.UnreadableMessage, error.Message);
    }

    [Fact]
    public void TryBind_NullResult_ReturnsError()
    {
        bool bound = ReportParameterBinder.TryBind(typeof(NullParams), Json("{}"), out _, out ReportValidationError? error);

        Assert.False(bound);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryBind_NullType_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportParameterBinder.TryBind(null!, null, out _, out _));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("$", null)]
    [InlineData("$.", null)]
    [InlineData("$.startDate", "startDate")]
    [InlineData("$.departments[1]", "departments")]
    [InlineData("$.inner.value", "inner")]
    [InlineData("startDate", null)]
    public void ParameterName_ExtractsTopLevelName(string? path, string? expected)
    {
        Assert.Equal(expected, ReportParameterBinder.ParameterName(path));
    }
}
