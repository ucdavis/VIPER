using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Viper.Areas.Reports.Data;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;

namespace Viper.Areas.Reports;

public static class ReportsServiceCollectionExtensions
{
    /// <summary>
    /// Registers every <see cref="IReportDefinition"/> and <see cref="IPlannedReportSource"/> in
    /// <paramref name="reportAssembly"/>, the registry that indexes them, and the export formats.
    /// Definitions are scoped because they depend on scoped data services; planned report lists
    /// are fixed data, so they are singletons. Also registers the stored procedure runner that
    /// report data services use for databases without an EF context. <see cref="Services.ReportService"/>
    /// itself is registered by the Scrutor service scan.
    /// </summary>
    public static IServiceCollection AddReports(this IServiceCollection services, Assembly reportAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(reportAssembly);

        services.TryAddSingleton(TimeProvider.System);
        services.Scan(scan => scan
            .FromAssemblies(reportAssembly)
            .AddClasses(classes => classes.AssignableTo<IReportDefinition>(), publicOnly: false)
            .As<IReportDefinition>()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo<IPlannedReportSource>(), publicOnly: false)
            .As<IPlannedReportSource>()
            .WithSingletonLifetime());
        services.TryAddScoped<IReportRegistry, ReportRegistry>();
        services.TryAddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.TryAddSingleton<IStoredProcedureRunner, StoredProcedureRunner>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportExporter, ReportCsvExporter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportExporter, ReportExcelExporter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportExporter, ReportPdfExporter>());
        return services;
    }
}
