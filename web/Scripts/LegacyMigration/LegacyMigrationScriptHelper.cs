using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Shared utilities for the legacy migration toolkit, independent of any one legacy database.
    /// </summary>
    public static class LegacyMigrationScriptHelper
    {
        private static readonly string[] Environments = ["Development", "Test", "Production"];

        /// <summary>
        /// Returns the environment named by ASPNETCORE_ENVIRONMENT. It must be set explicitly, because
        /// the environment decides which Parameter Store connection string is used and which output
        /// folder is written; silently defaulting would make a Development export look like a Test one.
        /// </summary>
        public static string RequireEnvironment()
        {
            var value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return MatchEnvironment(value) ?? throw new InvalidOperationException(
                $"ASPNETCORE_ENVIRONMENT must be one of {string.Join(", ", Environments)} (was '{value}'). " +
                "Run the tool through its .bat launcher, which sets it from the environment argument.");
        }

        /// <summary>
        /// Returns the canonical spelling of an environment name given as an argument, which is also
        /// the name of its output folder.
        /// </summary>
        public static string ParseEnvironment(string? value)
        {
            return MatchEnvironment(value) ?? throw new ArgumentException(
                $"Environment must be one of {string.Join(", ", Environments)} (was '{value}').");
        }

        private static string? MatchEnvironment(string? value) =>
            Array.Find(Environments, e => e.Equals(value, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Reads a required "--name value" or "--name=value" argument.
        /// </summary>
        public static string GetRequiredArg(string[] args, string name)
        {
            var flag = $"--{name}";
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith(flag + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i][(flag.Length + 1)..];
                }
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }

            throw new ArgumentException($"Missing required {flag} <value> argument.");
        }

        /// <summary>
        /// A duration for the console: "42.3s" under a minute, else "3m 12s".
        /// </summary>
        public static string Duration(TimeSpan elapsed) =>
            elapsed.TotalSeconds < 60 ? $"{elapsed.TotalSeconds:0.0}s" : $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";

        /// <summary>
        /// The closing line of a command, pairing its "Started" line, so a run's length can be read off the console.
        /// </summary>
        public static void WriteFinished(System.Diagnostics.Stopwatch timer) =>
            Console.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss} (took {Duration(timer.Elapsed)})");

        /// <summary>
        /// Reads an optional "--name value" or "--name=value" argument; null when absent or blank.
        /// </summary>
        public static string? GetOptionalArg(string[] args, string name)
        {
            try
            {
                var value = GetRequiredArg(args, name);
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Connection names become output folder names, so only plain identifiers are accepted.
        /// </summary>
        public static string ValidateConnectionName(string? name) => ValidateFolderName(name, "Connection name");

        /// <summary>
        /// Project names become declarations and output folder names, as connection names do. Null means
        /// no project: the connection's own analysis.json.
        /// </summary>
        public static string? ValidateProjectName(string? name) => name == null ? null : ValidateFolderName(name, "Project name");

        private static string ValidateFolderName(string? name, string what)
        {
            if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, "^[A-Za-z0-9_]+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
            {
                throw new ArgumentException($"{what} must contain only letters, digits, and underscores (was '{name}').");
            }

            return name;
        }

        /// <summary>
        /// Where one analysis's declarations and results live: Areas/{connection}/analysis.json and
        /// Output/{connection}/analysis/{environment}/, or, for one project of a database holding several,
        /// Areas/{connection}/{project}/analysis.json and Output/{connection}/analysis/{environment}/{project}/.
        /// The schema export is per connection, shared by its projects.
        ///
        /// A run with no project, on a connection that has no analysis.json of its own but has project
        /// folders, is refused: with no declarations it would check every table, including those the
        /// projects' include lists leave out on purpose.
        /// </summary>
        public static (string DeclarationsPath, string OutputDir) AnalysisPaths(string toolRoot, string connection, string environment, string? project)
        {
            var areaDir = Path.Join(toolRoot, "Areas", connection);
            var outputDir = Path.Join(toolRoot, "Output", connection, "analysis", environment);
            if (project != null)
            {
                return (Path.Join(areaDir, project, "analysis.json"), Path.Join(outputDir, project));
            }

            var declarationsPath = Path.Join(areaDir, "analysis.json");
            if (!File.Exists(declarationsPath) && Directory.Exists(areaDir))
            {
                var projects = Directory.GetDirectories(areaDir)
                    .Where(d => File.Exists(Path.Join(d, "analysis.json")))
                    .Select(Path.GetFileName)
                    .Order(StringComparer.Ordinal)
                    .ToList();
                if (projects.Count > 0)
                {
                    throw new ArgumentException(
                        $"{connection} has projects ({string.Join(", ", projects)}) but no analysis.json of its own, so a run " +
                        "without a project would check every table. Name the project as the third argument.");
                }
            }
            return (declarationsPath, outputDir);
        }

        /// <summary>
        /// The tool's own folder, found by walking up from the build output to the project file, so
        /// output lands in the gitignored Output folder regardless of the caller's working directory.
        /// </summary>
        public static string GetToolRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Join(dir.FullName, "LegacyMigration.csproj")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                $"Could not find LegacyMigration.csproj above {AppContext.BaseDirectory}. Run the tool from its project folder.");
        }

        public static string GetConnectionString(IConfiguration configuration, string name, string environment, bool readOnly = true)
        {
            var connectionString = configuration.GetConnectionString(name);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"{name} database connection string not found in configuration for environment '{environment}'. " +
                    $"It resolves from AWS Parameter Store as /{environment}/ConnectionStrings/{name} " +
                    $"(or /Shared/ConnectionStrings/{name}).");
            }

            // ApplicationIntent=ReadOnly routes to a readable secondary where one exists. It does not
            // block writes on a standalone server; read-only safety comes from the commands only reading.
            if (readOnly)
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ApplicationIntent = ApplicationIntent.ReadOnly
                };
                connectionString = builder.ConnectionString;
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
        /// The configuration the tool can always read: the appsettings files copied next to the build
        /// output, and the environment.
        /// </summary>
        private static IConfigurationBuilder LocalConfiguration(string environment)
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

        /// <summary>
        /// Loads configuration from appsettings.json files and AWS Parameter Store: /{environment}, then
        /// /Shared, so a /Shared value wins. Each path is read on its own, so a login that can read one but
        /// not the other keeps the one it can read; a path that cannot be read is left out with a warning.
        /// </summary>
        public static IConfiguration LoadConfiguration(string environment)
        {
            AWSOptions awsOptions = new()
            {
                Region = RegionEndpoint.USWest1
            };

            // AddSystemsManager only registers the source; Build() is the Parameter Store call.
            return LoadConfiguration(environment, LocalConfiguration(environment),
                path => new ConfigurationBuilder().AddSystemsManager(path, awsOptions).Build());
        }

        /// <summary>
        /// LoadConfiguration with the local settings and the Parameter Store read passed in, for tests.
        /// </summary>
        internal static IConfiguration LoadConfiguration(string environment, IConfigurationBuilder local,
            Func<string, IConfiguration> readParameterStore)
        {
            Console.WriteLine($"Loading configuration for environment: {environment}");

            var loaded = new[] { "/" + environment, "/Shared" }
                .Select(path => TryReadParameterStore(path, readParameterStore))
                .OfType<IConfiguration>();
            foreach (var parameters in loaded)
            {
                local.AddConfiguration(parameters);
            }
            return local.Build();
        }

        private static IConfiguration? TryReadParameterStore(string path, Func<string, IConfiguration> read)
        {
            try
            {
                var loaded = read(path);
                Console.WriteLine($"Read {path} from AWS Parameter Store.");
                return loaded;
            }
            catch (Amazon.Runtime.AmazonServiceException ex)
            {
                WarnParameterStorePath(path, $"could not read it: {ex.Message}");
            }
            catch (Amazon.Runtime.AmazonClientException ex)
            {
                WarnParameterStorePath(path, $"could not read it: {ex.Message}");
            }
            // Off the network, an unreachable host surfaces as a timeout and an unresolvable one as a
            // failed HTTP request, not as an Amazon exception.
            catch (TimeoutException ex)
            {
                WarnParameterStorePath(path, $"timed out: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                WarnParameterStorePath(path, $"could not reach it: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                WarnParameterStorePath(path, $"AWS configuration error: {ex.Message}");
            }
            return null;
        }

        private static void WarnParameterStorePath(string path, string reason)
        {
            Console.WriteLine($"Warning: AWS Parameter Store {path}: {reason}");
            Console.WriteLine($"Continuing without {path}.");
        }

        public static void WriteError(string label, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n{label}: {message}");
            Console.ResetColor();
        }

        /// <summary>
        /// Writes text with CRLF line endings. Module definitions come back from SQL Server with
        /// whatever endings they were saved with, and mixed endings would show up as noise when
        /// comparing exports.
        /// </summary>
        public static void WriteCrlfFile(string path, string content)
        {
            var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
            File.WriteAllText(path, normalized);
        }
    }
}
