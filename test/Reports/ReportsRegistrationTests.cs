using Microsoft.Extensions.DependencyInjection;
using Viper.Areas.Reports;
using Viper.Areas.Reports.Data;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

public sealed class ReportsRegistrationTests
{
    [Fact]
    public void AddReports_RegistersDefinitionsPlannedReportsRegistryAndClock()
    {
        var services = new ServiceCollection();
        services.AddReports(typeof(ReportsRegistrationTests).Assembly);

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();

        IReportRegistry registry = scope.ServiceProvider.GetRequiredService<IReportRegistry>();
        Assert.NotNull(registry.Find("test.sample"));
        Assert.Contains(TestPlannedReports.Visible, registry.Planned);
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Equal(["csv", "xlsx", "pdf"], provider.GetServices<IReportExporter>().Select(exporter => exporter.Format));
    }

    [Fact]
    public void AddReports_RegistersTheStoredProcedureRunner()
    {
        var services = new ServiceCollection();

        services.AddReports(typeof(ReportsRegistrationTests).Assembly);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStoredProcedureRunner)
            && descriptor.ImplementationType == typeof(StoredProcedureRunner)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IDbConnectionFactory)
            && descriptor.ImplementationType == typeof(SqlConnectionFactory)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddReports_KeepsAnExistingClock()
    {
        var clock = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);

        services.AddReports(typeof(ReportsRegistrationTests).Assembly);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddReports_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ReportsServiceCollectionExtensions.AddReports(null!, typeof(ReportsRegistrationTests).Assembly));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddReports(null!));
    }
}
