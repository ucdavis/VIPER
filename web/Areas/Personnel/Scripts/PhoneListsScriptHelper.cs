using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Viper.Areas.Personnel.Scripts
{
    /// <summary>
    /// A person resolved from users.Person by legacy MothraId. IamId is nullable because
    /// the column itself is nullable in users.Person - the analysis script treats a
    /// resolved-but-null IamId as its own reportable case rather than a defensive corner case.
    /// </summary>
    public sealed record PersonLookup(string? IamId, string FullName, bool CurrentEmployee);

    /// <summary>
    /// Shared utilities for the PhoneLists data migration scripts.
    /// </summary>
    public static class PhoneListsScriptHelper
    {
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
                // Name the environment: the checked-in appsettings hold empty placeholders, so a
                // missing value almost always means AWS Parameter Store had nothing for THIS
                // environment - which is easy to miss when another environment resolved fine.
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                throw new InvalidOperationException(
                    $"{name} database connection string not found in configuration for environment '{environment}'. " +
                    $"It resolves from AWS Parameter Store (/{environment} or /Shared); the checked-in " +
                    $"appsettings.{environment}.json holds only an empty placeholder."
                );
            }

            // SECURITY: Automatically add ApplicationIntent=ReadOnly to the "PhoneLists" connection
            // string to prevent accidental modifications to the legacy database during analysis.
            if (name.Equals("PhoneLists", StringComparison.OrdinalIgnoreCase) && readOnly)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);

                if (builder.ApplicationIntent != ApplicationIntent.ReadOnly)
                {
                    builder.ApplicationIntent = ApplicationIntent.ReadOnly;
                    connectionString = builder.ConnectionString;
                    Console.WriteLine("  Added ApplicationIntent=ReadOnly to PhoneLists connection for safety");
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

            var fullPath = Path.GetFullPath(outputPath);

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

        /// <summary>
        /// Builds a MothraId -> PersonLookup map from users.Person, for resolving legacy
        /// PhoneLists identity references onto the IamId the new phones.Person schema keys on.
        /// Unlike Effort's PersonId-keyed map, this targets IamId since that's what
        /// phones.Person.PersonIam actually is a foreign key to.
        /// </summary>
        public static Dictionary<string, PersonLookup> BuildMothraIdToPersonLookupMap(SqlConnection viperConnection)
        {
            var map = new Dictionary<string, PersonLookup>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT MothraId, IamId, FullName, CurrentEmployee
                FROM [users].[Person]
                WHERE MothraId IS NOT NULL";

            using var cmd = new SqlCommand(sql, viperConnection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var mothraId = reader.GetString(0).Trim();
                var iamId = reader.IsDBNull(1) ? null : reader.GetString(1);
                var fullName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var currentEmployee = !reader.IsDBNull(3) && reader.GetBoolean(3);

                map[mothraId] = new PersonLookup(iamId, fullName, currentEmployee);
            }

            return map;
        }

        /// <summary>
        /// Builds a LoginId -> IamId map from users.Person. Used as the fallback when a legacy
        /// Who_Mod value doesn't resolve as a MothraId - the legacy schema never documented which
        /// identifier it stored, and Effort's audit_ModBy turned out to hold a mix of both.
        /// </summary>
        public static Dictionary<string, string> BuildLoginIdToIamIdMap(SqlConnection viperConnection)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT LoginId, IamId
                FROM [users].[Person]
                WHERE LoginId IS NOT NULL AND IamId IS NOT NULL";

            using var cmd = new SqlCommand(sql, viperConnection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var loginId = reader.GetString(0).Trim();
                var iamId = reader.GetString(1);

                // First mapping wins, in case of duplicate LoginIds.
                if (!map.ContainsKey(loginId))
                {
                    map[loginId] = iamId;
                }
            }

            return map;
        }

        /// <summary>
        /// Reports whether a destination column is a physical IDENTITY column. The phones schema
        /// was created outside this repo (no EF migrations, no checked-in DDL), so whether the
        /// caller-supplied PKs need SET IDENTITY_INSERT can only be determined at runtime.
        /// </summary>
        public static bool IsIdentityColumn(SqlConnection connection, string schema, string table, string column,
            SqlTransaction? transaction = null)
        {
            const string sql = @"
                SELECT c.is_identity
                FROM sys.columns c
                WHERE c.object_id = OBJECT_ID(@qualifiedTable) AND c.name = @column";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@qualifiedTable", $"[{schema}].[{table}]");
            cmd.Parameters.AddWithValue("@column", column);

            var result = cmd.ExecuteScalar();
            return result is bool isIdentity && isIdentity;
        }

        /// <summary>
        /// Reports whether a legacy MothraId names a person at all. Both a blank and an all-zero
        /// placeholder mean "nobody listed" - neither resolves against users.Person, and neither
        /// is a data problem worth failing the migration or reporting on.
        /// </summary>
        public static bool HasMothraId([NotNullWhen(true)] string? mothraId)
        {
            return !string.IsNullOrWhiteSpace(mothraId) && mothraId.Trim().TrimStart('0').Length > 0;
        }

        /// <summary>
        /// Collapses the two ways the legacy lists write the same local number: the 530 area code
        /// that VMDO spells out and SVM omits, and the campus-wide "75" prefix that short
        /// extensions drop. 530-752-0123, 752-0123, and 2-0123 all normalize alike. Anything not
        /// in one of those shapes is returned unchanged.
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            var trimmed = Regex.Replace(phone.Trim(), @"^530-", "");
            return Regex.IsMatch(trimmed, @"^\d-\d{4}$") ? "75" + trimmed : trimmed;
        }

        /// <summary>
        /// Writes a progress message to the console if the current count is at the specified interval.
        /// </summary>
        public static void ShowProgress(int current, int total, int interval = 5000, string itemName = "records")
        {
            if (current % interval == 0)
            {
                int percent = total > 0 ? current * 100 / total : 0;
                Console.WriteLine($"    Processing: {current:N0} / {total:N0} {itemName} ({percent}%)...");
            }
        }
    }
}
