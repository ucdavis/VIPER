// ============================================
// Script: CreateEffortReportingProcedures.cs
// Description: Create modernized reporting stored procedures in Effort database
// Author: VIPER2 Development Team
// Date: 2025-11-14
// ============================================
// This script creates 23 reporting stored procedures and 1 shared function for the Effort database.
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

        // List of all managed stored procedures and functions
        static readonly HashSet<string> ManagedProcedures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Shared Functions (1 function)
            "fn_qualified_job_groups",
            // Merit & Promotion Reports (11 procedures)
            "sp_merit_summary_report",
            "sp_merit_summary",
            "sp_merit_report",
            "sp_merit_multiyear",
            "sp_merit_average",
            "sp_merit_clinical_percent",
            "sp_person_learning_activities",
            "sp_dept_activity_total_exclude",
            "sp_dept_count_by_job_group_exclude",
            "sp_instructor_evals_average_exclude",
            "sp_get_sabbatical_terms",
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
                // Shared Functions (must be created before procedures that reference them)
                CreateProcedure(connection, transaction, "fn_qualified_job_groups", GetQualifiedJobGroupsFunctionSql(), ref successCount, ref failureCount, failedProcedures);

                // Merit & Promotion Reports (11 procedures)
                CreateProcedure(connection, transaction, "sp_merit_summary_report", GetMeritSummaryReportSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_summary", GetMeritSummarySql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_report", GetMeritReportSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_multiyear", GetMeritMultiyearSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_average", GetMeritAverageSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_merit_clinical_percent", GetMeritClinicalPercentSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_person_learning_activities", GetPersonLearningActivitiesSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_dept_activity_total_exclude", GetDeptActivityTotalExcludeSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_dept_count_by_job_group_exclude", GetDeptCountByJobGroupExcludeSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_instructor_evals_average_exclude", GetInstructorEvalsAverageExcludeSql(), ref successCount, ref failureCount, failedProcedures);
                CreateProcedure(connection, transaction, "sp_get_sabbatical_terms", GetSabbaticalTermsSql(), ref successCount, ref failureCount, failedProcedures);

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

            // Query all procedures and functions in [effort] schema
            string queryExistingProcs = @"
                SELECT ROUTINE_NAME, ROUTINE_TYPE
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = 'effort'
                    AND ROUTINE_TYPE IN ('PROCEDURE', 'FUNCTION')
                ORDER BY ROUTINE_NAME";

            var existingRoutines = new List<(string Name, string Type)>();
            using (var cmd = new SqlCommand(queryExistingProcs, connection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingRoutines.Add((reader.GetString(0), reader.GetString(1)));
                }
            }

            // Find orphaned routines (exist in DB but not in managed list)
            // SAFETY: Only consider sp_* and fn_* prefixes as potential orphans
            // to avoid accidentally deleting routines that this tool doesn't manage
            var orphanedRoutines = existingRoutines
                .Where(r => (r.Name.StartsWith("sp_", StringComparison.OrdinalIgnoreCase)
                             || r.Name.StartsWith("fn_", StringComparison.OrdinalIgnoreCase))
                            && !ManagedProcedures.Contains(r.Name))
                .ToList();

            Console.WriteLine($"  Found {existingRoutines.Count} total routines in [effort] schema");
            Console.WriteLine($"  Managed: {ManagedProcedures.Count} routines");
            Console.WriteLine($"  Orphaned: {orphanedRoutines.Count} routines");

            if (orphanedRoutines.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Orphaned routines (not in managed list):");
                foreach (var (name, type) in orphanedRoutines)
                {
                    Console.WriteLine($"  ⚠ {name} ({type})");
                }
                Console.ResetColor();

                if (executeMode)
                {
                    Console.WriteLine();
                    Console.WriteLine("Deleting orphaned routines...");
                    int deletedCount = 0;

                    foreach (var (name, type) in orphanedRoutines)
                    {
                        try
                        {
                            string dropSql = type == "FUNCTION"
                                ? $"DROP FUNCTION [effort].[{name}]"
                                : $"DROP PROCEDURE [effort].[{name}]";
                            using var cmd = new SqlCommand(dropSql, connection, transaction);
                            cmd.ExecuteNonQuery();

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ Deleted: [effort].[{name}]");
                            Console.ResetColor();
                            deletedCount++;
                        }
                        catch (SqlException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  ✗ Failed to delete [effort].[{name}]: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Deleted {deletedCount} of {orphanedRoutines.Count} orphaned routines");
                }
                else
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⊘ Dry-run mode - no routines deleted");
                    Console.WriteLine("Run with --apply --clean to delete orphaned routines");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ No orphaned routines found");
                Console.ResetColor();
            }
        }

        // ============================================
        // Shared Functions
        // ============================================

        /// <summary>
        /// Inline table-valued function that returns the set of job groups qualifying
        /// for effort reports. Replaces the legacy scalar UDF fn_checkJobGroupAndEffortCode.
        /// Using an iTVF instead of a scalar UDF allows SQL Server to inline the function
        /// into the query plan, avoiding the row-by-row execution penalty of scalar UDFs.
        /// </summary>
        static string GetQualifiedJobGroupsFunctionSql()
        {
            return @"
CREATE OR ALTER FUNCTION [effort].[fn_qualified_job_groups]()
RETURNS TABLE
AS RETURN (
    SELECT JobGroupId, EffortTitleCode
    FROM (VALUES
        ('010', NULL),      -- I&R Professor
        ('011', NULL),      -- I&R Professor
        ('114', NULL),      -- Acting Professor, Senate
        ('311', NULL),      -- Professor in Residence
        ('317', NULL),      -- Clin X
        ('335', NULL),      -- Adjunct Professors
        ('341', NULL),      -- HS Clinical Professor
        ('124', '001898'),  -- Acting Professor, Non-Senate
        ('S56', '001067')   -- Academic Administrator
    ) AS v(JobGroupId, EffortTitleCode)
);
";
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
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
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
    @PersonId INT = NULL,
    @StartTermCode INT,
    @EndTermCode INT,
    @Department CHAR(6) = NULL,
    @Role CHAR(1) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Detailed merit report across date range, optionally filtered to a specific person
    -- Returns effort breakdown by course and effort type

    SELECT
        r.TermCode,
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.EffortDept as Department,
        c.Id as CourseId,
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
        c.Units,
        c.Enrollment,
        CAST(r.RoleId as varchar(50)) as RoleId,
        r.EffortTypeId,
        COALESCE(r.Weeks, r.Hours, 0) as Effort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    WHERE (@PersonId IS NULL OR r.PersonId = @PersonId)
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND (@Department IS NULL OR p.EffortDept = @Department)
        AND (@Role IS NULL OR CAST(r.RoleId as varchar(50)) = @Role)
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
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeClinicalTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Parse didactic term exclusions
    IF @ExcludeDidacticTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeDidacticTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeDidacticTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    SELECT
        r.TermCode,
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.EffortDept as Department,
        c.Id as CourseId,
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
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
    -- Groups by job group description: 335/341/317 use dictionary title, others = PROFESSOR/IR

    SELECT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124', 'S56') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END as Department,
        p.JobGroupId,
        CASE
            WHEN p.JobGroupId IN ('335', '341', '317') THEN t.dvtTitle_JobGroup_Abbrev
            ELSE 'PROFESSOR/IR'
        END as JobGroupDescription,
        p.PercentAdmin,
        r.EffortTypeId,
        SUM(COALESCE(r.Weeks, r.Hours, 0)) as TotalEffort
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [dictionary].[dbo].[dvtTitle] t
        ON p.JobGroupId = t.dvtTitle_JobGroupID
        AND RIGHT('00' + p.EffortTitleCode, 6) = RIGHT('00' + t.dvtTitle_code, 6)
    WHERE r.TermCode = @TermCode
        AND (@Department IS NULL OR
            CASE
                WHEN p.JobGroupId IN ('010', '011', '114', '311', '124', 'S56') THEN p.EffortDept
                WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
                WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
                WHEN p.JobGroupId = '317' THEN 'VMTH'
                ELSE 'All'
            END = @Department)
        AND (@PersonId IS NULL OR p.PersonId = @PersonId)
        AND (@Role IS NULL OR r.RoleId = @Role)
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
        AND (COALESCE(r.Weeks, r.Hours, 0) > 0)
        -- Job group qualification (see effort.fn_qualified_job_groups)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
        )
    GROUP BY
        up.MothraId,
        p.LastName,
        p.FirstName,
        p.EffortDept,
        p.ReportUnit,
        p.JobGroupId,
        t.dvtTitle_JobGroup_Abbrev,
        p.PercentAdmin,
        r.EffortTypeId
    ORDER BY
        JobGroupDescription,
        Department,
        p.LastName,
        p.FirstName,
        r.EffortTypeId;
END;
";
        }

        static string GetMeritClinicalPercentSql()
        {
            // Used by MP Vote ColdFusion app
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_merit_clinical_percent]
    @TermCode VARCHAR(10),
    @type INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Clinical percentage report for merit review (MP Vote ColdFusion app)

    DECLARE @ColumnName AS NVARCHAR(MAX);
    DECLARE @ColumnIsNulls AS NVARCHAR(MAX);
    DECLARE @DynamicPivotQuery AS NVARCHAR(MAX);
    DECLARE @start DATE, @end DATE;
    DECLARE @AcademicYear INT;

    -- Extract academic year from term code
    -- Term codes are either 4 digits (YYYY) or 6 digits (YYYYTT where TT is term)
    -- For 6-digit codes like 202410, we need LEFT(4) to get 2024
    -- For 4-digit codes like 2024, we use the value directly
    SET @AcademicYear = CASE
        WHEN LEN(@TermCode) = 6 THEN CAST(LEFT(@TermCode, 4) AS INT)
        WHEN LEN(@TermCode) = 4 THEN CAST(@TermCode AS INT)
        ELSE CAST(RIGHT(@TermCode, 4) AS INT)  -- Fallback for unexpected formats
    END;

    -- Get end date of academic year (June 30 of the academic year)
    SET @end = CAST(CAST(@AcademicYear AS VARCHAR(4)) + '-06-30' AS DATE);
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
    WHERE ts.TermCode / 100 = @AcademicYear  -- All terms in academic year
        -- Job group qualification (see effort.fn_qualified_job_groups)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
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
    WHERE ts.TermCode / 100 = @AcademicYear;  -- All terms in academic year

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
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
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
            // Used by ColdFusion EIS.cfc (getPersonMPEffortWithExcludeTerms)
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_activity_exclude]
    @MothraId VARCHAR(9),
    @YearStart INT,
    @YearEnd INT,
    @Activity VARCHAR(3),
    @ExcludedTerms NVARCHAR(MAX) = NULL,
    @UseAcademicYear BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Used by ColdFusion EIS.cfc (getPersonMPEffortWithExcludeTerms)

    DECLARE @ExcludeTable TABLE (TermCode INT);
    DECLARE @StartTerm INT, @EndTerm INT;

    -- Parse excluded terms using TRY_CAST for defensive parsing
    IF @ExcludedTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludedTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Calculate term range based on academic year setting
    -- Academic year: Summer (YearStart*100+04) through Spring (YearEnd*100+03)
    -- Calendar year: All terms in the year range
    IF @UseAcademicYear = 1
    BEGIN
        SET @StartTerm = @YearStart * 100 + 04;  -- Summer term (e.g., 202404)
        SET @EndTerm = @YearEnd * 100 + 03;      -- Spring term (e.g., 202503)
    END
    ELSE
    BEGIN
        SET @StartTerm = @YearStart * 100;       -- Start of year (e.g., 202400)
        SET @EndTerm = (@YearEnd + 1) * 100 - 1; -- End of year (e.g., 202499)
    END

    -- Return aggregated weeks and hours for the activity type
    -- Matches legacy output: weeks, hours
    SELECT
        SUM(ISNULL(r.Weeks, 0)) AS weeks,
        SUM(ISNULL(r.Hours, 0)) AS hours
    FROM [effort].[Records] r
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    WHERE up.MothraId = @MothraId
        AND r.TermCode >= @StartTerm
        AND r.TermCode <= @EndTerm
        AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeTable)
        AND r.EffortTypeId = @Activity
        AND c.Enrollment > 0
        AND p.VolunteerWos = 0;
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
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
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
            // Used by ColdFusion MP apps for per-term evaluation details
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_evals_multiyear]
    @StartYear INT,
    @EndYear INT,
    @MothraId VARCHAR(9) = NULL,
    @ExcludedTerms NVARCHAR(MAX) = NULL,
    @UseAcademicYear BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Multi-year instructor evaluation data with term exclusions

    DECLARE @ExcludeTable TABLE (TermCode INT);
    DECLARE @MyEndYear INT = @EndYear - 1;

    -- Parse excluded terms using TRY_CAST for defensive parsing
    IF @ExcludedTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludedTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Query EvalHarvest database for per-term evaluation details
    -- Matches legacy output structure with course details from EvalHarvest
    SELECT
        ti.term_desc AS TermName,
        course_TermCode AS TermCode,
        ti.term_academic_year AS AcademicYear,
        CAST(LEFT(CAST(course_TermCode AS VARCHAR(6)), 4) AS INT) AS CalendarYear,
        people_MothraID AS MothraID,
        p.EffortDept AS Dept,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) AS Instructor,
        RTRIM(course_subj_code) + ' ' + RTRIM(course_crse_numb) +
            ' (' + ISNULL(bi.baseinfo_title, '') + ')' AS Course,
        CASE WHEN poa.poa_mailID IS NULL THEN 2 ELSE 1 END AS Role,
        quant_mean AS Average,
        quant_5_n AS n5,
        quant_4_n AS n4,
        quant_3_n AS n3,
        quant_2_n AS n2,
        quant_1_n AS n1,
        course_CRN AS CRN,
        course_subj_code AS SubjCode,
        course_crse_numb AS CourseNum,
        CAST(course_TermCode AS VARCHAR(6)) + course_subj_code + course_crse_numb +
            CASE WHEN course_facilitator_evalid = 0 THEN '0' ELSE '1' END AS CourseKey,
        p.ReportUnit,
        course_enrollment AS NumEnrolled,
        course_enrollment AS OriginalNumEnrolled,
        quant_respondants AS NumResponses,
        course_facilitator_evalid AS evalid
    FROM evalharvest.dbo.eh_questions
    INNER JOIN evalharvest.dbo.eh_quant ON quest_ID = quant_QuestionID_FK
    INNER JOIN evalharvest.dbo.eh_Courses ON quest_CRN = course_CRN
        AND quest_TermCode = course_termCode
        AND ISNULL(quest_facilitator_evalid, 0) = course_facilitator_evalid
    INNER JOIN evalharvest.dbo.eh_People ON quant_mailID = people_mailid
        AND people_TermCode = course_TermCode
    LEFT JOIN evalharvest.dbo.eh_POA poa ON course_CRN = poa_crn
        AND course_TermCode = poa_termcode
        AND people_mailid = poa_mailID
    LEFT JOIN Courses.dbo.baseinfo bi ON quest_TermCode = bi.baseinfo_term_code
        AND quest_CRN = bi.baseinfo_crn
    -- Join with Courses.dbo.terminfo for term name and academic year
    INNER JOIN Courses.dbo.terminfo ti ON CAST(course_TermCode AS VARCHAR(6)) = ti.term_code
    -- Join with effort.Persons for job group validation
    INNER JOIN [users].[Person] up ON people_MothraID = up.MothraId
    INNER JOIN [effort].[Persons] p ON up.PersonId = p.PersonId AND people_TermCode = p.TermCode
    WHERE (
            (@UseAcademicYear = 1 AND LEFT(ti.term_academic_year, 4) BETWEEN @StartYear AND @MyEndYear)
            OR (@UseAcademicYear = 0 AND course_TermCode >= @StartYear * 100 AND course_TermCode < (@EndYear + 1) * 100)
        )
        AND course_TermCode NOT IN (SELECT TermCode FROM @ExcludeTable)
        AND (@MothraId IS NULL OR people_MothraID = @MothraId)
        AND quant_mailID IS NOT NULL
        AND quest_overall = 1
        -- Job group qualification (see effort.fn_qualified_job_groups)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
        )
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
        AND quant_mean > 0
    ORDER BY course_TermCode, course_subj_code, course_facilitator_evalid, course_crse_numb, CourseKey, Course;
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
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludeTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
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
        RTRIM(c.SubjCode) + ' ' + RTRIM(c.CrseNumb) + '-' + RTRIM(c.SeqNumb) as Course,
        c.Crn,
        c.Units,
        c.Enrollment,
        CAST(r.RoleId as varchar(50)) as Role,
        r.EffortTypeId,
        r.Hours,
        r.Weeks
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[Courses] c ON r.CourseId = c.Id
    LEFT JOIN [effort].[Roles] rl ON r.RoleId = rl.Id
    WHERE r.TermCode = @TermCode
        AND (@Department IS NULL OR p.EffortDept = @Department OR p.ReportUnit = @Department)
        AND (@PersonId IS NULL OR p.PersonId = @PersonId)
        AND (@Role IS NULL OR r.RoleId = @Role)
        AND (@JobGroupId IS NULL OR p.JobGroupId = @JobGroupId)
        AND c.Enrollment > 0

    UNION ALL

    -- Include shared/cross-listed child courses with 0 effort
    SELECT
        up.MothraId,
        RTRIM(p.LastName) + ', ' + RTRIM(p.FirstName) as Instructor,
        p.JobGroupId,
        p.EffortDept as Department,
        child.Id as CourseId,
        RTRIM(child.SubjCode) + ' ' + RTRIM(child.CrseNumb) + '-' + RTRIM(child.SeqNumb) + '-(' + LEFT(cr.RelationshipType, 1) + ')' as Course,
        child.Crn,
        child.Units,
        child.Enrollment,
        CAST(r.RoleId as varchar(50)) as Role,
        r.EffortTypeId,
        0 as Hours,
        0 as Weeks
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[CourseRelationships] cr ON r.CourseId = cr.ParentCourseId
    INNER JOIN [effort].[Courses] child ON cr.ChildCourseId = child.Id
    WHERE r.TermCode = @TermCode
        AND (@Department IS NULL OR p.EffortDept = @Department OR p.ReportUnit = @Department)
        AND (@PersonId IS NULL OR p.PersonId = @PersonId)
        AND (@Role IS NULL OR r.RoleId = @Role)
        AND (@JobGroupId IS NULL OR p.JobGroupId = @JobGroupId)
        AND child.Enrollment > 0
        -- Anti-join: exclude child courses where instructor already has a direct effort record
        AND NOT EXISTS (
            SELECT 1 FROM [effort].[Records] r2
            WHERE r2.PersonId = r.PersonId
              AND r2.TermCode = r.TermCode
              AND r2.CourseId = child.Id
        )

    ORDER BY Department, Instructor, Course;
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
    -- Available for reporting integrations and external consumers
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

        // ============================================
        // Merit & Promotion - ColdFusion Integration Procedures
        // ============================================

        static string GetPersonLearningActivitiesSql()
        {
            // Used by ColdFusion MPVote and VMTH letter generation
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_person_learning_activities]
    @MothraId VARCHAR(9),
    @StartTermCode INT,
    @EndTermCode INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Returns DISTINCT session types (effort types) for an instructor within a term range
    -- Used by ColdFusion MPVote and VMTH letter generation

    SELECT DISTINCT
        r.EffortTypeId AS sessionType,
        et.Description AS sessionDescription
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    INNER JOIN [effort].[EffortTypes] et ON r.EffortTypeId = et.Id
    WHERE up.MothraId = @MothraId
        AND r.TermCode BETWEEN @StartTermCode AND @EndTermCode
        AND (COALESCE(r.Hours, 0) > 0 OR COALESCE(r.Weeks, 0) > 0)
    ORDER BY et.Description;
END;
";
        }

        static string GetDeptActivityTotalExcludeSql()
        {
            // Used by ColdFusion EIS.cfc (getDepartmentMPEffortExcludingTerms)
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_dept_activity_total_exclude]
    @MothraId VARCHAR(9),
    @YearStart INT,
    @YearEnd INT,
    @Activity VARCHAR(3),
    @ExcludedTerms NVARCHAR(MAX) = NULL,
    @AllDepts BIT = 0,
    @UseAcademicYear BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Used by ColdFusion EIS.cfc (getDepartmentMPEffortExcludingTerms)

    DECLARE @ExcludeTable TABLE (TermCode INT);
    DECLARE @StartTerm INT, @EndTerm INT;
    DECLARE @PersonDept CHAR(6), @PersonJobGroupId CHAR(3), @PersonReportUnit VARCHAR(50);
    DECLARE @NamedDept VARCHAR(8), @NamedJobDescription VARCHAR(50);

    -- Parse excluded terms
    IF @ExcludedTerms IS NOT NULL AND @ExcludedTerms <> ''
    BEGIN
        INSERT INTO @ExcludeTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludedTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Calculate term range based on academic year setting
    IF @UseAcademicYear = 1
    BEGIN
        SET @StartTerm = @YearStart * 100 + 04;  -- Summer term (e.g., 202404)
        SET @EndTerm = @YearEnd * 100 + 03;      -- Spring term (e.g., 202503)
    END
    ELSE
    BEGIN
        SET @StartTerm = @YearStart * 100;       -- Start of year (e.g., 202400)
        SET @EndTerm = (@YearEnd + 1) * 100 - 1; -- End of year (e.g., 202499)
    END

    -- Get person's department, job group, and report unit (most recent within range)
    SELECT TOP 1
        @PersonDept = p.EffortDept,
        @PersonJobGroupId = p.JobGroupId,
        @PersonReportUnit = p.ReportUnit
    FROM [effort].[Persons] p
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    WHERE up.MothraId = @MothraId
        AND p.TermCode >= @StartTerm
        AND p.TermCode <= @EndTerm
    ORDER BY p.TermCode DESC;

    -- If no records in range, try with extended end range (legacy behavior)
    IF @PersonDept IS NULL
    BEGIN
        SELECT TOP 1
            @PersonDept = p.EffortDept,
            @PersonJobGroupId = p.JobGroupId,
            @PersonReportUnit = p.ReportUnit
        FROM [effort].[Persons] p
        INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
        WHERE up.MothraId = @MothraId
            AND p.TermCode >= @StartTerm
            AND p.TermCode <= @EndTerm + 100
        ORDER BY p.TermCode DESC;
    END

    -- Calculate named dept (equivalent to fn_getEffortDept)
    SET @NamedDept = CASE
        WHEN @PersonJobGroupId IN ('010', '011', '114', '311', '124') THEN @PersonDept
        WHEN @PersonReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
        WHEN @PersonReportUnit LIKE '%WHC%' THEN 'WHC'
        WHEN @PersonJobGroupId = '317' THEN 'VMTH'
        ELSE 'All'
    END;

    -- Calculate named job description (equivalent to fn_getEffortTitle)
    SET @NamedJobDescription = CASE
        WHEN @PersonJobGroupId IN ('335', '341', '317') THEN
            (SELECT TOP 1 p.Title FROM [effort].[Persons] p
             INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
             WHERE up.MothraId = @MothraId ORDER BY p.TermCode DESC)
        WHEN @PersonJobGroupId IN ('210', '211', '212', '216', '221', '223', '225', '357', '928') THEN 'LECTURER'
        WHEN @PersonJobGroupId IN ('B0A', 'B24', 'B25', 'I15') THEN 'STAFF VET'
        WHEN @PersonJobGroupId = '729' THEN 'CE SPECIALIST'
        ELSE 'PROFESSOR/IR'
    END;

    -- Create temp table for effort report data
    CREATE TABLE #EffortReport (
        TermCode INT,
        MothraID VARCHAR(9),
        Dept VARCHAR(8),
        JGDDesc VARCHAR(50),
        Activity INT DEFAULT 0
    );

    -- Insert qualifying instructors with their effort
    INSERT INTO #EffortReport (TermCode, MothraID, Dept, JGDDesc)
    SELECT DISTINCT
        r.TermCode,
        up.MothraId,
        CASE
            WHEN p.JobGroupId IN ('010', '011', '114', '311', '124') THEN p.EffortDept
            WHEN p.ReportUnit LIKE '%CAHFS%' THEN 'CAHFS'
            WHEN p.ReportUnit LIKE '%WHC%' THEN 'WHC'
            WHEN p.JobGroupId = '317' THEN 'VMTH'
            ELSE 'All'
        END,
        CASE
            WHEN p.JobGroupId IN ('335', '341', '317') THEN p.Title
            WHEN p.JobGroupId IN ('210', '211', '212', '216', '221', '223', '225', '357', '928') THEN 'LECTURER'
            WHEN p.JobGroupId IN ('B0A', 'B24', 'B25', 'I15') THEN 'STAFF VET'
            WHEN p.JobGroupId = '729' THEN 'CE SPECIALIST'
            ELSE 'PROFESSOR/IR'
        END
    FROM [effort].[Records] r
    INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
    INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
    WHERE r.TermCode >= @StartTerm
        AND r.TermCode <= @EndTerm
        AND r.TermCode NOT IN (SELECT TermCode FROM @ExcludeTable)
        AND r.EffortTypeId = @Activity
        -- Job group qualification (see effort.fn_qualified_job_groups)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
        )
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
    GROUP BY r.TermCode, up.MothraId, p.JobGroupId, p.EffortDept, p.ReportUnit, p.Title
    HAVING SUM(COALESCE(r.Weeks, r.Hours, 0)) > 0;

    -- Calculate the sum of activity for each person and term
    UPDATE #EffortReport
    SET Activity = (
        SELECT SUM(COALESCE(r.Weeks, r.Hours, 0))
        FROM [effort].[Records] r
        INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
        INNER JOIN [users].[Person] up ON p.PersonId = up.PersonId
        WHERE r.EffortTypeId = @Activity
            AND r.TermCode = #EffortReport.TermCode
            AND up.MothraId = #EffortReport.MothraID
    );

    -- Update the dept/role for the target person
    UPDATE #EffortReport
    SET Dept = @NamedDept,
        JGDDesc = @NamedJobDescription
    WHERE MothraID = @MothraId;

    -- Return results: Hours, Dept, JGDDesc
    IF @AllDepts = 1
    BEGIN
        SELECT
            ISNULL(SUM(Activity), 0) AS Hours,
            '' AS Dept,
            JGDDesc
        FROM #EffortReport
        WHERE JGDDesc = @NamedJobDescription
        GROUP BY JGDDesc;
    END
    ELSE
    BEGIN
        SELECT
            ISNULL(SUM(Activity), 0) AS Hours,
            Dept,
            JGDDesc
        FROM #EffortReport
        WHERE Dept = @NamedDept
            AND JGDDesc = @NamedJobDescription
        GROUP BY Dept, JGDDesc;
    END

    -- Return empty row if no results (expected by ColdFusion callers)
    IF @@ROWCOUNT = 0
        SELECT 0 AS Hours, '' AS Dept, '' AS JGDDesc;

    DROP TABLE #EffortReport;
END;
";
        }

        static string GetDeptCountByJobGroupExcludeSql()
        {
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_dept_count_by_job_group_exclude]
    @Year VARCHAR(10),
    @Dept CHAR(6),
    @JobGroupDesc VARCHAR(50),
    @ExcludedTerms NVARCHAR(MAX) = NULL,
    @FilterOutNoCLIAssigned BIT = 0,
    @UseAcademicYear BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Used by ColdFusion MP apps for department instructor counts

    DECLARE @ExcludeTable TABLE (TermCode INT);
    DECLARE @StartTerm INT, @EndTerm INT;

    -- Parse excluded terms
    IF @ExcludedTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludedTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Parse year parameter
    IF CHARINDEX('-', @Year) > 0
    BEGIN
        -- Academic year format: '2024-2025'
        DECLARE @AcadYear INT = CAST(LEFT(@Year, 4) AS INT);
        SET @StartTerm = @AcadYear * 100 + 04;
        SET @EndTerm = (@AcadYear + 1) * 100 + 03;
    END
    ELSE
    BEGIN
        -- Calendar year format: '2024'
        DECLARE @CalYear INT = CAST(@Year AS INT);
        SET @StartTerm = @CalYear * 100;
        SET @EndTerm = (@CalYear + 1) * 100 - 1;
    END

    -- Legacy fn_getEffortTitle maps job groups to descriptions:
    --   335 -> 'ADJUNCT PROFESSOR'
    --   341 -> 'CLINICAL PROFESSOR'
    --   317 -> 'PROF OF CLIN ______'
    --   All others -> 'PROFESSOR/IR' (010, 011, 114, 311, 124, S56)

    SELECT COUNT(DISTINCT p.PersonId) AS myCount
    FROM [effort].[Persons] p
    WHERE p.EffortDept = @Dept
        AND p.TermCode >= @StartTerm
        AND p.TermCode <= @EndTerm
        AND p.TermCode NOT IN (SELECT TermCode FROM @ExcludeTable)
        AND p.VolunteerWos = 0
        -- Only qualified job groups (legacy fn_checkJobGroupAndEffortCode)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
        )
        -- Job group description filter (legacy fn_getEffortTitle + WHERE JgdDesc = @jgd_desc)
        AND (
            (@JobGroupDesc = 'ADJUNCT PROFESSOR' AND p.JobGroupId = '335')
            OR (@JobGroupDesc = 'CLINICAL PROFESSOR' AND p.JobGroupId = '341')
            OR (@JobGroupDesc = 'PROF OF CLIN ______' AND p.JobGroupId = '317')
            OR (@JobGroupDesc = 'PROFESSOR/IR' AND p.JobGroupId NOT IN ('335', '341', '317'))
        )
        -- Must have actual effort records (legacy HAVING SUM > 0)
        AND EXISTS (
            SELECT 1 FROM [effort].[Records] r
            WHERE r.PersonId = p.PersonId
                AND r.TermCode = p.TermCode
                AND COALESCE(r.Hours, r.Weeks, 0) > 0
        )
        AND (
            @FilterOutNoCLIAssigned = 0
            OR EXISTS (
                SELECT 1 FROM [effort].[Records] r
                WHERE r.PersonId = p.PersonId
                    AND r.TermCode = p.TermCode
                    AND r.EffortTypeId = 'CLI'
                    AND COALESCE(r.Hours, r.Weeks, 0) > 0
            )
        );
END;
";
        }

        static string GetInstructorEvalsAverageExcludeSql()
        {
            // Used by ColdFusion MP apps for instructor evaluation averages
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_instructor_evals_average_exclude]
    @StartTerm INT,
    @EndTerm INT,
    @Dept VARCHAR(3) = NULL,
    @MothraId VARCHAR(9) = NULL,
    @ExcludedTerms NVARCHAR(MAX) = NULL,
    @FacilitatorEvals BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Used by ColdFusion MP apps for instructor evaluation reporting

    DECLARE @ExcludeTable TABLE (TermCode INT);

    -- Parse excluded terms using TRY_CAST for defensive parsing
    IF @ExcludedTerms IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeTable (TermCode)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ExcludedTerms, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END

    -- Query EvalHarvest database for evaluation averages
    -- Weighted average: (5*n5 + 4*n4 + 3*n3 + 2*n2 + 1*n1) / total responses
    SELECT
        ROUND(
            (SUM(quant_5_n) * 5.0 + SUM(quant_4_n) * 4.0 + SUM(quant_3_n) * 3.0 +
             SUM(quant_2_n) * 2.0 + SUM(quant_1_n) * 1.0) /
            NULLIF(SUM(quant_5_n + quant_4_n + quant_3_n + quant_2_n + quant_1_n), 0),
            2
        ) AS avgEval,
        MIN(quant_mean) AS minEval,
        MAX(quant_mean) AS maxEval
    FROM evalharvest.dbo.eh_questions
    INNER JOIN evalharvest.dbo.eh_quant ON quest_ID = quant_QuestionID_FK
    INNER JOIN evalharvest.dbo.eh_Courses ON quest_CRN = course_CRN
        AND quest_TermCode = course_termCode
        AND ISNULL(quest_facilitator_evalid, 0) = course_facilitator_evalid
    INNER JOIN evalharvest.dbo.eh_People ON quant_mailID = people_mailid
        AND people_TermCode = course_TermCode
    -- Join with effort.Persons via users.Person to get job group and department
    -- Matches legacy: INNER JOIN tblPerson ON people_mothraID = person_MothraID AND people_termCode = person_termCode
    INNER JOIN [users].[Person] up ON people_MothraID = up.MothraId
    INNER JOIN [effort].[Persons] p ON up.PersonId = p.PersonId AND people_TermCode = p.TermCode
    WHERE course_TermCode BETWEEN @StartTerm AND @EndTerm
        AND course_TermCode NOT IN (SELECT TermCode FROM @ExcludeTable)
        AND quant_mailID IS NOT NULL
        AND quest_overall = 1
        -- Job group qualification (see effort.fn_qualified_job_groups)
        AND EXISTS (
            SELECT 1 FROM effort.fn_qualified_job_groups() q
            WHERE q.JobGroupId = p.JobGroupId
            AND (q.EffortTitleCode IS NULL OR RIGHT('00' + p.EffortTitleCode, 6) = q.EffortTitleCode)
        )
        AND p.EffortDept <> 'OTH'
        AND p.VolunteerWos = 0
        AND (@Dept IS NULL OR p.EffortDept = @Dept)
        AND (@MothraId IS NULL OR people_MothraID = @MothraId)
        -- Facilitator filter: @FacilitatorEvals = 0 means non-facilitator evals only,
        -- @FacilitatorEvals = 1 means facilitator evals only
        AND @FacilitatorEvals = (CASE WHEN course_facilitator_evalid = 0 THEN 0 ELSE 1 END)
        AND quant_mean > 0;
END;
";
        }

        static string GetSabbaticalTermsSql()
        {
            // Used by ColdFusion MPVote and VMTH Letter for exclusion terms
            return @"
CREATE OR ALTER PROCEDURE [effort].[sp_get_sabbatical_terms]
    @MothraId VARCHAR(9)
AS
BEGIN
    SET NOCOUNT ON;

    -- Returns sabbatical exclusion terms for a person
    -- Used by ColdFusion MPVote and VMTH Letter generation
    -- Replaces legacy effort.cfc.Instructor.getInstructorSabbaticTerms()

    SELECT
        s.Id,
        s.PersonId,
        p.MothraId,
        s.ExcludeClinicalTerms AS sab_ExcludeClinTerms,
        s.ExcludeDidacticTerms AS sab_ExcludeDidacticTerms,
        s.ModifiedDate,
        s.ModifiedBy
    FROM [effort].[Sabbaticals] s
    INNER JOIN [users].[Person] p ON s.PersonId = p.PersonId
    WHERE p.MothraId = @MothraId;
END;
";
        }
    }
}
