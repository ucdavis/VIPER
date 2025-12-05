// ============================================
// Script: CreateEffortShadow.cs
// Description: Create EffortShadow compatibility schema for ColdFusion application
// Author: VIPER2 Development Team
// Date: 2025-11-04
// ============================================
// This script creates the [VIPER].[EffortShadow] schema with:
// - 19 compatibility views mapping to [VIPER].[effort] schema
// - 87 stored procedures rewritten to work with modern tables
// - NO DATA - all data lives in [VIPER].[effort] schema
// ============================================
// PREREQUISITES:
// 1. [VIPER].[effort] schema must exist with all 20 tables
// 2. CreateEffortDatabase.cs must have been run
// 3. MigrateEffortData.cs must have been run
// 4. Effort_Database_Schema_And_Data_LEGACY.txt must exist (run RunExportSchema.bat)
// ============================================
// USAGE:
// dotnet script CreateEffortShadow.cs                 (dry-run mode - validates SQL, no schema creation)
// dotnet script CreateEffortShadow.cs --apply         (actually create shadow schema)
// dotnet script CreateEffortShadow.cs --force         (drop and recreate shadow schema with confirmation)
// dotnet script CreateEffortShadow.cs --drop          (drop shadow schema only without recreating)
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
// ============================================

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    public class CreateEffortShadow
    {
        public static int Run(string[] args)
        {
            // ============================================
            // Parse command-line arguments
            // ============================================
            bool executeMode = args.Any(arg => arg.Equals("--apply", StringComparison.OrdinalIgnoreCase));
            bool dropMode = args.Any(arg => arg.Equals("--drop", StringComparison.OrdinalIgnoreCase));
            bool forceMode = args.Any(arg => arg.Equals("--force", StringComparison.OrdinalIgnoreCase));

            var configuration = EffortScriptHelper.LoadConfiguration();
            string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER");

            // Handle --drop or --force mode
            if (dropMode || forceMode)
            {
                // Check if schema exists
                bool schemaExists = CheckSchemaExists(viperConnectionString);

                if (schemaExists)
                {
                    // Check for data accessible through shadow schema views
                    var dataCounts = CheckShadowSchemaData(viperConnectionString);
                    int totalRecords = dataCounts.Values.Sum();

                    if (totalRecords > 0)
                    {
                        // Objects exist - require confirmation
                        if (!ConfirmDeletion(dataCounts, totalRecords))
                        {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Operation cancelled by user.");
                            Console.ResetColor();
                            return 2;  // User cancelled - distinct from success (0) and error (1)
                        }
                    }
                    else
                    {
                        Console.WriteLine("EffortShadow schema exists but has no objects. Proceeding with drop...");
                        Console.WriteLine();
                    }

                    // Drop the schema
                    DropEffortShadowSchemaWithoutConfirmation(viperConnectionString);

                    // If --drop only, exit here
                    if (dropMode)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ EffortShadow schema dropped successfully!");
                        Console.ResetColor();
                        return 0;  // Success
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ EffortShadow schema does not exist. Nothing to drop.");
                    Console.ResetColor();

                    if (dropMode)
                    {
                        return 0;  // Nothing to drop - not an error
                    }
                }

                // If --force mode, continue with creation
                if (forceMode)
                {
                    executeMode = true; // Force apply mode after dropping
                }
            }

            Console.WriteLine("============================================");
            Console.WriteLine("Creating EffortShadow Compatibility Schema");
            Console.WriteLine($"Mode: {(executeMode ? "APPLY (permanent changes)" : "DRY-RUN (validates SQL only, no schema creation)")}");
            Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("============================================");
            Console.WriteLine();

            Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
            Console.WriteLine();

            int totalFailures = 0;  // Track migration failures

            try
            {
                // Step 1: Verify prerequisites
                if (!VerifyPrerequisites(viperConnectionString))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: Prerequisites not met. Exiting.");
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                Console.WriteLine();

                // Step 2: Create shadow schema, views, and stored procedures
                // Wrap everything in a transaction for dry-run support
                using (var connection = new SqlConnection(viperConnectionString))
                {
                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    try
                    {
                        // Check if schema already exists
                        bool schemaExists = CheckSchemaExists(viperConnectionString, connection, transaction);

                        if (!schemaExists)
                        {
                            Console.WriteLine("Creating EffortShadow schema...");
                            using var createSchemaCmd = new SqlCommand("CREATE SCHEMA [EffortShadow];", connection, transaction);
                            createSchemaCmd.ExecuteNonQuery();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("  ✓ EffortShadow schema created");
                            Console.ResetColor();
                        }
                        else if (executeMode)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("  ⚠ EffortShadow schema already exists. Skipping schema creation.");
                            Console.WriteLine("  Views and procedures will be updated.");
                            Console.ResetColor();
                        }

                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("Step 1: Create Compatibility Views");
                        Console.WriteLine("============================================");
                        Console.WriteLine();

                        CreateViewsInTransaction(connection, transaction);

                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("Step 2: Migrate Functions");
                        Console.WriteLine("============================================");
                        Console.WriteLine();

                        int functionFailures = MigrateFunctionsInTransaction(connection, transaction);
                        totalFailures += functionFailures;

                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("Step 3: Migrate Stored Procedures");
                        Console.WriteLine("============================================");
                        Console.WriteLine();

                        int procedureFailures = MigrateStoredProceduresInTransaction(connection, transaction);
                        totalFailures += procedureFailures;

                        // Recompile all procedures to refresh dependencies on views
                        Console.WriteLine();
                        Console.WriteLine("============================================");
                        Console.WriteLine("Step 4: Recompile Stored Procedures");
                        Console.WriteLine("============================================");
                        Console.WriteLine();
                        RecompileAllProcedures(connection, transaction);

                        // Commit or rollback based on mode
                        if (executeMode)
                        {
                            // Only commit if there were no failures
                            if (totalFailures == 0)
                            {
                                transaction.Commit();
                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("✓ All changes committed to database");
                                Console.ResetColor();
                            }
                            else
                            {
                                transaction.Rollback();
                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"✗ Migration had {totalFailures} failures - changes rolled back");
                                Console.WriteLine("  Fix the errors and try again");
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            transaction.Rollback();
                            Console.WriteLine();
                            if (totalFailures == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("✓ DRY-RUN: All SQL validated successfully, changes rolled back");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"✗ DRY-RUN: Validation had {totalFailures} failures");
                                Console.WriteLine("  Fix the errors before running with --apply");
                                Console.ResetColor();
                            }
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                Console.WriteLine();
                Console.WriteLine("============================================");
                Console.WriteLine("EffortShadow Schema Creation Summary");
                Console.WriteLine("============================================");
                Console.WriteLine();
                Console.WriteLine($"Mode: {(executeMode ? "EXECUTE" : "DRY-RUN")}");
                Console.WriteLine($"Result: {(totalFailures == 0 ? "SUCCESS" : $"FAILED ({totalFailures} errors)")}");
                Console.WriteLine("Schema: [VIPER].[EffortShadow]");
                Console.WriteLine("Purpose: Legacy compatibility layer for ColdFusion application");
                Console.WriteLine("Contents: 19 views + ~58 stored procedures (no data storage)");
                Console.WriteLine("Data Source: All data lives in [VIPER].[effort] schema");
                Console.WriteLine();

                if (!executeMode)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ DRY-RUN MODE:");
                    Console.WriteLine("  - Schema creation, views, and stored procedures were tested");
                    Console.WriteLine("  - All SQL validated successfully");
                    Console.WriteLine("  - Changes rolled back - no permanent changes made");
                    Console.WriteLine();
                    Console.WriteLine("  To apply changes permanently, run:");
                    Console.WriteLine("    .\\RunCreateShadow.bat --apply");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("Next Steps:");
                    Console.WriteLine("  1. Run RunVerifyShadow.bat to verify shadow schema procedures");
                    Console.WriteLine("  2. DBA will configure database permissions for applications");
                    Console.WriteLine("  3. Test ColdFusion application against [EffortShadow] schema");
                }

                Console.WriteLine("============================================");
                Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("============================================");

                // Return exit code based on results
                return totalFailures == 0 ? 0 : 1;
            }
            catch (SqlException sqlEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {sqlEx.Message}");
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"Stack Trace: {sqlEx.StackTrace}");
                Console.ResetColor();
                return 1;  // Error exit code
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                return 1;  // Error exit code
            }
        }

        static Dictionary<string, int> CheckShadowSchemaData(string connectionString)
        {
            var objectCounts = new Dictionary<string, int>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Count stored procedures
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.procedures
            WHERE schema_id = SCHEMA_ID('EffortShadow')", connection))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        objectCounts["Stored Procedures"] = count;
                    }
                }

                // Count functions
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.objects
            WHERE schema_id = SCHEMA_ID('EffortShadow') AND type IN ('FN', 'IF', 'TF')", connection))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        objectCounts["Functions"] = count;
                    }
                }

                // Count views
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.views
            WHERE schema_id = SCHEMA_ID('EffortShadow')", connection))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        objectCounts["Views"] = count;
                    }
                }

                // Count tables
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM sys.tables
            WHERE schema_id = SCHEMA_ID('EffortShadow')", connection))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        objectCounts["Tables"] = count;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Warning: Could not check shadow schema objects: {ex.Message}");
                Console.ResetColor();
            }

            return objectCounts;
        }

        static bool ConfirmDeletion(Dictionary<string, int> objectCounts, int totalObjects)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ⚠ DESTRUCTIVE OPERATION ⚠                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("The following EffortShadow schema objects will be PERMANENTLY DELETED:");
            Console.WriteLine();

            // Display object counts
            foreach (var kvp in objectCounts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  • {kvp.Key,-25} {kvp.Value,8:N0} object(s)");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  TOTAL: {totalObjects:N0} database objects will be PERMANENTLY DELETED");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  NOTE: Underlying data in [effort] schema will NOT be deleted.");
            Console.WriteLine("        Only the shadow compatibility layer (views, procedures, functions) will be removed.");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("This action cannot be undone.");
            Console.WriteLine();
            Console.Write("Type 'DELETE' (in capital letters) to confirm: ");

            string confirmation = Console.ReadLine()?.Trim() ?? "";

            if (confirmation == "DELETE")
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Deletion confirmed. Proceeding...");
                Console.ResetColor();
                return true;
            }
            else
            {
                return false;
            }
        }

        static void DropEffortShadowSchemaWithoutConfirmation(string connectionString)
        {
            Console.WriteLine("============================================");
            Console.WriteLine("Dropping EffortShadow Schema");
            Console.WriteLine("============================================");
            Console.WriteLine();

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            Console.WriteLine("Dropping EffortShadow schema objects...");

            try
            {
                // Drop all stored procedures
                Console.WriteLine("  Dropping stored procedures...");
                var dropProcsQuery = @"
            DECLARE @sql NVARCHAR(MAX) = ''
            SELECT @sql = @sql + 'DROP PROCEDURE [EffortShadow].[' + name + ']; '
            FROM sys.procedures
            WHERE schema_id = SCHEMA_ID('EffortShadow')
            EXEC sp_executesql @sql";

                using (var cmd = new SqlCommand(dropProcsQuery, connection))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Stored procedures dropped");
                Console.ResetColor();

                // Drop all functions
                Console.WriteLine("  Dropping functions...");
                var dropFuncsQuery = @"
            DECLARE @sql NVARCHAR(MAX) = ''
            SELECT @sql = @sql + 'DROP FUNCTION [EffortShadow].[' + name + ']; '
            FROM sys.objects
            WHERE schema_id = SCHEMA_ID('EffortShadow') AND type IN ('FN', 'IF', 'TF')
            EXEC sp_executesql @sql";

                using (var cmd = new SqlCommand(dropFuncsQuery, connection))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Functions dropped");
                Console.ResetColor();

                // Drop all views
                Console.WriteLine("  Dropping views...");
                var dropViewsQuery = @"
            DECLARE @sql NVARCHAR(MAX) = ''
            SELECT @sql = @sql + 'DROP VIEW [EffortShadow].[' + name + ']; '
            FROM sys.views
            WHERE schema_id = SCHEMA_ID('EffortShadow')
            EXEC sp_executesql @sql";

                using (var cmd = new SqlCommand(dropViewsQuery, connection))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Views dropped");
                Console.ResetColor();

                // Drop all tables
                Console.WriteLine("  Dropping tables...");
                var dropTablesQuery = @"
            DECLARE @sql NVARCHAR(MAX) = ''
            SELECT @sql = @sql + 'DROP TABLE [EffortShadow].[' + name + ']; '
            FROM sys.tables
            WHERE schema_id = SCHEMA_ID('EffortShadow')
            EXEC sp_executesql @sql";

                using (var cmd = new SqlCommand(dropTablesQuery, connection))
                {
                    cmd.CommandTimeout = 60;
                    cmd.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Tables dropped");
                Console.ResetColor();

                // Drop the schema
                Console.WriteLine("  Dropping schema...");
                var dropSchemaQuery = "DROP SCHEMA [EffortShadow]";
                using (var cmd = new SqlCommand(dropSchemaQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Schema dropped");
                Console.ResetColor();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("============================================");
                Console.WriteLine("EffortShadow schema dropped successfully!");
                Console.WriteLine("============================================");
                Console.ResetColor();
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR dropping schema: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static bool VerifyPrerequisites(string connectionString)
        {
            Console.WriteLine("Verifying prerequisites...");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Check if VIPER.effort schema exists
            using var cmdEffort = new SqlCommand("SELECT COUNT(*) FROM [VIPER].sys.schemas WHERE name = 'effort'", connection);
            int effortExists = (int)cmdEffort.ExecuteScalar();
            if (effortExists == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ [VIPER].[effort] schema not found. Run CreateEffortDatabase.cs first.");
                Console.ResetColor();
                return false;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ [VIPER].[effort] schema found");
            Console.ResetColor();

            // Check if effort schema has data
            string checkDataSql = "SELECT COUNT(*) FROM [effort].[Records];";
            using var cmdData = new SqlCommand(checkDataSql, connection);
            int recordCount = 0;
            try
            {
                recordCount = (int)cmdData.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ WARNING: Could not check [VIPER].[effort] schema for data.");
                Console.WriteLine($"     Exception: {ex.Message}");
                Console.ResetColor();
            }

            if (recordCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ WARNING: [VIPER].[effort] schema has no records. Run MigrateEffortData.cs first.");
                Console.WriteLine("  Continuing with database creation...");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ [VIPER].[effort] schema contains {recordCount} records");
                Console.ResetColor();
            }

            // Check if Effort_Database_Schema_And_Data_LEGACY.txt exists
            string exportFilePath = Path.Join(Environment.CurrentDirectory, "Effort_Database_Schema_And_Data_LEGACY.txt");
            if (!File.Exists(exportFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Export file not found: {exportFilePath}");
                Console.WriteLine("  Run RunExportSchema.bat first to export stored procedures");
                Console.ResetColor();
                return false;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Export file found: Effort_Database_Schema_And_Data_LEGACY.txt");
            Console.ResetColor();

            return true;
        }

        static bool CheckSchemaExists(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.schemas WHERE name = 'EffortShadow'", connection);
            int exists = (int)checkCmd.ExecuteScalar();

            return exists > 0;
        }

        static bool CheckSchemaExists(string connectionString, SqlConnection connection, SqlTransaction transaction)
        {
            using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.schemas WHERE name = 'EffortShadow'", connection, transaction);
            int exists = (int)checkCmd.ExecuteScalar();

            return exists > 0;
        }

        static bool CreateSchema(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            Console.WriteLine("Creating EffortShadow schema...");

            // Check if schema already exists
            if (CheckSchemaExists(connectionString))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠ EffortShadow schema already exists. Skipping creation.");
                Console.WriteLine("  To recreate, manually drop the schema first.");
                Console.ResetColor();
                return false;
            }

            // Create schema permanently
            using var createCmd = new SqlCommand("CREATE SCHEMA [EffortShadow];", connection);
            createCmd.ExecuteNonQuery();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ EffortShadow schema created successfully.");
            Console.ResetColor();
            return true;
        }

        static void CreateViewsInTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            // Create 19 compatibility views
            CreateTblEffortView(connection, transaction);
            CreateTblPersonView(connection, transaction);
            CreateTblCoursesView(connection, transaction);
            CreateTblPercentView(connection, transaction);
            CreateTblStatusView(connection, transaction);
            CreateTblSabbaticView(connection, transaction);
            CreateTblRolesView(connection, transaction);
            CreateTblEffortTypeLUView(connection, transaction);
            CreateUserAccessView(connection, transaction);
            CreateTblUnitsLUView(connection, transaction);
            CreateTblJobCodeView(connection, transaction);
            CreateTblReportUnitsView(connection, transaction);
            CreateTblAltTitlesView(connection, transaction);
            CreateTblCourseRelationshipsView(connection, transaction);
            CreateTblAuditView(connection, transaction);

            // Additional composite views used by stored procedures
            CreateVwInstructorEffortView(connection, transaction);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All 16 views created successfully");
            Console.ResetColor();
        }

        static void CreateViews(string connectionString, bool executeMode)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Start transaction for dry-run support
            using var transaction = connection.BeginTransaction();

            try
            {
                CreateViewsInTransaction(connection, transaction);

                // Commit or rollback based on mode
                if (executeMode)
                {
                    transaction.Commit();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Views committed to database");
                    Console.ResetColor();
                }
                else
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ DRY-RUN: Views rolled back (not saved)");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error creating views: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        static void CreateTblEffortView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblEffort (effort records)...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblEffort]
        AS
        SELECT
            r.Id as effort_ID,
            r.CourseId as effort_CourseID,
            p.MothraId as effort_MothraID,  -- Map PersonId back to MothraId
            p.ClientId as effort_clientid,  -- Client ID from users.Person
            r.TermCode as effort_termCode,  -- lowercase 't' to match legacy casing
            r.SessionType as effort_SessionType,
            CAST(r.Role as char(1)) as effort_Role,  -- Convert int to char for legacy
            r.Hours as effort_Hours,
            r.Weeks as effort_Weeks,
            r.Crn as effort_CRN
        FROM [effort].[Records] r
        INNER JOIN [users].[Person] p ON r.PersonId = p.PersonId
        LEFT JOIN [effort].[Courses] c ON r.CourseId = c.Id;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF triggers for tblEffort
            CreateTblEffortInsertTrigger(connection, transaction);
            CreateTblEffortUpdateTrigger(connection, transaction);
            CreateTblEffortDeleteTrigger(connection, transaction);
        }

        static void CreateTblEffortInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblEffort...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblEffort_Insert]
        ON [EffortShadow].[tblEffort]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Insert into Records table (main data)
            -- Note: PersonId is looked up from MothraId
            INSERT INTO [effort].[Records] (
                CourseId,
                PersonId,
                TermCode,
                SessionType,
                Role,
                Hours,
                Weeks,
                Crn,
                ModifiedDate,
                ModifiedBy
            )
            SELECT
                i.effort_CourseID,
                p.PersonId,  -- Map MothraId to PersonId
                i.effort_termCode,
                i.effort_SessionType,
                i.effort_Role,
                i.effort_Hours,
                i.effort_Weeks,
                i.effort_CRN,
                GETDATE(),
                p.PersonId  -- Use same PersonId as ModifiedBy
            FROM inserted i
            INNER JOIN [users].[Person] p ON i.effort_MothraID = p.MothraId;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateTblEffortUpdateTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF UPDATE trigger for tblEffort...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblEffort_Update]
        ON [EffortShadow].[tblEffort]
        INSTEAD OF UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Update the Records table
            UPDATE r
            SET
                r.CourseId = i.effort_CourseID,
                r.SessionType = i.effort_SessionType,
                r.Role = i.effort_Role,
                r.Hours = i.effort_Hours,
                r.Weeks = i.effort_Weeks
            FROM [effort].[Records] r
            INNER JOIN inserted i ON r.Id = i.effort_ID;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF UPDATE trigger created");
        }

        static void CreateTblEffortDeleteTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF DELETE trigger for tblEffort...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblEffort_Delete]
        ON [EffortShadow].[tblEffort]
        INSTEAD OF DELETE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Delete from Records table
            DELETE r
            FROM [effort].[Records] r
            INNER JOIN deleted d ON r.Id = d.effort_ID;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF DELETE trigger created");
        }

        static void CreateTblPersonView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblPerson...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblPerson]
        AS
        SELECT
            p.MothraId as person_MothraID,
            ps.TermCode as person_TermCode,
            ps.FirstName as person_FirstName,
            ps.LastName as person_LastName,
            ps.MiddleInitial as person_MiddleIni,
            ps.EffortTitleCode as person_EffortTitleCode,
            ps.EffortDept as person_EffortDept,
            ps.PercentAdmin as person_PercentAdmin,
            ps.JobGroupId as person_JobGrpID,
            ps.Title as person_Title,
            ps.AdminUnit as person_AdminUnit,
            p.ClientId as person_ClientID,
            ps.EffortVerified as person_EffortVerified,
            ps.ReportUnit as person_ReportUnit,
            ps.VolunteerWos as person_Volunteer_WOS,
            ps.PercentClinical as person_PercentClinical
        FROM [effort].[Persons] ps
        INNER JOIN [users].[Person] p ON ps.PersonId = p.PersonId;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF triggers for tblPerson
            CreateTblPersonInsertTrigger(connection, transaction);
            CreateTblPersonUpdateTrigger(connection, transaction);
            CreateTblPersonDeleteTrigger(connection, transaction);
        }

        static void CreateTblPersonInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblPerson...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPerson_Insert]
        ON [EffortShadow].[tblPerson]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Insert into Persons table (main data)
            -- Note: PersonId is looked up from MothraId
            INSERT INTO [effort].[Persons] (
                PersonId,
                TermCode,
                FirstName,
                LastName,
                MiddleInitial,
                EffortTitleCode,
                EffortDept,
                PercentAdmin,
                JobGroupId,
                Title,
                AdminUnit,
                EffortVerified,
                ReportUnit,
                VolunteerWos,
                PercentClinical
            )
            SELECT
                p.PersonId,  -- Map MothraId to PersonId
                i.person_TermCode,
                i.person_FirstName,
                i.person_LastName,
                i.person_MiddleIni,
                i.person_EffortTitleCode,
                i.person_EffortDept,
                COALESCE(i.person_PercentAdmin, 0),
                i.person_JobGrpID,
                i.person_Title,
                i.person_AdminUnit,
                i.person_EffortVerified,
                i.person_ReportUnit,
                i.person_Volunteer_WOS,
                i.person_PercentClinical
            FROM inserted i
            INNER JOIN [users].[Person] p ON i.person_MothraID = p.MothraId;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateTblPersonUpdateTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF UPDATE trigger for tblPerson...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPerson_Update]
        ON [EffortShadow].[tblPerson]
        INSTEAD OF UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Update only the effort.Persons table (not users.Person)
            -- Match on both PersonId AND TermCode for the snapshot
            UPDATE ps
            SET
                FirstName = i.person_FirstName,
                LastName = i.person_LastName,
                MiddleInitial = i.person_MiddleIni,
                EffortTitleCode = i.person_EffortTitleCode,
                EffortDept = i.person_EffortDept,
                PercentAdmin = i.person_PercentAdmin,
                JobGroupId = i.person_JobGrpID,
                Title = i.person_Title,
                AdminUnit = i.person_AdminUnit,
                EffortVerified = i.person_EffortVerified,
                ReportUnit = i.person_ReportUnit,
                VolunteerWos = i.person_Volunteer_WOS,
                PercentClinical = i.person_PercentClinical
            FROM [effort].[Persons] ps
            INNER JOIN inserted i ON ps.PersonId = (
                SELECT PersonId FROM [users].[Person] WHERE MothraId = i.person_MothraID
            ) AND ps.TermCode = i.person_TermCode;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF UPDATE trigger created");
        }

        static void CreateTblPersonDeleteTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF DELETE trigger for tblPerson...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPerson_Delete]
        ON [EffortShadow].[tblPerson]
        INSTEAD OF DELETE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Delete from effort.Persons (the term-specific snapshot)
            -- Do NOT delete from users.Person (the master user record)
            DELETE ps
            FROM [effort].[Persons] ps
            INNER JOIN deleted d ON ps.PersonId = (
                SELECT PersonId FROM [users].[Person] WHERE MothraId = d.person_MothraID
            ) AND ps.TermCode = d.person_TermCode;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF DELETE trigger created");
        }

        static void CreateTblCoursesView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblCourses...");

            string sql = @"
        -- NOTE: course_Title not included - ColdFusion should fetch from VIPER course catalog
        -- To get course title: JOIN [VIPER].[courses].[Catalog] ON SubjCode + CrseNumb
        CREATE OR ALTER VIEW [EffortShadow].[tblCourses]
        AS
        SELECT
            Id as course_id,
            Crn as course_CRN,
            TermCode as course_TermCode,
            SubjCode as course_subjCode,  -- lowercase to match legacy procedures
            CrseNumb as course_crseNumb,  -- lowercase to match legacy procedures
            SeqNumb as course_seqNumb,    -- lowercase to match legacy procedures
            Enrollment as course_enrollment,
            CAST(Units AS float) as course_units,  -- Cast to float for legacy compatibility (matches float(53) display format)
            CustDept as course_custDept
        FROM [effort].[Courses];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF triggers for tblCourses
            // Required because the view has a derived column (CAST(Units AS float))
            CreateTblCoursesInsertTrigger(connection, transaction);
            CreateTblCoursesUpdateTrigger(connection, transaction);
        }

        static void CreateTblCoursesInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblCourses...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblCourses_Insert]
        ON [EffortShadow].[tblCourses]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            INSERT INTO [effort].[Courses] (Crn, TermCode, SubjCode, CrseNumb, SeqNumb, Enrollment, Units, CustDept)
            SELECT
                i.course_CRN,
                i.course_TermCode,
                i.course_subjCode,
                i.course_crseNumb,
                i.course_seqNumb,
                i.course_enrollment,
                i.course_units,
                i.course_custDept
            FROM inserted i;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateTblCoursesUpdateTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF UPDATE trigger for tblCourses...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblCourses_Update]
        ON [EffortShadow].[tblCourses]
        INSTEAD OF UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            UPDATE c
            SET
                c.Crn = i.course_CRN,
                c.TermCode = i.course_TermCode,
                c.SubjCode = i.course_subjCode,
                c.CrseNumb = i.course_crseNumb,
                c.SeqNumb = i.course_seqNumb,
                c.Enrollment = i.course_enrollment,
                c.Units = i.course_units,
                c.CustDept = i.course_custDept
            FROM [effort].[Courses] c
            INNER JOIN inserted i ON c.Id = i.course_id;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF UPDATE trigger created");
        }

        static void CreateTblPercentView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblPercent...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblPercent]
        AS
        SELECT
            pct.Id as percent_ID,
            p.MothraId as percent_MothraID,
            -- Calculate academic year from TermCode via Terms table lookup
            -- Legacy system used tblStatus to map TermCode -> AcademicYear
            -- Academic year format is 'YYYY-YYYY' (e.g., '2019-2020')
            -- NOTE: vwTerms.AcademicYear contains the ENDING year of the academic year
            --       So AcademicYear=2020 means '2019-2020', not '2020-2021'
            -- For records without TermCode, fall back to StartDate calculation
            CASE
                WHEN t.AcademicYear IS NOT NULL THEN
                    CAST(t.AcademicYear - 1 AS varchar(4)) + '-' + CAST(t.AcademicYear AS varchar(4))
                WHEN pct.StartDate IS NULL THEN NULL
                WHEN MONTH(pct.StartDate) >= 7 THEN
                    CAST(YEAR(pct.StartDate) AS varchar(4)) + '-' + CAST(YEAR(pct.StartDate) + 1 AS varchar(4))
                ELSE
                    CAST(YEAR(pct.StartDate) - 1 AS varchar(4)) + '-' + CAST(YEAR(pct.StartDate) AS varchar(4))
            END as percent_AcademicYear,
            CAST(pct.Percentage AS FLOAT) as percent_Percent,
            pct.EffortTypeId as percent_TypeID,
            pct.Unit as percent_Unit,
            pct.Modifier as percent_Modifier,
            pct.Comment as percent_Comment,
            pct.ModifiedDate as percent_modifiedOn,
            COALESCE(mp.MailId, 'unknown') as percent_modifiedBy,
            pct.StartDate as percent_start,
            pct.EndDate as percent_end,
            pct.Compensated as percent_compensated,
            pct.PersonId as percent_PersonId_Internal,  -- Internal use only
            pct.TermCode as percent_TermCode_Internal   -- Internal use only
        FROM [effort].[Percentages] pct
        INNER JOIN [users].[Person] p ON pct.PersonId = p.PersonId
        LEFT JOIN [dbo].[vwTerms] t ON pct.TermCode = t.TermCode
        LEFT JOIN [users].[Person] mp ON pct.ModifiedBy = mp.PersonId;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF triggers for tblPercent
            CreateTblPercentInsertTrigger(connection, transaction);
            CreateTblPercentUpdateTrigger(connection, transaction);
            CreateTblPercentDeleteTrigger(connection, transaction);
        }

        static void CreateTblPercentInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblPercent...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPercent_Insert]
        ON [EffortShadow].[tblPercent]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Parse academic year (e.g., '2023-2024') to get the ending year
            -- The academic year string is in format 'YYYY-YYYY' where second year is the AcademicYear value
            INSERT INTO [effort].[Percentages] (
                PersonId,
                TermCode,
                Percentage,
                EffortTypeId,
                Unit,
                Modifier,
                Comment,
                StartDate,
                EndDate,
                Compensated,
                ModifiedDate,
                ModifiedBy
            )
            SELECT
                p.PersonId,
                -- Get TermCode: Use the internal column if provided, otherwise derive from academic year
                COALESCE(
                    i.percent_TermCode_Internal,
                    (SELECT TOP 1 TermCode FROM [dbo].[vwTerms]
                     WHERE AcademicYear = CAST(RIGHT(i.percent_AcademicYear, 4) AS int)
                     ORDER BY TermCode)
                ),
                i.percent_Percent,
                i.percent_TypeID,
                i.percent_Unit,
                i.percent_Modifier,
                i.percent_Comment,
                COALESCE(i.percent_start,
                         DATEFROMPARTS(CAST(LEFT(i.percent_AcademicYear, 4) AS int), 7, 1)),  -- Default to July 1
                COALESCE(i.percent_end,
                         DATEFROMPARTS(CAST(RIGHT(i.percent_AcademicYear, 4) AS int), 6, 30)), -- Default to June 30
                COALESCE(i.percent_compensated, 0),
                COALESCE(i.percent_modifiedOn, GETDATE()),
                COALESCE(mp.PersonId, p.PersonId)  -- Use modifier PersonId or self if not found
            FROM inserted i
            INNER JOIN [users].[Person] p ON i.percent_MothraID = p.MothraId
            LEFT JOIN [users].[Person] mp ON i.percent_modifiedBy = mp.MailId;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateTblPercentUpdateTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF UPDATE trigger for tblPercent...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPercent_Update]
        ON [EffortShadow].[tblPercent]
        INSTEAD OF UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Update the Percentages table (only non-derived columns)
            UPDATE pct
            SET
                pct.Percentage = i.percent_Percent,
                pct.EffortTypeId = i.percent_TypeID,
                pct.Unit = i.percent_Unit,
                pct.Modifier = i.percent_Modifier,
                pct.Comment = i.percent_Comment,
                pct.StartDate = i.percent_start,
                pct.EndDate = i.percent_end,
                pct.Compensated = i.percent_compensated,
                pct.ModifiedDate = GETDATE(),
                pct.ModifiedBy = COALESCE(mp.PersonId, 1)  -- Fallback to system user
            FROM [effort].[Percentages] pct
            INNER JOIN inserted i ON pct.Id = i.percent_ID
            LEFT JOIN [users].[Person] mp ON i.percent_modifiedBy = mp.MailId;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF UPDATE trigger created");
        }

        static void CreateTblPercentDeleteTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF DELETE trigger for tblPercent...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblPercent_Delete]
        ON [EffortShadow].[tblPercent]
        INSTEAD OF DELETE
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Delete from Percentages table
            DELETE pct
            FROM [effort].[Percentages] pct
            INNER JOIN deleted d ON pct.Id = d.percent_ID;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF DELETE trigger created");
        }

        static void CreateTblStatusView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblStatus...");

            // Calculate academic year from term code:
            // - Term codes are YYYYMM format (e.g., 200207 = July 2002)
            // - Effort system academic year runs Summer through Spring
            // - Summer/Fall terms (05-12) START a new academic year (e.g., 200207 -> 2002-2003)
            // - Winter/Spring terms (01-04) are CONTINUATION of previous year (e.g., 200301 -> 2002-2003)
            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblStatus]
        AS
        SELECT
            ts.TermCode as status_TermCode,
            ts.HarvestedDate as status_Harvested,
            ts.OpenedDate as status_Opened,
            ts.ClosedDate as status_Closed,
            ISNULL(t.Description,
                CASE
                    WHEN ts.TermCode % 100 IN (1, 2, 3, 4) THEN 'Winter/Spring ' + CAST(ts.TermCode / 100 AS varchar(4))
                    WHEN ts.TermCode % 100 IN (5, 6, 7, 8) THEN 'Summer ' + CAST(ts.TermCode / 100 AS varchar(4))
                    ELSE 'Fall ' + CAST(ts.TermCode / 100 AS varchar(4))
                END
            ) as status_TermName,
            -- Calculate academic year from term code (matches GetAcademicYearFromTermCode logic):
            -- Terms 01, 02, 03 (Winter Quarter, Spring Semester, Spring Quarter): previous year's academic year
            -- All other terms (04+): current year's academic year (e.g., 201404 -> 2014-2015)
            CASE
                WHEN ts.TermCode % 100 BETWEEN 1 AND 3  -- Winter/Spring terms only (01, 02, 03)
                THEN CAST((ts.TermCode / 100) - 1 AS varchar(4)) + '-' + CAST(ts.TermCode / 100 AS varchar(4))
                ELSE  -- All other terms (04+)
                CAST(ts.TermCode / 100 AS varchar(4)) + '-' + CAST((ts.TermCode / 100) + 1 AS varchar(4))
            END as status_AcademicYear
        FROM [effort].[TermStatus] ts
        LEFT JOIN [dbo].[vwTerms] t ON ts.TermCode = t.TermCode;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblSabbaticView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblSabbatic...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblSabbatic]
        AS
        SELECT
            s.Id as sab_ID,
            p.MothraId as sab_MothraID,  -- Map PersonId back to MothraId
            s.ExcludeClinicalTerms as sab_ExcludeClinTerms,
            s.ExcludeDidacticTerms as sab_ExcludeDidacticTerms
        FROM [effort].[Sabbaticals] s
        INNER JOIN [users].[Person] p ON s.PersonId = p.PersonId;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblRolesView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblRoles...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblRoles]
        AS
        SELECT
            Id as Role_ID,
            Description as Role_Desc
        FROM [effort].[Roles];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblEffortTypeLUView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblEffortType_LU...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblEffortType_LU]
        AS
        SELECT
            Id as type_ID,
            Name as type_Name,
            Class as type_Class,
            ShowOnTemplate as type_showOnMPVoteTemplate
        FROM [effort].[EffortTypes];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateUserAccessView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: userAccess (CRITICAL - user authorization)...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[userAccess]
        AS
        SELECT
            ua.Id as userAccessID,
            p.MothraId as mothraID,  -- Map PersonId back to MothraId
            ua.DepartmentCode as departmentAbbreviation
        FROM [effort].[UserAccess] ua
        INNER JOIN [users].[Person] p ON ua.PersonId = p.PersonId
        WHERE ua.IsActive = 1;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblUnitsLUView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblUnits_LU...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblUnits_LU]
        AS
        SELECT
            Id as unit_ID,
            Name as unit_Name
        FROM [effort].[Units];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblJobCodeView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblJobCode...");

            // Legacy schema: jobcode, faculty (unused), include_clinschedule
            // Note: Legacy has NO jobTitle/description column - don't expose one
            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblJobCode]
        AS
        SELECT
            Code as jobCode,
            CAST(1 AS bit) as faculty,              -- All rows have faculty=1, column unused
            IncludeClinSchedule as include_clinschedule
        FROM [effort].[JobCodes]
        WHERE IsActive = 1;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblReportUnitsView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblReportUnits...");

            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblReportUnits]
        AS
        SELECT
            Id as ru_id,
            UnitCode as ru_abbrev,
            UnitName as ru_unit
        FROM [effort].[ReportUnits];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static void CreateTblAltTitlesView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating table: tblAltTitles...");

            // Legacy tblAltTitles was a static lookup table with 5 special "V*" job codes
            // These are special job groups not found in the dictionary database
            string sql = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('EffortShadow') AND name = 'tblAltTitles')
        BEGIN
            CREATE TABLE [EffortShadow].[tblAltTitles]
            (
                JobGrpID char(3) NOT NULL,
                JobGrpName varchar(50) NOT NULL
            );

            INSERT INTO [EffortShadow].[tblAltTitles] (JobGrpID, JobGrpName)
            VALUES
                ('V1V', 'CLINICAL PROF W/O SALARY'),
                ('V4V', 'SPECIALIST IN CE'),
                ('V2V', 'VGAP'),
                ('V0V', 'GUEST'),
                ('V3V', 'SRA');
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Table created (5 special job codes)");
        }

        static void CreateUspGetJobGroups(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating stored procedure: usp_getJobGroups...");

            // This procedure queries the external dictionary database and UNIONs with tblAltTitles
            // Replicates the legacy usp_getJobGroups behavior
            string sql = @"
        CREATE OR ALTER PROCEDURE [EffortShadow].[usp_getJobGroups]
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Get job groups from external dictionary database
            -- INNER JOIN with tblPerson to only include job groups that are actually used
            SELECT DISTINCT
                t.dvtTitle_JobGroupID AS jobGrpID,
                t.dvtTitle_JobGroup_Name AS jobGrpName
            FROM dictionary.dbo.dvtTitle t
            INNER JOIN [EffortShadow].[tblPerson] ON t.dvtTitle_JobGroupID = person_JobGrpID

            UNION

            -- Add the 5 special job codes from tblAltTitles
            SELECT TOP 100 PERCENT jobGrpID, jobGrpName
            FROM [EffortShadow].[tblAltTitles]
            ORDER BY jobGrpName;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Stored procedure created");
        }

        static void CreateTblCourseRelationshipsView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblCourseRelationships...");

            // Note: Modern schema normalizes relationship types (e.g., "Cross" -> "CrossList")
            // but Legacy stored procedures concatenate cr_Relationship into display strings.
            // Map "CrossList" back to "Cross" for byte-for-byte compatibility with Legacy output.
            string sql = @"
        CREATE OR ALTER VIEW [EffortShadow].[tblCourseRelationships]
        AS
        SELECT
            ParentCourseId as cr_ParentID,
            ChildCourseId as cr_ChildID,
            CASE WHEN RelationshipType = 'CrossList' THEN 'Cross' ELSE RelationshipType END as cr_Relationship
        FROM [effort].[CourseRelationships];";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF INSERT trigger for tblCourseRelationships
            // Required because the view has a derived column (CASE expression for cr_Relationship)
            CreateTblCourseRelationshipsInsertTrigger(connection, transaction);
        }

        static void CreateTblCourseRelationshipsInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblCourseRelationships...");

            // Map legacy "Cross" back to normalized "CrossList" on insert
            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblCourseRelationships_Insert]
        ON [EffortShadow].[tblCourseRelationships]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            INSERT INTO [effort].[CourseRelationships] (ParentCourseId, ChildCourseId, RelationshipType)
            SELECT
                i.cr_ParentID,
                i.cr_ChildID,
                CASE WHEN i.cr_Relationship = 'Cross' THEN 'CrossList' ELSE i.cr_Relationship END
            FROM inserted i;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateTblAuditView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: tblAudit...");

            string sql = @"
        -- Maps Audits table to legacy tblAudit view for ColdFusion compatibility
        -- Uses legacy preservation columns for 1:1 verification against legacy tblAudit
        -- Note: This view requires an INSTEAD OF INSERT trigger to handle legacy audit writes
        CREATE OR ALTER VIEW [EffortShadow].[tblAudit]
        AS
        SELECT
            a.Id as audit_ID,
            pModBy.MothraId as audit_ModBy,  -- MothraId of person making change (for INSERT)
            a.ChangedDate as audit_ModTime,  -- Timestamp of change
            a.LegacyCRN as audit_CRN,  -- Preserved from legacy audit_CRN
            a.LegacyTermCode as audit_TermCode,  -- Preserved from legacy audit_TermCode
            COALESCE(a.LegacyMothraID, pSubject.MothraId) as audit_MothraID,  -- Preserved or derived
            COALESCE(a.LegacyAction, a.Action) as audit_Action,  -- Preserved original or normalized
            a.Changes as Audit_Audit,  -- Audit text
            a.TableName as audit_TableName,
            a.RecordId as audit_RecordID,
            a.ChangedBy as audit_ChangedBy_PersonId  -- PersonId for internal use
        FROM [effort].[Audits] a
        LEFT JOIN [users].[Person] pModBy ON a.ChangedBy = pModBy.PersonId
        LEFT JOIN [users].[Person] pSubject ON a.RecordId = pSubject.PersonId AND a.TableName IN ('Records', 'Persons', 'Percentages');";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");

            // Create INSTEAD OF INSERT trigger for tblAudit
            CreateTblAuditInsertTrigger(connection, transaction);
        }

        static void CreateTblAuditInsertTrigger(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("  Creating INSTEAD OF INSERT trigger for tblAudit...");

            string sql = @"
        CREATE OR ALTER TRIGGER [EffortShadow].[trg_tblAudit_Insert]
        ON [EffortShadow].[tblAudit]
        INSTEAD OF INSERT
        AS
        BEGIN
            SET NOCOUNT ON;

            -- Insert into modern Audits table from legacy audit format
            INSERT INTO [effort].[Audits] (
                TableName,
                RecordId,
                Action,
                ChangedBy,
                ChangedDate,
                Changes,
                LegacyAction,
                LegacyCRN,
                LegacyTermCode,
                LegacyMothraID
            )
            SELECT
                -- Map legacy action to table name
                CASE
                    WHEN i.audit_Action LIKE '%Course%' THEN 'Courses'
                    WHEN i.audit_Action LIKE '%Effort%' OR i.audit_Action LIKE '%Record%' THEN 'Records'
                    WHEN i.audit_Action LIKE '%Person%' OR i.audit_Action LIKE '%Instructor%' THEN 'Persons'
                    WHEN i.audit_Action LIKE '%Percent%' THEN 'Percentages'
                    WHEN i.audit_Action LIKE '%Term%' THEN 'TermStatus'
                    ELSE 'Unknown'
                END,
                COALESCE(i.audit_RecordID, 0),  -- Record ID if available
                -- Map to standardized action types (modern schema only allows INSERT/UPDATE/DELETE)
                CASE
                    WHEN i.audit_Action LIKE 'Create%' OR i.audit_Action LIKE 'Insert%' OR
                         i.audit_Action LIKE 'Open%' THEN 'INSERT'
                    WHEN i.audit_Action LIKE 'Update%' OR i.audit_Action LIKE 'Close%' OR
                         i.audit_Action LIKE 'Reopen%' OR i.audit_Action LIKE 'Verified%' THEN 'UPDATE'
                    WHEN i.audit_Action LIKE 'Delete%' THEN 'DELETE'
                    ELSE 'UPDATE'  -- Default to UPDATE for unknown actions
                END,
                -- Map MothraId to PersonId, fallback to system user (PersonId = 1) if not found
                COALESCE(p.PersonId, 1),
                COALESCE(i.audit_ModTime, GETDATE()),  -- Use provided timestamp or current time
                i.Audit_Audit,  -- Audit text
                -- Legacy preservation columns
                i.audit_Action,  -- Preserve original action text
                i.audit_CRN,     -- Preserve original CRN
                i.audit_TermCode,-- Preserve original TermCode
                i.audit_MothraID -- Preserve original MothraID
            FROM inserted i
            LEFT JOIN [users].[Person] p ON i.audit_ModBy = p.MothraId;
        END;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ INSTEAD OF INSERT trigger created");
        }

        static void CreateVwInstructorEffortView(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Creating view: vw_InstructorEffort (composite instructor effort data)...");

            string sql = @"
        -- Composite view joining instructor effort with person, course, and role information
        -- Used by various reporting and query stored procedures
        CREATE OR ALTER VIEW [EffortShadow].[vw_InstructorEffort]
        AS
        SELECT
            e.effort_ID,
            e.effort_CourseID,
            e.effort_MothraID,
            e.effort_termCode as effort_TermCode,
            e.effort_SessionType,
            e.effort_Role,
            e.effort_Hours,
            e.effort_Weeks,
            e.effort_CRN,
            p.person_FirstName,
            p.person_LastName,
            p.person_MiddleIni,
            p.person_EffortDept,
            p.person_JobGrpID,
            p.person_Title,
            c.course_subjCode,
            c.course_crseNumb,
            c.course_seqNumb,
            c.course_units,
            c.course_enrollment,
            c.course_custDept,
            r.Role_Desc
        FROM [EffortShadow].[tblEffort] e
        LEFT JOIN [EffortShadow].[tblPerson] p ON e.effort_MothraID = p.person_MothraID AND e.effort_termCode = p.person_TermCode
        LEFT JOIN [EffortShadow].[tblCourses] c ON e.effort_CourseID = c.course_id
        LEFT JOIN [EffortShadow].[tblRoles] r ON e.effort_Role = r.Role_ID;";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ View created");
        }

        static string RewriteStoredProcedure(string procName, string procBody)
        {
            // Rewrite legacy table references to EffortShadow views
            // This allows stored procedures to use legacy column names through the shadow layer
            // Views handle the mapping to modern [effort] tables

            var tableMapping = new Dictionary<string, string>
    {
        { "vw_InstructorEffort", "[EffortShadow].[vw_InstructorEffort]" },  // Process views first (longer names)
        { "tblEffort", "[EffortShadow].[tblEffort]" },
        { "tblPerson", "[EffortShadow].[tblPerson]" },
        { "tblCourses", "[EffortShadow].[tblCourses]" },
        { "tblPercent", "[EffortShadow].[tblPercent]" },
        { "tblStatus", "[EffortShadow].[tblStatus]" },
        { "tblSabbatic", "[EffortShadow].[tblSabbatic]" },
        { "tblRoles", "[EffortShadow].[tblRoles]" },
        { "tblEffortType_LU", "[EffortShadow].[tblEffortType_LU]" },
        { "userAccess", "[EffortShadow].[userAccess]" },
        { "tblUnits_LU", "[EffortShadow].[tblUnits_LU]" },
        { "tblJobCode", "[EffortShadow].[tblJobCode]" },
        { "tblReportUnits", "[EffortShadow].[tblReportUnits]" },
        { "tblAltTitles", "[EffortShadow].[tblAltTitles]" },
        { "tblCourseRelationships", "[EffortShadow].[tblCourseRelationships]" },
        { "tblAudit", "[EffortShadow].[tblAudit]" }
    };

            string rewritten = procBody;

            // Replace table names in DML statements (INSERT, UPDATE, DELETE, FROM)
            // Process in descending name-length order so longer names (e.g., tblEffortType_LU)
            // are substituted before their prefixes (e.g., tblEffort) to prevent partial matches
            foreach (var (legacyTable, modernTable) in tableMapping
                         .OrderByDescending(kvp => kvp.Key.Length))
            {
                // Match table names with optional [dbo] schema qualifier
                // Pattern matches: [dbo].[tblEffort], dbo.tblEffort, [tblEffort], or tblEffort
                // Uses word boundaries and bracket matching to prevent partial identifier matches
                var pattern =
                    $@"(?ix)
               (?:\[\s*dbo\s*\]\.|\bdbo\.)?   # optional dbo qualifier
               (\[\s*{Regex.Escape(legacyTable)}\s*\]  # bracketed identifier
                |\b{Regex.Escape(legacyTable)}\b)       # or bare identifier with word boundaries";
                rewritten = Regex.Replace(rewritten, pattern, modernTable);
            }

            // Replace references to dbo functions with EffortShadow functions
            // Handle all variations: dbo.funcRef, [dbo].funcRef, dbo.[funcRef], [dbo].[funcRef]
            var functionReferences = new[] {
        "fn_checkJobGroupAndEffortCode",
        "fn_TsqlSplit",
        "fn_getEffortDept",
        "fn_getEffortTitle",
        "fn_getJobGroupName",
        "getFirstTermInYear",
        "getLastTermInYear",
        "academic_year_start",
        "Fiscal_Year",
        "isClinicalEffort"
    };

            foreach (var funcRef in functionReferences)
            {
                // Pattern matches: dbo.funcRef, [dbo].funcRef, dbo.[funcRef], [dbo].[funcRef]
                var pattern = $@"(?:\[?\s*dbo\s*\]?\.)\s*\[?\s*{Regex.Escape(funcRef)}\s*\]?";
                rewritten = Regex.Replace(rewritten,
                    pattern,
                    $"[EffortShadow].[{funcRef}]",
                    RegexOptions.IgnoreCase);
            }

            // Fix isdate() function calls on datetime2 columns
            // The legacy database used datetime, but modern uses datetime2(7)
            // isdate() requires a string input, so we need to cast datetime2 to varchar first
            // Pattern matches: isdate(column_name) and replaces with: CASE WHEN column_name IS NULL THEN 0 ELSE 1 END
            rewritten = Regex.Replace(rewritten,
                @"isdate\s*\(\s*([a-zA-Z_][a-zA-Z0-9_\.]*)\s*\)",
                "CASE WHEN $1 IS NULL THEN 0 ELSE 1 END",
                RegexOptions.IgnoreCase);

            return rewritten;
        }

        static void RecompileAllProcedures(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Recompiling all stored procedures to refresh view dependencies...");
            Console.WriteLine();

            try
            {
                string sql = @"
            DECLARE @sql NVARCHAR(MAX) = '';
            SELECT @sql = @sql + 'EXEC sp_recompile ''[EffortShadow].[' + name + ']'';' + CHAR(13)
            FROM sys.procedures
            WHERE schema_id = SCHEMA_ID('EffortShadow');
            EXEC sp_executesql @sql;";

                using var cmd = new SqlCommand(sql, connection, transaction);
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All stored procedures recompiled successfully");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Warning: Some procedures may not have recompiled: {ex.Message}");
                Console.ResetColor();
            }
        }

        static int MigrateFunctionsInTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating functions from legacy database...");
            Console.WriteLine();

            // Read exported schema file
            string exportFilePath = Path.Join(Environment.CurrentDirectory, "Effort_Database_Schema_And_Data_LEGACY.txt");
            string exportContent = File.ReadAllText(exportFilePath);

            // Extract all function definitions
            // Pattern matches: FUNCTION: [schema].[name] (type)
            // followed by CREATE FUNCTION definition (with optional ### delimiter and comments in between)
            // Some functions have ### delimiter after header, some don't (e.g., academic_year_start, Fiscal_Year)
            // Ends when it hits ### or === delimiters or end of string
            var functionPattern = @"FUNCTION: \[([\w]+)\]\.\[([\w]+)\] \(([\w_]+)\)\s*(?:#{78,})?(.*?)(CREATE\s+(FUNCTION|PROCEDURE)\s+.*?)(?=\s*[#=]{78,}|\z)";
            var matches = Regex.Matches(exportContent, functionPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            Console.WriteLine($"Found {matches.Count} functions in legacy schema");
            Console.WriteLine();

            int successCount = 0;
            int skippedCount = 0;
            var failedFunctions = new List<(string Name, string Error)>();

            foreach (Match match in matches)
            {
                var name = match.Groups[2].Value;
                // Note: match.Groups[4] contains optional comments before CREATE, currently unused
                var definition = match.Groups[5].Value;  // CREATE FUNCTION ... statement

                // Check if function should be excluded
                if (EffortSchemaConfig.ShouldExcludeFunction(name))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  ⊘ Skipping: {name} - {EffortSchemaConfig.GetExclusionReason(name)}");
                    Console.ResetColor();
                    skippedCount++;
                    continue;
                }

                // Rewrite function to use EffortShadow schema and shadow views
                var rewritten = RewriteFunction(name, definition);

                // DEBUG: Show first 200 chars of rewritten SQL
                var preview = rewritten.Length > 200 ? rewritten.Substring(0, 200) + "..." : rewritten;
                Console.WriteLine($"  → {name}: {preview.Replace(Environment.NewLine, " ")}");

                try
                {
                    using var cmd = new SqlCommand(rewritten, connection, transaction);
                    cmd.ExecuteNonQuery();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Migrated: {name}");
                    Console.ResetColor();
                    successCount++;
                }
                catch (SqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ FAILED: {name} - {ex.Message}");
                    Console.ResetColor();
                    failedFunctions.Add((name, ex.Message));
                }
            }

            Console.WriteLine();
            Console.WriteLine("============================================");
            Console.WriteLine("Function Migration Summary");
            Console.WriteLine("============================================");
            Console.WriteLine($"✓ Successfully migrated: {successCount} functions");
            Console.WriteLine($"⊘ Skipped (unused): {skippedCount} functions");

            if (failedFunctions.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to migrate: {failedFunctions.Count} functions");
                Console.WriteLine();
                Console.WriteLine("Failed functions (require manual review):");
                foreach (var (funcName, error) in failedFunctions)
                {
                    Console.WriteLine($"  - {funcName}");
                    Console.WriteLine($"    Error: {error}");
                }
                Console.ResetColor();
            }

            Console.WriteLine();

            return failedFunctions.Count;
        }

        static string RewriteFunction(string funcName, string funcBody)
        {
            // Rewrite legacy table references to EffortShadow views
            // Similar to RewriteStoredProcedure but for functions

            var tableMapping = new Dictionary<string, string>
    {
        { "vw_InstructorEffort", "[EffortShadow].[vw_InstructorEffort]" },  // Process views first (longer names)
        { "tblEffort", "[EffortShadow].[tblEffort]" },
        { "tblPerson", "[EffortShadow].[tblPerson]" },
        { "tblCourses", "[EffortShadow].[tblCourses]" },
        { "tblPercent", "[EffortShadow].[tblPercent]" },
        { "tblStatus", "[EffortShadow].[tblStatus]" },
        { "tblSabbatic", "[EffortShadow].[tblSabbatic]" },
        { "tblRoles", "[EffortShadow].[tblRoles]" },
        { "tblEffortType_LU", "[EffortShadow].[tblEffortType_LU]" },
        { "userAccess", "[EffortShadow].[userAccess]" },
        { "tblUnits_LU", "[EffortShadow].[tblUnits_LU]" },
        { "tblJobCode", "[EffortShadow].[tblJobCode]" },
        { "tblReportUnits", "[EffortShadow].[tblReportUnits]" },
        { "tblAltTitles", "[EffortShadow].[tblAltTitles]" },
        { "tblCourseRelationships", "[EffortShadow].[tblCourseRelationships]" },
        { "tblAudit", "[EffortShadow].[tblAudit]" }
    };

            string rewritten = funcBody;

            // Replace CREATE FUNCTION [dbo].[name] with CREATE OR ALTER FUNCTION [EffortShadow].[name]
            // Handle both formats: "CREATE FUNCTION [dbo].[name]" and "CREATE FUNCTION name" (no schema)
            rewritten = Regex.Replace(rewritten,
                @"CREATE\s+FUNCTION\s+(?:\[?\s*dbo\s*\]?\.\s*)?\[?\s*" + Regex.Escape(funcName) + @"\s*\]?",
                $"CREATE OR ALTER FUNCTION [EffortShadow].[{funcName}]",
                RegexOptions.IgnoreCase);

            // Replace table names
            foreach (var (legacyTable, modernTable) in tableMapping.OrderByDescending(kvp => kvp.Key.Length))
            {
                var pattern =
                    $@"(?ix)
               (?:\[\s*dbo\s*\]\.|\bdbo\.)?
               (\[\s*{Regex.Escape(legacyTable)}\s*\]
                |\b{Regex.Escape(legacyTable)}\b)";
                rewritten = Regex.Replace(rewritten, pattern, modernTable);
            }

            // Replace references to other dbo functions with EffortShadow functions
            // Handle all variations: dbo.funcRef, [dbo].funcRef, dbo.[funcRef], [dbo].[funcRef]
            var functionReferences = new[] {
        "fn_checkJobGroupAndEffortCode",
        "fn_TsqlSplit",
        "fn_getEffortDept",
        "fn_getEffortTitle",
        "fn_getJobGroupName",
        "getFirstTermInYear",
        "getLastTermInYear",
        "academic_year_start",
        "Fiscal_Year",
        "isClinicalEffort"
    };

            foreach (var funcRef in functionReferences)
            {
                // Pattern matches: dbo.funcRef, [dbo].funcRef, dbo.[funcRef], [dbo].[funcRef]
                var pattern = $@"(?:\[?\s*dbo\s*\]?\.)\s*\[?\s*{Regex.Escape(funcRef)}\s*\]?";
                rewritten = Regex.Replace(rewritten,
                    pattern,
                    $"[EffortShadow].[{funcRef}]",
                    RegexOptions.IgnoreCase);
            }

            return rewritten;
        }

        static int MigrateStoredProceduresInTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            Console.WriteLine("Migrating stored procedures from legacy database...");
            Console.WriteLine();

            // Read exported schema file
            string exportFilePath = Path.Join(Environment.CurrentDirectory, "Effort_Database_Schema_And_Data_LEGACY.txt");
            string exportContent = File.ReadAllText(exportFilePath);

            // Extract all CREATE PROCEDURE statements using regex
            // Match from CREATE PROCEDURE to the next CREATE PROCEDURE or end of file
            // Handle both "CREATE PROCEDURE [dbo].[name]" and "CREATE PROCEDURE name" (no schema)
            var procedurePattern = new Regex(
                @"(?ms)^CREATE\s+PROCEDURE\s+(?:\[?dbo\]?\.)?\[?(\w+)\]?\s+(.*?)(?=^CREATE\s+PROCEDURE|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            var matches = procedurePattern.Matches(exportContent);
            var procedures = new List<(string Name, string Definition)>();
            int skippedCount = 0;

            Console.WriteLine($"Found {matches.Count} stored procedures in legacy schema");

            foreach (Match match in matches)
            {
                string procName = match.Groups[1].Value;
                string procBody = match.Groups[2].Value;

                // Check if procedure should be excluded (obsolete or unused)
                if (EffortSchemaConfig.ShouldExcludeProcedure(procName))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  ⊘ Skipping: {procName} - {EffortSchemaConfig.GetExclusionReason(procName)}");
                    Console.ResetColor();
                    skippedCount++;
                    continue;
                }

                // Remove header sections like:
                // ################################################################################
                // STORED PROCEDURE: [dbo].[usp_foo]
                // ################################################################################
                procBody = Regex.Replace(procBody, @"^\s*#{20,}.*?#{20,}\s*$", "", RegexOptions.Multiline);
                procBody = Regex.Replace(procBody, @"^\s*STORED PROCEDURE:.*?$", "", RegexOptions.Multiline);

                procedures.Add((procName, procBody));
            }

            Console.WriteLine();
            Console.WriteLine($"Migrating {procedures.Count} stored procedures (skipped {skippedCount} unused/obsolete)");
            Console.WriteLine();

            int successCount = 0;
            int failureCount = 0;
            var failedProcedures = new List<(string Name, string Error)>();

            foreach (var (procName, procBody) in procedures)
            {
                try
                {
                    // Rewrite procedure to use modern table names
                    string rewrittenBody = RewriteStoredProcedure(procName, procBody);

                    // Reconstruct full CREATE PROCEDURE statement with EffortShadow schema
                    string fullProcedure = $"CREATE PROCEDURE [EffortShadow].[{procName}] {rewrittenBody}";

                    // Use CREATE OR ALTER for idempotency
                    fullProcedure = fullProcedure.Replace("CREATE PROCEDURE", "CREATE OR ALTER PROCEDURE");

                    using var cmd = new SqlCommand(fullProcedure, connection, transaction);
                    cmd.CommandTimeout = 60; // 60 seconds timeout
                    cmd.ExecuteNonQuery();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ Migrated: {procName}");
                    Console.ResetColor();
                    successCount++;
                }
                catch (SqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ FAILED: {procName} - {ex.Message}");
                    Console.ResetColor();
                    failedProcedures.Add((procName, ex.Message));
                    failureCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("============================================");
            Console.WriteLine("Stored Procedure Migration Summary");
            Console.WriteLine("============================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Successfully migrated: {successCount} procedures");
            Console.ResetColor();
            Console.WriteLine($"⊘ Skipped (unused/obsolete): {skippedCount} procedures");

            if (failureCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Failed to migrate: {failureCount} procedures");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Failed procedures (require manual review):");
                foreach (var (name, error) in failedProcedures)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  - {name}");
                    Console.WriteLine($"    Error: {error}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.WriteLine("Creating special procedures that query external databases...");

            // Create usp_getJobGroups - queries external dictionary database
            try
            {
                CreateUspGetJobGroups(connection, transaction);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Created: usp_getJobGroups");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ FAILED: usp_getJobGroups - {ex.Message}");
                Console.ResetColor();
                failureCount++;
            }

            return failureCount;
        }

        static void MigrateStoredProcedures(string connectionString, bool executeMode)
        {
            Console.WriteLine("Migrating stored procedures from legacy database...");
            Console.WriteLine();

            // Read exported schema file
            string exportFilePath = Path.Join(Environment.CurrentDirectory, "Effort_Database_Schema_And_Data_LEGACY.txt");
            string exportContent = File.ReadAllText(exportFilePath);

            // Extract all CREATE PROCEDURE statements using regex
            // Match from CREATE PROCEDURE to the next CREATE PROCEDURE or end of file
            // Handle both "CREATE PROCEDURE [dbo].[name]" and "CREATE PROCEDURE name" (no schema)
            var procedurePattern = new Regex(
                @"(?ms)^CREATE\s+PROCEDURE\s+(?:\[?dbo\]?\.)?\[?(\w+)\]?\s+(.*?)(?=^CREATE\s+PROCEDURE|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            var matches = procedurePattern.Matches(exportContent);
            var procedures = new List<(string Name, string Definition)>();
            int totalFound = matches.Count;
            int skippedCount = 0;

            foreach (Match match in matches)
            {
                string procName = match.Groups[1].Value;
                string procBody = match.Groups[2].Value;

                // Check if procedure should be excluded (obsolete or unused)
                if (EffortSchemaConfig.ShouldExcludeProcedure(procName))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  ⊘ Skipping: {procName} - {EffortSchemaConfig.GetExclusionReason(procName)}");
                    Console.ResetColor();
                    skippedCount++;
                    continue;
                }

                // Remove header sections like:
                // ################################################################################
                // STORED PROCEDURE: [dbo].[usp_foo]
                // ################################################################################
                procBody = Regex.Replace(procBody, @"^\s*#{20,}.*?#{20,}\s*$", "", RegexOptions.Multiline);
                procBody = Regex.Replace(procBody, @"^\s*STORED PROCEDURE:.*?$", "", RegexOptions.Multiline);

                procedures.Add((procName, procBody));
            }

            Console.WriteLine();
            Console.WriteLine($"Found {totalFound} procedures in legacy schema");
            Console.WriteLine($"Migrating {procedures.Count} procedures (skipped {skippedCount} unused/obsolete)");
            Console.WriteLine();

            // NOTE: CRUD procedures will be rewritten to work with [effort] tables
            // Read-only reporting procedures can reference views directly
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Start transaction for dry-run support
            using var transaction = connection.BeginTransaction();

            try
            {
                MigrateStoredProceduresInTransaction(connection, transaction);

                // Commit or rollback based on mode
                if (executeMode)
                {
                    transaction.Commit();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Stored procedures committed to database");
                    Console.ResetColor();
                }
                else
                {
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ DRY-RUN: Stored procedures rolled back (not saved)");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error migrating stored procedures: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }
    }
}
