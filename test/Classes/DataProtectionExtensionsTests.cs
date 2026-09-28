using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Viper.Classes;

namespace Viper.test.Classes;

/// <summary>
/// The blue/green slots only share auth cookies if they share both a key ring
/// and an application name. See DataProtectionExtensions.
/// </summary>
public class DataProtectionExtensionsTests
{
    [Fact]
    public void AddViperDataProtection_WithKeyRingPath_PersistsKeysThere()
    {
        var keyRingPath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var services = BuildProvider(new() { ["DataProtection:KeyRingPath"] = keyRingPath });

            var repository = Assert.IsType<FileSystemXmlRepository>(
                services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
            Assert.Equal(keyRingPath, repository.Directory.FullName);
        }
        finally
        {
            if (Directory.Exists(keyRingPath))
            {
                Directory.Delete(keyRingPath, recursive: true);
            }
        }
    }

    [Fact]
    public void AddViperDataProtection_SetsTheApplicationNameBothSlotsShare()
    {
        var services = BuildProvider([]);

        Assert.Equal(
            "VIPER2",
            services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection()
            .AddLogging()
            .AddViperDataProtection(configuration)
            .BuildServiceProvider();
    }
}
