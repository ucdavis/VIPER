using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Each Parameter Store path is read on its own, so a login that cannot read one path keeps what the
/// other gave, and /Shared still wins over /{environment} when both load.
/// </summary>
[InvariantCulture]
public class LoadConfigurationTests
{
    private static IConfigurationBuilder Local() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Local"] = "local",
            ["ConnectionStrings:Overridden"] = "local"
        });

    private static IConfiguration Values(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    private static IConfiguration Load(Func<string, IConfiguration> readParameterStore) =>
        LegacyMigrationScriptHelper.LoadConfiguration("Test", Local(), readParameterStore);

    [Fact]
    public void SharedFails_KeepsTheEnvironmentPath()
    {
        var configuration = Load(path => path == "/Test"
            ? Values(("ConnectionStrings:Legacy", "from test"))
            : throw new AmazonServiceException("AccessDeniedException"));

        Assert.Equal("from test", configuration.GetConnectionString("Legacy"));
        Assert.Equal("local", configuration.GetConnectionString("Local"));
    }

    [Fact]
    public void EnvironmentFails_KeepsShared()
    {
        var configuration = Load(path => path == "/Shared"
            ? Values(("ConnectionStrings:Legacy", "from shared"))
            : throw new TimeoutException("timed out"));

        Assert.Equal("from shared", configuration.GetConnectionString("Legacy"));
    }

    [Fact]
    public void BothLoad_SharedWinsAndBothWinOverLocal()
    {
        var configuration = Load(path => path == "/Test"
            ? Values(("ConnectionStrings:Legacy", "from test"), ("ConnectionStrings:Overridden", "from test"))
            : Values(("ConnectionStrings:Legacy", "from shared")));

        Assert.Equal("from shared", configuration.GetConnectionString("Legacy"));
        Assert.Equal("from test", configuration.GetConnectionString("Overridden"));
        Assert.Equal("local", configuration.GetConnectionString("Local"));
    }

    [Fact]
    public void BothFail_FallsBackToLocal()
    {
        var configuration = Load(_ => throw new HttpRequestException("no such host"));

        Assert.Equal("local", configuration.GetConnectionString("Overridden"));
        Assert.Null(configuration.GetConnectionString("Legacy"));
    }

    [Fact]
    public void ReadsEachPathOnce_EnvironmentFirst()
    {
        var read = new List<string>();

        Load(path =>
        {
            read.Add(path);
            return Values();
        });

        Assert.Equal(["/Test", "/Shared"], read);
    }
}
