global using Xunit;

namespace Viper.test
{
    /// <summary>
    /// Marks the tests that call HttpHelper.Configure (directly, or via IntegrationTestBase /
    /// EffortIntegrationTestBase) as needing to run sequentially with each other, since
    /// HttpHelper's cache/settings are static and get clobbered by concurrent test runs.
    /// Other test classes are unaffected and keep running in parallel.
    /// </summary>
    [CollectionDefinition("HttpHelper static state", DisableParallelization = true)]
    public class HttpHelperStaticStateCollection
    {
    }
}
