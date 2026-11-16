using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>A parameter's fixed choice list is a JSON array of strings or value/label objects.</summary>
public sealed class ReportChoiceListTests
{
    [Fact]
    public void Parse_StringsAndObjects()
    {
        IReadOnlyList<ReportChoice>? choices = ReportChoiceList.Parse(
            """["VME", {"Value": "PMI", "Label": "Pathology"}, {"value": 3}, {"value": "X", "label": " "}, {"value": "Y", "label": 2}]""");

        Assert.Equal(
            [
                new ReportChoice { Value = "VME", Label = "VME" },
                new ReportChoice { Value = "PMI", Label = "Pathology" },
                new ReportChoice { Value = "3", Label = "3" },
                new ReportChoice { Value = "X", Label = "X" },
                new ReportChoice { Value = "Y", Label = "2" },
            ],
            choices);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not json")]
    [InlineData("""{"value": "VME"}""")]
    [InlineData("""["VME", 3]""")]
    [InlineData("""[""]""")]
    [InlineData("""[{"label": "No value"}]""")]
    public void Parse_AnythingElse_IsNull(string? json)
    {
        Assert.Null(ReportChoiceList.Parse(json));
    }

    [Fact]
    public void Parse_EmptyArray_IsEmpty()
    {
        IReadOnlyList<ReportChoice>? choices = ReportChoiceList.Parse("[]");

        Assert.NotNull(choices);
        Assert.Empty(choices);
    }
}
