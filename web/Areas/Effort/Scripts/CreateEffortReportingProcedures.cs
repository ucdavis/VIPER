// ============================================
// Script: CreateEffortReportingProcedures.cs
// Description: Create modernized reporting stored procedures in Effort database
// Author: VIPER2 Development Team
// Date: 2025-11-14
// ============================================
// This script creates 16 complex reporting stored procedures for the Effort database.
// These SPs are optimized for performance on large datasets and complex aggregations.
// CRUD operations are handled by Entity Framework repositories instead.
// ============================================
// PREREQUISITES:
// 1. CreateEffortDatabase.cs must have been run (tables exist)
// 2. MigrateEffortData.cs must have been run (data migrated)
// ============================================
// USAGE:
// dotnet script CreateEffortReportingProcedures.cs                 (dry-run mode)
// dotnet script CreateEffortReportingProcedures.cs --apply         (create procedures)
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell)
// ============================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    public class CreateEffortReportingProcedures
    {
        public static void Run(string[] args)
        {
            bool executeMode = args.Any(arg => arg.Equals("--apply", StringComparison.OrdinalIgnoreCase));
            bool cleanMode = args.Any(arg => arg.Equals("--clean", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("============================================");
            Console.WriteLine("Creating Effort Reporting Stored Procedures");
            Console.WriteLine($"Mode: {(executeMode ? "APPLY (permanent)" : "DRY-RUN (validates only)")}");
            if (cleanMode)
            {
                Console.WriteLine($"Clean Mode: {(executeMode ? "DELETE orphaned procedures" : "REPORT orphaned procedures")}");
            }
            Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("============================================");
            Console.WriteLine();

            var configuration = EffortScriptHelper.LoadConfiguration();
            string effortConnectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER");

            Console.WriteLine($"Target: {EffortScriptHelper.GetServerAndDatabase(effortConnectionString)} - [effort] schema");
            Console.WriteLine();

            try
            {
                // Verify prerequisites
                if (!VerifyPrerequisites(effortConnectionString))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: Prerequisites not met. Exiting.");
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                Console.WriteLine();

                // Create reporting stored procedures
                CreateReportingProcedures(effortConnectionString, executeMode, cleanMode);

                Console.WriteLine();
                Console.WriteLine("============================================");
                Console.WriteLine("✓ Reporting stored procedures completed successfully");
                Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("============================================");
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
                // Rethrow critical exceptions that should never be handled
                if (ex is OutOfMemoryException || ex is StackOverflowException || ex is System.Threading.ThreadAbortException || ex is OperationCanceledException)
                    throw;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static bool VerifyPrerequisites(string connectionString)
        {
            Console.WriteLine("Verifying prerequisites...");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Check if effort schema exists
            using var checkSchema = new SqlCommand("SELECT COUNT(*) FROM sys.schemas WHERE name = 'effort'", connection);
            int schemaExists = (int)checkSchema.ExecuteScalar();

            if (schemaExists == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ✗ [effort] schema does not exist");
                Console.WriteLine("  Run CreateEffortDatabase.cs first");
                Console.ResetColor();
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ [effort] schema exists");
            Console.ResetColor();

            // Check if key tables have data
            string[] requiredTables = { "Records", "Persons", "Courses" };
            foreach (var table in requiredTables)
            {
                using var checkData = new SqlCommand($"SELECT COUNT(*) FROM [effort].[{table}]", connection);
                int count = (int)checkData.ExecuteScalar();

                if (count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ WARNING: [effort].[{table}] has no data");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ [effort].[{table}] has {count:N0} records");
                    Console.ResetColor();
                }
            }

            return true;
        }

        // List of all managed stored procedures (18 total)
        static readonly HashSet<string> ManagedProcedures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Merit & Promotion Reports (6 procedures)
            "sp_merit_summary_report",
            "sp_merit_summary",
            "sp_merit_report",
            "sp_merit_multiyear",
            "sp_merit_average",
            "sp_merit_clinical_percent",
            // Department Analysis Reports (3 procedures)
            "sp_dept_activity_summary",
            "sp_dept_job_group_count",
            "sp_dept_summary",
            // Instructor Reports (5 procedures)
            "sp_instructor_activity",
            "sp_instructor_activity_exclude",
            "sp_instructor_evals",
            "sp_instructor_evals_multiyear",
            "sp_instructor_evals_average",
            // Other Reports (2 procedures)
            "sp_effort_general_report",
            "sp_zero_effort_check",
            // Banner Integration (1 procedure)
            "sp_search_banner_courses",
            // Percent Assignment Reports (1 procedure)
            "sp_percent_assignments_for_person"
        };

        static void CreateReportingProcedures(string connectionString, bool executeMode, bool cleanMode)
        {
            Console.WriteLine("Creating reporting stored procedures...");
            Console.WriteLine();

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            int successCount = 0;
            int failureCount = 0;
            var failedProcedures = new List<(string Name, string Error)>();

            try
            {
                // Merit & Promotion Reports (6 procedures)
                CreateProcedure(connection, transaction, "sp_merit_summary_report", GetMeritSummaryReportSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_summary", GetMeritSummarySql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_report", GetMeritReportSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_multiyear", GetMeritMultiyearSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_average", GetMeritAverageSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_clinical_percent", GetMeritClinicalPercentSql(), ref successCount, ref failureCount, failedProcedures);

                // Department Analysis Reports (3 procedures)
                CreateProcedure(connection, transaction, "sp_dept_activity_summary", GetDeptActivitySummarySql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_dept_job_group_count", GetDeptJobGroupCountSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_dept_summary", GetDeptSummarySql(), ref successCount, ref failureCount, failedProcedures);

                // Instructor Reports (5 procedures)
                CreateProcedure(connection, transaction, "sp_instructor_activity", GetInstructorActivitySql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_instructor_activity_exclude", GetInstructorActivityExcludeSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_instructor_evals", GetInstructorEvalsSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_instructor_evals_multiyear", GetInstructorEvalsMultiyearSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_instructor_evals_average", GetInstructorEvalsAverageSql(), ref successCount, ref failureCount, failedProcedures);

                // Other Reports (2 procedures)
                CreateProcedure(connection, transaction, "sp_effort_general_report", GetEffortGeneralReportSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_zero_effort_check", GetZeroEffortCheckSql(), ref successCount, ref failureCount, failedProcedures);

                // Banner Integration (1 procedure)
                CreateProcedure(connection, transaction, "sp_search_banner_courses", GetSearchBannerCoursesSql(), ref successCount, ref failureCount, failedProcedures);

                // Percent Assignment Reports (1 procedure)
                CreateProcedure(connection, transaction, "sp_percent_assignments_for_person", GetPercentAssignmentsForPersonSql(), ref successCount, ref failureCount, failedProcedures);

                Console.WriteLine();
                Console.WriteLine($"Summary: {successCount} succeeded, {failureCount} failed");

                if (failureCount > 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed procedures:");
                    foreach (var (name, error) in failedProcedures)
                    {
                        Console.WriteLine($"  ✗ {name}: {error}");
                    }
                    Console.ResetColor();
                    throw new InvalidOperationException($"Failed to create {failureCount} stored procedure(s). Rolling back transaction.");
                }

                // Check for orphaned procedures
                if (cleanMode)
                {
                    Console.WriteLine();
                    CleanOrphanedProcedures(connection, transaction, executeMode);
                }

                if (executeMode)
                {
                    transaction.Commit();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Transaction committed - procedures created permanently");
                    Console.ResetColor();
                }
                else
                {
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⊘ Transaction rolled back (dry-run mode)");
                    Console.WriteLine("Run with --apply to create procedures permanently");
                    Console.ResetColor();
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        static void CreateProcedure(SqlConnection connection, SqlTransaction transaction, string procName,
            string sql, ref int successCount, ref int failureCount,
            List<(string, string)> failedProcedures)
        {
            try
            {
                using var cmd = new SqlCommand(sql, connection, transaction);
                cmd.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Created: [effort].[{procName}]");
                Console.ResetColor();
                successCount++;
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ FAILED: [effort].[{procName}] - {ex.Message}");
                Console.ResetColor();
                failedProcedures.Add((procName, ex.Message));
                failureCount++;
            }
        }

        static void CleanOrphanedProcedures(SqlConnection connection, SqlTransaction transaction, bool executeMode)
        {
            Console.WriteLine("Checking for orphaned procedures...");

            // Query all procedures in [effort] schema
            string queryExistingProcs = @"
                SELECT ROUTINE_NAME
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = 'effort'
                    AND ROUTINE_TYPE = 'PROCEDURE'
                ORDER BY ROUTINE_NAME";

            var existingProcedures = new List<string>();
            using (var cmd = new SqlCommand(queryExistingProcs, connection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingProcedures.Add(reader.GetString(0));
                }
            }

            // Find orphaned procedures (exist in DB but not in managed list)
            // SAFETY: Only consider procedures with sp_* prefix as potential orphans
            // to avoid accidentally deleting procedures that this tool doesn't manage
            var orphanedProcedures = existingProcedures
                .Where(proc => proc.StartsWith("sp_", StringComparison.OrdinalIgnoreCase)
                               && !ManagedProcedures.Contains(proc))
                .ToList();

            Console.WriteLine($"  Found {existingProcedures.Count} total procedures in [effort] schema");
            Console.WriteLine($"  Managed: {ManagedProcedures.Count} procedures");
            Console.WriteLine($"  Orphaned: {orphanedProcedures.Count} procedures");

            if (orphanedProcedures.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Orphaned procedures (not in managed list):");
                foreach (var procName in orphanedProcedures)
                {
                    Console.WriteLine($"  ⚠ {procName}");
                }
                Console.ResetColor();

                if (executeMode)
                {
                    Console.WriteLine();
                    Console.WriteLine("Deleting orphaned procedures...");
                    int deletedCount = 0;

                    foreach (var procName in orphanedProcedures)
                    {
                        try
                        {
                            string dropSql = $"DROP PROCEDURE [effort].[{procName}]";
                            using var cmd = new SqlCommand(dropSql, connection, transaction);
                            cmd.ExecuteNonQuery();

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ Deleted: [effort].[{procName}]");
                            Console.ResetColor();
                            deletedCount++;
                        }
                        catch (SqlException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  ✗ Failed to delete [effort].[{procName}]: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Deleted {deletedCount} of {orphanedProcedures.Count} orphaned procedures");
                }
                else
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⊘ Dry-run mode - no procedures deleted");
                    Console.WriteLine("Run with --apply --clean to delete orphaned procedures");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No orphaned procedures found");
                Console.ResetColor();
            }
        }

        // ============================================
        // Merit & Promotion Report Procedures
        // ============================================

        static string GetMeritSummaryReportSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_summary_report]
    @TermCode INT,
    @Department CHAR(6) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Comprehensive merit summary report (Lairmore Report)
    -- Returns instructor details with effort pivoted by effort type
    -- This is the most comprehensive report, showing all course-level detail

    -- Create temp table to hold pivoted results
    CREATE TABLE #EffortPivot (
        MothraId VARCHAR(9),
        Instructor VARCHAR(250),
        Department CHAR(6),
        AcademicDepartment CHAR(6),
        JobGroupDescription VARCHAR(50),
        CourseId INT,
        Course VARCHAR(50),
        Units VARCHAR(25),
        Enrollment INT,
        Role CHAR(1),
        CLI INT DEFAULT 0,
        DIS INT DEFAULT 0,
        EXM INT DEFAULT 0,
        LAB INT DEFAULT 0,
        LEC INT DEFAULT 0,
        LED INT DEFAULT 0,
        SEM INT DEFAULT 0,
        VAR INT DEFAULT 0,
        AUT INT DEFAULT 0,
        FWK INT DEFAULT 0,
        INT INT DEFAULT 0,
        LAD INT DEFAULT 0,  -- L/D effort type
        PRJ INT DEFAULT 0,
        TUT INT DEFAULT 0,
        FAS INT DEFAULT 0,
        PBL INT DEFAULT 0,
        JLC INT DEFAULT 0,
        ACT INT DEFAULT 0,
        CBL INT DEFAULT 0,
        PRS INT DEFAULT 0,
        TBL INT DEFAULT 0,
        PRB INT DEFAULT 0,
        [T-D] INT DEFAULT 0,
        WVL INT DEFAULT 0,
        CON INT DEFAULT 0,
        DAL INT DEFAULT 0,  -- D/L effort type
        DSL INT DEFAULT 0,
        IND INT DEFAULT 0,
        LIS INT DEFAULT 0,
        LLA INT DEFAULT 0,
        TMP INT DEFAULT 0,
        WED INT DEFAULT 0,
        WRK INT DEFAULT 0
    );

    -- Insert base course data
    INSERT INTO #EffortPivot (
        MothraId, Instructor, Department, AcademicDepartment,
        JobGroupDescription, CourseId, Course, Units, Enrollment, Role
    )
    SELECT DISTINCT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE p.EffortDept
        END as Department,
        p.EffortDept as AcademicDepartment,
        CASE
            WHEN p.JobGroupId IN ('335', '341', '317') THEN p.Title
            WHEN p.JobGroupId IN ('210', '211', '212', '216', '221', '223', '225', '357', '928') THEN 'LECTURER'
            WHEN p.JobGroupId IN ('B0A', 'B24', 'B25', 'I15') THEN 'STAFF VET'
            WHEN p.JobGroupId = '729' THEN 'CE SPECIALIST'
            ELSE 'PROFESSOR/IR'
        END as JobGroupDescription,
        c.Id as CourseId,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        CAST(c.Units AS VARCHAR(25)) as Units,
        c.Enrollment,
        r.RoleId
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.TermCode = @TermCode
        AND (
            p.JobGroupId IN ('010', '011', '114', '311', '317', '335', '341', '124')
            OR p.JobGroupId IN ('210', '211', '212', '216', '221', '223', '225', '357', '928')
            OR p.JobGroupId IN ('B0A', 'B24', 'B25', 'I15')
            OR p.JobGroupId IN ('729', 'S56', 'S21')
        )
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
    GROUP BY
        up.MothraId,
        p.LastName,
        p.FirstName,
        p.EffortDept,
        p.ReportUnit,
        p.JobGroupId,
        p.Title,
        c.Id,
        c.SubjCode,
        c.CrseNumb,
        c.SeqNumb,
        c.Units,
        c.Enrollment,
        r.RoleId
    HAVING SUM(COALESCE(r.Weeks, r.Hours, 0)) > 0;

    -- Update effort type columns using PIVOT to aggregate effort values
    -- This automatically handles multiple records per effort type by summing them
    UPDATE ep
    SET CLI = ISNULL(pvt.CLI, 0),
        DIS = ISNULL(pvt.DIS, 0),
        EXM = ISNULL(pvt.EXM, 0),
        LAB = ISNULL(pvt.LAB, 0),
        LEC = ISNULL(pvt.LEC, 0),
        LED = ISNULL(pvt.LED, 0),
        SEM = ISNULL(pvt.SEM, 0),
        VAR = ISNULL(pvt.VAR, 0),
        AUT = ISNULL(pvt.AUT, 0),
        FWK = ISNULL(pvt.FWK, 0),
        INT = ISNULL(pvt.INT, 0),
        LAD = ISNULL(pvt.[L/D], 0),
        PRJ = ISNULL(pvt.PRJ, 0),
        TUT = ISNULL(pvt.TUT, 0),
        FAS = ISNULL(pvt.FAS, 0),
        PBL = ISNULL(pvt.PBL, 0),
        JLC = ISNULL(pvt.JLC, 0),
        ACT = ISNULL(pvt.ACT, 0),
        CBL = ISNULL(pvt.CBL, 0),
        PRS = ISNULL(pvt.PRS, 0),
        TBL = ISNULL(pvt.TBL, 0),
        PRB = ISNULL(pvt.PRB, 0),
        [T-D] = ISNULL(pvt.[T-D], 0),
        WVL = ISNULL(pvt.WVL, 0),
        CON = ISNULL(pvt.CON, 0),
        DAL = ISNULL(pvt.[D/L], 0),
        DSL = ISNULL(pvt.DSL, 0),
        IND = ISNULL(pvt.IND, 0),
        LIS = ISNULL(pvt.LIS, 0),
        LLA = ISNULL(pvt.LLA, 0),
        TMP = ISNULL(pvt.TMP, 0),
        WED = ISNULL(pvt.WED, 0),
        WRK = ISNULL(pvt.WRK, 0)
    FROM #EffortPivot ep
    LEFT JOIN (
        SELECT
            up.MothraId,
            r.CourseId,
            [CLI], [DIS], [EXM], [LAB], [LEC], [LED], [SEM], [VAR],
            [AUT], [FWK], [INT], [L/D], [PRJ], [TUT], [FAS], [PBL],
            [JLC], [ACT], [CBL], [PRS], [TBL], [PRB], [T-D], [WVL],
            [CON], [D/L], [DSL], [IND], [LIS], [LLA], [TMP], [WED], [WRK]
        FROM (
            SELECT
                up.MothraId,
                r.CourseId,
                r.EffortTypeId,
                COALESCE(r.Weeks, r.Hours, 0) AS Effort
            FROM [effort].[Records] r
            INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
            INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
            WHERE r.TermCode = @TermCode
                AND COALESCE(r.Weeks, r.Hours, 0) > 0
        ) AS SourceData
        PIVOT (
            SUM(Effort)
            FOR EffortType IN (
                [CLI], [DIS], [EXM], [LAB], [LEC], [LED], [SEM], [VAR],
                [AUT], [FWK], [INT], [L/D], [PRJ], [TUT], [FAS], [PBL],
                [JLC], [ACT], [CBL], [PRS], [TBL], [PRB], [T-D], [WVL],
                [CON], [D/L], [DSL], [IND], [LIS], [LLA], [TMP], [WED], [WRK]
            )
        ) AS PivotTable
    ) AS pvt ON ep.CourseId = pvt.CourseId AND ep.MothraId = pvt.MothraId;

    -- Return final results with optional department filter
    SELECT *
    FROM #EffortPivot
    WHERE @Department IS NULL
        OR Department = @Department
        OR Department IN ('All', 'CAHFS', 'VMTH')
    ORDER BY Department, Instructor, Course;

    -- Clean up
    DROP TABLE #EffortPivot;
END;
";
        }

        static string GetMeritSummarySql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_summary]
    @TermCode INT,
    @Department CHAR(6) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Merit summary aggregated by department and effort type
    -- Returns instructor details with effort totals for merit review

    SELECT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END as Department,
        CASE
            WHEN p.JobGroupId IN ('335', '341', '317') THEN p.Title
            ELSE 'PROFESSOR/IR'
        END as JobGroupDescription,
        p.PercentAdmin,
        r.EffortTypeId,
        SUM(COALESCE(r.Weeks, r.Hours, 0)) as TotalEffort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    WHERE r.TermCode = @TermCode
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
        AND (COALESCE(r.Weeks, r.Hours, 0) > 0)
    GROUP BY
        up.MothraId,
        p.LastName,
        p.FirstName,
        p.EffortDept,
        p.ReportUnit,
        p.JobGroupId,
        p.Title,
        p.PercentAdmin,
        r.EffortTypeId
    HAVING
        @Department IS NULL
        OR CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END = @Department
        OR CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END IN ('All', 'CAHFS', 'VMTH')
    ORDER BY
        Department,
        p.LastName,
        p.FirstName;
END;
";
        }

        static string GetMeritReportSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_report]
    @PersonId INT,
    @StartTermCode INT,
    @EndTermCode INT,
    @Department CHAR(6) = NULL,
    @Role CHAR(1) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Detailed merit report for a specific person across date range
    -- Returns effort breakdown by course and effort type

    SELECT
        r.TermCode,
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.EffortDept as Department,
        c.Id as CourseId,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Units,
        c.Enrollment,
        r.RoleId,
        r.EffortTypeId,
        COALESCE(r.Weeks, r.Hours, 0) as Effort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND (@Department IS NULL OR p.EffortDept = @Department)
        AND (@Role IS NULL OR r.RoleId = @Role)
    ORDER BY r.TermCode, c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetMeritMultiyearSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_multiyear]
    @PersonId INT,
    @StartTermCode INT,
    @EndTermCode INT,
    @ExcludeClinicalTerms NVARCHAR(MAX) = NULL,
    @ExcludeDidacticTerms NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Multi-year merit analysis with separate term exclusions for clinical vs didactic
    -- Used for sabbatical exclusions where clinical and didactic may differ

    DECLARE @ExcludeClinicalTable TABLE (TermCode INT);
    DECLARE @ExcludeDidacticTable TABLE (TermCode INT);

    -- Parse clinical term exclusions
    IF @ExcludeClinicalTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeClinicalTable (TermCode)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeClinicalTerms, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    -- Parse didactic term exclusions
    IF @ExcludeDidacticTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeDidacticTable (TermCode)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeDidacticTerms, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    SELECT
        r.TermCode,
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.EffortDept as Department,
        c.Id as CourseId,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Units,
        c.Enrollment,
        r.RoleId,
        r.EffortTypeId,
        COALESCE(r.Weeks, r.Hours, 0) as Effort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND c.Enrollment > 0
        AND p.VolunteerWos = 0
        -- Apply exclusions: different lists for clinical vs non-clinical
        AND (
            (r.EffortTypeId = 'CLI' AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeClinicalTable))
            OR
            (r.EffortTypeId <> 'CLI' AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeDidacticTable))
        )
        AND (COALESCE(r.Weeks, r.Hours, 0) > 0)
    ORDER BY r.TermCode, c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetMeritAverageSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_average]
    @TermCode INT,
    @Department CHAR(6) = NULL,
    @PersonId INT = NULL,
    @Role CHAR(1) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Department-wide merit averages aggregated by effort type
    -- Returns instructor details with effort broken down by effort type

    SELECT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END as Department,
        p.JobGroupId,
        p.Title as JobGroupDescription,
        p.PercentAdmin,
        r.EffortTypeId,
        SUM(COALESCE(r.Weeks, r.Hours, 0)) as TotalEffort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    WHERE r.TermCode = @TermCode
        AND (@Department IS NULL OR p.EffortDept = @Department OR p.ReportUnit LIKE '%' + @Department + '%')
        AND (@PersonId IS NULL OR p.PersonId = @PersonId)
        AND (@Role IS NULL OR r.RoleId = @Role)
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
        AND (COALESCE(r.Weeks, r.Hours, 0) > 0)
    GROUP BY
        up.MothraId,
        p.LastName,
        p.FirstName,
        p.EffortDept,
        p.ReportUnit,
        p.JobGroupId,
        p.Title,
        p.PercentAdmin,
        r.EffortTypeId
    ORDER BY
        Department,
        p.LastName,
        p.FirstName,
        r.EffortTypeId;
END;
";
        }

        static string GetMeritClinicalPercentSql()
        {
            // IMPORTANT: This procedure matches the legacy dbo.usp_getEffortReportMeritWithClinPercent
            // signature exactly for backward compatibility with MP Vote.
            // Parameters: @TermCode varchar(10), @type int (PercentAssignTypeId)
            // Output: MothraID, Instructor, JGDDesc, plus dynamic PIVOT columns for effort types
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_clinical_percent]
    @TermCode VARCHAR(10),
    @type INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Clinical percentage report for merit review (MP Vote integration)
    -- Matches legacy dbo.usp_getEffortReportMeritWithClinPercent exactly
    -- Returns instructors with clinical percentages for the academic year
    -- with effort data PIVOTed by effort session type

    DECLARE @ColumnName AS NVARCHAR(MAX);
    DECLARE @ColumnIsNulls AS NVARCHAR(MAX);
    DECLARE @DynamicPivotQuery AS NVARCHAR(MAX);
    DECLARE @start DATE, @end DATE;

    -- Get end date of academic year (June 30 of the year in term code)
    SET @end = CAST(RIGHT(@TermCode, 4) + '-06-30' AS DATE);
    -- Get start date of academic year (July 1 of previous year)
    SET @start = DATEADD(YEAR, -1, DATEADD(DAY, 1, @end));

    -- Get distinct values of effort type to turn into columns via PIVOT
    -- ColumnNames will contain [CLI],[DIS],[L/D]...etc.
    -- ColumnIsNulls will contain ISNULL([CLI],0) AS [CLI], ISNULL([DIS],0) AS [DIS]...etc.
    SELECT @ColumnName = ISNULL(@ColumnName + ',', '') + QUOTENAME(EffortTypeId),
           @ColumnIsNulls = ISNULL(@ColumnIsNulls + ', ', '') +
               'ISNULL(' + QUOTENAME(EffortTypeId) + ',0) AS ' + QUOTENAME(EffortTypeId)
    FROM (SELECT DISTINCT EffortTypeId FROM [effort].[Records]) AS efforttype;

    -- Create temp table for effort report data
    CREATE TABLE #EffortReport(
        [MothraID] VARCHAR(9) NULL,
        [Instructor] VARCHAR(250) NULL,
        [JGDDesc] VARCHAR(50) NULL,
        [EffortType] VARCHAR(3) NULL,
        [Effort] INT NULL
    );

    -- Create temp table for eligible instructors
    CREATE TABLE #Instructors(
        [MothraID] VARCHAR(9) NULL,
        [PersonId] INT NULL,
        [Instructor] VARCHAR(250) NULL,
        [JGDDesc] VARCHAR(50) NULL
    );

    -- Get all faculty in an eligible job group at least one term this academic year
    -- who also have a clinical percentage assignment for the specified type
    INSERT INTO #Instructors(MothraID, PersonId, Instructor, JGDDesc)
    SELECT DISTINCT
        up.MothraId,
        p.PersonId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName),
        CASE
            WHEN p.JobGroupId IN ('335', '341', '317') THEN p.Title
            ELSE 'PROFESSOR/IR'
        END
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[TermStatus] ts ON r.TermCode = ts.TermCode
    WHERE ts.TermCode / 100 = CAST(RIGHT(@TermCode, 4) AS INT)  -- All terms in academic year
        -- Job group filtering matching legacy fn_checkJobGroupAndEffortCode:
        -- Include job groups: 010, 011, 114, 311, 317, 335, 341
        -- Include 124 with effort title code 1898 (acting prof)
        -- Include S56 with effort title code 001067 (academic administrators)
        AND (
            p.JobGroupId IN ('010', '011', '114', '311', '317', '335', '341')
            OR (p.JobGroupId = '124' AND RIGHT('00' + p.EffortTitleCode, 6) = '001898')
            OR (p.JobGroupId = 'S56' AND RIGHT('00' + p.EffortTitleCode, 6) = '001067')
        )
        -- Filter to instructors with clinical percentage that applies during this academic year
        AND p.PersonId IN (
            SELECT pct.PersonId
            FROM [effort].[Percentages] pct
            WHERE pct.PercentAssignTypeId = @type
                AND pct.Percentage > 0
                AND pct.StartDate <= @end
                AND (pct.EndDate IS NULL OR pct.EndDate >= @start)
        );

    -- Get all effort for the academic year for these instructors
    INSERT INTO #EffortReport (MothraID, Instructor, JGDDesc, EffortType, Effort)
    SELECT
        I.MothraID,
        I.Instructor,
        I.JGDDesc,
        r.EffortTypeId,
        ISNULL(r.Weeks, ISNULL(r.Hours, 0))
    FROM [effort].[Records] r
    INNER JOIN #Instructors I ON r.PersonId = I.PersonId
    INNER JOIN [effort].[TermStatus] ts ON r.TermCode = ts.TermCode
    WHERE ts.TermCode / 100 = CAST(RIGHT(@TermCode, 4) AS INT);  -- All terms in academic year

    -- Build dynamic pivot query matching legacy output format exactly
    SET @DynamicPivotQuery =
    'SELECT [MothraID], [Instructor], [JGDDesc],
    ' + @ColumnIsNulls + '
    FROM #EffortReport
    PIVOT
    (
    SUM(Effort)
    FOR [EffortType] IN (' + @ColumnName + ')
    ) AS p
    ORDER BY JGDDesc, Instructor';

    EXEC sp_executesql @DynamicPivotQuery;

    -- Clean up
    DROP TABLE #EffortReport;
    DROP TABLE #Instructors;
END;
";
        }

        // ============================================
        // Department Analysis Procedures
        // ============================================

        static string GetDeptActivitySummarySql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_dept_activity_summary]
    @Department CHAR(6),
    @TermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Department activity breakdown by effort type

    SELECT
        r.EffortTypeId,
        COUNT(r.Id) as ActivityCount,
        SUM(CASE WHEN r.Hours IS NOT NULL THEN r.Hours ELSE 0 END) as TotalHours,
        SUM(CASE WHEN r.Weeks IS NOT NULL THEN r.Weeks ELSE 0 END) as TotalWeeks,
        COUNT(DISTINCT r.PersonId) as InstructorCount,
        COUNT(DISTINCT r.CourseId) as CourseCount
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    WHERE p.EffortDept = @Department
        AND r.TermCode = @TermCode
    GROUP BY r.EffortTypeId
    ORDER BY r.EffortTypeId;
END;
";
        }

        static string GetDeptJobGroupCountSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_dept_job_group_count]
    @Department CHAR(6),
    @TermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Job group distribution by department for a specific term

    SELECT
        p.JobGroupId,
        p.Title,
        COUNT(DISTINCT p.PersonId) as InstructorCount
    FROM [effort].[Persons] p
    WHERE p.EffortDept = @Department
        AND p.TermCode = @TermCode
    GROUP BY p.JobGroupId, p.Title
    ORDER BY COUNT(DISTINCT p.PersonId) DESC, p.JobGroupId;
END;
";
        }

        static string GetDeptSummarySql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_dept_summary]
    @TermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Department overview for administrators

    SELECT
        p.EffortDept,
        COUNT(DISTINCT p.PersonId) as InstructorCount,
        COUNT(r.Id) as TotalRecords,
        COUNT(DISTINCT r.CourseId) as CourseCount,
        SUM(CASE WHEN r.Hours IS NOT NULL THEN r.Hours ELSE 0 END) as TotalHours,
        SUM(CASE WHEN r.Weeks IS NOT NULL THEN r.Weeks ELSE 0 END) as TotalWeeks
    FROM [effort].[Persons] p
    LEFT JOIN [effort].[Records] r ON p.PersonId = r.PersonId AND p.TermCode = r.TermCode
    WHERE p.TermCode = @TermCode
    GROUP BY p.EffortDept
    ORDER BY p.EffortDept;
END;
";
        }

        // ============================================
        // Instructor Report Procedures
        // ============================================

        static string GetInstructorActivitySql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_activity]
    @PersonId INT,
    @TermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Show teaching activity for specific instructor and term

    SELECT
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Crn,
        r.EffortTypeId,
        r.Hours,
        r.Weeks,
        c.Enrollment,
        r.RoleId
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode = @TermCode
    ORDER BY c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetInstructorActivityExcludeSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_activity_exclude]
    @PersonId INT,
    @StartTermCode INT,
    @EndTermCode INT,
    @ExcludeTerms NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Instructor activity across date range with term exclusions (e.g., sabbaticals)

    DECLARE @ExcludeTermTable TABLE (TermCode INT);

    IF @ExcludeTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTermTable (TermCode)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeTerms, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    SELECT
        r.TermCode,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Crn,
        r.EffortTypeId,
        r.Hours,
        r.Weeks,
        c.Enrollment,
        r.RoleId
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeTermTable)
    ORDER BY r.TermCode, c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetInstructorEvalsSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_evals]
    @PersonId INT,
    @TermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Instructor evaluations report for a single term
    -- Returns course activity that can be joined with evaluation system data

    SELECT
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Crn,
        r.EffortTypeId,
        c.Enrollment,
        r.Hours,
        r.Weeks,
        r.RoleId
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode = @TermCode
    ORDER BY c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetInstructorEvalsMultiyearSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_evals_multiyear]
    @PersonId INT,
    @StartTermCode INT,
    @EndTermCode INT,
    @ExcludeTerms NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Multi-year instructor course activity with term exclusions
    -- Returns course-level data that can be joined with evaluation system

    DECLARE @ExcludeTermTable TABLE (TermCode INT);

    IF @ExcludeTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTermTable (TermCode)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeTerms, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    SELECT
        r.TermCode,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Crn,
        r.EffortTypeId,
        r.RoleId,
        c.Enrollment,
        r.Hours,
        r.Weeks
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeTermTable)
        AND c.Enrollment > 0
    ORDER BY r.TermCode, c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetInstructorEvalsAverageSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_evals_average]
    @PersonId INT,
    @StartTermCode INT,
    @EndTermCode INT,
    @ExcludeTerms NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Instructor evaluation averages with term exclusions (e.g., sabbaticals)

    DECLARE @ExcludeTermTable TABLE (TermCode INT);

    IF @ExcludeTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTermTable (TermCode)
        SELECT CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeTerms, ',')
        WHERE LTRIM(RTRIM(value)) <> '';
    END

    SELECT
        AVG(CAST(c.Enrollment AS FLOAT)) as AvgEnrollment,
        AVG(CASE WHEN r.Hours IS NOT NULL THEN CAST(r.Hours AS FLOAT) ELSE NULL END) as AvgHours,
        AVG(CASE WHEN r.Weeks IS NOT NULL THEN CAST(r.Weeks AS FLOAT) ELSE NULL END) as AvgWeeks,
        COUNT(DISTINCT r.TermCode) as TermCount,
        COUNT(DISTINCT c.Id) as CourseCount,
        COUNT(r.Id) as TotalRecords
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.PersonId = @PersonId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeTermTable);
END;
";
        }

        // ============================================
        // Other Report Procedures
        // ============================================

        static string GetEffortGeneralReportSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_effort_general_report]
    @TermCode INT,
    @Department CHAR(6) = NULL,
    @PersonId INT = NULL,
    @Role CHAR(1) = NULL,
    @JobGroupId CHAR(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Comprehensive effort overview for a term with optional filtering
    -- Returns all effort records with instructor, course, and session details

    SELECT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.JobGroupId,
        p.EffortDept as Department,
        c.Id as CourseId,
        c.SubjCode + ' ' + c.CrseNumb + '-' + c.SeqNumb as Course,
        c.Crn,
        c.Units,
        c.Enrollment,
        r.RoleId,
        r.EffortTypeId,
        r.Hours,
        r.Weeks
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE r.TermCode = @TermCode
        AND (@Department IS NULL OR p.EffortDept = @Department OR p.ReportUnit = @Department)
        AND (@PersonId IS NULL OR p.PersonId = @PersonId)
        AND (@Role IS NULL OR r.RoleId = @Role)
        AND (@JobGroupId IS NULL OR p.JobGroupId = @JobGroupId)
        AND c.Enrollment > 0
    ORDER BY p.EffortDept, p.LastName, p.FirstName, c.SubjCode, c.CrseNumb, c.SeqNumb;
END;
";
        }

        static string GetZeroEffortCheckSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_zero_effort_check]
    @TermCode INT,
    @Department CHAR(6) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Identifies instructors with courses assigned but zero effort recorded
    -- Critical for data validation before closing a term

    SELECT DISTINCT
        up.MothraId,
        p.FirstName,
        p.LastName,
        p.MiddleInitial,
        p.EffortDept,
        p.EffortVerified
    FROM [effort].[Persons] p
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Records] r ON p.PersonId = r.PersonId AND p.TermCode = r.TermCode
    WHERE r.TermCode = @TermCode
        AND r.Hours = 0
        AND r.Weeks = 0
        AND (@Department IS NULL OR p.EffortDept = @Department)
    ORDER BY p.EffortDept, p.LastName, p.FirstName;
END;
";
        }

        // ============================================
        // Banner Integration Procedures
        // ============================================

        static string GetSearchBannerCoursesSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_search_banner_courses]
    @TermCode VARCHAR(6),
    @SubjCode VARCHAR(4) = NULL,
    @CrseNumb VARCHAR(5) = NULL,
    @SeqNumb VARCHAR(3) = NULL,
    @Crn VARCHAR(5) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Search Banner courses directly via linked server
    -- Based on legacy usp_get_foreign_course but returns multiple results
    -- Allows flexible searching by any combination of parameters

    -- Validate TermCode format (must be exactly 6 digits)
    IF @TermCode IS NULL OR @TermCode LIKE '%[^0-9]%' OR LEN(@TermCode) <> 6
    BEGIN
        RAISERROR('TermCode must be a 6-digit number', 16, 1);
        RETURN;
    END

    -- Require at least one search parameter to prevent returning all courses
    IF @SubjCode IS NULL AND @CrseNumb IS NULL AND @SeqNumb IS NULL AND @Crn IS NULL
    BEGIN
        RAISERROR('At least one search parameter (SubjCode, CrseNumb, SeqNumb, or Crn) is required', 16, 1);
        RETURN;
    END

    -- Validate inputs to prevent injection and malformed queries
    IF @SubjCode IS NOT NULL AND @SubjCode LIKE '%[^A-Za-z0-9]%'
    BEGIN
        RAISERROR('SubjCode must be alphanumeric', 16, 1);
        RETURN;
    END
    IF @CrseNumb IS NOT NULL AND @CrseNumb LIKE '%[^A-Za-z0-9]%'
    BEGIN
        RAISERROR('CrseNumb must be alphanumeric', 16, 1);
        RETURN;
    END
    IF @SeqNumb IS NOT NULL AND @SeqNumb LIKE '%[^0-9]%'
    BEGIN
        RAISERROR('SeqNumb must be numeric', 16, 1);
        RETURN;
    END
    IF @Crn IS NOT NULL AND @Crn LIKE '%[^0-9]%'
    BEGIN
        RAISERROR('Crn must be numeric', 16, 1);
        RETURN;
    END

    DECLARE @bannerCmd NVARCHAR(MAX);
    DECLARE @whereClause NVARCHAR(1000) = '';

    -- Build dynamic WHERE clause based on provided parameters
    IF @SubjCode IS NOT NULL
        SET @whereClause = @whereClause + ' AND SSBSECT_SUBJ_CODE = ''''' + REPLACE(@SubjCode, '''', '''''''''') + ''''' ';
    IF @CrseNumb IS NOT NULL
        SET @whereClause = @whereClause + ' AND SSBSECT_CRSE_NUMB = ''''' + REPLACE(@CrseNumb, '''', '''''''''') + ''''' ';
    IF @SeqNumb IS NOT NULL
        SET @whereClause = @whereClause + ' AND SSBSECT_SEQ_NUMB = ''''' + REPLACE(@SeqNumb, '''', '''''''''') + ''''' ';
    IF @Crn IS NOT NULL
        SET @whereClause = @whereClause + ' AND SSBSECT_CRN = ''''' + REPLACE(@Crn, '''', '''''''''') + ''''' ';

    SET @bannerCmd = '
        SELECT DISTINCT
            SSBSECT_CRN as Crn,
            SSBSECT_SUBJ_CODE as SubjCode,
            SSBSECT_CRSE_NUMB as CrseNumb,
            SSBSECT_SEQ_NUMB as SeqNumb,
            SCBCRSE_TITLE as Title,
            SSBSECT_ENRL as Enrollment,
            DECODE(NVL(SCBCRSE_CREDIT_HR_IND,''''F''''),''''F'''',''''F'''',''''V'''') as UnitType,
            NVL(SCBCRSE_CREDIT_HR_LOW,0.0) as UnitLow,
            NVL(SCBCRSE_CREDIT_HR_HIGH,0.0) as UnitHigh,
            SCBCRSE_DEPT_CODE as DeptCode
        FROM SATURN.SSBSECT, SATURN.SCBCRSE
        WHERE SSBSECT_TERM_CODE = ''''' + @TermCode + '''''
            AND SCBCRSE_SUBJ_CODE = SSBSECT_SUBJ_CODE
            AND SCBCRSE_CRSE_NUMB = SSBSECT_CRSE_NUMB
            AND SCBCRSE_EFF_TERM = (
                SELECT MAX(SCBCRSE_EFF_TERM) FROM SATURN.SCBCRSE
                WHERE SCBCRSE_SUBJ_CODE = SSBSECT_SUBJ_CODE
                AND SCBCRSE_CRSE_NUMB = SSBSECT_CRSE_NUMB
            )' + @whereClause + '
        ORDER BY SSBSECT_SUBJ_CODE, SSBSECT_CRSE_NUMB, SSBSECT_SEQ_NUMB
        FETCH FIRST 100 ROWS ONLY';

    EXEC('SELECT * FROM OPENQUERY(UCDBanner, ''' + @bannerCmd + ''')');
END;
";
        }

        // ============================================
        // Percent Assignment Procedures
        // ============================================

        static string GetPercentAssignmentsForPersonSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_percent_assignments_for_person]
    @MothraId VARCHAR(9),
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Returns raw percent assignment data for a person within a date range
    -- Used by PercentageService.GetAveragePercentsByTypeAsync to calculate weighted averages
    -- Returns all percentage assignments that overlap with the specified date range

    SELECT
        p.Id,
        p.PersonId,
        p.PercentAssignTypeId,
        t.Class AS TypeClass,
        t.Name AS TypeName,
        p.UnitId,
        u.Name AS UnitName,
        p.Modifier,
        p.Percentage,
        p.StartDate,
        p.EndDate,
        p.Comment,
        p.Compensated,
        p.AcademicYear
    FROM [effort].[Percentages] p
    INNER JOIN [users].[Person] per ON p.PersonId = per.PersonId
    INNER JOIN [effort].[PercentAssignTypes] t ON p.PercentAssignTypeId = t.Id
    LEFT JOIN [effort].[Units] u ON p.UnitId = u.Id
    WHERE per.MothraId = @MothraId
      AND p.StartDate <= @EndDate
      AND (p.EndDate IS NULL OR p.EndDate >= @StartDate)
      AND p.Percentage > 0
    ORDER BY t.Class, p.StartDate;
END;
";
        }
    }
}
