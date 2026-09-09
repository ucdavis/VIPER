namespace Viper.test;

/// <summary>
/// HttpHelper.Configure swaps a process-wide static cache, and xUnit runs test classes in parallel by
/// default. Every class that configures the cache must join this collection.
/// </summary>
[CollectionDefinition(HttpHelperCacheCollection.Name)]
public static class HttpHelperCacheCollection
{
    public const string Name = "HttpHelperCache";
}
