using System;
using System.Collections.Generic;
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
        /// <summary>
        /// All tables in the effort schema - single source of truth
        /// </summary>
        public static readonly string[] EffortTables =
        [
            "Records", "Percentages", "Persons", "Courses", "TermStatus",
            "Roles", "EffortTypes", "SessionTypes", "Sabbaticals", "UserAccess", "Units",
            "JobCodes", "ReportUnits", "AlternateTitles",
            "CourseRelationships", "Audits"
        ];

        /// <summary>
        /// Expected number of tables in the effort schema
        /// </summary>
        public static readonly int ExpectedTableCount = EffortTables.Length;

        /// <summary>
        /// All valid SessionType codes - single source of truth for both schema creation and validation
        /// These values are seeded into effort.SessionTypes table during database creation
        /// </summary>
        public static readonly string[] ValidSessionTypes =
        [
            "ACT", "AUT", "CBL", "CLI", "CON", "D/L", "DIS", "DSL", "EXM", "FAS",
            "FWK", "IND", "INT", "JLC", "L/D", "LAB", "LEC", "LED", "LIS", "LLA",
            "PBL", "PER", "PRA", "PRB", "PRJ", "PRS", "SEM", "STD", "T-D", "TBL",
            "TMP", "TUT", "VAR", "WED", "WRK", "WVL"
        ];

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

        public static string GetConnectionString(IConfiguration configuration, string name, bool readOnly = true)
        {
            var connectionString = configuration.GetConnectionString(name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"{name} database connection string not found in configuration."
                );
            }

            // SECURITY: Automatically add ApplicationIntent=ReadOnly to the "Effort" connection string
            // to prevent accidental modifications to the legacy database during migration
            // Exception: Data remediation script needs write access to fix data quality issues
            if (name.Equals("Effort", StringComparison.OrdinalIgnoreCase) && readOnly)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);

                // Only add if not already present
                if (builder.ApplicationIntent != ApplicationIntent.ReadOnly)
                {
                    builder.ApplicationIntent = ApplicationIntent.ReadOnly;
                    connectionString = builder.ConnectionString;
                    Console.WriteLine("  ℹ Added ApplicationIntent=ReadOnly to Effort connection for safety");
                }
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

        /// <summary>
        /// Gets the current academic year from the VIPER system by querying the current term.
        /// Uses the system's CurrentTerm flag to determine the active academic year.
        /// Queries the Courses database to get the academic year for the term.
        /// </summary>
        /// <param name="connection">Open SQL connection to the VIPER database</param>
        /// <param name="transaction">Optional transaction to use for the query</param>
        /// <returns>Academic year string in format "YYYY-YYYY" (e.g., "2024-2025")</returns>
        public static string GetCurrentAcademicYear(SqlConnection connection, SqlTransaction? transaction = null)
        {
            string sql = "SELECT TOP 1 TermCode FROM [VIPER].[dbo].[vwTerms] WHERE CurrentTerm = 1";
            using var cmd = new SqlCommand(sql, connection, transaction);
            var result = cmd.ExecuteScalar();

            if (result == null)
            {
                throw new InvalidOperationException(
                    "No current term found in VIPER database. Please ensure CurrentTerm flag is set in vwTerms.");
            }

            int termCode = Convert.ToInt32(result);

            // Convert the term code to academic year format
            return GetAcademicYearFromTermCode(termCode);
        }

        /// <summary>
        /// Converts a term code to academic year format (YYYY-YYYY).
        /// Academic year runs from Fall through Spring (e.g., 2024-2025 includes Fall 2024 and Spring 2025).
        /// Winter (01), Spring Semester (02), and Spring Quarter (03) belong to the academic year that started in the previous calendar year.
        /// All other terms (Summer, Fall) belong to the academic year starting in the current calendar year.
        /// </summary>
        /// <param name="termCode">Term code (e.g., 202502 returns "2024-2025", 202410 returns "2024-2025")</param>
        /// <returns>Academic year string (e.g., "2024-2025")</returns>
        private static string GetAcademicYearFromTermCode(int termCode)
        {
            int year = termCode / 100;
            int term = termCode % 100;

            // Winter Quarter (01), Spring Semester (02), and Spring Quarter (03) belong to academic year that started previous calendar year
            // All other terms (Summer, Fall) start or belong to academic year in the current calendar year
            int startYear = (term >= 1 && term <= 3) ? year - 1 : year;

            return $"{startYear}-{startYear + 1}";
        }

        /// <summary>
        /// Loads configuration from appsettings.json files and AWS Parameter Store.
        /// Falls back gracefully to appsettings.json only if AWS is unavailable.
        /// </summary>
        public static IConfiguration LoadConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var appRoot = GetApplicationRoot();

            Console.WriteLine($"Loading configuration for environment: {environment}");
            Console.WriteLine($"Configuration root: {appRoot}");

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
            catch (Amazon.Runtime.AmazonServiceException ex)
            {
                Console.WriteLine($"Warning: Could not connect to AWS Parameter Store: {ex.Message}");
                Console.WriteLine("Continuing with appsettings.json configuration only.");
            }
            catch (Amazon.Runtime.AmazonClientException ex)
            {
                Console.WriteLine($"Warning: Could not connect to AWS Parameter Store: {ex.Message}");
                Console.WriteLine("Continuing with appsettings.json configuration only.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Warning: AWS configuration error: {ex.Message}");
                Console.WriteLine("Continuing with appsettings.json configuration only.");
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
