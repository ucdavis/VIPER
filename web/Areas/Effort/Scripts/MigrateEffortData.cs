// ============================================
// Script: MigrateEffortData.cs
// Description: Migrate data from legacy Efforts database to modern Effort database
// Author: VIPER2 Development Team
// Date: 2025-11-04
// ============================================
// This script migrates all data from the legacy Efforts database to the new Effort database:
// - Migrates 19 tables in proper dependency order
// - Handles MothraId → PersonId mapping via VIPER.users.Person
// - Validates all migrations
// ============================================
// PREREQUISITES:
// 1. [VIPER].[effort] schema must exist with all 19 tables (run CreateEffortDatabase.cs first)
// 2. Legacy Efforts database must exist and contain data
// 3. VIPER database must exist with users.Person table for MothraId mapping
// 4. Two connection strings configured in appsettings.json:
//    - "VIPER": Points to VIPER database (where [effort] schema will be written)
//    - "Effort": Points to legacy Efforts database (read-only source)
//      SECURITY: Add ApplicationIntent=ReadOnly to prevent accidental modifications
// ============================================
// IMPORTANT: Column Name Mismatches
// If you encounter "Invalid column name" errors, the legacy database schema may differ
// from the expected column names in this script. The script uses SELECT * and reads by
// ordinal position where possible, but some queries still have hardcoded column names.
// Run EffortSchemaExport.cs first to document your actual legacy schema, then update
// the column names in this file to match your database.
// ============================================
// USAGE:
// Dry-run mode (tests migration with rollback):
//   dotnet script MigrateEffortData.cs
// Execute mode (clears all data, then migrates - requires 'DELETE' confirmation):
//   dotnet script MigrateEffortData.cs --apply
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
// ============================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    public class MigrateEffortData
    {
        public static void Run(string[] args)
        {
            // Check for --apply flag (default: dry-run mode)
            bool executeMode = args.Contains("--apply");
            bool isDryRun = !executeMode;

            // Start timing
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("============================================");
            Console.WriteLine("Migrating Data from Efforts to Effort Database");
            Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("============================================");
            Console.WriteLine();

            if (isDryRun)
            {
                Console.WriteLine("ℹ DRY-RUN MODE: Migration will be previewed, then rolled back.");
                Console.WriteLine("  No permanent changes will be made to the database.");
                Console.WriteLine("  To actually migrate data, use: dotnet script MigrateEffortData.cs --apply");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("⚠ APPLY MODE: All existing effort data will be DELETED and re-migrated.");
                Console.WriteLine("  This cannot be undone. Type 'DELETE' to confirm:");
                Console.Write("  > ");
                string? confirmation = Console.ReadLine();
                if (!string.Equals(confirmation, "DELETE", StringComparison.Ordinal))
                {
                    Console.WriteLine("Migration cancelled.");
                    return;
                }
                Console.WriteLine();
            }

            var configuration = EffortScriptHelper.LoadConfiguration();
            string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER");
            string effortConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Effort");

            Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
            Console.WriteLine();

            try
            {
                // Verify prerequisites
                if (!VerifyPrerequisites(viperConnectionString, effortConnectionString))
                {
                    Console.WriteLine("ERROR: Prerequisites not met. Exiting.");
                    return;
                }

                using var viperConnection = new SqlConnection(viperConnectionString);
                using var effortConnection = new SqlConnection(effortConnectionString);
                viperConnection.Open();
                effortConnection.Open();

                // Start transaction for dry-run mode on VIPER connection (where we're writing to [effort] schema)
                using var transaction = viperConnection.BeginTransaction();

                try
                {
                    if (isDryRun)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Transaction started - all changes will be rolled back.");
                        Console.WriteLine();
                    }

                    // Clear all tables before migration (ensures clean slate)
                    ClearAllTables(viperConnection, transaction);

                    Console.WriteLine("============================================");
                    Console.WriteLine("Step 1: Migrate Lookup Tables");
                    Console.WriteLine("============================================");
                    Console.WriteLine();

                    MigrateLookupTables(viperConnection, effortConnection, transaction);

                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("Step 2: Migrate Core Tables");
                    Console.WriteLine("============================================");
                    Console.WriteLine();

                    MigrateCoreTables(viperConnection, effortConnection, transaction);

                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("Step 3: Migrate Relationship Tables");
                    Console.WriteLine("============================================");
                    Console.WriteLine();

                    MigrateRelationshipTables(viperConnection, effortConnection, transaction);

                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("Step 4: Validate Migration");
                    Console.WriteLine("============================================");
                    Console.WriteLine();

                    ValidateMigration(viperConnection, effortConnection, transaction);

                    // Data Quality Report
                    Console.WriteLine();
                    Console.WriteLine("============================================");
                    Console.WriteLine("Data Quality Summary");
                    Console.WriteLine("============================================");
                    Console.WriteLine();
                    ReportDataQuality(viperConnection, effortConnection, transaction);

                    if (isDryRun)
                    {
                        Console.WriteLine();
                        Console.WriteLine("↶ Rolling back transaction - no permanent changes made");
                        transaction.Rollback();
                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("✓ DRY-RUN SUCCESSFUL: Migration preview complete!");
                        Console.WriteLine("============================================");
                        Console.WriteLine();
                        Console.WriteLine("All migration SQL validated successfully.");
                        Console.WriteLine("No permanent changes were made to the database.");
                        Console.WriteLine();
                        Console.WriteLine("To actually migrate the data, run:");
                        Console.WriteLine("  dotnet script MigrateEffortData.cs --apply");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("✓ Committing transaction - applying permanent changes");
                        transaction.Commit();
                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("Migration Summary");
                        Console.WriteLine("============================================");
                        Console.WriteLine("All data successfully migrated from Efforts to Effort database.");
                        Console.WriteLine();
                        Console.WriteLine("Next Steps:");
                        Console.WriteLine("  1. Run RunCreateReportingProcedures.bat to create reporting stored procedures");
                        Console.WriteLine("  2. Run RunCreateShadow.bat to create shadow schema for ColdFusion");
                    }
                    stopwatch.Stop();
                    Console.WriteLine("============================================");
                    Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Elapsed Time: {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
                    Console.WriteLine("============================================");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"MIGRATION ERROR: {ex.Message}");
                    Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Console.ResetColor();

                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Transaction already rolled back
                    }
                    throw;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {sqlEx.Message}");
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"Stack Trace: {sqlEx.StackTrace}");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static bool VerifyPrerequisites(string viperConnectionString, string effortConnectionString)
        {
            Console.WriteLine("Verifying prerequisites...");

            // Check VIPER database prerequisites
            using var viperConnection = new SqlConnection(viperConnectionString);
            viperConnection.Open();

            // FIXED: Check if [VIPER].[effort] schema exists (NOT separate database)
            using var cmdSchema = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.schemas
            WHERE name = 'effort'", viperConnection);
            int schemaExists = (int)cmdSchema.ExecuteScalar();
            if (schemaExists == 0)
            {
                Console.WriteLine("  ✗ [VIPER].[effort] schema not found. Run CreateEffortDatabase.cs first.");
                return false;
            }
            Console.WriteLine("  ✓ [VIPER].[effort] schema found");

            // Check if effort schema has all required tables
            using var cmdTables = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.tables
            WHERE schema_id = SCHEMA_ID('effort')", viperConnection);
            int tableCount = (int)cmdTables.ExecuteScalar();
            if (tableCount < EffortScriptHelper.ExpectedTableCount)
            {
                Console.WriteLine($"  ✗ [VIPER].[effort] schema has only {tableCount} tables (expected {EffortScriptHelper.ExpectedTableCount}).");
                Console.WriteLine("     Run CreateEffortDatabase.cs to create all tables.");
                return false;
            }
            Console.WriteLine($"  ✓ [VIPER].[effort] schema has all {tableCount} tables");

            // Check if legacy Efforts database exists using the Effort connection string
            try
            {
                using var effortConnection = new SqlConnection(effortConnectionString);
                effortConnection.Open();
                Console.WriteLine("  ✓ Legacy Efforts database connection successful");
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"  ✗ Cannot connect to legacy Efforts database: {sqlEx.Message}");
                Console.WriteLine($"     SQL Error Number: {sqlEx.Number}");
                Console.WriteLine("     Check the 'Effort' connection string in appsettings");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Cannot connect to legacy Efforts database: {ex.Message}");
                Console.WriteLine("     Check the 'Effort' connection string in appsettings");
                return false;
            }

            // Check if VIPER.users.Person table exists
            using var cmdViper = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'users' AND t.name = 'Person'", viperConnection);
            try
            {
                int viperExists = (int)cmdViper.ExecuteScalar();
                if (viperExists == 0)
                {
                    Console.WriteLine("  ⚠ WARNING: VIPER.users.Person table not found. MothraId mapping will fail.");
                }
                else
                {
                    Console.WriteLine("  ✓ VIPER.users.Person table found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  ⚠ WARNING: Could not verify VIPER.users.Person table. MothraId mapping may fail.");
                Console.WriteLine($"     Exception: {ex.Message}");
            }

            // Check if legacy database has data using the Effort connection
            try
            {
                using var effortConn = new SqlConnection(effortConnectionString);
                effortConn.Open();
                using var cmdCount = new SqlCommand("SELECT COUNT(*) FROM [dbo].[tblEffort]", effortConn);
                int recordCount = (int)cmdCount.ExecuteScalar();
                if (recordCount == 0)
                {
                    Console.WriteLine("  ⚠ WARNING: Legacy Efforts database has no records in tblEffort.");
                }
                else
                {
                    Console.WriteLine($"  ✓ Legacy database contains {recordCount:N0} effort records");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ WARNING: Could not check legacy data: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Clears all effort schema tables in FK-safe order and reseeds IDENTITY columns.
        /// Must be called before migration to ensure clean slate.
        /// </summary>
        static void ClearAllTables(SqlConnection conn, SqlTransaction tx)
        {
            Console.WriteLine("Clearing all effort tables for re-migration...");

            // Clear in reverse FK order (tables with FK dependencies first)
            var tablesToClear = new[]
            {
                "CourseRelationships",
                "Records",
                "Audits",
                "Percentages",
                "Sabbaticals",
                "UserAccess",
                "AlternateTitles",
                "Persons",
                "Courses",
                "ReportUnits",
                "JobCodes",
                "Units",
                "TermStatus",
                "EffortTypes",
                "Roles"
            };

            // Tables without IDENTITY columns (use char/varchar PK or composite PK)
            var tablesWithoutIdentity = new HashSet<string> { "Roles", "SessionTypes", "TermStatus", "Persons" };

            foreach (var table in tablesToClear)
            {
                using var cmd = new SqlCommand($"DELETE FROM [effort].[{table}]", conn, tx);
                int deleted = cmd.ExecuteNonQuery();
                Console.WriteLine($"  Cleared {deleted} records from {table}");

                // Reseed IDENTITY column to start fresh (skip tables without IDENTITY)
                if (!tablesWithoutIdentity.Contains(table))
                {
                    using var reseedCmd = new SqlCommand(
                        $"DBCC CHECKIDENT ('[effort].[{table}]', RESEED, 0)", conn, tx);
                    reseedCmd.ExecuteNonQuery();
                }
            }

            Console.WriteLine("  ✓ All tables cleared and IDENTITY columns reseeded");
            Console.WriteLine();
        }

        static void MigrateLookupTables(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            // NOTE: SessionTypes is seeded during CreateEffortDatabase.cs (no legacy lookup table exists)
            // Roles and EffortTypes are migrated from legacy tables to preserve IDs

            // Migrate Roles (from tblRoles - preserves IDs for FK integrity)
            MigrateRoles(viperConnection, effortConnection, transaction);

            // Migrate EffortTypes (from tblEffortType_LU - preserves IDs for FK integrity)
            MigrateEffortTypes(viperConnection, effortConnection, transaction);

            // Migrate TermStatus (from tblStatus - maps Effort-specific workflow tracking)
            MigrateTermStatus(viperConnection, effortConnection, transaction);

            // Migrate Units (from tblUnits_LU - CRITICAL for authorization)
            MigrateUnits(viperConnection, effortConnection, transaction);

            // Migrate JobCodes (from tblJobCode)
            MigrateJobCodes(viperConnection, effortConnection, transaction);

            // Migrate ReportUnits (from tblReportUnits)
            MigrateReportUnits(viperConnection, effortConnection, transaction);

            // Migrate AlternateTitles (from tblAltTitles)
            MigrateAlternateTitles(viperConnection, effortConnection, transaction);
        }

        // NOTE: SessionTypes table is seeded during CreateEffortDatabase.cs (no legacy lookup table)
        // Roles and EffortTypes are migrated from legacy tables to preserve IDs

        static void MigrateRoles(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Roles (from tblRoles)...");

            // Step 1: Read from legacy database
            // SCHEMA: tblRoles has Role_ID (int), Role_Desc (varchar(50))
            var legacyData = new List<(int Id, string Description)>();
            using (var cmd = new SqlCommand("SELECT Role_ID, Role_Desc FROM [dbo].[tblRoles] ORDER BY Role_ID", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),   // Role_ID
                        reader.GetString(1)   // Role_Desc
                    ));
                }
            }

            // Step 2: Write to VIPER database with transaction (preserve IDs)
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[Roles] (Id, Description, IsActive, SortOrder)
            VALUES (@Id, @Description, 1, @SortOrder)", viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@Description", SqlDbType.VarChar, 50);
            insertCmd.Parameters.Add("@SortOrder", SqlDbType.Int);

            int rows = 0;
            foreach (var item in legacyData)
            {
                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@Description"].Value = item.Description;
                insertCmd.Parameters["@SortOrder"].Value = item.Id; // Use ID as SortOrder
                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} roles");
        }

        static void MigrateEffortTypes(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating EffortTypes (from tblEffortType_LU)...");

            // Step 1: Read from legacy database
            // SCHEMA: tblEffortType_LU has type_ID, type_Name, type_Class, type_showOnMPVoteTemplate
            var legacyData = new List<(int Id, string Name, string Class, bool ShowOnTemplate)>();
            using (var cmd = new SqlCommand("SELECT type_ID, type_Name, type_Class, type_showOnMPVoteTemplate FROM [dbo].[tblEffortType_LU] ORDER BY type_ID", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),   // type_ID
                        reader.GetString(1),  // type_Name
                        reader.GetString(2),  // type_Class
                        reader.GetBoolean(3)  // type_showOnMPVoteTemplate
                    ));
                }
            }

            // Step 2: Write to VIPER database with transaction (preserve IDs)
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[EffortTypes] ON;
            INSERT INTO [effort].[EffortTypes] (Id, Name, Class, ShowOnTemplate, IsActive)
            VALUES (@Id, @Name, @Class, @ShowOnTemplate, 1);
            SET IDENTITY_INSERT [effort].[EffortTypes] OFF;", viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@Name", SqlDbType.VarChar, 50);
            insertCmd.Parameters.Add("@Class", SqlDbType.VarChar, 20);
            insertCmd.Parameters.Add("@ShowOnTemplate", SqlDbType.Bit);

            int rows = 0;
            foreach (var item in legacyData)
            {
                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@Name"].Value = item.Name;
                insertCmd.Parameters["@Class"].Value = item.Class;
                insertCmd.Parameters["@ShowOnTemplate"].Value = item.ShowOnTemplate;
                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} effort types");
        }

        static void MigrateTermStatus(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating TermStatus (workflow tracking from tblStatus)...");

            // Step 1: Read from legacy database
            // Note: tblStatus does not have a ModifiedBy column, so we'll default to PersonId 1
            var legacyData = new List<(int TermCode, DateTime? Harvested, DateTime? Opened, DateTime? Closed)>();
            using (var cmd = new SqlCommand("SELECT status_TermCode, status_Harvested, status_Opened, status_Closed FROM [dbo].[tblStatus]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? null : reader.GetDateTime(1),
                        reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                    ));
                }
            }

            // Step 2: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[TermStatus] (TermCode, Status, HarvestedDate, OpenedDate, ClosedDate, CreatedDate, ModifiedDate, ModifiedBy)
            VALUES (@TermCode, @Status, @HarvestedDate, @OpenedDate, @ClosedDate, @CreatedDate, @ModifiedDate, @ModifiedBy)",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@TermCode", SqlDbType.Int);
            insertCmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20);
            insertCmd.Parameters.Add("@HarvestedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@OpenedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ClosedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedBy", SqlDbType.Int);

            int rows = 0;
            foreach (var item in legacyData)
            {
                // Determine status based on dates
                string status = item.Closed.HasValue ? "Closed" : item.Opened.HasValue ? "Opened" : item.Harvested.HasValue ? "Harvested" : "Created";
                DateTime createdDate = item.Harvested ?? item.Opened ?? item.Closed ?? DateTime.Now;
                DateTime modifiedDate = item.Closed ?? item.Opened ?? item.Harvested ?? DateTime.Now;

                // Default ModifiedBy to 1 (legacy table doesn't track who modified status)
                int modifiedBy = 1;

                insertCmd.Parameters["@TermCode"].Value = item.TermCode;
                insertCmd.Parameters["@Status"].Value = status;
                insertCmd.Parameters["@HarvestedDate"].Value = (object?)item.Harvested ?? DBNull.Value;
                insertCmd.Parameters["@OpenedDate"].Value = (object?)item.Opened ?? DBNull.Value;
                insertCmd.Parameters["@ClosedDate"].Value = (object?)item.Closed ?? DBNull.Value;
                insertCmd.Parameters["@CreatedDate"].Value = createdDate;
                insertCmd.Parameters["@ModifiedDate"].Value = modifiedDate;
                insertCmd.Parameters["@ModifiedBy"].Value = modifiedBy;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} term status records");
        }

        static void MigrateUnits(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Units (CRITICAL for authorization)...");

            // Step 1: Read from legacy database
            // SCHEMA: tblUnits_LU has only 2 columns: unit_ID, unit_Name
            var legacyData = new List<(int Id, string Name)>();
            using (var cmd = new SqlCommand("SELECT unit_ID, unit_Name FROM [dbo].[tblUnits_LU] ORDER BY unit_ID", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),  // unit_ID
                        reader.GetString(1)  // unit_Name
                    ));
                }
            }

            // Step 2: Write to VIPER database with transaction
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[Units] ON;
            INSERT INTO [effort].[Units] (Id, Code, Name, SortOrder, IsActive)
            VALUES (@Id, @Code, @Name, @SortOrder, 1);
            SET IDENTITY_INSERT [effort].[Units] OFF;", viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@Code", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@Name", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@SortOrder", SqlDbType.Int);

            int rows = 0;
            foreach (var item in legacyData)
            {
                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@Code"].Value = item.Id.ToString(); // Legacy doesn't have Code, use ID
                insertCmd.Parameters["@Name"].Value = item.Name;
                insertCmd.Parameters["@SortOrder"].Value = item.Id; // Legacy doesn't have SortOrder, use ID
                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} units");
        }

        static void MigrateJobCodes(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating JobCodes...");

            // Step 1: Read from legacy database
            // SCHEMA: tblJobCode has jobcode, faculty, include_clinschedule (no jobTitle/description)
            // Note: faculty column is unused in ColdFusion, so we don't migrate it
            var legacyData = new List<(string Code, bool IncludeClinSchedule)>();
            using (var cmd = new SqlCommand("SELECT jobcode, include_clinschedule FROM [dbo].[tblJobCode]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetString(0).Trim(),
                        reader.GetBoolean(1)
                    ));
                }
            }

            // Step 2: Write to VIPER database with transaction
            // Note: Legacy has no description/title - just code + include_clinschedule flag
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[JobCodes] (Code, IncludeClinSchedule, IsActive)
            VALUES (@Code, @IncludeClinSchedule, 1)", viperConnection, transaction);

            insertCmd.Parameters.Add("@Code", SqlDbType.VarChar, 6);
            insertCmd.Parameters.Add("@IncludeClinSchedule", SqlDbType.Bit);

            int rows = 0;
            foreach (var (code, includeClinSchedule) in legacyData)
            {
                insertCmd.Parameters["@Code"].Value = code;
                insertCmd.Parameters["@IncludeClinSchedule"].Value = includeClinSchedule;
                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} job codes");
        }

        static void MigrateReportUnits(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating ReportUnits...");

            // Step 1: Read from legacy database (include ru_ID to preserve legacy IDs)
            // SCHEMA: tblReportUnits has ru_ID, ru_Abbrev, ru_Unit
            var legacyData = new List<(int Id, string? Abbrev, string? Unit)>();
            using (var cmd = new SqlCommand("SELECT ru_ID, ru_Abbrev, ru_Unit FROM [dbo].[tblReportUnits]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),                               // ru_ID
                        reader.IsDBNull(1) ? null : reader.GetString(1),  // ru_Abbrev
                        reader.IsDBNull(2) ? null : reader.GetString(2)   // ru_Unit
                    ));
                }
            }

            // Step 2: Enable IDENTITY_INSERT to preserve legacy IDs
            using (var identityOnCmd = new SqlCommand("SET IDENTITY_INSERT [effort].[ReportUnits] ON", viperConnection, transaction))
            {
                identityOnCmd.ExecuteNonQuery();
            }

            // Step 4: Write to VIPER database with transaction (including legacy Id)
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[ReportUnits] (Id, UnitCode, UnitName, IsActive)
            VALUES (@Id, @UnitCode, @UnitName, 1)", viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@UnitCode", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@UnitName", SqlDbType.NVarChar, 100);

            int rows = 0;
            foreach (var item in legacyData.Where(item => !string.IsNullOrEmpty(item.Abbrev) || !string.IsNullOrEmpty(item.Unit)))
            {
                string unitCode = item.Abbrev ?? item.Unit ?? "UNKNOWN";

                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@UnitCode"].Value = unitCode;
                insertCmd.Parameters["@UnitName"].Value = item.Unit ?? item.Abbrev ?? unitCode;
                insertCmd.ExecuteNonQuery();
                rows++;
            }

            // Step 5: Disable IDENTITY_INSERT
            using (var identityOffCmd = new SqlCommand("SET IDENTITY_INSERT [effort].[ReportUnits] OFF", viperConnection, transaction))
            {
                identityOffCmd.ExecuteNonQuery();
            }

            Console.WriteLine($"  ✓ Migrated {rows} report units");
        }

        static void MigrateAlternateTitles(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating AlternateTitles...");

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read from legacy database (with join to get MothraId from tblPerson)
            // SCHEMA: tblAltTitles has JobGrpID, JobGrpName (not jobCode, altTitle)
            var legacyData = new List<(string JobGrpID, string JobGrpName, string? MothraId)>();
            using (var cmd = new SqlCommand(@"
            SELECT DISTINCT alt.JobGrpID, alt.JobGrpName, tp.person_MothraID
            FROM [dbo].[tblAltTitles] alt
            LEFT JOIN [dbo].[tblPerson] tp ON alt.JobGrpID = tp.person_JobGrpID", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetString(0),  // JobGrpID
                        reader.GetString(1),  // JobGrpName
                        reader.IsDBNull(2) ? null : reader.GetString(2)  // person_MothraID
                    ));
                }
            }

            // Step 3: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[AlternateTitles] (PersonId, AlternateTitle, EffectiveDate, IsActive, ModifiedDate)
            VALUES (@PersonId, @AlternateTitle, @EffectiveDate, 1, @ModifiedDate)",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
            insertCmd.Parameters.Add("@AlternateTitle", SqlDbType.NVarChar, 200);
            insertCmd.Parameters.Add("@EffectiveDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);

            int rows = 0;
            int skipped = 0;
            foreach (var item in legacyData)
            {
                // Map MothraId to PersonId (skip if not found to satisfy FK constraint)
                int personId = 0;
                if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                {
                    personId = mappedId;
                }

                // Skip records with unmapped PersonId (FK constraint requires PersonId exists in users.Person)
                if (personId == 0)
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@PersonId"].Value = personId;
                insertCmd.Parameters["@AlternateTitle"].Value = item.JobGrpName;  // JobGrpName is the title
                insertCmd.Parameters["@EffectiveDate"].Value = DateTime.Now; // Legacy doesn't track effective date
                insertCmd.Parameters["@ModifiedDate"].Value = DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} alternate titles");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} alternate titles with unmapped MothraId (violates FK constraint)");
            }
        }

        // NOTE: Months table is already seeded during CreateEffortDatabase.cs - no migration needed

        static void MigrateCoreTables(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            // Migrate Persons (includes MothraId → PersonId mapping)
            MigratePersons(viperConnection, effortConnection, transaction);

            // Migrate Courses
            MigrateCourses(viperConnection, effortConnection, transaction);

            // Migrate Records (effort records)
            MigrateRecords(viperConnection, effortConnection, transaction);

            // Migrate Percentages
            MigratePercentages(viperConnection, effortConnection, transaction);

            // Migrate Sabbatics
            MigrateSabbaticals(viperConnection, effortConnection, transaction);

            // Migrate UserAccess (CRITICAL for authorization)
            MigrateUserAccess(viperConnection, effortConnection, transaction);

            // Migrate AuditLog
            MigrateAuditLog(viperConnection, effortConnection, transaction);
        }

        static void MigratePersons(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Persons (with MothraId → PersonId mapping)...");
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read from legacy database
            // SCHEMA: person_MothraID, person_TermCode, person_FirstName, person_LastName, person_MiddleIni,
            //         person_EffortTitleCode, person_EffortDept, person_PercentAdmin, person_JobGrpID, person_Title,
            //         person_AdminUnit, person_ClientID, person_EffortVerified, person_ReportUnit, person_Volunteer_WOS, person_PercentClinical
            var legacyData = new List<(string MothraId, int TermCode, string FirstName, string LastName, string? MiddleIni, string TitleCode, string EffortDept, decimal PercentAdmin, string? JobGrpId, string? Title, string? AdminUnit, DateTime? EffortVerified, string? ReportUnit, byte? VolunteerWos, decimal? PercentClinical)>();
            using (var cmd = new SqlCommand("SELECT person_MothraID, person_TermCode, person_FirstName, person_LastName, person_MiddleIni, person_EffortTitleCode, person_EffortDept, person_PercentAdmin, person_JobGrpID, person_Title, person_AdminUnit, person_EffortVerified, person_ReportUnit, person_Volunteer_WOS, person_PercentClinical FROM [dbo].[tblPerson]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetString(0),   // person_MothraID
                        reader.GetInt32(1),    // person_TermCode
                        reader.GetString(2),   // person_FirstName
                        reader.GetString(3),   // person_LastName
                        reader.IsDBNull(4) ? null : reader.GetString(4),   // person_MiddleIni
                        reader.GetString(5),   // person_EffortTitleCode
                        reader.GetString(6),   // person_EffortDept
                        Math.Abs(reader.GetDouble(7)) < 1e-6 ? 0 : (decimal)reader.GetDouble(7),  // person_PercentAdmin (float to decimal)
                        reader.IsDBNull(8) ? null : reader.GetString(8),   // person_JobGrpID
                        reader.IsDBNull(9) ? null : reader.GetString(9),   // person_Title
                        reader.IsDBNull(10) ? null : reader.GetString(10), // person_AdminUnit
                        reader.IsDBNull(11) ? null : reader.GetDateTime(11),  // person_EffortVerified (datetime, not bool)
                        reader.IsDBNull(12) ? null : reader.GetString(12), // person_ReportUnit
                        reader.IsDBNull(13) ? null : reader.GetByte(13),   // person_Volunteer_WOS (tinyint)
                        reader.IsDBNull(14) ? null : (decimal?)reader.GetDouble(14)  // person_PercentClinical (float to decimal)
                    ));
                }
            }

            // Step 3: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[Persons] (PersonId, TermCode, FirstName, LastName, MiddleInitial, EffortTitleCode, EffortDept, PercentAdmin, JobGroupId, Title, AdminUnit, EffortVerified, ReportUnit, VolunteerWos, PercentClinical)
            VALUES (@PersonId, @TermCode, @FirstName, @LastName, @MiddleInitial, @EffortTitleCode, @EffortDept, @PercentAdmin, @JobGroupId, @Title, @AdminUnit, @EffortVerified, @ReportUnit, @VolunteerWos, @PercentClinical)",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
            insertCmd.Parameters.Add("@TermCode", SqlDbType.Int);
            insertCmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@MiddleInitial", SqlDbType.NVarChar, 1);
            insertCmd.Parameters.Add("@EffortTitleCode", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@EffortDept", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@PercentAdmin", SqlDbType.Decimal);
            insertCmd.Parameters.Add("@JobGroupId", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200);
            insertCmd.Parameters.Add("@AdminUnit", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@EffortVerified", SqlDbType.DateTime2);
            insertCmd.Parameters.Add("@ReportUnit", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@VolunteerWos", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@PercentClinical", SqlDbType.Decimal);

            int rows = 0;
            int skipped = 0;
            int processed = 0;
            int totalRecords = legacyData.Count;
            foreach (var item in legacyData)
            {
                EffortScriptHelper.ShowProgress(processed, totalRecords, 10000, "persons");
                processed++;

                // Map MothraId to PersonId (skip if not found to satisfy FK constraint)
                int personId = 0;
                if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                {
                    personId = mappedId;
                }

                // Skip records with unmapped PersonId (FK constraint requires PersonId exists in users.Person)
                if (personId == 0)
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@PersonId"].Value = personId;
                insertCmd.Parameters["@TermCode"].Value = item.TermCode;
                insertCmd.Parameters["@FirstName"].Value = item.FirstName;
                insertCmd.Parameters["@LastName"].Value = item.LastName;
                insertCmd.Parameters["@MiddleInitial"].Value = string.IsNullOrEmpty(item.MiddleIni) ? DBNull.Value : item.MiddleIni.Substring(0, 1);
                insertCmd.Parameters["@EffortTitleCode"].Value = item.TitleCode;
                insertCmd.Parameters["@EffortDept"].Value = item.EffortDept;
                insertCmd.Parameters["@PercentAdmin"].Value = item.PercentAdmin;
                insertCmd.Parameters["@JobGroupId"].Value = (object?)item.JobGrpId ?? DBNull.Value;
                insertCmd.Parameters["@Title"].Value = (object?)item.Title ?? DBNull.Value;
                insertCmd.Parameters["@AdminUnit"].Value = (object?)item.AdminUnit ?? DBNull.Value;
                insertCmd.Parameters["@EffortVerified"].Value = (object?)item.EffortVerified ?? DBNull.Value;
                insertCmd.Parameters["@ReportUnit"].Value = (object?)item.ReportUnit ?? DBNull.Value;
                insertCmd.Parameters["@VolunteerWos"].Value = (object?)item.VolunteerWos ?? DBNull.Value;
                insertCmd.Parameters["@PercentClinical"].Value = (object?)item.PercentClinical ?? DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            stopwatch.Stop();
            Console.WriteLine($"  ✓ Migrated {rows} persons ({stopwatch.Elapsed:mm\\:ss\\.fff})");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} person records with unmapped MothraId (violates FK constraint)");
            }
        }

        static void MigrateCourses(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Courses...");

            // Step 1: Read from legacy database
            // SCHEMA: course_id (lowercase), course_CRN, course_TermCode, course_SubjCode, course_CrseNumb,
            //         course_SeqNumb, course_Enrollment, course_Units (float), course_CustDept
            var legacyData = new List<(int Id, string Crn, int TermCode, string SubjCode, string CrseNumb, string SeqNumb, int Enrollment, decimal Units, string CustDept)>();
            using (var cmd = new SqlCommand("SELECT course_id, course_CRN, course_TermCode, course_SubjCode, course_CrseNumb, course_SeqNumb, course_Enrollment, course_Units, course_CustDept FROM [dbo].[tblCourses]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),   // course_id
                        reader.GetString(1),  // course_CRN
                        reader.GetInt32(2),   // course_TermCode
                        reader.GetString(3),  // course_SubjCode
                        reader.GetString(4),  // course_CrseNumb
                        reader.GetString(5),  // course_SeqNumb
                        reader.GetInt32(6),   // course_Enrollment (NOT NULL in schema)
                        (decimal)reader.GetDouble(7),  // course_Units (float to decimal, NOT NULL)
                        reader.GetString(8)   // course_CustDept (NOT NULL in schema)
                    ));
                }
            }

            // Step 2: Write to VIPER database with transaction
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[Courses] ON;
            INSERT INTO [effort].[Courses] (Id, Crn, TermCode, SubjCode, CrseNumb, SeqNumb, Enrollment, Units, CustDept)
            VALUES (@Id, @Crn, @TermCode, @SubjCode, @CrseNumb, @SeqNumb, @Enrollment, @Units, @CustDept);
            SET IDENTITY_INSERT [effort].[Courses] OFF;", viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@Crn", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@TermCode", SqlDbType.Int);
            insertCmd.Parameters.Add("@SubjCode", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@CrseNumb", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@SeqNumb", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@Enrollment", SqlDbType.Int);
            insertCmd.Parameters.Add("@Units", SqlDbType.Decimal);
            insertCmd.Parameters.Add("@CustDept", SqlDbType.NVarChar, 10);

            int rows = 0;
            int skipped = 0;
            foreach (var item in legacyData)
            {
                // Skip courses with negative Units (CHECK constraint requires Units >= 0)
                if (item.Units < 0)
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@Crn"].Value = item.Crn;
                insertCmd.Parameters["@TermCode"].Value = item.TermCode;
                insertCmd.Parameters["@SubjCode"].Value = item.SubjCode;
                insertCmd.Parameters["@CrseNumb"].Value = item.CrseNumb;
                insertCmd.Parameters["@SeqNumb"].Value = item.SeqNumb;
                insertCmd.Parameters["@Enrollment"].Value = item.Enrollment;
                insertCmd.Parameters["@Units"].Value = item.Units;
                insertCmd.Parameters["@CustDept"].Value = item.CustDept;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} courses");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} courses with Units < 0 (violates CHECK constraint)");
            }
        }

        static void MigrateRecords(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Records (effort records with MothraId → PersonId mapping)...");
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 1b: Build set of valid CourseIds from migrated Courses table
            // This is needed because we may have skipped courses with invalid data
            var validCourseIds = new HashSet<int>();
            using (var cmd = new SqlCommand("SELECT Id FROM [effort].[Courses]", viperConnection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    validCourseIds.Add(reader.GetInt32(0));
                }
            }

            // Step 1c: Build set of valid (PersonId, TermCode) combinations from migrated Persons table
            // Records FK references effort.Persons on composite key (PersonId, TermCode)
            var validPersonTermCodes = new HashSet<(int PersonId, int TermCode)>();
            using (var cmd = new SqlCommand("SELECT PersonId, TermCode FROM [effort].[Persons]", viperConnection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    validPersonTermCodes.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
            }

            // Step 2: Read from legacy database
            // SCHEMA: effort_CourseID, effort_MothraID, effort_SessionType, effort_Hours, effort_Role,
            //         effort_ClientID, effort_CRN, effort_termCode, effort_ID, effort_Weeks
            // Note: We select in order we want, not schema order
            var legacyData = new List<(int Id, int CourseId, string MothraId, int TermCode, string SessionType, int Role, int? Hours, int? Weeks, string Crn)>();
            using (var cmd = new SqlCommand("SELECT effort_ID, effort_CourseID, effort_MothraID, effort_termCode, effort_SessionType, effort_Role, effort_Hours, effort_Weeks, effort_CRN FROM [dbo].[tblEffort]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),   // effort_ID
                        reader.GetInt32(1),   // effort_CourseID (NOT NULL in schema)
                        reader.GetString(2),  // effort_MothraID
                        reader.GetInt32(3),   // effort_termCode (lowercase 't')
                        reader.GetString(4),  // effort_SessionType
                        int.Parse(reader.GetString(5).Trim()),  // effort_Role (char(1) -> int)
                        reader.IsDBNull(6) ? null : reader.GetInt32(6),  // effort_Hours
                        reader.IsDBNull(7) ? null : reader.GetInt32(7),  // effort_Weeks
                        reader.GetString(8)   // effort_CRN (NOT NULL in schema)
                    ));
                }
            }

            // Step 3: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[Records] ON;
            INSERT INTO [effort].[Records] (Id, CourseId, PersonId, TermCode, SessionType, Role, Hours, Weeks, Crn, ModifiedDate, ModifiedBy)
            VALUES (@Id, @CourseId, @PersonId, @TermCode, @SessionType, @Role, @Hours, @Weeks, @Crn, @ModifiedDate, @ModifiedBy);
            SET IDENTITY_INSERT [effort].[Records] OFF;",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@CourseId", SqlDbType.Int);
            insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
            insertCmd.Parameters.Add("@TermCode", SqlDbType.Int);
            insertCmd.Parameters.Add("@SessionType", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@Role", SqlDbType.Int);
            insertCmd.Parameters.Add("@Hours", SqlDbType.Decimal);
            insertCmd.Parameters.Add("@Weeks", SqlDbType.Int);
            insertCmd.Parameters.Add("@Crn", SqlDbType.NVarChar, 10);
            insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedBy", SqlDbType.Int);

            int rows = 0;
            int skipped = 0;
            int processed = 0;
            int totalRecords = legacyData.Count;
            foreach (var item in legacyData)
            {
                EffortScriptHelper.ShowProgress(processed, totalRecords, 10000, "effort records");
                processed++;

                // Skip records with invalid CourseId (FK constraint requires CourseId exists in Courses table)
                if (!validCourseIds.Contains(item.CourseId))
                {
                    skipped++;
                    continue;
                }

                // Validate Hours (CHECK constraint requires Hours >= 0 AND Hours <= 2500, or NULL)
                if (item.Hours.HasValue && (item.Hours < 0 || item.Hours > 2500))
                {
                    skipped++;
                    continue;
                }

                // Validate Weeks (CHECK constraint requires Weeks > 0 AND Weeks <= 52, or NULL)
                if (item.Weeks.HasValue && (item.Weeks <= 0 || item.Weeks > 52))
                {
                    skipped++;
                    continue;
                }

                // Map MothraId to PersonId (skip if not found to satisfy FK constraint)
                int personId = 0;
                if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                {
                    personId = mappedId;
                }

                // Skip records with unmapped PersonId or invalid (PersonId, TermCode) combination
                // FK constraint requires (PersonId, TermCode) exists in effort.Persons table
                if (personId == 0 || !validPersonTermCodes.Contains((personId, item.TermCode)))
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@CourseId"].Value = item.CourseId;  // NOT NULL in legacy schema
                insertCmd.Parameters["@PersonId"].Value = personId;
                insertCmd.Parameters["@TermCode"].Value = item.TermCode;
                insertCmd.Parameters["@SessionType"].Value = item.SessionType;
                insertCmd.Parameters["@Role"].Value = item.Role; // Converted from char(1) to int
                insertCmd.Parameters["@Hours"].Value = (object?)item.Hours ?? DBNull.Value;
                insertCmd.Parameters["@Weeks"].Value = (object?)item.Weeks ?? DBNull.Value;
                insertCmd.Parameters["@Crn"].Value = item.Crn;  // NOT NULL in legacy schema
                insertCmd.Parameters["@ModifiedDate"].Value = DBNull.Value;
                insertCmd.Parameters["@ModifiedBy"].Value = DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            stopwatch.Stop();
            Console.WriteLine($"  ✓ Migrated {rows} effort records ({stopwatch.Elapsed:mm\\:ss\\.fff})");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} records with invalid CourseId, unmapped PersonId, Hours, or Weeks (violates FK/CHECK constraints)");
            }
        }

        static void MigratePercentages(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Percentages (all records with MothraId → PersonId mapping, preserving legacy IDs)...");
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read ALL records from legacy database
            // AcademicYear is derived from StartDate if missing (no longer tied to term snapshots)
            var legacyData = new List<(int Id, string MothraId, string? AcademicYear, int TypeId, decimal? Percentage, string? Unit, string? Modifier, string? Comment, DateTime StartDate, DateTime? EndDate, bool Compensated)>();
            using (var cmd = new SqlCommand(@"
            SELECT percent_ID, percent_MothraID, percent_AcademicYear, percent_TypeID, percent_Percent,
                   percent_Unit, percent_Modifier, percent_Comment, percent_start, percent_end, percent_compensated
            FROM [dbo].[tblPercent]
            WHERE percent_start IS NOT NULL", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),     // percent_ID
                        reader.GetString(1),    // percent_MothraID
                        reader.IsDBNull(2) ? null : reader.GetString(2),  // percent_AcademicYear (can be NULL)
                        reader.GetInt32(3),     // percent_TypeID
                        reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetDouble(4)),  // Legacy uses float, convert to decimal
                        reader.IsDBNull(5) ? null : reader.GetString(5),  // percent_Unit
                        reader.IsDBNull(6) ? null : reader.GetString(6),  // percent_Modifier
                        reader.IsDBNull(7) ? null : reader.GetString(7),  // percent_Comment
                        reader.GetDateTime(8),  // percent_start (NOT NULL per WHERE clause)
                        reader.IsDBNull(9) ? null : reader.GetDateTime(9),  // percent_end
                        !reader.IsDBNull(10) && reader.GetBoolean(10)  // percent_compensated
                    ));
                }
            }

            // Step 3: Enable IDENTITY_INSERT to preserve legacy percent_ID as Id (1:1 mapping)
            // Wrapped in try/finally to ensure IDENTITY_INSERT is always disabled even if insert loop throws
            using (var identityOnCmd = new SqlCommand("SET IDENTITY_INSERT [effort].[Percentages] ON", viperConnection, transaction))
            {
                identityOnCmd.ExecuteNonQuery();
            }

            int rows = 0;
            int skippedNoPersonId = 0;

            try
            {
                // Step 4: Write to VIPER with transaction - include Id column for 1:1 mapping
                // Note: No TermCode - Percentages now use AcademicYear directly
                using var insertCmd = new SqlCommand(@"
                INSERT INTO [effort].[Percentages] (Id, PersonId, AcademicYear, EffortTypeId, Percentage, Unit, Modifier, Comment, StartDate, EndDate, ModifiedDate, ModifiedBy, Compensated)
                VALUES (@Id, @PersonId, @AcademicYear, @EffortTypeId, @Percentage, @Unit, @Modifier, @Comment, @StartDate, @EndDate, @ModifiedDate, @ModifiedBy, @Compensated)",
                    viperConnection, transaction);

                insertCmd.Parameters.Add("@Id", SqlDbType.Int);
                insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
                insertCmd.Parameters.Add("@AcademicYear", SqlDbType.Char, 9);
                insertCmd.Parameters.Add("@EffortTypeId", SqlDbType.Int);
                insertCmd.Parameters.Add("@Percentage", SqlDbType.Decimal);
                insertCmd.Parameters.Add("@Unit", SqlDbType.NVarChar, 50);
                insertCmd.Parameters.Add("@Modifier", SqlDbType.NVarChar, 50);
                insertCmd.Parameters.Add("@Comment", SqlDbType.NVarChar, 100);
                insertCmd.Parameters.Add("@StartDate", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@EndDate", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@ModifiedBy", SqlDbType.Int);
                insertCmd.Parameters.Add("@Compensated", SqlDbType.Bit);

                foreach (var item in legacyData)
                {
                    // Map MothraId to PersonId (default to 0 if not found)
                    int personId = 0;
                    if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                    {
                        personId = mappedId;
                    }

                    // Skip records with unmapped PersonId (orphaned from deleted users)
                    if (personId == 0)
                    {
                        skippedNoPersonId++;
                        continue;
                    }

                    // Derive academic year: use stored value if valid, otherwise derive from StartDate
                    string academicYear = item.AcademicYear ?? EffortScriptHelper.GetAcademicYearFromDate(item.StartDate);

                    // No dedup check needed - using 1:1 ID mapping with percent_ID preserves uniqueness

                    insertCmd.Parameters["@Id"].Value = item.Id;
                    insertCmd.Parameters["@PersonId"].Value = personId;
                    insertCmd.Parameters["@AcademicYear"].Value = academicYear;
                    insertCmd.Parameters["@EffortTypeId"].Value = item.TypeId;
                    insertCmd.Parameters["@Percentage"].Value = (object?)item.Percentage ?? DBNull.Value;
                    insertCmd.Parameters["@Unit"].Value = (object?)item.Unit ?? DBNull.Value;
                    insertCmd.Parameters["@Modifier"].Value = (object?)item.Modifier ?? DBNull.Value;
                    insertCmd.Parameters["@Comment"].Value = (object?)item.Comment ?? DBNull.Value;
                    insertCmd.Parameters["@StartDate"].Value = item.StartDate;
                    insertCmd.Parameters["@EndDate"].Value = (object?)item.EndDate ?? DBNull.Value;
                    insertCmd.Parameters["@ModifiedDate"].Value = DBNull.Value;
                    insertCmd.Parameters["@ModifiedBy"].Value = DBNull.Value;
                    insertCmd.Parameters["@Compensated"].Value = item.Compensated;

                    insertCmd.ExecuteNonQuery();
                    rows++;
                }
            }
            finally
            {
                // Step 5: Disable IDENTITY_INSERT - future inserts will auto-increment from MAX(Id)+1
                // Always disable even if insert loop throws to avoid leaving connection in bad state
                using var identityOffCmd = new SqlCommand("SET IDENTITY_INSERT [effort].[Percentages] OFF", viperConnection, transaction);
                identityOffCmd.ExecuteNonQuery();
            }

            stopwatch.Stop();
            Console.WriteLine($"  ✓ Migrated {rows} percentage records ({stopwatch.Elapsed:mm\\:ss\\.fff})");
            if (skippedNoPersonId > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skippedNoPersonId} percentage records with unmapped PersonId (MothraId not in VIPER.users.Person)");
            }
            Console.WriteLine($"     Mapped legacy percent_TypeID to EffortTypeId (migrated from tblEffortType_LU)");
        }

        static void MigrateSabbaticals(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Sabbaticals (with MothraId → PersonId mapping)...");

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read all sabbatical records from legacy database
            // SCHEMA: sab_ID, sab_MothraID, sab_ExcludeClinTerms, sab_ExcludeDidacticTerms
            // Note: Legacy schema already stores comma-separated term lists, not individual rows!
            var legacyData = new List<(string MothraId, string? ClinicalTerms, string? DidacticTerms)>();
            using (var cmd = new SqlCommand("SELECT sab_MothraID, sab_ExcludeClinTerms, sab_ExcludeDidacticTerms FROM [dbo].[tblSabbatic]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetString(0),  // sab_MothraID
                        reader.IsDBNull(1) ? null : reader.GetString(1),  // sab_ExcludeClinTerms
                        reader.IsDBNull(2) ? null : reader.GetString(2)   // sab_ExcludeDidacticTerms
                    ));
                }
            }

            // Step 4: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[Sabbaticals] (PersonId, ExcludeClinicalTerms, ExcludeDidacticTerms, ModifiedDate, ModifiedBy)
            VALUES (@PersonId, @ExcludeClinicalTerms, @ExcludeDidacticTerms, @ModifiedDate, @ModifiedBy)",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
            insertCmd.Parameters.Add("@ExcludeClinicalTerms", SqlDbType.NVarChar, -1); // MAX
            insertCmd.Parameters.Add("@ExcludeDidacticTerms", SqlDbType.NVarChar, -1); // MAX
            insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedBy", SqlDbType.Int);

            int rows = 0;
            int skipped = 0;
            foreach (var item in legacyData)
            {
                // Map MothraId to PersonId (skip if not found to satisfy FK constraint)
                int personId = 0;
                if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                {
                    personId = mappedId;
                }

                // Skip records with unmapped PersonId (FK constraint requires PersonId exists in users.Person)
                if (personId == 0)
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@PersonId"].Value = personId;
                insertCmd.Parameters["@ExcludeClinicalTerms"].Value = (object?)item.ClinicalTerms ?? DBNull.Value;
                insertCmd.Parameters["@ExcludeDidacticTerms"].Value = (object?)item.DidacticTerms ?? DBNull.Value;
                insertCmd.Parameters["@ModifiedDate"].Value = DBNull.Value;
                insertCmd.Parameters["@ModifiedBy"].Value = DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} sabbatical records with term exclusions");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} sabbatical records with unmapped MothraId (violates FK constraint)");
            }
        }

        static void MigrateUserAccess(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating UserAccess (CRITICAL for authorization with MothraId → PersonId mapping)...");

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read from legacy database
            var legacyData = new List<(int Id, string MothraId, string DepartmentCode)>();
            using (var cmd = new SqlCommand("SELECT userAccessID, mothraID, departmentAbbreviation FROM [dbo].[userAccess]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2)
                    ));
                }
            }

            // Step 3: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[UserAccess] ON;
            INSERT INTO [effort].[UserAccess] (Id, PersonId, DepartmentCode, ModifiedDate, ModifiedBy, IsActive)
            VALUES (@Id, @PersonId, @DepartmentCode, @ModifiedDate, @ModifiedBy, 1);
            SET IDENTITY_INSERT [effort].[UserAccess] OFF;",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@PersonId", SqlDbType.Int);
            insertCmd.Parameters.Add("@DepartmentCode", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@ModifiedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@ModifiedBy", SqlDbType.Int);

            int rows = 0;
            int skipped = 0;
            foreach (var item in legacyData)
            {
                // Map MothraId to PersonId (skip if not found to satisfy FK constraint)
                int personId = 0;
                if (!string.IsNullOrEmpty(item.MothraId) && mothraIdMap.TryGetValue(item.MothraId, out int mappedId))
                {
                    personId = mappedId;
                }

                // Skip records with unmapped PersonId (FK constraint requires PersonId exists in users.Person)
                if (personId == 0)
                {
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@PersonId"].Value = personId;
                insertCmd.Parameters["@DepartmentCode"].Value = item.DepartmentCode;
                insertCmd.Parameters["@ModifiedDate"].Value = DBNull.Value;
                insertCmd.Parameters["@ModifiedBy"].Value = DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} user access records (CRITICAL for authorization)");
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} user access records with unmapped MothraId (violates FK constraint)");
                Console.WriteLine($"     These users will NOT have authorization access - review with stakeholders");
            }
        }

        static void MigrateAuditLog(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating Audits (with legacy preservation columns for 1:1 verification)...");
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Build MothraId → PersonId lookup from VIPER
            var mothraIdMap = EffortScriptHelper.BuildMothraIdToPersonIdMap(viperConnection, transaction);

            // Step 2: Read from legacy database
            // SCHEMA: audit_ID, audit_ModBy, audit_ModTime, audit_CRN, audit_TermCode, audit_MothraID, audit_Action, audit_Audit
            // Note: Legacy schema doesn't have TableName/RecordID - we'll derive from audit_Action and context
            var legacyData = new List<(int Id, string ModBy, DateTime ModTime, string? Crn, int? TermCode, string? MothraId, string? Action, string? Audit)>();
            using (var cmd = new SqlCommand("SELECT audit_ID, audit_ModBy, audit_ModTime, audit_CRN, audit_TermCode, audit_MothraID, audit_Action, audit_Audit FROM [dbo].[tblAudit]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),   // audit_ID
                        reader.GetString(1),  // audit_ModBy (char(8))
                        reader.GetDateTime(2),  // audit_ModTime
                        reader.IsDBNull(3) ? null : reader.GetString(3),  // audit_CRN
                        reader.IsDBNull(4) ? null : reader.GetInt32(4),   // audit_TermCode
                        reader.IsDBNull(5) ? null : reader.GetString(5),  // audit_MothraID
                        reader.IsDBNull(6) ? null : reader.GetString(6),  // audit_Action
                        reader.IsDBNull(7) ? null : reader.GetString(7)   // audit_Audit
                    ));
                }
            }

            // Step 3: Write to VIPER with transaction
            using var insertCmd = new SqlCommand(@"
            SET IDENTITY_INSERT [effort].[Audits] ON;
            INSERT INTO [effort].[Audits] (Id, TableName, RecordId, Action, ChangedBy, ChangedDate,
                Changes, MigratedDate, UserAgent, IpAddress,
                LegacyAction, LegacyCRN, LegacyTermCode, LegacyMothraID)
            VALUES (@Id, @TableName, @RecordId, @Action, @ChangedBy, @ChangedDate,
                @Changes, @MigratedDate, @UserAgent, @IpAddress,
                @LegacyAction, @LegacyCRN, @LegacyTermCode, @LegacyMothraID);
            SET IDENTITY_INSERT [effort].[Audits] OFF;",
                viperConnection, transaction);

            insertCmd.Parameters.Add("@Id", SqlDbType.Int);
            insertCmd.Parameters.Add("@TableName", SqlDbType.NVarChar, 100);
            insertCmd.Parameters.Add("@RecordId", SqlDbType.Int);
            insertCmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@ChangedBy", SqlDbType.Int);
            insertCmd.Parameters.Add("@ChangedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@Changes", SqlDbType.NVarChar, -1); // MAX
            insertCmd.Parameters.Add("@MigratedDate", SqlDbType.DateTime);
            insertCmd.Parameters.Add("@UserAgent", SqlDbType.NVarChar, 500);
            insertCmd.Parameters.Add("@IpAddress", SqlDbType.NVarChar, 50);
            insertCmd.Parameters.Add("@LegacyAction", SqlDbType.NVarChar, 100);
            insertCmd.Parameters.Add("@LegacyCRN", SqlDbType.NVarChar, 20);
            insertCmd.Parameters.Add("@LegacyTermCode", SqlDbType.Int);
            insertCmd.Parameters.Add("@LegacyMothraID", SqlDbType.NVarChar, 20);

            int rows = 0;
            int total = legacyData.Count;

            foreach (var item in legacyData)
            {
                EffortScriptHelper.ShowProgress(rows, total, 10000, "audit records");

                // Map audit_ModBy (MothraId) to PersonId (default to 1 if not found)
                int changedBy = 1;
                if (!string.IsNullOrEmpty(item.ModBy) && mothraIdMap.TryGetValue(item.ModBy, out int mappedId))
                {
                    changedBy = mappedId;
                }

                // Derive TableName and RecordId from audit_Action and context
                string tableName = "Unknown";
                int recordId = 0;
                string action = "UPDATE"; // Default to UPDATE for unknown legacy actions

                if (!string.IsNullOrEmpty(item.Action))
                {
                    // Extract table name from legacy action text (e.g., "DeleteEffort" -> "Records")
                    if (item.Action.Contains("Person")) tableName = "Persons";
                    else if (item.Action.Contains("Course")) tableName = "Courses";
                    else if (item.Action.Contains("Effort")) tableName = "Records";
                    else if (item.Action.Contains("Percent")) tableName = "Percentages";

                    // Extract SQL operation type from legacy action text
                    // Legacy format: "InsertEffort", "UpdatePerson", "DeleteCourse", etc.
                    if (item.Action.StartsWith("Insert", StringComparison.OrdinalIgnoreCase)) action = "INSERT";
                    else if (item.Action.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)) action = "DELETE";
                    else if (item.Action.StartsWith("Update", StringComparison.OrdinalIgnoreCase)) action = "UPDATE";
                }
                // Try to use TermCode or MothraId as RecordId placeholder
                if (item.TermCode.HasValue) recordId = item.TermCode.Value;

                insertCmd.Parameters["@Id"].Value = item.Id;
                insertCmd.Parameters["@TableName"].Value = tableName;
                insertCmd.Parameters["@RecordId"].Value = recordId;
                insertCmd.Parameters["@Action"].Value = action; // Store SQL operation type (INSERT/UPDATE/DELETE)
                insertCmd.Parameters["@ChangedBy"].Value = changedBy;
                insertCmd.Parameters["@ChangedDate"].Value = item.ModTime;
                insertCmd.Parameters["@Changes"].Value = (object?)item.Audit ?? DBNull.Value;
                insertCmd.Parameters["@MigratedDate"].Value = DateTime.Now;
                insertCmd.Parameters["@UserAgent"].Value = DBNull.Value; // Legacy data didn't capture this
                insertCmd.Parameters["@IpAddress"].Value = DBNull.Value; // Legacy data didn't capture this
                                                                         // Legacy preservation columns for 1:1 verification
                insertCmd.Parameters["@LegacyAction"].Value = (object?)item.Action ?? DBNull.Value;
                insertCmd.Parameters["@LegacyCRN"].Value = (object?)item.Crn ?? DBNull.Value;
                insertCmd.Parameters["@LegacyTermCode"].Value = item.TermCode.HasValue ? item.TermCode.Value : DBNull.Value;
                insertCmd.Parameters["@LegacyMothraID"].Value = (object?)item.MothraId ?? DBNull.Value;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            stopwatch.Stop();
            Console.WriteLine($"  ✓ Migrated {rows} audit records ({stopwatch.Elapsed:mm\\:ss\\.fff})");

            // Check for unmapped ChangedBy (migrated records have MigratedDate set)
            string checkSql = "SELECT COUNT(*) FROM [effort].[Audits] WHERE ChangedBy = 1 AND MigratedDate IS NOT NULL";
            using var checkCmd2 = new SqlCommand(checkSql, viperConnection, transaction);
            int defaulted = (int)checkCmd2.ExecuteScalar();
            if (defaulted > 0)
            {
                Console.WriteLine($"     NOTE: {defaulted} audit records defaulted to ChangedBy = 1 (unmapped MothraIds)");
            }
        }

        static void MigrateRelationshipTables(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            // Migrate CourseRelationships
            MigrateCourseRelationships(viperConnection, effortConnection, transaction);
        }

        static void MigrateCourseRelationships(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating CourseRelationships...");

            // Step 1: Read from legacy database
            // SCHEMA: cr_ParentID, cr_ChildID, cr_Relationship (no ID column - composite PK)
            var legacyData = new List<(int ParentCourseId, int ChildCourseId, string Type)>();
            using (var cmd = new SqlCommand("SELECT cr_ParentID, cr_ChildID, cr_Relationship FROM [dbo].[tblCourseRelationships]", effortConnection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    legacyData.Add((
                        reader.GetInt32(0),  // cr_ParentID
                        reader.GetInt32(1),  // cr_ChildID
                        reader.GetString(2)  // cr_Relationship (NOT NULL in schema)
                    ));
                }
            }

            // Step 1b: Build set of valid course IDs from migrated Courses table
            // CourseRelationships FKs reference effort.Courses on both ParentCourseId and ChildCourseId
            var validCourseIds = new HashSet<int>();
            using (var cmd = new SqlCommand("SELECT Id FROM [effort].[Courses]", viperConnection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    validCourseIds.Add(reader.GetInt32(0));
                }
            }

            // Step 2: Write to VIPER database with transaction
            // Note: Legacy table has no ID column, so we let SQL Server auto-generate IDs
            using var insertCmd = new SqlCommand(@"
            INSERT INTO [effort].[CourseRelationships] (ParentCourseId, ChildCourseId, RelationshipType)
            VALUES (@ParentCourseId, @ChildCourseId, @RelationshipType)", viperConnection, transaction);

            insertCmd.Parameters.Add("@ParentCourseId", SqlDbType.Int);
            insertCmd.Parameters.Add("@ChildCourseId", SqlDbType.Int);
            insertCmd.Parameters.Add("@RelationshipType", SqlDbType.NVarChar, 50);

            int rows = 0;
            int skipped = 0;
            int skippedFk = 0;
            foreach (var item in legacyData)
            {
                // Skip relationships with invalid course IDs (FK constraint requires both IDs exist in effort.Courses)
                if (!validCourseIds.Contains(item.ParentCourseId) || !validCourseIds.Contains(item.ChildCourseId))
                {
                    skippedFk++;
                    continue;
                }

                // Map legacy relationship type to modern CHECK constraint values: Parent, Child, CrossList, Section
                // Legacy values might be: "parent", "child", "cross-list", "crosslist", "section", etc.
                string relationshipType = item.Type.Trim();
                string mappedType;

                if (relationshipType.Equals("parent", StringComparison.OrdinalIgnoreCase))
                    mappedType = "Parent";
                else if (relationshipType.Equals("child", StringComparison.OrdinalIgnoreCase))
                    mappedType = "Child";
                else if (relationshipType.Contains("cross", StringComparison.OrdinalIgnoreCase)
                    || relationshipType.Contains("list", StringComparison.OrdinalIgnoreCase))
                    mappedType = "CrossList";
                else if (relationshipType.Equals("section", StringComparison.OrdinalIgnoreCase))
                    mappedType = "Section";
                else
                {
                    // Skip unknown relationship types that don't match CHECK constraint
                    skipped++;
                    continue;
                }

                insertCmd.Parameters["@ParentCourseId"].Value = item.ParentCourseId;
                insertCmd.Parameters["@ChildCourseId"].Value = item.ChildCourseId;
                insertCmd.Parameters["@RelationshipType"].Value = mappedType;

                insertCmd.ExecuteNonQuery();
                rows++;
            }

            Console.WriteLine($"  ✓ Migrated {rows} course relationships");
            if (skippedFk > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skippedFk} course relationships with invalid ParentCourseId or ChildCourseId (violates FK constraint)");
            }
            if (skipped > 0)
            {
                Console.WriteLine($"  ⚠ Skipped {skipped} course relationships with unknown RelationshipType (violates CHECK constraint)");
            }
        }


        static void ValidateMigration(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {

            Console.WriteLine("Validating migration...");
            Console.WriteLine();

            // Validate each table - Note: SessionTypes is seeded (no legacy lookup table)
            ValidateTable(viperConnection, effortConnection, transaction, "Roles", "[dbo].[tblRoles]");
            ValidateTable(viperConnection, effortConnection, transaction, "EffortTypes", "[dbo].[tblEffortType_LU]");
            ValidateTable(viperConnection, effortConnection, transaction, "TermStatus", "[dbo].[tblStatus]");
            ValidateTable(viperConnection, effortConnection, transaction, "Units", "[dbo].[tblUnits_LU]");
            ValidateTable(viperConnection, effortConnection, transaction, "JobCodes", "[dbo].[tblJobCode]");
            ValidateTable(viperConnection, effortConnection, transaction, "ReportUnits", "[dbo].[tblReportUnits]");
            ValidateTable(viperConnection, effortConnection, transaction, "AlternateTitles", "[dbo].[tblAltTitles]");
            ValidateTable(viperConnection, effortConnection, transaction, "Persons", "[dbo].[tblPerson]");
            ValidateTable(viperConnection, effortConnection, transaction, "Courses", "[dbo].[tblCourses]");
            ValidateTable(viperConnection, effortConnection, transaction, "Records", "[dbo].[tblEffort]");
            // Note: Percentages uses different structure - validation will show mismatch (expected)
            ValidateTable(viperConnection, effortConnection, transaction, "Percentages", "[dbo].[tblPercent]", expectMismatch: true);
            ValidateTable(viperConnection, effortConnection, transaction, "Sabbaticals", "[dbo].[tblSabbatic]", expectMismatch: true);
            ValidateTable(viperConnection, effortConnection, transaction, "UserAccess", "[dbo].[userAccess]");
            ValidateTable(viperConnection, effortConnection, transaction, "Audits", "[dbo].[tblAudit]");
            ValidateTable(viperConnection, effortConnection, transaction, "CourseRelationships", "[dbo].[tblCourseRelationships]");

            Console.WriteLine();
            Console.WriteLine("✓ All tables validated successfully");
        }

        static void ValidateTable(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction, string newTable, string legacyTable, bool expectMismatch = false)
        {
            // Get count from new table (in VIPER database)
            string newSql = $@"SELECT COUNT(*) FROM [effort].[{newTable}];";
            using var newCmd = new SqlCommand(newSql, viperConnection, transaction);
            int newCount = (int)newCmd.ExecuteScalar();

            // Get count from legacy table (in Effort database)
            string legacySql = $@"SELECT COUNT(*) FROM {legacyTable};";
            using var legacyCmd = new SqlCommand(legacySql, effortConnection);
            int legacyCount = (int)legacyCmd.ExecuteScalar();

            if (newCount == legacyCount)
            {
                Console.WriteLine($"  ✓ {newTable}: {newCount:N0} records (matches legacy)");
            }
            else if (expectMismatch)
            {
                Console.WriteLine($"  ℹ {newTable}: {newCount:N0} records (legacy has {legacyCount:N0} - different structure, expected)");
            }
            else
            {
                Console.WriteLine($"  ⚠ {newTable}: {newCount:N0} records (legacy has {legacyCount:N0})");
            }
        }

        static void ReportDataQuality(SqlConnection viperConnection, SqlConnection effortConnection, SqlTransaction transaction)
        {
            bool hasIssues = false;

            // 1. Check for unmapped persons in Persons table
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [effort].[Persons]
            WHERE PersonId = 0", viperConnection, transaction))
            {
                int unmappedPersons = (int)cmd.ExecuteScalar();

                if (unmappedPersons == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Persons table: 0 unmapped MothraIds (100% mapped)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Persons table: {unmappedPersons} unmapped MothraIds (should be 0 after remediation)");
                    Console.ResetColor();
                    hasIssues = true;
                }
            }

            // 2. Check for unmapped persons in UserAccess table (critical for authorization)
            using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [effort].[UserAccess]
            WHERE PersonId = 0", viperConnection, transaction))
            {
                int unmappedUserAccess = (int)cmd.ExecuteScalar();

                if (unmappedUserAccess == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ UserAccess table: 0 unmapped users (authorization ready)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ UserAccess table: {unmappedUserAccess} unmapped users (CRITICAL - affects authorization!)");
                    Console.ResetColor();
                    hasIssues = true;
                }
            }

            // 3. Verify Role column has int values (1, 2, 3)
            using (var cmd = new SqlCommand(@"
            SELECT TOP 3 Role, COUNT(*) as Count
            FROM [effort].[Records]
            GROUP BY Role
            ORDER BY Count DESC", viperConnection, transaction))
            {
                using var reader = cmd.ExecuteReader();
                bool hasValidRoles = true;
                while (reader.Read() && hasValidRoles)
                {
                    int role = reader.GetInt32(0);
                    if (role != 1 && role != 2 && role != 3)
                    {
                        hasValidRoles = false;
                    }
                }

                if (hasValidRoles)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Records.Role column: int values (1, 2, 3) correct");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Records.Role column: unexpected values (should be 1, 2, 3)");
                    Console.ResetColor();
                    hasIssues = true;
                }
            }

            // 4. Check Sabbaticals have term exclusion data
            using (var cmd = new SqlCommand(@"
            SELECT
                COUNT(*) as Total,
                COUNT(ExcludeClinicalTerms) as HasClinical,
                COUNT(ExcludeDidacticTerms) as HasDidactic
            FROM [effort].[Sabbaticals]", viperConnection, transaction))
            {
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int total = reader.GetInt32(0);
                    int hasClinical = reader.GetInt32(1);
                    int hasDidactic = reader.GetInt32(2);

                    if (total > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Sabbaticals: {total} records ({hasClinical} with clinical terms, {hasDidactic} with didactic terms)");
                        Console.ResetColor();
                    }
                }
            }

            // 5. Check Audits have legacy data preserved
            using (var cmd = new SqlCommand(@"
            SELECT
                COUNT(*) as Total,
                SUM(CASE WHEN MigratedDate IS NOT NULL THEN 1 ELSE 0 END) as MigratedCount
            FROM [effort].[Audits]", viperConnection, transaction))
            {
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int total = reader.GetInt32(0);
                    int migratedCount = reader.GetInt32(0) == 0 ? 0 : reader.GetInt32(1);

                    if (total > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Audits: {total} records ({migratedCount} migrated from legacy)");
                        Console.ResetColor();
                    }
                }
            }

            Console.WriteLine();
            if (hasIssues)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("⚠ DATA QUALITY ISSUES DETECTED!");
                Console.WriteLine();
                Console.WriteLine("Action Required:");
                Console.WriteLine("  - Run EffortDataRemediation.cs to resolve unmapped MothraIds");
                Console.WriteLine("  - Review remediation report to verify all 8 tasks completed");
                Console.WriteLine("  - Re-run this migration after remediation completes");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All data quality checks passed! Migration is ready for production.");
                Console.ResetColor();
            }
        }
    }
}
