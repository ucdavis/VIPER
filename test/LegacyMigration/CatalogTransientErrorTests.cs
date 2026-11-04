using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// A module's references record SQL Server's error only when the module itself is at fault. A failed read
/// must end the export instead, or an empty list would pass for a module that uses nothing.
/// </summary>
public class CatalogTransientErrorTests
{
    [Theory]
    [InlineData(-2, true)]
    [InlineData(1205, true)]
    [InlineData(207, false)]
    [InlineData(208, false)]
    [InlineData(229, false)]
    public void IsTransientError_OnlyForTimeoutsAndDeadlocks(int errorNumber, bool expected)
    {
        Assert.Equal(expected, Catalog.IsTransientError(errorNumber));
    }
}
