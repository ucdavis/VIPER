using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Viper.Areas.Students.Scripts
{
    /// <summary>
    /// A person resolved from users.Person by legacy MothraId, plus whether AAUD still lists them
    /// as a current SVM affiliate. The mentor picker validates against vw_CurrentAffiliates, so a
    /// mentor who resolves but is no longer an affiliate migrates fine and then fails the first
    /// time an admin tries to save that student's record - which is worth reporting separately
    /// from a MothraId that names nobody at all.
    /// </summary>
    public sealed record MentorLookup(string FullName, bool IsCurrentAffiliate);

    /// <summary>
    /// Shared utilities for the CareerSelection data migration scripts.
    /// </summary>
    public static class CareerSelectionScriptHelper
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

            // SECURITY: SIS is the live student information system and this migration only ever
            // reads from it. Forcing ReadOnly means a mistake in a legacy query cannot write.
            if (name.Equals("SIS", StringComparison.OrdinalIgnoreCase) && readOnly)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);

                if (builder.ApplicationIntent != ApplicationIntent.ReadOnly)
                {
                    builder.ApplicationIntent = ApplicationIntent.ReadOnly;
                    connectionString = builder.ConnectionString;
                    Console.WriteLine("  Added ApplicationIntent=ReadOnly to SIS connection for safety");
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
        /// The configuration the script can always read: the appsettings files next to it and the
        /// environment. Built fresh on each call because a builder that has had Parameter Store
        /// registered on it cannot be reused as the fallback.
        /// </summary>
        private static IConfigurationBuilder LocalConfiguration(string appRoot, string environment)
        {
            return new ConfigurationBuilder()
                .SetBasePath(appRoot)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

        private static void WarnParameterStoreUnavailable(string reason)
        {
            Console.WriteLine($"Warning: {reason}");
            Console.WriteLine("Continuing with appsettings.json configuration only.");
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

            try
            {
                AWSOptions awsOptions = new()
                {
                    Region = RegionEndpoint.USWest1
                };

                // Build() has to be inside the try. AddSystemsManager only appends a source; the
                // Parameter Store call happens when Build() creates the providers and loads them.
                var configuration = LocalConfiguration(appRoot, environment)
                    .AddSystemsManager("/" + environment, awsOptions)
                    .AddSystemsManager("/Shared", awsOptions)
                    .Build();

                Console.WriteLine($"Successfully connected to AWS Parameter Store for environment: {environment}");
                return configuration;
            }
            catch (Amazon.Runtime.AmazonServiceException ex)
            {
                WarnParameterStoreUnavailable($"Could not connect to AWS Parameter Store: {ex.Message}");
            }
            catch (Amazon.Runtime.AmazonClientException ex)
            {
                WarnParameterStoreUnavailable($"Could not connect to AWS Parameter Store: {ex.Message}");
            }
            // Running off the network is the case this fallback exists for, and it does not arrive
            // as an Amazon exception: an unreachable host surfaces as a timeout and an unresolvable
            // one as a failed HTTP request.
            catch (TimeoutException ex)
            {
                WarnParameterStoreUnavailable($"Timed out reaching AWS Parameter Store: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                WarnParameterStoreUnavailable($"Could not reach AWS Parameter Store: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                WarnParameterStoreUnavailable($"AWS configuration error: {ex.Message}");
            }

            return LocalConfiguration(appRoot, environment).Build();
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
        /// Builds a MothraId -> MentorLookup map for resolving tb_CareerSelection.facultyMothraID.
        /// Unlike the PhoneLists migration this does not resolve onto IamId - students.CareerSelection
        /// stores the MothraId itself - so the lookup exists only to report who no longer resolves.
        /// The affiliate flag comes from AAUD's vw_CurrentAffiliates, which is what
        /// CareerSelectionService.IsCurrentAffiliateAsync validates a submitted mentor against.
        /// </summary>
        public static Dictionary<string, MentorLookup> BuildMentorLookupMap(
            SqlConnection viperConnection, SqlConnection aaudConnection)
        {
            var map = new Dictionary<string, MentorLookup>(StringComparer.OrdinalIgnoreCase);

            const string personSql = @"
                SELECT MothraId, FullName
                FROM [users].[Person]
                WHERE MothraId IS NOT NULL";

            using (var cmd = new SqlCommand(personSql, viperConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var mothraId = reader.GetString(0).Trim();
                    var fullName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    map[mothraId] = new MentorLookup(fullName, false);
                }
            }

            const string affiliateSql = @"
                SELECT DISTINCT ids_mothraid
                FROM [dbo].[vw_CurrentAffiliates]
                WHERE ids_mothraid IS NOT NULL";

            using (var cmd = new SqlCommand(affiliateSql, aaudConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var mothraId = reader.GetString(0).Trim();

                    // An affiliate with no users.Person row still counts as a current affiliate;
                    // record it so the two failure modes stay distinguishable in the report.
                    map[mothraId] = map.TryGetValue(mothraId, out var existing)
                        ? existing with { IsCurrentAffiliate = true }
                        : new MentorLookup("", true);
                }
            }

            return map;
        }

        /// <summary>
        /// The PIDMs of every current DVM student, as the set the front end actually works from:
        /// DvmStudentLookupService reads vw_DVM_Students_maxTerm unfiltered, so being in the view
        /// is what makes a student current. Keyed on the parsed int for the same reason the app
        /// parses it - the view stores PIDM as a string and the new table keys on an int.
        /// </summary>
        public static HashSet<int> LoadActiveStudentPidms(SqlConnection aaudConnection)
        {
            var pidms = new HashSet<int>();

            const string sql = @"
                SELECT ids_pidm
                FROM [dbo].[vw_DVM_Students_maxTerm]
                WHERE ids_pidm IS NOT NULL";

            using var cmd = new SqlCommand(sql, aaudConnection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (TryParsePidm(reader.GetString(0), out var pidm))
                {
                    pidms.Add(pidm);
                }
            }

            return pidms;
        }

        /// <summary>
        /// Reports whether a legacy MothraId names a person at all. Both a blank and an all-zero
        /// placeholder mean "nobody listed" - neither resolves, and neither is a data problem.
        /// </summary>
        public static bool HasMothraId([NotNullWhen(true)] string? mothraId)
        {
            return !string.IsNullOrWhiteSpace(mothraId) && mothraId.Trim().TrimStart('0').Length > 0;
        }

        /// <summary>
        /// Parses a legacy VARCHAR(8) PIDM into the int the new schema stores. Rejects anything
        /// non-numeric rather than coercing it, so a bad value is reported instead of migrated as
        /// a plausible-looking wrong number.
        /// </summary>
        public static bool TryParsePidm(string? raw, out int pidm)
        {
            pidm = 0;
            var trimmed = raw?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            foreach (var c in trimmed)
            {
                if (!char.IsAsciiDigit(c))
                {
                    return false;
                }
            }

            return int.TryParse(trimmed, out pidm);
        }

        /// <summary>
        /// A PIDM whose stored spelling carries a leading zero the int column cannot keep.
        /// "00012345" and "12345" both become 12345, so two such rows would also collide on the
        /// destination's unique index - worth reporting even though each one parses cleanly.
        /// </summary>
        public static bool HasLeadingZero(string? raw)
        {
            var trimmed = raw?.Trim();
            return trimmed is { Length: > 1 } && trimmed[0] == '0';
        }

        /// <summary>
        /// Identifies the catch-all row in a legacy lookup table by an "Other" prefix, which in
        /// practice means LK_PostGrad's "Other: Please explain in the short-term career section below".
        /// LK_careers and LK_species have no such row at all - legacy marked those choices with a
        /// null id plus text in the matching Other column - so the migration creates one.
        ///
        /// A prefix rather than an exact match, because the one real row carries trailing
        /// instructions. Confirmed against the data that this catches no genuine option; the
        /// analysis still prints every candidate, and the migration refuses to guess if a table
        /// turns up more than one.
        /// </summary>
        public static bool IsOtherLabel(string? label)
        {
            return label?.Trim().StartsWith("Other", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// The label every catch-all row ends up with. LK_PostGrad's row is renamed to this, and
        /// the rows the migration creates for CareerOption and SpeciesOption are created with it -
        /// the trailing instructions belong in the UI, not in the stored option.
        /// </summary>
        public const string OtherLabel = "Other";

        /// <summary>
        /// Whether a legacy row encoded "Other" the way legacy actually recorded it: no option
        /// selected, but free text in the matching Other column. Those rows are re-pointed at the
        /// catch-all option. A null id with no text is genuinely unanswered and stays null.
        /// </summary>
        public static bool IsImplicitOther(int? optionId, string? otherText)
        {
            return optionId is null && !string.IsNullOrWhiteSpace(otherText);
        }

        /// <summary>One row of a legacy lookup table.</summary>
        public sealed record LookupRow(int Id, string Label);

        /// <summary>
        /// What to do about one lookup table's catch-all row. <see cref="Error"/> being set means
        /// the data is not what the migration was designed against and nothing should be written;
        /// otherwise <see cref="OtherId"/> is the id the catch-all ends up with, whether it was
        /// renamed in place or has to be created.
        /// </summary>
        public sealed record LookupOtherPlan(
            int OtherId, bool IsNewRow, string? RenamedFrom, string? Error)
        {
            public string Describe() => Error is not null
                ? $"ERROR: {Error}"
                : IsNewRow
                    ? $"create '{OtherLabel}' at id {OtherId}"
                    : RenamedFrom is not null
                        ? $"rename id {OtherId} from '{RenamedFrom}' to '{OtherLabel}'"
                        : $"keep id {OtherId}, already '{OtherLabel}'";
        }

        /// <summary>
        /// Decides a lookup table's catch-all row once, so the analysis pass and the transform
        /// cannot disagree about which row it is or what id it lands on.
        ///
        /// LK_PostGrad has one already and it is renamed in place; LK_careers and LK_species have
        /// none - legacy recorded that choice as a null id beside free text - so one is created at
        /// MAX(id) + 1, which keeps every legacy id untouched.
        /// </summary>
        public static LookupOtherPlan PlanOtherRow(IReadOnlyList<LookupRow> rows)
        {
            var candidates = rows.Where(r => IsOtherLabel(r.Label)).ToList();

            if (candidates.Count > 1)
            {
                // Nothing here can pick the right one, and picking wrong makes a real option
                // permanently un-editable: the app refuses to rename or delete whatever holds
                // IsOther.
                return new LookupOtherPlan(0, false, null,
                    $"{candidates.Count} rows start with \"{OtherLabel}\" - " +
                    string.Join("; ", candidates.Select(c => $"Id={c.Id} '{c.Label}'")));
            }

            if (candidates.Count == 0)
            {
                // No candidate means no row is labeled "Other" either, since a bare "Other" would
                // have matched the prefix - so the new row cannot collide.
                var maxId = rows.Count == 0 ? 0 : rows.Max(r => r.Id);
                return new LookupOtherPlan(maxId + 1, true, null, null);
            }

            var existing = candidates[0];

            // The destination label column is UNIQUE, so a different row already holding the bare
            // label would collide on the rename.
            var colliding = rows
                .Where(r => r.Id != existing.Id
                    && r.Label.Trim().Equals(OtherLabel, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Id)
                .ToList();

            if (colliding.Count > 0)
            {
                return new LookupOtherPlan(0, false, null,
                    $"renaming Id={existing.Id} to '{OtherLabel}' collides with Id(s) " +
                    string.Join(", ", colliding));
            }

            var alreadyBare = existing.Label.Trim().Equals(OtherLabel, StringComparison.OrdinalIgnoreCase);
            return new LookupOtherPlan(existing.Id, false, alreadyBare ? null : existing.Label, null);
        }

        /// <summary>
        /// Reads one legacy lookup table. Shared so the analysis pass and the transform work from
        /// the same rows, and so the catch-all decision is made against identical input.
        /// </summary>
        public static List<LookupRow> ReadLookupTable(
            SqlConnection legacyConn, string table, string idColumn, string labelColumn)
        {
            var rows = new List<LookupRow>();

            using var cmd = new SqlCommand($"SELECT {idColumn}, {labelColumn} FROM [dbo].[{table}]", legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var label = reader.IsDBNull(1) ? null : reader.GetString(1).Trim();
                rows.Add(new LookupRow(reader.GetInt32(0), label ?? string.Empty));
            }

            return rows;
        }

        /// <summary>
        /// Folds the legacy postGradOther free text into ShortTermStatement, which is where VIPER 2
        /// expects an "Other" post-graduation choice to be described - the new schema has no
        /// PostGradOther column and the form says as much (see CareerSelectionService's
        /// PostGradCompleted, which measures that choice against ShortTermStatement).
        ///
        /// A blank ShortTermStatement takes the text outright rather than gaining a leading blank
        /// line, so nothing marks these rows out as migrated.
        /// </summary>
        public static string CombineShortTermStatement(string? shortTermStatement, string? postGradOther)
        {
            var shortTerm = (shortTermStatement ?? string.Empty).Trim();
            var other = (postGradOther ?? string.Empty).Trim();

            if (other.Length == 0)
            {
                return shortTerm;
            }

            return shortTerm.Length == 0 ? other : $"{shortTerm}\r\n\r\n{other}";
        }

        /// <summary>
        /// Reads the real column widths from the destination, since the physical columns are the
        /// authority rather than the EF model's HasMaxLength calls. Without this a too-long value
        /// surfaces only as SQL Server's "String or binary data would be truncated", which names
        /// neither the column nor the value.
        /// <para>
        /// Read in characters, not bytes: callers compare these against string.Length. The
        /// equivalent sys.columns.max_length is a byte count, which would report double the real
        /// limit for an nvarchar column and let an over-long value through the very check this
        /// exists to make. Only string columns are returned; a MAX column reports -1, which
        /// callers already read as "no limit".
        /// </para>
        /// </summary>
        public static Dictionary<string, int> GetColumnMaxLengths(
            SqlConnection connection, string schema, string table, SqlTransaction? transaction = null)
        {
            var lengths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT COLUMN_NAME, CAST(CHARACTER_MAXIMUM_LENGTH AS int)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                  AND CHARACTER_MAXIMUM_LENGTH IS NOT NULL";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@schema", schema);
            cmd.Parameters.AddWithValue("@table", table);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lengths[reader.GetString(0)] = reader.GetInt32(1);
            }

            return lengths;
        }

        /// <summary>
        /// Reports whether a destination table exists yet. The students career tables are created
        /// by hand-run DDL rather than an EF migration, so the analysis can be run before they
        /// exist and should say so plainly instead of failing on a missing object.
        /// </summary>
        public static bool TableExists(SqlConnection connection, string schema, string table)
        {
            const string sql = "SELECT OBJECT_ID(@qualifiedTable)";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@qualifiedTable", $"[{schema}].[{table}]");

            var result = cmd.ExecuteScalar();
            return result is not null && result is not DBNull;
        }

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
