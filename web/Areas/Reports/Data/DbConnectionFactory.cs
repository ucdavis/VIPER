using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Reports.Data;

/// <summary>
/// Opens connections to databases that have no EF context, such as PPS and AcademicPersonnel,
/// by connection string name. Tests substitute a fake connection.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// A new, unopened connection. Throws <see cref="InvalidOperationException"/> when the
    /// connection string isn't configured.
    /// </summary>
    DbConnection Create(string connectionStringName);
}

/// <summary>
/// Creates SQL Server connections from <c>ConnectionStrings</c>, which AWS Systems Manager fills
/// in each environment. A missing string fails the report that needs it, not application startup.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Only creates a real SqlConnection; its callers are tested with a fake connection.")]
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbConnection Create(string connectionStringName)
    {
        string? connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{connectionStringName}' is not configured.");
        }

        return new SqlConnection(connectionString);
    }
}
