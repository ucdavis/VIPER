// ============================================
// Script: MigratePhoneListsData.cs
// Description: Migrate data from the legacy PhoneLists database into the phones schema
// ============================================
// Transforms both legacy phone-directory features into their normalized Viper 2 shape:
// - SVM school-wide list: SVM_Phones_Sections/dvtUnit/SVM_Phones -> SVMSection/SVMUnit/SVMUnitPerson
// - VMDO Dean's Office list: VMDOUnits/VMDOPeople -> PhoneList/PhoneListUnit/PhoneListUnitPerson
// - phones.Person is shared by both, and is the only place office/direct-phone data lands
// - SVMFrequentNumber has no legacy table and is seeded from constants below
//
// Run `analysis` first - this script re-checks its structural assertions as pre-flight guards
// and aborts rather than writing if an environment's data violates them.
// ============================================
// USAGE:
//   dotnet run -- migrate-data             (dry run: everything rolls back)
//   dotnet run -- migrate-data Production  (dry run on Production data: everything rolls back)
//   dotnet run -- migrate-data --apply     (writes; requires typing DELETE to confirm)
// ============================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Personnel.Scripts
{
    public class MigratePhoneListsData
    {
        /// <summary>Per-section display metadata that has no legacy column and must be supplied here.</summary>
        private sealed record SectionMetadata(bool IncludeAbbrv, string DirectorTitle, string UnitName);

        // Keyed by the legacy SVM_Phones_Sections.Section value, as confirmed against the real data.
        // The dry run prints what each section resolved to and warns unless exactly one Dean and one
        // Director come out, since a mis-keyed entry here would fall through to the default silently.
        private static readonly Dictionary<string, SectionMetadata> SectionMetadataByName =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Dean's Office"] = new(false, "Dean", "Units"),
                ["Departments"] = new(true, "Chair", "Departments"),
                ["Units"] = new(true, "Director", "Units"),
                ["Executive Committee"] = new(false, "Chair", "Executive Committee"),
            };

        private static readonly SectionMetadata DefaultSectionMetadata = new(false, "Chair", "Units");

        private const string SectionNameSuffix = " Phone Information";

        /// <summary>No legacy table backs these; they are environment-independent.</summary>
        private static readonly (string Label, string Phone, int SortOrder)[] FrequentNumbers =
        [
            ("Health Sciences Library", "2-1162", 1),
            ("Health Science Bookstore", "2-3369", 2),
            ("HYPP", "2-2211", 3),
            ("CAHFS, Tulare", "559-688-7543", 4),
            ("CAHFS, San Bernardino", "909-383-4287", 5),
            ("CAHFS, Turlock", "209-634-5837", 6),
            ("Large Animal Clinic", "2-0290, 2-9815", 7),
            ("Small Animal Clinic", "2-1393", 8),
        ];

        private const string VmdoListCode = "VMDO";
        private const string VmdoListName = "Dean's Office";
        private const string VmdoListMaintainRole = "SVMSecure.PhoneLists.VMDOMaintain";

        /// <summary>Matches the HasMaxLength(25) on Fax, Phone, and DirectPhone.</summary>
        private const int PhoneFieldMaxLength = 25;

        private sealed record SvmPhoneRow(
            int SectionId, int UnitId, int? UnitOrder, string? Fax, string? Location,
            string? DeanMothraId, string? DirPhone, string? InterimDirector,
            string? AdminMothraId, string? AdminPhone, string? InterimAdmin,
            DateTime? DateMod, string? WhoMod);

        private sealed record DvtUnitRow(int SectionId, int UnitId, string UnitName, string? Abbreviation);

        private sealed record VmdoPersonRow(
            int? UnitId, string? MothraId, string? PublicNum, string? DirectNum,
            string? Office, bool ListFirst, DateTime? Updated);

        /// <summary>Accumulates one shared phones.Person row from both legacy sources.</summary>
        private sealed class PersonAccumulator
        {
            /// <summary>Keyed by normalized form so equivalent numbers collapse; value keeps the fuller raw form.</summary>
            public Dictionary<string, string> Phones { get; } = new(StringComparer.OrdinalIgnoreCase);
            public string? DirectPhone { get; set; }
            public string? Office { get; set; }
            public DateTime? ModifiedDate { get; set; }
            public string? ModifiedBy { get; set; }
        }

        private readonly List<string> _overflowReports = [];
        private readonly List<string> _faxConflictReports = [];
        private readonly List<string> _widthViolations = [];
        private readonly Dictionary<(string Table, string Column), int> _columnWidths = [];
        private int _whoModAsMothraId;
        private int _whoModAsLoginId;
        private int _whoModUnresolved;

        private Dictionary<string, PersonLookup> _personLookup = new();
        private Dictionary<string, string> _loginIdLookup = new();

        public static void Run(string[] args)
        {
            bool executeMode = args.Contains("--apply");
            bool isDryRun = !executeMode;
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("============================================");
            Console.WriteLine("Migrating PhoneLists into the phones schema");
            Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            Console.WriteLine("============================================");
            Console.WriteLine();

            if (isDryRun)
            {
                Console.WriteLine("DRY-RUN MODE: the migration is previewed, then rolled back.");
                Console.WriteLine("  No permanent changes are made. To migrate for real, add --apply");
                Console.WriteLine();
            }

            try
            {
                // Config and connectivity are settled before the confirmation prompt: there is no
                // point asking anyone to authorise a destructive run that cannot reach a database.
                var configuration = PhoneListsScriptHelper.LoadConfiguration();
                string viperConnectionString = PhoneListsScriptHelper.GetConnectionString(configuration, "VIPER");
                string legacyConnectionString = PhoneListsScriptHelper.GetConnectionString(configuration, "PhoneLists");

                Console.WriteLine($"Target: {PhoneListsScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine($"Source: {PhoneListsScriptHelper.GetServerAndDatabase(legacyConnectionString)}");
                Console.WriteLine();

                if (!VerifyPrerequisites(viperConnectionString, legacyConnectionString))
                {
                    Console.WriteLine("ERROR: Prerequisites not met. Exiting.");
                    Environment.Exit(1);
                    return;
                }

                if (!isDryRun && !ConfirmDestructiveRun())
                {
                    return;
                }

                new MigratePhoneListsData().Execute(viperConnectionString, legacyConnectionString, isDryRun);
            }
            catch (SqlException ex)
            {
                WriteFatalError(ex);
            }
            catch (InvalidOperationException ex)
            {
                WriteFatalError(ex);
            }

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed:mm\\:ss\\.fff}");
        }

        private static bool ConfirmDestructiveRun()
        {
            Console.WriteLine("APPLY MODE: existing phones data for the SVM and VMDO lists will be");
            Console.WriteLine("  DELETED and rebuilt from the legacy database. This cannot be undone.");
            Console.WriteLine("  Type 'DELETE' to confirm:");
            Console.Write("  > ");
            string? confirmation = Console.ReadLine();
            if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
            {
                Console.WriteLine("Migration cancelled.");
                return false;
            }
            Console.WriteLine();
            return true;
        }

        private static void WriteFatalError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: {ex.Message}");
            if (ex is SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
            }
            Console.WriteLine("\nStack Trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            Environment.Exit(1);
        }

        private void Execute(string viperConnectionString, string legacyConnectionString, bool isDryRun)
        {
            using var viperConn = new SqlConnection(viperConnectionString);
            using var legacyConn = new SqlConnection(legacyConnectionString);
            viperConn.Open();
            legacyConn.Open();

            _personLookup = PhoneListsScriptHelper.BuildMothraIdToPersonLookupMap(viperConn);
            _loginIdLookup = PhoneListsScriptHelper.BuildLoginIdToIamIdMap(viperConn);
            Console.WriteLine($"Loaded {_personLookup.Count:N0} people from users.Person.");
            Console.WriteLine();

            var svmRows = ReadSvmPhoneRows(legacyConn);
            var dvtUnits = ReadDvtUnits(legacyConn);
            var vmdoPeople = ReadVmdoPeople(legacyConn);

            if (!RunPreflightGuards(svmRows, dvtUnits, vmdoPeople))
            {
                Console.WriteLine("ERROR: Pre-flight guards failed. Nothing was written. Exiting.");
                Environment.Exit(1);
                return;
            }

            using var transaction = viperConn.BeginTransaction();
            try
            {
                LoadColumnWidths(viperConn, transaction);
                ClearExistingData(viperConn, transaction);

                MigrateSections(viperConn, transaction, legacyConn);
                var unitIdMap = MigrateUnits(viperConn, transaction, dvtUnits, svmRows);
                MigratePhonePersons(viperConn, transaction, svmRows, vmdoPeople);
                MigrateUnitPersons(viperConn, transaction, svmRows, unitIdMap);
                MigrateFrequentNumbers(viperConn, transaction);
                MigrateVmdoList(viperConn, transaction, legacyConn, vmdoPeople);

                ReportDeferredFindings();
                ValidateMigration(viperConn, transaction, legacyConn);

                if (_widthViolations.Count > 0)
                {
                    // Committing here would persist values this script had to cut down to fit.
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("ROLLED BACK: values too long for their columns (listed above).");
                    Console.WriteLine("Nothing was written. Resolve those before migrating.");
                    Console.WriteLine("============================================");
                }
                else if (isDryRun)
                {
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("DRY RUN SUCCESSFUL - all changes rolled back.");
                    Console.WriteLine("Re-run with --apply to migrate for real.");
                    Console.WriteLine("============================================");
                }
                else
                {
                    transaction.Commit();
                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("MIGRATION COMMITTED.");
                    Console.WriteLine("Next: exercise both phone lists in the app to confirm the data renders.");
                    Console.WriteLine("============================================");
                }
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back.");
                }
                catch (InvalidOperationException rollbackEx)
                {
                    Console.WriteLine($"WARNING: rollback failed: {rollbackEx.Message}");
                }
                throw;
            }
        }

        private static bool VerifyPrerequisites(string viperConnectionString, string legacyConnectionString)
        {
            Console.WriteLine("Verifying prerequisites...");

            using var viperConn = new SqlConnection(viperConnectionString);
            viperConn.Open();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.schemas WHERE name = 'phones'", viperConn))
            {
                if ((int)cmd.ExecuteScalar() == 0)
                {
                    Console.WriteLine("  ERROR: the [phones] schema does not exist in the target database.");
                    return false;
                }
            }

            string[] requiredTables =
            [
                "Person", "SVMSection", "SVMUnit", "SVMUnitPerson",
                "SVMFrequentNumber", "PhoneList", "PhoneListUnit", "PhoneListUnitPerson"
            ];
            foreach (var table in requiredTables)
            {
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('phones') AND name = @name", viperConn);
                cmd.Parameters.AddWithValue("@name", table);
                if ((int)cmd.ExecuteScalar() == 0)
                {
                    Console.WriteLine($"  ERROR: required table [phones].[{table}] does not exist.");
                    return false;
                }
            }

            try
            {
                using var legacyConn = new SqlConnection(legacyConnectionString);
                legacyConn.Open();
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"  ERROR: cannot connect to the legacy PhoneLists database: {ex.Message}");
                return false;
            }

            Console.WriteLine("  All prerequisites met.");
            Console.WriteLine();
            return true;
        }

        /// <summary>
        /// Re-asserts what the analysis pass proved on Development and Test. A later environment
        /// whose data differs aborts here rather than silently migrating something misshapen.
        /// </summary>
        private bool RunPreflightGuards(
            List<SvmPhoneRow> svmRows, List<DvtUnitRow> dvtUnits, List<VmdoPersonRow> vmdoPeople)
        {
            Console.WriteLine("Running pre-flight guards...");
            var failures = new List<string>();

            var unitOrderConflicts = svmRows
                .GroupBy(r => (r.SectionId, r.UnitId))
                .Where(g => g.Select(r => r.UnitOrder).Distinct().Count() > 1)
                .ToList();
            if (unitOrderConflicts.Count > 0)
            {
                failures.Add($"{unitOrderConflicts.Count} unit(s) disagree on UnitOrder - a single sort order per unit is assumed.");
            }

            var rawMothraIds = svmRows.Select(r => r.DeanMothraId)
                .Concat(svmRows.Select(r => r.AdminMothraId))
                .Concat(vmdoPeople.Select(p => p.MothraId))
                .ToList();

            // An all-zero id names nobody, so those rows are dropped rather than failing the guard.
            // Blanks are the ordinary "no person listed" case and are not worth counting.
            var placeholders = rawMothraIds
                .Count(m => !string.IsNullOrWhiteSpace(m) && !PhoneListsScriptHelper.HasMothraId(m));
            if (placeholders > 0)
            {
                Console.WriteLine($"  {placeholders} reference(s) carry a placeholder MothraId and will not be migrated.");
            }

            var allMothraIds = rawMothraIds
                .Where(PhoneListsScriptHelper.HasMothraId)
                .Select(m => m!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var unresolved = allMothraIds.Where(m => !_personLookup.ContainsKey(m)).ToList();
            if (unresolved.Count > 0)
            {
                failures.Add($"{unresolved.Count} MothraId(s) do not resolve against users.Person: {string.Join(", ", unresolved.Take(10))}");
            }

            var orphanUnits = svmRows
                .Select(r => (r.SectionId, r.UnitId))
                .Distinct()
                .Where(k => !dvtUnits.Any(d => d.SectionId == k.SectionId && d.UnitId == k.UnitId))
                .ToList();
            if (orphanUnits.Count > 0)
            {
                failures.Add($"{orphanUnits.Count} unit(s) appear in SVM_Phones but not dvtUnit - dvtUnit is assumed to be the superset.");
            }

            if (failures.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var failure in failures)
                {
                    Console.WriteLine($"  FAILED: {failure}");
                }
                Console.ResetColor();
                return false;
            }

            Console.WriteLine("  All guards passed.");
            Console.WriteLine();
            return true;
        }

        // ---------- Legacy reads ----------

        private static List<SvmPhoneRow> ReadSvmPhoneRows(SqlConnection legacyConn)
        {
            var rows = new List<SvmPhoneRow>();
            const string sql = @"
                SELECT SectionID, unitID, UnitOrder, Fax, Location,
                       Dean_Director_MothraID, Dir_Phone, InterimDirector,
                       Admin_MothraID, Phone, InterimAdmin,
                       Date_Mod, Who_Mod
                FROM [dbo].[SVM_Phones]
                WHERE SectionID IS NOT NULL AND unitID IS NOT NULL";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new SvmPhoneRow(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    ReadNullableString(reader, 3),
                    ReadNullableString(reader, 4),
                    ReadNullableString(reader, 5),
                    ReadNullableString(reader, 6),
                    ReadNullableString(reader, 7),
                    ReadNullableString(reader, 8),
                    ReadNullableString(reader, 9),
                    ReadNullableString(reader, 10),
                    reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    ReadNullableString(reader, 12)));
            }
            return rows;
        }

        private static List<DvtUnitRow> ReadDvtUnits(SqlConnection legacyConn)
        {
            var rows = new List<DvtUnitRow>();
            const string sql = @"
                SELECT dvtUnit_sectionID, dvtUnit_unitID, dvtUnit_unitName, dvtUnit_abbreviation
                FROM [dbo].[dvtUnit]
                WHERE dvtUnit_unitID IS NOT NULL
                ORDER BY dvtUnit_sectionID, dvtUnit_unitID";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new DvtUnitRow(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2).Trim(),
                    ReadNullableString(reader, 3)));
            }
            return rows;
        }

        private static List<VmdoPersonRow> ReadVmdoPeople(SqlConnection legacyConn)
        {
            var rows = new List<VmdoPersonRow>();
            const string sql = @"
                SELECT vmdoPeople_unitID, vmdoPeople_mothraID, vmdoPeople_publicNum,
                       vmdoPeople_directNum, vmdoPeople_office, vmdoPeople_listFirst, vmdoPeople_updated
                FROM [dbo].[VMDOPeople]";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new VmdoPersonRow(
                    reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    ReadNullableString(reader, 1),
                    ReadNullableString(reader, 2),
                    ReadNullableString(reader, 3),
                    ReadNullableString(reader, 4),
                    !reader.IsDBNull(5) && reader.GetBoolean(5),
                    reader.IsDBNull(6) ? null : reader.GetDateTime(6)));
            }
            return rows;
        }

        /// <summary>Maps a null of any type - including a nullable value type - onto DBNull.</summary>
        private static object ToDbValue(object? value) => value ?? DBNull.Value;

        /// <summary>
        /// Legacy wraps interim status in parentheses - "(Vice)". The new schema stores the bare
        /// word and the UI supplies the parentheses when rendering: SVMAddRecordDialog's
        /// interimOptions pair a "(Vice)" label with a "Vice" value, and its edit form re-wraps
        /// the stored value. Importing "(Vice)" verbatim would render as "((Vice))" and would not
        /// match any option in the dropdown.
        ///
        /// Blank becomes an empty string rather than null, matching what the live app writes via
        /// request.DeanInterim.Trim() when no interim status is selected.
        /// </summary>
        private static string NormalizeInterim(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            var trimmed = value.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '(' && trimmed[^1] == ')')
            {
                trimmed = trimmed[1..^1].Trim();
            }

            return trimmed;
        }

        /// <summary>
        /// Reads the real column widths from the destination, since the phones schema was created
        /// outside this repo and its physical columns are the authority, not the EF model's
        /// HasMaxLength calls. Without this a too-long value surfaces only as SQL Server's
        /// "String or binary data would be truncated", which names neither column nor value.
        /// </summary>
        private void LoadColumnWidths(SqlConnection conn, SqlTransaction tx)
        {
            string[] tables =
            [
                "Person", "SVMSection", "SVMUnit", "SVMUnitPerson",
                "SVMFrequentNumber", "PhoneList", "PhoneListUnit", "PhoneListUnitPerson"
            ];

            const string sql = @"
                SELECT c.name, c.max_length, t.name AS TypeName
                FROM sys.columns c
                INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                WHERE c.object_id = OBJECT_ID(@qualifiedTable)";

            foreach (var table in tables)
            {
                using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@qualifiedTable", $"[phones].[{table}]");
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var column = reader.GetString(0);
                    var maxLength = reader.GetInt16(1);
                    var typeName = reader.GetString(2);

                    // nvarchar/nchar report max_length in bytes; -1 means MAX, i.e. no practical limit.
                    int? charLimit = typeName switch
                    {
                        "nvarchar" or "nchar" => maxLength == -1 ? null : maxLength / 2,
                        "varchar" or "char" => maxLength == -1 ? null : maxLength,
                        _ => null,
                    };

                    if (charLimit.HasValue)
                    {
                        _columnWidths[(table, column)] = charLimit.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Binds a string, recording any value too long for its physical column. The value is cut
        /// down only so the run can continue and collect every violation in one pass - a run with
        /// any violation refuses to commit, so a truncated value is never persisted.
        /// </summary>
        private object ToDbString(string? value, string table, string column)
        {
            if (value is null)
            {
                return DBNull.Value;
            }
            if (_columnWidths.TryGetValue((table, column), out var limit) && value.Length > limit)
            {
                _widthViolations.Add(
                    $"[phones].[{table}].[{column}] holds {limit} chars, got {value.Length}: '{value}'");
                return value[..limit];
            }
            return value;
        }

        private static string? ReadNullableString(SqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            var value = reader.GetString(ordinal).Trim();
            return value.Length == 0 ? null : value;
        }

        // ---------- Destination clearing ----------

        /// <summary>
        /// Clears only what this migration owns. The SVM tables are cleared wholesale, but the
        /// PhoneList side is scoped to the VMDO list so any other list in the table survives, and
        /// phones.Person rows are removed only once nothing references them.
        /// </summary>
        private static void ClearExistingData(SqlConnection conn, SqlTransaction tx)
        {
            Console.WriteLine("Clearing existing data...");

            ExecuteAndReport(conn, tx, @"
                DELETE pup FROM [phones].[PhoneListUnitPerson] pup
                INNER JOIN [phones].[PhoneListUnit] plu ON plu.PhoneListUnitId = pup.PhoneListUnitId
                INNER JOIN [phones].[PhoneList] pl ON pl.PhoneListId = plu.PhoneListId
                WHERE pl.Code = @code", "PhoneListUnitPerson", ("@code", VmdoListCode));

            ExecuteAndReport(conn, tx, @"
                DELETE plu FROM [phones].[PhoneListUnit] plu
                INNER JOIN [phones].[PhoneList] pl ON pl.PhoneListId = plu.PhoneListId
                WHERE pl.Code = @code", "PhoneListUnit", ("@code", VmdoListCode));

            ExecuteAndReport(conn, tx, "DELETE FROM [phones].[SVMUnitPerson]", "SVMUnitPerson");
            ExecuteAndReport(conn, tx, "DELETE FROM [phones].[SVMFrequentNumber]", "SVMFrequentNumber");
            ExecuteAndReport(conn, tx, "DELETE FROM [phones].[SVMUnit]", "SVMUnit");
            ExecuteAndReport(conn, tx, "DELETE FROM [phones].[SVMSection]", "SVMSection");

            // Only unreferenced people - another phone list may still be using a shared row.
            ExecuteAndReport(conn, tx, @"
                DELETE p FROM [phones].[Person] p
                WHERE NOT EXISTS (SELECT 1 FROM [phones].[SVMUnitPerson] up WHERE up.PersonIam = p.PersonIam)
                  AND NOT EXISTS (SELECT 1 FROM [phones].[PhoneListUnitPerson] pup WHERE pup.PersonIam = p.PersonIam)",
                "Person (unreferenced)");

            foreach (var table in new[] { "SVMUnitPerson", "SVMFrequentNumber", "PhoneListUnit", "PhoneListUnitPerson" })
            {
                using var cmd = new SqlCommand($"DBCC CHECKIDENT ('[phones].[{table}]', RESEED, 0)", conn, tx);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine();
        }

        private static void ExecuteAndReport(SqlConnection conn, SqlTransaction tx, string sql, string label,
            params (string Name, object Value)[] parameters)
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            int deleted = cmd.ExecuteNonQuery();
            Console.WriteLine($"  Cleared {deleted:N0} rows from {label}");
        }

        // ---------- Step 1: sections ----------

        private void MigrateSections(SqlConnection conn, SqlTransaction tx, SqlConnection legacyConn)
        {
            Console.WriteLine("Step 1: SVMSection");

            var sections = new List<(int SectionId, string Name, int? SortOrder, SectionMetadata Metadata)>();
            const string sql = "SELECT SectionID, Section, Priority FROM [dbo].[SVM_Phones_Sections] ORDER BY Priority";

            using (var cmd = new SqlCommand(sql, legacyConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var sectionId = reader.GetInt32(0);
                    var legacyName = ReadNullableString(reader, 1) ?? "";
                    var priority = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);

                    if (!SectionMetadataByName.TryGetValue(legacyName, out var metadata))
                    {
                        metadata = DefaultSectionMetadata;
                    }
                    sections.Add((sectionId, legacyName + SectionNameSuffix, priority, metadata));
                }
            }

            Console.WriteLine("  Resolved section metadata:");
            foreach (var s in sections)
            {
                Console.WriteLine($"    [{s.SectionId}] {s.Name} | Abbrv={s.Metadata.IncludeAbbrv} " +
                    $"| DirectorTitle={s.Metadata.DirectorTitle} | UnitName={s.Metadata.UnitName}");
            }
            WarnOnImplausibleSectionTitles(sections.Select(s => s.Metadata).ToList());

            var isIdentity = PhoneListsScriptHelper.IsIdentityColumn(conn, "phones", "SVMSection", "SectionId", tx);
            var insertSql = WrapForIdentityInsert(isIdentity, "SVMSection", @"
                INSERT INTO [phones].[SVMSection] (SectionId, Name, IncludeAbbrv, UnitName, DirectorTitle, SortOrder)
                VALUES (@SectionId, @Name, @IncludeAbbrv, @UnitName, @DirectorTitle, @SortOrder);");

            foreach (var s in sections)
            {
                using var cmd = new SqlCommand(insertSql, conn, tx);
                cmd.Parameters.AddWithValue("@SectionId", s.SectionId);
                cmd.Parameters.AddWithValue("@Name", ToDbString(s.Name, "SVMSection", "Name"));
                cmd.Parameters.AddWithValue("@IncludeAbbrv", s.Metadata.IncludeAbbrv);
                cmd.Parameters.AddWithValue("@UnitName", ToDbString(s.Metadata.UnitName, "SVMSection", "UnitName"));
                cmd.Parameters.AddWithValue("@DirectorTitle", ToDbString(s.Metadata.DirectorTitle, "SVMSection", "DirectorTitle"));
                cmd.Parameters.AddWithValue("@SortOrder", ToDbValue(s.SortOrder));
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"  Migrated {sections.Count} sections.");
            Console.WriteLine();
        }

        private static void WarnOnImplausibleSectionTitles(List<SectionMetadata> metadata)
        {
            var deanCount = metadata.Count(m => m.DirectorTitle == "Dean");
            var directorCount = metadata.Count(m => m.DirectorTitle == "Director");
            if (deanCount == 1 && directorCount == 1)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  WARNING: expected exactly one Dean section and one Director section, " +
                $"but found {deanCount} Dean and {directorCount} Director.");
            Console.WriteLine("  A legacy section name probably doesn't match a key in SectionMetadataByName,");
            Console.WriteLine("  so it fell through to the default. Correct that table before running --apply.");
            Console.ResetColor();
        }

        private static string WrapForIdentityInsert(bool isIdentity, string table, string insertSql)
        {
            return isIdentity
                ? $"SET IDENTITY_INSERT [phones].[{table}] ON;{insertSql}SET IDENTITY_INSERT [phones].[{table}] OFF;"
                : insertSql;
        }

        // ---------- Step 2: units ----------

        private Dictionary<(int SectionId, int UnitId), int> MigrateUnits(
            SqlConnection conn, SqlTransaction tx, List<DvtUnitRow> dvtUnits, List<SvmPhoneRow> svmRows)
        {
            Console.WriteLine("Step 2: SVMUnit");

            var rowsByUnit = svmRows
                .GroupBy(r => (r.SectionId, r.UnitId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var isIdentity = PhoneListsScriptHelper.IsIdentityColumn(conn, "phones", "SVMUnit", "UnitId", tx);
            var insertSql = WrapForIdentityInsert(isIdentity, "SVMUnit", @"
                INSERT INTO [phones].[SVMUnit] (UnitId, SectionId, Name, Abbrv, SortOrder, Fax, ModifiedBy, ModifiedDate)
                VALUES (@UnitId, @SectionId, @Name, @Abbrv, @SortOrder, @Fax, @ModifiedBy, @ModifiedDate);");

            // Legacy unitID is unique only within a section, so it cannot serve as this
            // single-column PK - new ids are assigned here and mapped for the UnitPerson step.
            var unitIdMap = new Dictionary<(int SectionId, int UnitId), int>();
            int nextUnitId = 1;
            int unitsWithoutSvmRows = 0;

            foreach (var unit in dvtUnits)
            {
                var key = (unit.SectionId, unit.UnitId);
                var newUnitId = nextUnitId++;
                unitIdMap[key] = newUnitId;

                int? sortOrder = null;
                string? fax = null;
                string? modifiedBy = null;
                DateTime? modifiedDate = null;

                if (rowsByUnit.TryGetValue(key, out var unitRows))
                {
                    sortOrder = unitRows.Select(r => r.UnitOrder).FirstOrDefault(o => o.HasValue);
                    fax = ResolveFax(unitRows, unit.UnitName);

                    var latest = unitRows.Where(r => r.DateMod.HasValue).OrderByDescending(r => r.DateMod).FirstOrDefault();
                    if (latest is not null)
                    {
                        modifiedDate = latest.DateMod;
                        modifiedBy = ResolveModifiedBy(latest.WhoMod);
                    }
                }
                else
                {
                    unitsWithoutSvmRows++;
                }

                using var cmd = new SqlCommand(insertSql, conn, tx);
                cmd.Parameters.AddWithValue("@UnitId", newUnitId);
                cmd.Parameters.AddWithValue("@SectionId", unit.SectionId);
                cmd.Parameters.AddWithValue("@Name", ToDbString(unit.UnitName, "SVMUnit", "Name"));
                cmd.Parameters.AddWithValue("@Abbrv", ToDbString(unit.Abbreviation, "SVMUnit", "Abbrv"));
                cmd.Parameters.AddWithValue("@SortOrder", ToDbValue(sortOrder));
                cmd.Parameters.AddWithValue("@Fax", ToDbString(fax, "SVMUnit", "Fax"));
                cmd.Parameters.AddWithValue("@ModifiedBy", ToDbString(modifiedBy, "SVMUnit", "ModifiedBy"));
                cmd.Parameters.AddWithValue("@ModifiedDate", ToDbValue(modifiedDate));
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"  Migrated {dvtUnits.Count} units ({unitsWithoutSvmRows} with no SVM_Phones rows, so no people).");
            Console.WriteLine();
            return unitIdMap;
        }

        /// <summary>
        /// Fax is denormalized across a unit's rows. A blank never beats a real value; genuinely
        /// different values are kept together rather than one being silently dropped.
        /// </summary>
        private string? ResolveFax(List<SvmPhoneRow> unitRows, string unitName)
        {
            var distinct = unitRows
                .Select(r => r.Fax)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinct.Count > 1)
            {
                _faxConflictReports.Add($"{unitName}: {string.Join(" | ", distinct)}");
            }

            return CombineValues(distinct, $"Fax for unit '{unitName}'");
        }

        /// <summary>
        /// Joins several distinct values into the one column that has to hold them, falling back to
        /// the fullest single value rather than truncating into a malformed number.
        /// </summary>
        private string? CombineValues(List<string> values, string context)
        {
            if (values.Count == 0)
            {
                return null;
            }
            if (values.Count == 1)
            {
                return values[0];
            }

            var combined = string.Join(", ", values);
            if (combined.Length <= PhoneFieldMaxLength)
            {
                return combined;
            }

            var longest = values.OrderByDescending(v => v.Length).First();
            _overflowReports.Add($"{context}: '{combined}' ({combined.Length} chars) exceeds " +
                $"{PhoneFieldMaxLength}; stored '{longest}' instead.");
            return longest;
        }

        /// <summary>
        /// The legacy schema never recorded which identifier Who_Mod holds, so try MothraId, then
        /// LoginId, then give up. The bucket counts are reported so a bad guess is visible.
        /// </summary>
        private string? ResolveModifiedBy(string? whoMod)
        {
            if (string.IsNullOrWhiteSpace(whoMod))
            {
                return null;
            }

            var value = whoMod.Trim();
            if (_personLookup.TryGetValue(value, out var person) && person.IamId is not null)
            {
                _whoModAsMothraId++;
                return person.IamId;
            }
            if (_loginIdLookup.TryGetValue(value, out var iamId))
            {
                _whoModAsLoginId++;
                return iamId;
            }

            _whoModUnresolved++;
            return null;
        }

        // ---------- Step 3: shared people ----------

        private void MigratePhonePersons(
            SqlConnection conn, SqlTransaction tx, List<SvmPhoneRow> svmRows, List<VmdoPersonRow> vmdoPeople)
        {
            Console.WriteLine("Step 3: phones.Person");

            var accumulators = new Dictionary<string, PersonAccumulator>(StringComparer.OrdinalIgnoreCase);
            int skippedInactive = 0;

            foreach (var row in svmRows)
            {
                AccumulateSvmPerson(accumulators, row.DeanMothraId, row.DirPhone, row.DateMod, row.WhoMod, ref skippedInactive);
                AccumulateSvmPerson(accumulators, row.AdminMothraId, row.AdminPhone, row.DateMod, row.WhoMod, ref skippedInactive);
            }

            foreach (var person in vmdoPeople)
            {
                var iamId = ResolveActiveIamId(person.MothraId, ref skippedInactive);
                if (iamId is null)
                {
                    continue;
                }

                var accumulator = GetOrAdd(accumulators, iamId);
                AddPhone(accumulator, person.PublicNum);

                // Office and DirectPhone have no SVM analogue, so VMDO is their only source.
                accumulator.DirectPhone ??= person.DirectNum;
                accumulator.Office ??= person.Office;

                if (person.Updated.HasValue && (!accumulator.ModifiedDate.HasValue || person.Updated > accumulator.ModifiedDate))
                {
                    accumulator.ModifiedDate = person.Updated;
                    // VMDOPeople has no "who modified" column, so the attribution is genuinely unknown.
                    accumulator.ModifiedBy = null;
                }
            }

            const string insertSql = @"
                INSERT INTO [phones].[Person] (PersonIam, Phone, DirectPhone, Office, ModifiedDate, ModifiedBy)
                VALUES (@PersonIam, @Phone, @DirectPhone, @Office, @ModifiedDate, @ModifiedBy);";

            foreach (var (iamId, accumulator) in accumulators)
            {
                // Empty string rather than null when a person has no number on either list, since
                // that is what both of the live app's create paths write via .Trim().
                var phone = CombineValues(accumulator.Phones.Values.ToList(), $"Phone for {iamId}") ?? "";

                using var cmd = new SqlCommand(insertSql, conn, tx);
                cmd.Parameters.AddWithValue("@PersonIam", ToDbString(iamId, "Person", "PersonIam"));
                cmd.Parameters.AddWithValue("@Phone", ToDbString(phone, "Person", "Phone"));
                cmd.Parameters.AddWithValue("@DirectPhone", ToDbString(accumulator.DirectPhone, "Person", "DirectPhone"));
                cmd.Parameters.AddWithValue("@Office", ToDbString(accumulator.Office, "Person", "Office"));
                cmd.Parameters.AddWithValue("@ModifiedDate", ToDbValue(accumulator.ModifiedDate));
                cmd.Parameters.AddWithValue("@ModifiedBy", ToDbString(accumulator.ModifiedBy, "Person", "ModifiedBy"));
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"  Migrated {accumulators.Count} people.");
            if (skippedInactive > 0)
            {
                Console.WriteLine($"  Skipped {skippedInactive} reference(s) to people who are not current employees.");
            }
            Console.WriteLine();
        }

        private void AccumulateSvmPerson(
            Dictionary<string, PersonAccumulator> accumulators, string? mothraId, string? phone,
            DateTime? dateMod, string? whoMod, ref int skippedInactive)
        {
            var iamId = ResolveActiveIamId(mothraId, ref skippedInactive);
            if (iamId is null)
            {
                return;
            }

            var accumulator = GetOrAdd(accumulators, iamId);
            AddPhone(accumulator, phone);

            if (dateMod.HasValue && (!accumulator.ModifiedDate.HasValue || dateMod > accumulator.ModifiedDate))
            {
                accumulator.ModifiedDate = dateMod;
                accumulator.ModifiedBy = ResolveModifiedBy(whoMod);
            }
        }

        private static PersonAccumulator GetOrAdd(Dictionary<string, PersonAccumulator> accumulators, string iamId)
        {
            if (!accumulators.TryGetValue(iamId, out var accumulator))
            {
                accumulator = new PersonAccumulator();
                accumulators[iamId] = accumulator;
            }
            return accumulator;
        }

        /// <summary>
        /// Collapses numbers that differ only by the omitted 75 prefix, keeping whichever spelling
        /// carries more information so the stored value stays the fuller of the two.
        /// </summary>
        private static void AddPhone(PersonAccumulator accumulator, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var value = raw.Trim();
            var key = PhoneListsScriptHelper.NormalizePhone(value);
            if (!accumulator.Phones.TryGetValue(key, out var existing) || value.Length > existing.Length)
            {
                accumulator.Phones[key] = value;
            }
        }

        /// <summary>
        /// Resolves a legacy MothraId to an IamId, excluding rows that name no person at all and
        /// anyone no longer employed.
        /// </summary>
        private string? ResolveActiveIamId(string? mothraId, ref int skippedInactive)
        {
            if (!PhoneListsScriptHelper.HasMothraId(mothraId))
            {
                return null;
            }
            if (!_personLookup.TryGetValue(mothraId.Trim(), out var person))
            {
                return null;
            }
            if (!person.CurrentEmployee || person.IamId is null)
            {
                skippedInactive++;
                return null;
            }
            return person.IamId;
        }

        // ---------- Step 4: SVM unit people ----------

        private void MigrateUnitPersons(
            SqlConnection conn, SqlTransaction tx, List<SvmPhoneRow> svmRows,
            Dictionary<(int SectionId, int UnitId), int> unitIdMap)
        {
            Console.WriteLine("Step 4: SVMUnitPerson");

            const string insertSql = @"
                INSERT INTO [phones].[SVMUnitPerson]
                    (UnitId, PersonIam, Office, PosType, Interim, ModifiedDate, ModifiedBy, IsActive)
                VALUES (@UnitId, @PersonIam, @Office, @PosType, @Interim, @ModifiedDate, @ModifiedBy, 1);";

            int leaders = 0;
            int staff = 0;
            int skippedInactive = 0;

            foreach (var group in svmRows.GroupBy(r => (r.SectionId, r.UnitId)))
            {
                if (!unitIdMap.TryGetValue(group.Key, out var unitId))
                {
                    continue;
                }

                var seenLeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in group)
                {
                    var iamId = ResolveActiveIamId(row.DeanMothraId, ref skippedInactive);
                    if (iamId is null || !seenLeaders.Add(iamId))
                    {
                        continue;
                    }
                    InsertUnitPerson(conn, tx, insertSql, unitId, iamId, row.Location, "Dean",
                        row.InterimDirector, row.DateMod, ResolveModifiedBy(row.WhoMod));
                    leaders++;
                }

                // The admin staff is repeated on every row of a unit, so only one row is emitted.
                // Where legacy names two, the current-employee filter picks out the live one.
                // Resolving the distinct ids rather than every row keeps the skip count per
                // person - the denormalization would otherwise count one inactive staffer once
                // per row of their unit.
                var groupRows = group.ToList();
                var distinctAdminIds = groupRows
                    .Select(r => r.AdminMothraId)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(m => m!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var adminMothraId in distinctAdminIds)
                {
                    var staffIamId = ResolveActiveIamId(adminMothraId, ref skippedInactive);
                    if (staffIamId is null)
                    {
                        continue;
                    }

                    var staffRow = groupRows.First(r =>
                        string.Equals(r.AdminMothraId?.Trim(), adminMothraId, StringComparison.OrdinalIgnoreCase));
                    InsertUnitPerson(conn, tx, insertSql, unitId, staffIamId, staffRow.Location, "Staff",
                        staffRow.InterimAdmin, staffRow.DateMod, ResolveModifiedBy(staffRow.WhoMod));
                    staff++;
                    break;
                }
            }

            Console.WriteLine($"  Migrated {leaders} leader row(s) and {staff} admin-staff row(s).");
            if (skippedInactive > 0)
            {
                Console.WriteLine($"  Skipped {skippedInactive} assignment(s) for people who are not current employees.");
            }
            Console.WriteLine();
        }

        private void InsertUnitPerson(
            SqlConnection conn, SqlTransaction tx, string insertSql, int unitId, string personIam,
            string? office, string posType, string? interim, DateTime? modifiedDate, string? modifiedBy)
        {
            using var cmd = new SqlCommand(insertSql, conn, tx);
            cmd.Parameters.AddWithValue("@UnitId", unitId);
            cmd.Parameters.AddWithValue("@PersonIam", ToDbString(personIam, "SVMUnitPerson", "PersonIam"));
            // Location is a per-row value that the live app writes to both the leader and the staff.
            cmd.Parameters.AddWithValue("@Office", ToDbString(office, "SVMUnitPerson", "Office"));
            cmd.Parameters.AddWithValue("@PosType", ToDbString(posType, "SVMUnitPerson", "PosType"));
            // Normalized here rather than at each call site so neither the Dean nor the Staff path
            // can miss it.
            cmd.Parameters.AddWithValue(
                "@Interim", ToDbString(NormalizeInterim(interim), "SVMUnitPerson", "Interim"));
            cmd.Parameters.AddWithValue("@ModifiedDate", ToDbValue(modifiedDate));
            cmd.Parameters.AddWithValue("@ModifiedBy", ToDbString(modifiedBy, "SVMUnitPerson", "ModifiedBy"));
            cmd.ExecuteNonQuery();
        }

        // ---------- Step 5: frequent numbers ----------

        private void MigrateFrequentNumbers(SqlConnection conn, SqlTransaction tx)
        {
            Console.WriteLine("Step 5: SVMFrequentNumber");

            const string insertSql = @"
                INSERT INTO [phones].[SVMFrequentNumber] (Label, Phone, SortOrder, ModifiedBy, ModifiedDate, IsActive)
                VALUES (@Label, @Phone, @SortOrder, NULL, NULL, 1);";

            foreach (var (label, phone, sortOrder) in FrequentNumbers)
            {
                using var cmd = new SqlCommand(insertSql, conn, tx);
                cmd.Parameters.AddWithValue("@Label", ToDbString(label, "SVMFrequentNumber", "Label"));
                cmd.Parameters.AddWithValue("@Phone", ToDbString(phone, "SVMFrequentNumber", "Phone"));
                cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"  Seeded {FrequentNumbers.Length} frequent numbers.");
            Console.WriteLine();
        }

        // ---------- Step 6: VMDO list ----------

        private void MigrateVmdoList(
            SqlConnection conn, SqlTransaction tx, SqlConnection legacyConn, List<VmdoPersonRow> vmdoPeople)
        {
            Console.WriteLine("Step 6: VMDO phone list");

            var phoneListId = GetOrCreateVmdoList(conn, tx);

            var unitIdMap = new Dictionary<int, int>();
            const string unitSql = "SELECT vmdoUnits_recordID, vmdoUnits_name FROM [dbo].[VMDOUnits]";
            var legacyUnits = new List<(int RecordId, string Name)>();

            using (var cmd = new SqlCommand(unitSql, legacyConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyUnits.Add((reader.GetInt32(0), reader.GetString(1).Trim()));
                }
            }

            // SortOrder stays null so the app's alphabetical default applies - the legacy
            // ordering columns (listprocName, column) are not being carried over.
            const string insertUnitSql = @"
                INSERT INTO [phones].[PhoneListUnit] (PhoneListId, Name, SortOrder)
                VALUES (@PhoneListId, @Name, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            foreach (var (recordId, name) in legacyUnits)
            {
                using var cmd = new SqlCommand(insertUnitSql, conn, tx);
                cmd.Parameters.AddWithValue("@PhoneListId", phoneListId);
                cmd.Parameters.AddWithValue("@Name", ToDbString(name, "PhoneListUnit", "Name"));
                unitIdMap[recordId] = (int)cmd.ExecuteScalar();
            }

            const string insertPersonSql = @"
                INSERT INTO [phones].[PhoneListUnitPerson]
                    (PhoneListUnitId, PersonIam, ListFirst, IsActive, ModifiedBy, ModifiedDate)
                VALUES (@PhoneListUnitId, @PersonIam, @ListFirst, 1, NULL, @ModifiedDate);";

            int migrated = 0;
            int skippedInactive = 0;
            int skippedNoUnit = 0;

            foreach (var person in vmdoPeople)
            {
                var iamId = ResolveActiveIamId(person.MothraId, ref skippedInactive);
                if (iamId is null)
                {
                    continue;
                }
                if (person.UnitId is null || !unitIdMap.TryGetValue(person.UnitId.Value, out var phoneListUnitId))
                {
                    skippedNoUnit++;
                    continue;
                }

                using var cmd = new SqlCommand(insertPersonSql, conn, tx);
                cmd.Parameters.AddWithValue("@PhoneListUnitId", phoneListUnitId);
                cmd.Parameters.AddWithValue("@PersonIam", ToDbString(iamId, "PhoneListUnitPerson", "PersonIam"));
                cmd.Parameters.AddWithValue("@ListFirst", person.ListFirst);
                // ModifiedBy stays null: VMDOPeople records when a row changed but never by whom.
                cmd.Parameters.AddWithValue("@ModifiedDate", ToDbValue(person.Updated));
                cmd.ExecuteNonQuery();
                migrated++;
            }

            Console.WriteLine($"  Migrated {legacyUnits.Count} units and {migrated} people.");
            if (skippedInactive > 0)
            {
                Console.WriteLine($"  Skipped {skippedInactive} person(s) who are not current employees.");
            }
            if (skippedNoUnit > 0)
            {
                Console.WriteLine($"  Skipped {skippedNoUnit} person(s) whose unit could not be resolved.");
            }
            Console.WriteLine();
        }

        private static int GetOrCreateVmdoList(SqlConnection conn, SqlTransaction tx)
        {
            using (var lookup = new SqlCommand(
                "SELECT PhoneListId FROM [phones].[PhoneList] WHERE Code = @code", conn, tx))
            {
                lookup.Parameters.AddWithValue("@code", VmdoListCode);
                var existing = lookup.ExecuteScalar();
                if (existing is int existingId)
                {
                    Console.WriteLine($"  Reusing existing '{VmdoListCode}' phone list (PhoneListId={existingId}).");
                    return existingId;
                }
            }

            using var insert = new SqlCommand(@"
                INSERT INTO [phones].[PhoneList] (Code, Name, MaintainRole)
                VALUES (@code, @name, @role);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            insert.Parameters.AddWithValue("@code", VmdoListCode);
            insert.Parameters.AddWithValue("@name", VmdoListName);
            insert.Parameters.AddWithValue("@role", VmdoListMaintainRole);
            var newId = (int)insert.ExecuteScalar();
            Console.WriteLine($"  Created '{VmdoListCode}' phone list (PhoneListId={newId}).");
            return newId;
        }

        // ---------- Reporting ----------

        private void ReportDeferredFindings()
        {
            Console.WriteLine("Findings requiring review:");

            Console.WriteLine($"  Who_Mod resolved as MothraId: {_whoModAsMothraId:N0}");
            Console.WriteLine($"  Who_Mod resolved as LoginId:  {_whoModAsLoginId:N0}");
            Console.ForegroundColor = _whoModUnresolved == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine($"  Who_Mod unresolved (stored as null): {_whoModUnresolved:N0}");
            Console.ResetColor();

            if (_faxConflictReports.Count > 0)
            {
                Console.WriteLine($"  Units whose rows disagreed on fax ({_faxConflictReports.Count}):");
                foreach (var report in _faxConflictReports)
                {
                    Console.WriteLine($"    {report}");
                }
            }

            if (_widthViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  COLUMN WIDTH VIOLATIONS ({_widthViolations.Count}) - this run will not commit:");
                foreach (var violation in _widthViolations.Distinct())
                {
                    Console.WriteLine($"    {violation}");
                }
                Console.ResetColor();
            }

            if (_overflowReports.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Combined values too long for their column ({_overflowReports.Count}) - fix these by hand afterward:");
                foreach (var report in _overflowReports)
                {
                    Console.WriteLine($"    {report}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("  No values exceeded their column limit.");
            }

            Console.WriteLine();
        }

        private static void ValidateMigration(SqlConnection conn, SqlTransaction tx, SqlConnection legacyConn)
        {
            Console.WriteLine("Destination row counts:");
            string[] tables =
            [
                "SVMSection", "SVMUnit", "SVMUnitPerson", "SVMFrequentNumber",
                "Person", "PhoneList", "PhoneListUnit", "PhoneListUnitPerson"
            ];

            foreach (var table in tables)
            {
                using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [phones].[{table}]", conn, tx);
                Console.WriteLine($"  phones.{table}: {(int)cmd.ExecuteScalar():N0}");
            }

            Console.WriteLine("Legacy source row counts, for comparison:");
            foreach (var table in new[] { "SVM_Phones_Sections", "dvtUnit", "SVM_Phones", "VMDOUnits", "VMDOPeople" })
            {
                using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [dbo].[{table}]", legacyConn);
                Console.WriteLine($"  {table}: {(int)cmd.ExecuteScalar():N0}");
            }
        }
    }
}
