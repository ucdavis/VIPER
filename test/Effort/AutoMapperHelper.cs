using AutoMapper;
using Viper.Areas.Effort.Models;

namespace Viper.test.Effort;

/// <summary>
/// Helper class to create AutoMapper instances for tests.
/// </summary>
public static class AutoMapperHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfileEffort>();
        });
        return config.CreateMapper();
    }
}
