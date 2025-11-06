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

        public static string GetApplicationRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();

            if (currentDir.Contains("Scripts"))
            {
                currentDir = Path.GetFullPath(Path.Join(currentDir, "..", "..", ".."));
            }

            if (!File.Exists(Path.Join(currentDir, "appsettings.json")))
            {
                var parentDir = Path.GetFullPath(Path.Join(currentDir, "..", ".."));
                if (File.Exists(Path.Join(parentDir, "appsettings.json")))
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
                    $"{name} database connection string not found in configuration."
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
            catch (ArgumentException ex)
            {
                return $"Could not parse connection string: {ex.Message}";
            }
            catch (FormatException ex)
            {
                return $"Could not parse connection string: {ex.Message}";
            }
        }

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

        public static string ValidateOutputPath(string? outputPath, string defaultSubfolder)
        {
            // Validate defaultSubfolder to prevent path traversal or absolute paths
            if (string.IsNullOrWhiteSpace(defaultSubfolder)
                || Path.IsPathRooted(defaultSubfolder)
                || defaultSubfolder.Contains(".."))
            {
                throw new InvalidOperationException(
                    $"Default subfolder must be a non-empty, relative path without path traversal. Value: '{defaultSubfolder}'");
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return Path.Join(Directory.GetCurrentDirectory(), defaultSubfolder);
            }

            // Get full path to resolve any relative paths or path traversal attempts
            var fullPath = Path.GetFullPath(outputPath);

            // Ensure the path is under the current directory
            // This prevents path traversal attacks like "../../etc/passwd", sibling directory escapes,
            // and paths on different drives/UNC shares
            var currentDir = Directory.GetCurrentDirectory();
            var relative = Path.GetRelativePath(currentDir, fullPath);
            if (Path.IsPathRooted(relative)
                || relative.Equals("..", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Output path must be within the current directory. " +
                    $"Current directory: {currentDir}, Requested path: {fullPath}");
            }

            return fullPath;
        }
    }
}
