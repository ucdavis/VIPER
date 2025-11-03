using System;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Shared utilities for Effort data migration scripts
    /// </summary>
    public static class EffortScriptHelper
    {
        public static IConfiguration LoadConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var appRoot = GetApplicationRoot();

            var builder = new ConfigurationBuilder()
                .SetBasePath(appRoot)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            try
            {
                AWSOptions awsOptions = new()
                {
                    Region = RegionEndpoint.USWest1
                };

                builder.AddSystemsManager("/" + environment, awsOptions)
                       .AddSystemsManager("/Shared", awsOptions);

                Console.WriteLine($"Successfully connected to AWS Parameter Store for environment: {environment}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to connect to AWS Parameter Store. " +
                    "Ensure AWS credentials are configured (run the main VIPER application first or configure AWS credentials).",
                    ex
                );
            }

            return builder.Build();
        }

        public static string GetApplicationRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();

            if (currentDir.Contains("Scripts"))
            {
                currentDir = Path.GetFullPath(Path.Combine(currentDir, "..", "..", ".."));
            }

            if (!File.Exists(Path.Combine(currentDir, "appsettings.json")))
            {
                var parentDir = Path.GetFullPath(Path.Combine(currentDir, "..", ".."));
                if (File.Exists(Path.Combine(parentDir, "appsettings.json")))
                {
                    currentDir = parentDir;
                }
            }

            return currentDir;
        }

        public static string GetConnectionString(IConfiguration configuration, string name)
        {
            var connectionString = configuration.GetConnectionString(name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"{name} database connection string not found in configuration.\n" +
                    $"Available connection strings:\n" +
                    string.Join("\n", GetAvailableConnectionStrings(configuration))
                );
            }

            return connectionString;
        }

        public static string GetServerAndDatabase(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return $"{builder.DataSource}/{builder.InitialCatalog}";
            }
            catch
            {
                return "Could not parse connection string";
            }
        }

        public static string BuildMothraIdWhereClause(string columnName, string parameterName,
            bool excludeIfAlreadyNewValue = false, string? newValueParameter = null)
        {
            var whereClause = $"RTRIM({columnName}) = {parameterName}";

            if (excludeIfAlreadyNewValue && !string.IsNullOrEmpty(newValueParameter))
            {
                whereClause += $" AND RTRIM({columnName}) COLLATE Latin1_General_BIN <> {newValueParameter}";
            }

            return whereClause;
        }

        private static string[] GetAvailableConnectionStrings(IConfiguration configuration)
        {
            var connectionStrings = new System.Collections.Generic.List<string>();
            var connectionStringSection = configuration.GetSection("ConnectionStrings");

            foreach (var child in connectionStringSection.GetChildren())
            {
                var value = child.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    connectionStrings.Add($"  \"{child.Key}\": \"{MaskConnectionString(value)}\"");
                }
                else
                {
                    connectionStrings.Add($"  \"{child.Key}\": \"(empty)\"");
                }
            }

            return connectionStrings.Count > 0 ? connectionStrings.ToArray() : new[] { "  (no connection strings found)" };
        }

        private static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return "(empty)";

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return $"Server={builder.DataSource};Database={builder.InitialCatalog};...";
            }
            catch
            {
                return connectionString.Length > 20 ? connectionString.Substring(0, 20) + "..." : connectionString;
            }
        }
    }
}
