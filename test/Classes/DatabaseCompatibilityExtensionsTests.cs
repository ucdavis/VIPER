using Microsoft.Extensions.Configuration;
using Viper.Classes;

namespace Viper.test.Classes;

public class DatabaseCompatibilityExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] levels) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(levels.Select(l =>
                new KeyValuePair<string, string?>($"DatabaseCompatibilityLevels:{l.Key}", l.Value)))
            .Build();

    [Fact]
    public void UsesDatabaseEntryOverDefault()
    {
        var config = BuildConfig(("Default", "130"), ("RAPS", "170"));

        Assert.Equal(170, config.GetCompatibilityLevel("RAPS"));
    }

    [Fact]
    public void UsesDefaultWhenDatabaseHasNoEntry()
    {
        var config = BuildConfig(("Default", "140"), ("RAPS", "170"));

        Assert.Equal(140, config.GetCompatibilityLevel("VIPER"));
    }

    [Fact]
    public void Uses130WhenSectionIsMissing()
    {
        var config = BuildConfig();

        Assert.Equal(130, config.GetCompatibilityLevel("VIPER"));
    }
}
