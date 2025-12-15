using System;
using System.Collections.Generic;
using System.IO;
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
        /// Derives the academic year string from a date.
        /// Academic year runs July 1 to June 30 (e.g., July 2024 = "2024-2025", June 2024 = "2023-2024").
        /// This aligns with the legacy ColdFusion Effort system's date-based academic year calculation.
        /// </summary>
        /// <param name="date">The date to derive academic year from</param>
        /// <returns>Academic year string in format "YYYY-YYYY"</returns>
        public static string GetAcademicYearFromDate(DateTime date)
        {
            int startYear = date.Month >= 7 ? date.Year : date.Year - 1;
            return $"{startYear}-{startYear + 1}";
        }

        /// <summary>
        /// Returns the SQL CASE expression for deriving academic year from a date column.
        /// Use this in SQL queries that need to derive academic year from percent_start.
        /// This matches the logic in GetAcademicYearFromDate() and the shadow view derivation.
        /// </summary>
        /// <param name="dateColumn">The name of the date column (e.g., "perc.percent_start")</param>
        /// <returns>SQL CASE expression that evaluates to "YYYY-YYYY" format</returns>
        public static string GetAcademicYearFromDateSql(string dateColumn)
        {
            return $@"CASE
                WHEN MONTH({dateColumn}) >= 7 THEN
                    CAST(YEAR({dateColumn}) AS varchar) + '-' + CAST(YEAR({dateColumn})+1 AS varchar)
                ELSE
                    CAST(YEAR({dateColumn})-1 AS varchar) + '-' + CAST(YEAR({dateColumn}) AS varchar)
            END";
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

        #region Progress Indicator

        /// <summary>
        /// Writes a progress message to the console if the current count is at the specified interval.
        /// Provides consistent progress output format across all migration scripts.
        /// </summary>
        /// <param name="current">Current record count (0-based)</param>
        /// <param name="total">Total number of records</param>
        /// <param name="interval">How often to show progress (default: every 5000 records)</param>
        /// <param name="itemName">Name of items being processed (default: "records")</param>
        /// <example>
        /// foreach (var item in items)
        /// {
        ///     EffortScriptHelper.ShowProgress(processed, totalRecords, 5000, "persons");
        ///     // ... process item ...
        ///     processed++;
        /// }
        /// </example>
        public static void ShowProgress(int current, int total, int interval = 5000, string itemName = "records")
        {
            if (current % interval == 0)
            {
                int percent = total > 0 ? current * 100 / total : 0;
                Console.WriteLine($"    Processing: {current:N0} / {total:N0} {itemName} ({percent}%)...");
            }
        }

        #endregion

        #region Person Lookup Helpers

        private static Dictionary<string, int>? _mothraIdToPersonIdCache;
        private static (HashSet<string> MothraIds, Dictionary<string, string> LoginIdToMothraId)? _loginIdMappingsCache;

        /// <summary>
        /// Clears all cached person lookups. Call this at the start of a new script run
        /// or if the underlying Person data may have changed.
        /// </summary>
        public static void ClearPersonLookupCache()
        {
            _mothraIdToPersonIdCache = null;
            _loginIdMappingsCache = null;
        }

        /// <summary>
        /// Builds (or returns cached) MothraId → PersonId lookup dictionary.
        /// Used by migration scripts to convert legacy MothraId references to PersonId.
        /// Results are cached for performance when called multiple times.
        /// </summary>
        /// <param name="connection">Open connection to VIPER database</param>
        /// <param name="transaction">Optional transaction (for migration scripts)</param>
        /// <returns>Dictionary mapping MothraId (string) to PersonId (int)</returns>
        public static Dictionary<string, int> BuildMothraIdToPersonIdMap(
            SqlConnection connection,
            SqlTransaction? transaction = null)
        {
            if (_mothraIdToPersonIdCache != null)
            {
                return _mothraIdToPersonIdCache;
            }

            var map = new Dictionary<string, int>();

            using var cmd = new SqlCommand(
                "SELECT MothraId, PersonId FROM [users].[Person] WHERE MothraId IS NOT NULL",
                connection,
                transaction);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                map[reader.GetString(0)] = reader.GetInt32(1);
            }

            _mothraIdToPersonIdCache = map;
            return map;
        }

        /// <summary>
        /// Builds (or returns cached) lookup structures for LoginId → MothraId conversion and MothraId validation.
        /// Used by analysis and remediation scripts to normalize audit_ModBy values.
        /// Results are cached for performance when called multiple times.
        /// </summary>
        /// <param name="connection">Open connection to VIPER database</param>
        /// <returns>
        /// Tuple containing:
        /// - MothraIds: HashSet of all valid MothraIds for validation
        /// - LoginIdToMothraId: Dictionary mapping LoginId to MothraId for conversion
        /// </returns>
        public static (HashSet<string> MothraIds, Dictionary<string, string> LoginIdToMothraId) BuildLoginIdMappings(
            SqlConnection connection)
        {
            if (_loginIdMappingsCache.HasValue)
            {
                return _loginIdMappingsCache.Value;
            }

            var mothraIds = new HashSet<string>();
            var loginIdToMothraId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = new SqlCommand(
                "SELECT MothraId FROM [users].[Person] WHERE MothraId IS NOT NULL",
                connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    mothraIds.Add(reader.GetString(0));
                }
            }

            using (var cmd = new SqlCommand(
                "SELECT LoginId, MothraId FROM [users].[Person] WHERE LoginId IS NOT NULL AND MothraId IS NOT NULL",
                connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string loginId = reader.GetString(0);
                    string mothraId = reader.GetString(1);
                    // First mapping wins (in case of duplicates)
                    if (!loginIdToMothraId.ContainsKey(loginId))
                    {
                        loginIdToMothraId[loginId] = mothraId;
                    }
                }
            }

            _loginIdMappingsCache = (mothraIds, loginIdToMothraId);
            return _loginIdMappingsCache.Value;
        }

        #endregion
    }
}
