using System.Text.Json;
using Viper.Areas.Personnel.Reports;
using Viper.Areas.Reports.Engine;
using Viper.test.Personnel;

namespace Viper.test.Reports;

/// <summary>
/// Checks every report definition against the rules the engine relies on but the compiler can't
/// enforce. New reports join by being added to <see cref="Definitions"/>, so each one gets these
/// checks without writing them again.
/// </summary>
public sealed class ReportDefinitionContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, Func<IReportDefinition>> Definitions = new(StringComparer.Ordinal)
    {
        ["test.sample"] = () => new TestReport(),
        ["personnel.employees-on-leave"] = () =>
            new EmployeesOnLeaveReport(new StubPersonnelReportDataService(), new FixedTimeProvider(DateTimeOffset.UnixEpoch)),
        ["personnel.exempt-non-exempt"] = () => new ExemptStatusReport(new StubPersonnelReportDataService()),
        ["personnel.faculty-profile"] = () => new FacultyProfileReport(new StubPersonnelReportDataService()),
    };

    public static TheoryData<string> DefinitionKeys()
    {
        return [.. Definitions.Keys];
    }

    private static ReportContext Everyone()
    {
        return new ReportContext("contract", _ => true);
    }

    [Theory]
    [MemberData(nameof(DefinitionKeys))]
    public void Definition_HasAValidRegistryIdentity(string key)
    {
        IReportDefinition definition = Definitions[key]();

        var registry = new ReportRegistry([definition]);

        Assert.Same(definition, registry.Find(key));
    }

    [Theory]
    [MemberData(nameof(DefinitionKeys))]
    public void Metadata_SerializesToJson(string key)
    {
        IReportDefinition definition = Definitions[key]();

        string json = JsonSerializer.Serialize(definition.GetMetadata(Everyone()), WebJson);

        Assert.Contains($"\"key\":\"{key}\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(DefinitionKeys))]
    public void EveryParameterName_BindsFromJson(string key)
    {
        IReportDefinition definition = Definitions[key]();
        IEnumerable<string> names = definition.GetMetadata(Everyone()).Parameters.Select(parameter => parameter.Name);
        using JsonDocument body = JsonDocument.Parse("{" + string.Join(",", names.Select(name => $"\"{name}\":null")) + "}");

        bool bound = ReportParameterBinder.TryBind(definition.ParametersType, body.RootElement, out _, out ReportValidationError? error);

        Assert.True(bound, error?.Message);
    }

    [Theory]
    [MemberData(nameof(DefinitionKeys))]
    public async Task DefaultParameters_RunAndSerialize(string key)
    {
        IReportDefinition definition = Definitions[key]();
        ReportContext context = Everyone();
        bool bound = ReportParameterBinder.TryBind(definition.ParametersType, null, out object? parameters, out _);
        Assert.True(bound);
        Assert.NotNull(parameters);
        Assert.Empty(definition.PrepareParameters(parameters));

        ReportResult result = await definition.RunAsync(parameters, context, DateTimeOffset.UnixEpoch, TestContext.Current.CancellationToken);

        Assert.Equal(key, result.Key);
        Assert.NotEmpty(JsonSerializer.Serialize(result, WebJson));
    }
}
