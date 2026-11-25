using System.Text.Json;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.test.ReportBuilder;

/// <summary>What the browser receives: enums by name, row values keyed by column name.</summary>
public sealed class ReportBuilderJsonTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Enums_AreSentByName()
    {
        string json = JsonSerializer.Serialize(new ReportViewerParameter
        {
            Name = "Dept",
            Label = "Department",
            Type = ReportParameterType.MultiChoice,
            Required = false,
        }, Web);

        Assert.Contains("\"type\":\"MultiChoice\"", json);
    }

    [Fact]
    public void Enums_AreReadByName()
    {
        Assert.Equal(ReportColumnFormat.YesNo, JsonSerializer.Deserialize<ReportColumnFormat>("\"YesNo\"", Web));
    }

    [Fact]
    public void RunRequest_IsReadFromTheBrowsersShape()
    {
        ReportRunRequest? request = JsonSerializer.Deserialize<ReportRunRequest>(
            """{"parameters":{"Dept":["VME","PMI"]},"page":2,"perPage":50,"sortBy":"Name","sortDescending":true,"search":"ada"}""", Web);

        Assert.NotNull(request);
        Assert.Equal(["VME", "PMI"], request.Parameters["Dept"]);
        Assert.Equal(2, request.Page);
        Assert.Equal(50, request.PerPage);
        Assert.Equal("Name", request.SortBy);
        Assert.True(request.SortDescending);
        Assert.Equal("ada", request.Search);
    }

    [Fact]
    public void RunRequest_WithoutPage_IsRefused()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ReportRunRequest>("""{"parameters":{},"sortDescending":false}""", Web));
    }
}
