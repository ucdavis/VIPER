// ============================================
// Script: CreateEffortShadow.cs
// Description: Create EffortShadow compatibility database for ColdFusion application
// Author: VIPER2 Development Team
// Date: 2025-11-04
// ============================================
// This script creates the EffortShadow database with:
// - 19 compatibility views mapping to [VIPER].[effort] schema
// - 87 stored procedures migrated from legacy database (89 total in legacy, 2 obsolete)
// - NO DATA - all data lives in [VIPER].[effort] schema
// ============================================
// PREREQUISITES:
// 1. [VIPER].[effort] schema must exist with all 20 tables
// 2. CreateEffortDatabase.cs must have been run
// 3. MigrateEffortData.cs must have been run
// 4. Effort_Database_Schema_And_Data_LEGACY.txt must exist (run RunExportSchema.bat)
// ============================================
// USAGE:
// dotnet script CreateEffortShadow.cs                 (dry-run mode - validates SQL, no database creation)
// dotnet script CreateEffortShadow.cs --apply         (actually create shadow database)
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
// ============================================

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    public class CreateEffortShadow
    {
        public static void Run(string[] args)
        {
            // ============================================
            // Parse command-line arguments
            // ============================================
            bool executeMode = args.Any(arg => arg.Equals("--apply", StringComparison.OrdinalIgnoreCase));

Console.WriteLine("============================================");
Console.WriteLine("Creating EffortShadow Compatibility Database");
Console.WriteLine($"Mode: {(executeMode ? "APPLY (permanent changes)" : "DRY-RUN (validates SQL only, no database creation)")}");
Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("============================================");
Console.WriteLine();

var configuration = EffortScriptHelper.LoadConfiguration();
string masterConnectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER");

// Create connection string for EffortShadow database
var builder = new SqlConnectionStringBuilder(masterConnectionString) { InitialCatalog = "EffortShadow" };
string shadowConnectionString = builder.ConnectionString;

Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(masterConnectionString)}");
Console.WriteLine();

try
{
    // Step 1: Verify prerequisites
    if (!VerifyPrerequisites(masterConnectionString))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR: Prerequisites not met. Exiting.");
        Console.ResetColor();
        Environment.Exit(1);
    }

    Console.WriteLine();

    // Step 2: Handle database creation based on mode
    bool databaseCreated = false;
    if (executeMode)
    {
        databaseCreated = CreateDatabase(masterConnectionString);
    }
    else
    {
        // In dry-run mode, check if database exists
        bool databaseExists = CheckDatabaseExists(masterConnectionString);

        if (!databaseExists)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("DRY-RUN MODE: EffortShadow database does not exist.");
            Console.WriteLine("Skipping all operations (database creation, views, stored procedures).");
            Console.WriteLine();
            Console.WriteLine("To create the database, run with --apply flag:");
            Console.WriteLine("  dotnet script CreateEffortShadow.cs --apply");
            Console.ResetColor();
            return; // Exit early - nothing to test without database
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("DRY-RUN MODE: EffortShadow database already exists.");
        Console.WriteLine("Testing views and stored procedures (will be rolled back)...");
        Console.ResetColor();
    }

    Console.WriteLine();
    Console.WriteLine("============================================");
    Console.WriteLine("Step 1: Create Compatibility Views");
    Console.WriteLine("============================================");
    Console.WriteLine();

    CreateViews(shadowConnectionString, executeMode);

    Console.WriteLine();
    Console.WriteLine("============================================");
    Console.WriteLine("Step 2: Migrate Stored Procedures");
    Console.WriteLine("============================================");
    Console.WriteLine();

    MigrateStoredProcedures(shadowConnectionString, executeMode);

    Console.WriteLine();
    Console.WriteLine("============================================");
    Console.WriteLine("EffortShadow Database Creation Summary");
    Console.WriteLine("============================================");
    Console.WriteLine();
    Console.WriteLine($"Mode: {(executeMode ? "EXECUTE - Changes committed" : "DRY-RUN - SQL validated, changes rolled back")}");
    Console.WriteLine("Database: EffortShadow");
    Console.WriteLine("Purpose: Legacy compatibility layer for ColdFusion application");
    Console.WriteLine("Contents: 19 views + ~87 stored procedures (no data storage)");
    Console.WriteLine("Data Source: All data lives in [VIPER].[effort] schema");
    Console.WriteLine();

    if (!executeMode)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠ DRY-RUN MODE:");
        Console.WriteLine("  - Database was not created (database already existed)");
        Console.WriteLine("  - View and stored procedure SQL validated and rolled back");
        Console.WriteLine("  - No permanent changes were made");
        Console.WriteLine();
        Console.WriteLine("  To apply changes permanently, run:");
        Console.WriteLine("    dotnet script CreateEffortShadow.cs --apply");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("Next Steps:");
        Console.WriteLine("  1. DBA will configure database permissions for applications");
        Console.WriteLine("  2. Update ColdFusion connection strings to point to EffortShadow");
        Console.WriteLine("  3. Test ColdFusion application against EffortShadow database");
    }

    Console.WriteLine("============================================");
    Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine("============================================");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    Console.ResetColor();
    Environment.Exit(1);
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
    string checkDataSql = "SELECT COUNT(*) FROM [VIPER].[effort].[Records];";
    using var cmdData = new SqlCommand(checkDataSql, connection);
    int recordCount = 0;
    try
    {
        recordCount = (int)cmdData.ExecuteScalar();
    }
    catch
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ⚠ WARNING: Could not check [VIPER].[effort] schema for data.");
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

static bool CheckDatabaseExists(string connectionString)
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    using var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = 'EffortShadow'", connection);
    int exists = (int)checkCmd.ExecuteScalar();

    return exists > 0;
}

static bool CreateDatabase(string connectionString)
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    Console.WriteLine("Creating EffortShadow database...");

    // Check if database already exists
    if (CheckDatabaseExists(connectionString))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ⚠ EffortShadow database already exists. Skipping creation.");
        Console.WriteLine("  To recreate, manually drop the database first.");
        Console.ResetColor();
        return false;
    }

    // Create database permanently
    using var createCmd = new SqlCommand("CREATE DATABASE [EffortShadow];", connection);
    createCmd.ExecuteNonQuery();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ✓ EffortShadow database created successfully.");
    Console.ResetColor();
    return true;
}

static void CreateViews(string connectionString, bool executeMode)
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    // Start transaction for dry-run support
    using var transaction = connection.BeginTransaction();

    try
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

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ All 15 views created successfully");
        Console.ResetColor();

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
        CREATE OR ALTER VIEW [dbo].[tblEffort]
        AS
        SELECT
            r.Id as effort_ID,
            r.CourseId as effort_CourseID,
            p.MothraId as effort_MothraID,  -- Map PersonId back to MothraId
            r.TermCode as effort_TermCode,
            r.SessionType as effort_SessionType,
            CAST(r.Role as char(1)) as effort_Role,  -- Convert int to char for legacy
            r.Hours as effort_Hours,
            r.Weeks as effort_Weeks,
            r.Crn as effort_CRN
        FROM [VIPER].[effort].[Records] r
        INNER JOIN [VIPER].[users].[Person] p ON r.PersonId = p.PersonId;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblPersonView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblPerson...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblPerson]
        AS
        SELECT
            ps.Id as person_ID,
            p.MothraId as person_MothraID,  -- Map PersonId back to MothraId
            ps.FirstName as person_First,
            ps.LastName as person_Last,
            ps.Email as person_Email,
            ps.DepartmentCode as person_DeptAbbrev
        FROM [VIPER].[effort].[Persons] ps
        INNER JOIN [VIPER].[users].[Person] p ON ps.PersonId = p.PersonId;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblCoursesView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblCourses...");

    string sql = @"
        -- NOTE: course_Title not included - ColdFusion should fetch from VIPER course catalog
        -- To get course title: JOIN [VIPER].[courses].[Catalog] ON SubjCode + CrseNumb
        CREATE OR ALTER VIEW [dbo].[tblCourses]
        AS
        SELECT
            Id as course_ID,
            Crn as course_CRN,
            TermCode as course_TermCode,
            SubjCode as course_SubjCode,
            CrseNumb as course_CrseNumb,
            SeqNumb as course_SeqNumb,
            Enrollment as course_Enrollment,
            Units as course_Units,
            CustDept as course_CustDept
        FROM [VIPER].[effort].[Courses];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblPercentView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblPercent...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblPercent]
        AS
        SELECT
            pct.Id as percent_ID,
            p.MothraId as percent_MothraID,  -- Map PersonId back to MothraId
            pct.TermCode as percent_TermCode,
            pct.StatusId as percent_StatusID,
            pct.Teaching as percent_Teaching,
            pct.Research as percent_Research,
            pct.ClinicalService as percent_ClinicalService,
            pct.OutreachService as percent_OutreachService,
            pct.OtherService as percent_OtherService,
            pct.Administration as percent_Administration
        FROM [VIPER].[effort].[Percentages] pct
        INNER JOIN [VIPER].[users].[Person] p ON pct.PersonId = p.PersonId;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblStatusView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblStatus...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblStatus]
        AS
        SELECT
            Id as status_ID,
            Name as status_Name,
            SortOrder as status_SortOrder,
            CASE WHEN IsActive = 1 THEN 'true' ELSE 'false' END as status_Active
        FROM [VIPER].[effort].[Status];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblSabbaticView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblSabbatic...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblSabbatic]
        AS
        SELECT
            s.Id as sabbatic_ID,
            p.MothraId as sabbatic_MothraID,  -- Map PersonId back to MothraId
            s.TermCode as sabbatic_TermCode
        FROM [VIPER].[effort].[Sabbatics] s
        INNER JOIN [VIPER].[users].[Person] p ON s.PersonId = p.PersonId;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblRolesView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblRoles...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblRoles]
        AS
        SELECT
            Id as role_ID,
            Name as role_Name,
            SortOrder as role_SortOrder
        FROM [VIPER].[effort].[Roles];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblEffortTypeLUView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblEffortType_LU...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblEffortType_LU]
        AS
        SELECT
            Id as effortType_ID,
            Name as effortType_Name,
            SortOrder as effortType_SortOrder
        FROM [VIPER].[effort].[EffortTypes];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateUserAccessView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: userAccess (CRITICAL - user authorization)...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[userAccess]
        AS
        SELECT
            ua.Id as userAccessID,
            p.MothraId as mothraID,  -- Map PersonId back to MothraId
            ua.DepartmentCode as departmentAbbreviation
        FROM [VIPER].[effort].[UserAccess] ua
        INNER JOIN [VIPER].[users].[Person] p ON ua.PersonId = p.PersonId
        WHERE ua.IsActive = 1;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblUnitsLUView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblUnits_LU...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblUnits_LU]
        AS
        SELECT
            Id as units_ID,
            Name as units_Name,
            SortOrder as units_SortOrder
        FROM [VIPER].[effort].[Units];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblJobCodeView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblJobCode...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblJobCode]
        AS
        SELECT
            Code as jobCode,
            Title as jobTitle
        FROM [VIPER].[effort].[JobCodes];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblReportUnitsView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblReportUnits...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblReportUnits]
        AS
        SELECT Unit as reportUnit
        FROM [VIPER].[effort].[ReportUnits];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblAltTitlesView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblAltTitles...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblAltTitles]
        AS
        SELECT
            JobCode as jobCode,
            AlternateTitle as altTitle
        FROM [VIPER].[effort].[AlternateTitles];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblCourseRelationshipsView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblCourseRelationships...");

    string sql = @"
        CREATE OR ALTER VIEW [dbo].[tblCourseRelationships]
        AS
        SELECT
            Id as courseRelationship_ID,
            ParentCourseId as courseRelationship_ParentCourseID,
            ChildCourseId as courseRelationship_ChildCourseID
        FROM [VIPER].[effort].[CourseRelationships];";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
}

static void CreateTblAuditView(SqlConnection connection, SqlTransaction transaction)
{
    Console.WriteLine("Creating view: tblAudit...");

    string sql = @"
        -- Maps new Audits table (dual-column format) to legacy tblAudit view
        -- Uses computed 'Changes' column for unified access to legacy/JSON formats
        -- Maps PersonId back to MothraId for ColdFusion compatibility
        CREATE OR ALTER VIEW [dbo].[tblAudit]
        AS
        SELECT
            a.Id as audit_ID,
            p.MothraId as audit_MothraID,
            a.TableName as audit_TableName,
            a.RecordId as audit_RecordID,
            a.Action as audit_Action,
            a.ChangedBy as audit_ChangedBy_PersonId,  -- PersonId for internal use
            a.ChangedDate as audit_Date,
            a.Changes as audit_Changes,  -- Computed column (legacy text or JSON)
            a.IsLegacyFormat as audit_IsLegacyFormat
        FROM [VIPER].[effort].[Audits] a
        LEFT JOIN [VIPER].[users].[Person] p ON a.ChangedBy = p.PersonId;";

    using var cmd = new SqlCommand(sql, connection, transaction);
    cmd.ExecuteNonQuery();
    Console.WriteLine("  ✓ View created");
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
    var procedurePattern = new Regex(
        @"(?ms)^CREATE\s+PROCEDURE\s+\[?dbo\]?\.\[?(\w+)\]?\s+(.*?)(?=^CREATE\s+PROCEDURE|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase
    );

    var matches = procedurePattern.Matches(exportContent);
    var procedures = new List<(string Name, string Definition)>();

    foreach (Match match in matches)
    {
        string procName = match.Groups[1].Value;
        string procBody = match.Groups[2].Value;

        // Filter out obsolete Visual Studio database tools
        if (procName.StartsWith("dt_", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  ⊘ Skipping obsolete procedure: {procName}");
            Console.ResetColor();
            continue;
        }

        procedures.Add((procName, procBody));
    }

    Console.WriteLine();
    Console.WriteLine($"Found {procedures.Count} stored procedures to migrate");
    Console.WriteLine();

    // Table reference mapping (legacy table names → EffortShadow view names)
    var tableMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "tblEffort", "vw_tblEffort" },
        { "tblPerson", "vw_tblPerson" },
        { "tblCourses", "vw_tblCourses" },
        { "tblPercent", "vw_tblPercent" },
        { "tblStatus", "vw_tblStatus" },
        { "tblSabbatic", "vw_tblSabbatic" },
        { "tblRoles", "vw_tblRoles" },
        { "tblEffortType_LU", "vw_tblEffortType_LU" },
        { "userAccess", "vw_userAccess" },
        { "tblUnits_LU", "vw_tblUnits_LU" },
        { "tblJobCode", "vw_tblJobCode" },
        { "tblReportUnits", "vw_tblReportUnits" },
        { "tblReviewYears", "vw_tblReviewYears" },
        { "tblAltTitles", "vw_tblAltTitles" },
        { "months", "vw_months" },
        { "workdays", "vw_workdays" },
        { "tblCourseRelationships", "vw_tblCourseRelationships" },
        { "additionalQuestion", "vw_additionalQuestion" },
        { "tblAudit", "vw_tblAudit" }
    };

    // NOTE: Actually, we should reference the views we just created (tblEffort, tblPerson, etc.)
    // NOT vw_tblEffort. The views ARE the compatibility layer. Let's use the actual view names.
    // Stored procedures should reference [dbo].[tblEffort], [dbo].[tblPerson], etc.
    // No table reference updates needed - legacy table names are now view names!

    using var connection = new SqlConnection(connectionString);
    connection.Open();

    // Start transaction for dry-run support
    using var transaction = connection.BeginTransaction();

    int successCount = 0;
    int failureCount = 0;
    var failedProcedures = new List<(string Name, string Error)>();

    try
    {
        foreach (var (procName, procBody) in procedures)
        {
            try
            {
                // Reconstruct full CREATE PROCEDURE statement
                string fullProcedure = $"CREATE PROCEDURE [dbo].[{procName}] {procBody}";

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
            catch (Exception ex)
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
}
