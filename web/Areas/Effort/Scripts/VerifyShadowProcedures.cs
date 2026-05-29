using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    /*
     * VerifyShadowProcedures.cs
     *
     * Compares data returned from legacy Efforts database stored procedures (usp_*)
     * vs. EffortShadow schema stored procedures ([EffortShadow].[usp_*]) to verify
     * shadow schema compatibility layer works correctly.
     *
     * Usage:
     *   dotnet run --project EffortMigration.csproj -- verify-shadow                       # Auto-select random employee
     *   dotnet run --project EffortMigration.csproj -- verify-shadow --verbose             # Detailed output
     *   dotnet run --project EffortMigration.csproj -- verify-shadow --test-mothraid 00162858  # Use specific MothraID
     *
     * Prerequisites:
     *   - Legacy Efforts database accessible
     *   - VIPER database with [effort] schema and data migrated
     *   - [EffortShadow] schema created in VIPER (RunCreateShadow.bat --apply)
     */
    public class VerifyShadowProcedures
    {
        // ============================================================================
        // View/Table Verification Configuration
        // ============================================================================

        // Mapping of shadow views to legacy tables (view name -> legacy table name)
        // All views are in [EffortShadow] schema, tables are in legacy Efforts database
        private static readonly Dictionary<string, string> ViewToLegacyTable = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tblEffort"] = "tblEffort",
            ["tblPerson"] = "tblPerson",
            ["tblCourses"] = "tblCourses",
            ["tblPercent"] = "tblPercent",
            ["tblStatus"] = "tblStatus",
            ["tblSabbatic"] = "tblSabbatic",
            ["tblRoles"] = "tblRoles",
            ["tblEffortType_LU"] = "tblEffortType_LU",
            ["userAccess"] = "userAccess",
            ["tblUnits_LU"] = "tblUnits_LU",
            ["tblJobCode"] = "tblJobCode",
            ["tblReportUnits"] = "tblReportUnits",
            ["tblAltTitles"] = "tblAltTitles",
            ["tblCourseRelationships"] = "tblCourseRelationships",
            ["tblAudit"] = "tblAudit"
            // vw_InstructorEffort is a composite reporting view - verify separately if needed
        };

        // Views where row count differences are expected due to skipped migration records
        private static readonly Dictionary<string, string> ViewsWithExpectedRowDifferences = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tblPercent"] = "Records with unmapped MothraId are skipped during migration",
            ["tblPerson"] = "Records with invalid MothraId are skipped",
            ["tblEffort"] = "Records referencing skipped persons are skipped",
            ["tblAudit"] = "Records with unmapped audit_ModBy (ChangedBy) are skipped during migration"
        };

        // Primary key columns for each view (used for data comparison ordering)
        // Use comma-separated columns for composite keys
        private static readonly Dictionary<string, string> ViewPrimaryKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tblEffort"] = "effort_ID",
            ["tblPerson"] = "person_MothraID,person_TermCode",  // Composite: one row per person per term
            ["tblCourses"] = "course_ID",
            ["tblPercent"] = "percent_ID",
            ["tblStatus"] = "status_TermCode",
            ["tblSabbatic"] = "sab_ID",
            ["tblRoles"] = "Role_ID",
            ["tblEffortType_LU"] = "type_ID",
            ["userAccess"] = "userAccessID",
            ["tblUnits_LU"] = "unit_ID",
            ["tblJobCode"] = "jobcode",
            ["tblReportUnits"] = "ru_id",
            ["tblAltTitles"] = "JobGrpID",
            ["tblCourseRelationships"] = "cr_ParentID,cr_ChildID",  // Composite: parent + child
            ["tblAudit"] = "audit_ID"
        };

        // Known value differences that are acceptable (legacy value → shadow value)
        // These are intentional differences from data migration or view definition
        private static readonly Dictionary<string, Dictionary<string, string>> KnownValueMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            // tblPercent: NULL modifiedBy becomes 'unknown' in migration
            ["percent_modifiedBy"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NULL"] = "unknown"
            },
            // tblPerson: ClientID is unused in legacy (NULL or empty), populated from users.Person in shadow
            // Legacy never populated this field consistently, so any shadow value is acceptable
            ["person_ClientID"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NULL"] = "*",  // * means any non-null value is acceptable
                [""] = "*"       // Empty string also maps to any value
            },
            // tblPerson: MiddleIni empty string vs NULL
            ["person_MiddleIni"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [""] = "NULL",
                ["NULL"] = ""
            }
        };

        // Views where term name format differs (legacy has verbose names, shadow uses vwTerms)
        private static readonly HashSet<string> ViewsWithTermNameDifferences = new(StringComparer.OrdinalIgnoreCase)
        {
            "tblStatus"  // status_TermName comes from different source views
        };

        // Views that are large - use random sampling instead of TOP N
        private static readonly HashSet<string> LargeViewsForRandomSampling = new(StringComparer.OrdinalIgnoreCase)
        {
            "tblAudit"  // 266K+ rows - use random sampling
        };

        // ============================================================================
        // Stored Procedure Verification Configuration
        // ============================================================================

        // Procedures where ORDER BY differences are acceptable because the application re-sorts results
        private static readonly HashSet<string> OrderInsensitiveProcedures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "usp_getEffortReportMeritSummaryForLairmore",  // ColdFusion re-sorts by jgddesc,instructor in yearStats.cfc:276
            "usp_getInstructorsWithClinicalEffort"         // No explicit ORDER BY; ColdFusion handles display order
        };

        // Procedures where Shadow may return MORE rows than Legacy due to legacy NULL percent_AcademicYear bug
        // These are NOT failures - Shadow is correct, Legacy silently drops rows with NULL academic year
        private const string NullAcademicYearReason = "Legacy has NULL percent_AcademicYear that never matches; Shadow derives from TermCode/StartDate";
        private static readonly HashSet<string> ProceduresAllowingMoreShadowRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "usp_getEffortPercentsForInstructor",
            "usp_getInstructorsWithClinicalEffort",
            "usp_getListOfEffortPercentsForInstructor",
            "usp_getSumEffortPercentsForInstructor"
        };

        // Procedures where Legacy may return MORE rows than Shadow due to records with unmapped MothraId
        // Migration correctly skips these records (MothraId not in VIPER.users.Person), so Shadow has fewer rows
        private static readonly HashSet<string> ProceduresWithPotentialUnmappedLegacyRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "usp_getInstructorsWithClinicalEffort"
        };

        public static int Run(string[] args)
        {
            // ============================================================================
            // Configuration & Setup
            // ============================================================================

            var configuration = EffortScriptHelper.LoadConfiguration();

            var legacyConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Effort");
            // EffortShadow is now a schema within VIPER database
            var shadowConnectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER", readOnly: true);
            bool verboseMode = args.Contains("--verbose");

            // Parse optional test MothraID parameter
            string? testMothraId = null;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--test-mothraid")
                {
                    testMothraId = args[i + 1];
                    break;
                }
            }

            // If no MothraID provided, auto-select a random valid person from legacy database
            if (string.IsNullOrWhiteSpace(testMothraId))
            {
                try
                {
                    // Query the legacy Effort database for a random person with effort data in 2023-2024 academic year
                    // UC Davis SVM uses semesters: Fall (09), Spring (02), Summer (04)
                    // Academic year runs from Fall Semester (09) through Summer Semester (04)
                    // Much simpler than querying migrated data - no joins needed
                    using var lookupConn = new SqlConnection(legacyConnectionString);
                    lookupConn.Open();
                    using var cmd = new SqlCommand(@"
                        SELECT TOP 1 person_MothraID
                        FROM tblPerson
                        WHERE person_MothraID IS NOT NULL
                          AND person_TermCode BETWEEN 202309 AND 202404
                        ORDER BY NEWID()", lookupConn);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        testMothraId = reader.IsDBNull(0) ? null : reader.GetString(0);

                        if (!string.IsNullOrWhiteSpace(testMothraId))
                        {
                            Console.WriteLine($"Auto-selected test employee from legacy data: MothraID={testMothraId}");
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: No valid test employees found in legacy data (termcodes 202309-202404)");
                        Console.WriteLine("Cannot proceed with verification - no test data available.");
                        Console.ResetColor();
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR: Failed to auto-select test employee: {ex.Message}");
                    Console.WriteLine("Cannot proceed with verification.");
                    Console.ResetColor();
                    return 1;
                }
            }
            else
            {
                Console.WriteLine($"Using provided test employee: MothraID={testMothraId}");
                Console.WriteLine();
            }

            // Ensure AnalysisOutput directory exists
            string outputDir = Path.Join(Directory.GetCurrentDirectory(), "AnalysisOutput");
            Directory.CreateDirectory(outputDir);
            string reportFile = Path.Join(outputDir, "shadow-verification.txt");

            Console.WriteLine("================================================================================");
            Console.WriteLine("Effort Shadow Schema Verification");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Legacy DB: {GetDatabaseName(legacyConnectionString)}");
            Console.WriteLine($"Shadow Schema: [VIPER].[EffortShadow] (within {GetDatabaseName(shadowConnectionString)})");
            Console.WriteLine($"Verbose Mode: {verboseMode}");
            Console.WriteLine($"Report File: {reportFile}");
            Console.WriteLine();
            Console.WriteLine("This script compares legacy Efforts database stored procedures");
            Console.WriteLine("against [EffortShadow] schema procedures to verify the shadow");
            Console.WriteLine("schema compatibility layer works correctly.");
            Console.WriteLine();

            // ============================================================================
            // Run View/Table Schema Verification
            // ============================================================================
            // Run BEFORE procedure verification - views are prerequisites for SPs
            var viewReport = VerifyShadowViews(legacyConnectionString, shadowConnectionString, verboseMode);

            // If view verification has failures, warn but continue with SP verification
            // This allows seeing all issues at once rather than fixing views first
            if (viewReport.FailedViews > 0)
            {
                Console.WriteLine();
                Console.WriteLine("================================================================================");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ VIEW VERIFICATION HAS {viewReport.FailedViews} FAILURE(S)");
                Console.ResetColor();
                Console.WriteLine("   Continuing with stored procedure verification...");
                Console.WriteLine("   Note: SP results may be affected by view issues.");
                Console.WriteLine("================================================================================");
                Console.WriteLine();
            }

            // ============================================================================
            // Dynamically Discover Test Procedures
            // ============================================================================
            // Discover all procedures in EffortShadow schema and generate test parameters
            // Shadow schema should have ~92 procedures rewritten from legacy

            var testProcedures = DiscoverProcedures(shadowConnectionString, verboseMode, testMothraId);

            // ============================================================================
            // Main Verification Logic
            // ============================================================================

            var report = new VerificationReport { TotalProcedures = testProcedures.Count };

            using var legacyConn = new SqlConnection(legacyConnectionString);
            using var shadowConn = new SqlConnection(shadowConnectionString);
            legacyConn.Open();
            shadowConn.Open();

            Console.WriteLine($"Verifying {testProcedures.Count} stored procedure samples...");
            Console.WriteLine("================================================================================");

            foreach (var test in testProcedures)
            {
                Console.Write($"Testing {test.Name}... ");

                // In verbose mode, output the parameters being used
                if (verboseMode && test.Parameters.Length > 0)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"    Parameters:");
                    foreach (var param in test.Parameters)
                    {
                        string valueStr = param.Value == DBNull.Value ? "NULL" : param.Value?.ToString() ?? "NULL";
                        Console.WriteLine($"      {param.ParameterName} = {valueStr} ({param.SqlDbType})");
                    }
                    Console.ResetColor();
                    Console.Write($"    Result: ");
                }

                var result = new ComparisonResult
                {
                    ProcedureName = test.Name
                };

                try
                {
                    // Check if this is a CRUD procedure (create, update, delete, etc.)
                    bool isCrudProcedure = IsCrudProcedure(test.Name);

                    if (isCrudProcedure)
                    {
                        // Test CRUD procedures with transactions and rollback
                        result.Passed = TestCrudProcedure(shadowConn, test, result);
                    }
                    else
                    {
                        // Test read-only procedures by comparing results
                        Exception? legacyException = null;
                        Exception? shadowException = null;
                        DataTable? legacyData = null;
                        DataTable? shadowData = null;

                        // Execute on legacy database
                        var legacyStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            legacyData = ExecuteProcedure(legacyConn, test.Name, test.Parameters.ToList());
                            result.LegacyRowCount = legacyData.Rows.Count;
                        }
                        catch (Exception ex)
                        {
                            legacyException = ex;
                        }
                        legacyStopwatch.Stop();
                        result.LegacyExecutionMs = legacyStopwatch.ElapsedMilliseconds;

                        // Execute on shadow schema (procedure name with schema prefix)
                        var shadowStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        string shadowProcName = $"[EffortShadow].{test.Name}";
                        try
                        {
                            shadowData = ExecuteProcedure(shadowConn, shadowProcName, test.Parameters.ToList());
                            result.ShadowRowCount = shadowData.Rows.Count;
                        }
                        catch (Exception ex)
                        {
                            shadowException = ex;
                        }
                        shadowStopwatch.Stop();
                        result.ShadowExecutionMs = shadowStopwatch.ElapsedMilliseconds;

                        // Determine test result based on which procedures failed
                        if (legacyException != null && shadowException != null)
                        {
                            // Both failed - NEEDS INVESTIGATION (environmental issue, permissions, etc.)
                            result.Status = TestStatus.NeedsInvestigation;
                            result.Passed = false;  // Changed: Don't hide this as a pass
                            result.Warnings.Add($"⚠ BOTH PROCEDURES FAILED - Requires investigation");
                            result.Warnings.Add($"Legacy error: {legacyException.Message}");
                            result.Warnings.Add($"Shadow error: {shadowException.Message}");
                            result.Warnings.Add("Likely cause: External database access, missing permissions, or data dependencies");
                        }
                        else if (legacyException != null)
                        {
                            // Legacy failed but shadow worked - NEEDS INVESTIGATION
                            // We cannot verify shadow correctness without a working legacy baseline
                            result.Status = TestStatus.NeedsInvestigation;
                            result.Passed = false;
                            result.Warnings.Add($"⚠ LEGACY PROCEDURE FAILED, SHADOW SUCCEEDED - Requires investigation");
                            result.Warnings.Add($"Legacy error: {legacyException.Message}");
                            result.Warnings.Add("Shadow procedure succeeded, but we cannot verify correctness without working legacy baseline");
                            result.Warnings.Add("Likely cause: Legacy procedure is broken (references non-existent columns/tables), or is unused");
                        }
                        else if (shadowException != null)
                        {
                            // Shadow failed but legacy worked - this IS a problem with shadow schema
                            result.Status = TestStatus.Failed;
                            result.Passed = false;
                            result.Differences.Add($"Shadow procedure failed: {shadowException.Message}");
                            result.Differences.Add("Legacy procedure succeeded - shadow schema has a defect");
                        }
                        else
                        {
                            // Both succeeded - compare results
                            bool orderInsensitive = OrderInsensitiveProcedures.Contains(test.Name);
                            CompareResults(legacyData!, shadowData!, result, test.Name, orderInsensitive, legacyConn);
                            if (result.Differences.Count == 0)
                            {
                                result.Status = TestStatus.Passed;
                                result.Passed = true;
                            }
                            else
                            {
                                result.Status = TestStatus.Failed;
                                result.Passed = false;
                            }
                        }
                    }

                    // Display results based on status
                    switch (result.Status)
                    {
                        case TestStatus.Passed:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"✓ PASS ({result.LegacyRowCount} rows, {result.ShadowExecutionMs}ms)");
                            Console.ResetColor();
                            report.PassedProcedures++;

                            // Display warnings if any (e.g., legacy failed but shadow improved)
                            if (verboseMode && result.Warnings.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                foreach (var warning in result.Warnings)
                                {
                                    Console.WriteLine($"    {warning}");
                                }
                                Console.ResetColor();
                            }
                            break;

                        case TestStatus.Failed:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"✗ FAIL ({result.Differences.Count} differences)");
                            Console.ResetColor();
                            report.FailedProcedures++;

                            if (verboseMode)
                            {
                                foreach (var diff in result.Differences.Take(10))
                                {
                                    Console.WriteLine($"    - {diff}");
                                }
                                if (result.Differences.Count > 10)
                                    Console.WriteLine($"    ... and {result.Differences.Count - 10} more differences");
                            }
                            break;

                        case TestStatus.NeedsInvestigation:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"⚠ NEEDS INVESTIGATION (both legacy and shadow failed)");
                            Console.ResetColor();
                            report.NeedsInvestigation++;

                            if (verboseMode)
                            {
                                foreach (var warning in result.Warnings)
                                {
                                    Console.WriteLine($"    {warning}");
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Catch unexpected exceptions not handled by individual execution blocks
                    // (e.g., CRUD procedure testing errors, connection issues, etc.)
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ ERROR: {ex.Message}");
                    Console.ResetColor();

                    result.Passed = false;
                    result.Differences.Add($"Unexpected exception: {ex.Message}");
                    report.FailedProcedures++;
                }

                report.Results.Add(result);
            }

            // ============================================================================
            // Run tblPercent Migration Verification
            // ============================================================================
            // Run this BEFORE generating report so results can be included
            var percentReport = VerifyTblPercentByTopUsers(legacyConnectionString, shadowConnectionString, verboseMode);

            // ============================================================================
            // Generate Report
            // ============================================================================

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("Verification Summary");
            Console.WriteLine("================================================================================");

            // View Verification Summary
            Console.WriteLine("View/Table Verification:");
            Console.WriteLine($"  Total Views: {viewReport.TotalViews}");
            if (viewReport.FailedViews == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Passed: {viewReport.PassedViews} ✓");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Passed: {viewReport.PassedViews}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed: {viewReport.FailedViews}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("Stored Procedure Verification:");
            Console.WriteLine($"  Total Procedures Tested: {report.TotalProcedures}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Passed: {report.PassedProcedures}");
            Console.ResetColor();

            if (report.FailedProcedures > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Failed (Shadow Defects): {report.FailedProcedures}");
                Console.ResetColor();
            }

            if (report.NeedsInvestigation > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Needs Investigation (Both Failed): {report.NeedsInvestigation}");
                Console.ResetColor();
            }

            // Count and display other warnings (e.g., CRUD rollback warnings, legacy-only failures)
            int proceduresWithOtherWarnings = report.Results.Count(r =>
                r.Status == TestStatus.Passed && r.Warnings.Count > 0);
            if (proceduresWithOtherWarnings > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  Other Warnings (CRUD rollback, etc.): {proceduresWithOtherWarnings}");
                Console.ResetColor();
            }

            // tblPercent Migration Verification Summary
            Console.WriteLine();
            Console.WriteLine("tblPercent Migration Verification:");
            Console.WriteLine($"  Users Checked: {percentReport.TotalUsersChecked}");
            if (percentReport.UsersWithDiscrepancies == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  All Matched: ✓");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Users with Discrepancies: {percentReport.UsersWithDiscrepancies}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine($"Writing detailed report to: {reportFile}");

            // Generate human-readable text report
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("EFFORT SHADOW SCHEMA VERIFICATION REPORT");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Legacy Database: {GetDatabaseName(legacyConnectionString)}");
            sb.AppendLine($"Shadow Schema: [VIPER].[EffortShadow] (within {GetDatabaseName(shadowConnectionString)})");
            sb.AppendLine();
            sb.AppendLine("PURPOSE");
            sb.AppendLine("================================================================================");
            sb.AppendLine("Verifies that [EffortShadow] schema stored procedures return identical results");
            sb.AppendLine("to legacy Efforts database procedures. Shadow procedures query views that map");
            sb.AppendLine("to the new [VIPER].[effort] schema, providing ColdFusion compatibility.");
            sb.AppendLine();
            sb.AppendLine("SUMMARY");
            sb.AppendLine("================================================================================");
            sb.AppendLine();
            sb.AppendLine("View/Table Verification:");
            sb.AppendLine($"  Total Views: {viewReport.TotalViews}");
            sb.AppendLine($"  Passed: {viewReport.PassedViews}");
            sb.AppendLine($"  Failed: {viewReport.FailedViews}");
            sb.AppendLine();
            sb.AppendLine("Stored Procedure Verification:");
            sb.AppendLine($"  Total Procedures Tested: {report.TotalProcedures} (representative sample)");
            sb.AppendLine($"  Passed: {report.PassedProcedures} (shadow works correctly)");
            sb.AppendLine($"  Failed: {report.FailedProcedures} (shadow defects - shadow fails, legacy works)");
            sb.AppendLine($"  Needs Investigation: {report.NeedsInvestigation} (both legacy and shadow fail)");

            if (proceduresWithOtherWarnings > 0)
            {
                sb.AppendLine($"  Other Warnings: {proceduresWithOtherWarnings} (CRUD rollback tests, legacy-only failures, etc.)");
            }

            sb.AppendLine();
            sb.AppendLine("tblPercent Migration Verification (Top 25 Users):");
            sb.AppendLine($"  Users Checked: {percentReport.TotalUsersChecked}");
            sb.AppendLine($"  Users Matched: {percentReport.TotalUsersChecked - percentReport.UsersWithDiscrepancies}");
            sb.AppendLine($"  Users with Discrepancies: {percentReport.UsersWithDiscrepancies}");

            sb.AppendLine();

            // View Verification Details
            if (viewReport.FailedViews > 0)
            {
                sb.AppendLine("VIEW/TABLE VERIFICATION FAILURES");
                sb.AppendLine("================================================================================");
                sb.AppendLine("These shadow views have schema or data mismatches with legacy tables.");
                sb.AppendLine();
                sb.AppendLine("⚠ ACTION REQUIRED: Fix these view issues before stored procedure verification.");
                sb.AppendLine();

                foreach (var viewResult in viewReport.Results.Where(r => !r.SchemaPassed || !r.DataPassed))
                {
                    sb.AppendLine($"View: [EffortShadow].{viewResult.ViewName} vs Legacy {viewResult.LegacyTable}");
                    sb.AppendLine($"  Schema: {(viewResult.SchemaPassed ? "✓ PASS" : "✗ FAIL")} ({viewResult.LegacyColumnCount} legacy cols, {viewResult.ShadowColumnCount} shadow cols)");
                    sb.AppendLine($"  Data: {(viewResult.DataPassed ? "✓ PASS" : "✗ FAIL")} ({viewResult.LegacyRowCount} legacy rows, {viewResult.ShadowRowCount} shadow rows)");

                    foreach (var error in viewResult.Errors)
                    {
                        sb.AppendLine($"  ERROR: {error}");
                    }
                    foreach (var warning in viewResult.Warnings)
                    {
                        sb.AppendLine($"  WARNING: {warning}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                }

                sb.AppendLine();
            }

            // View verification passed - show summary
            sb.AppendLine("VIEW/TABLE VERIFICATION DETAILS");
            sb.AppendLine("================================================================================");
            foreach (var viewResult in viewReport.Results)
            {
                string schemaStatus = viewResult.SchemaPassed ? "✓" : "✗";
                string dataStatus = viewResult.DataPassed ? "✓" : "✗";
                string contentStatus = viewResult.ContentPassed ? "✓" : "✗";
                string rowInfo = viewResult.LegacyRowCount == viewResult.ShadowRowCount
                    ? $"{viewResult.LegacyRowCount} rows"
                    : $"{viewResult.LegacyRowCount} legacy, {viewResult.ShadowRowCount} shadow";
                string contentInfo = viewResult.SampleRowsCompared > 0
                    ? $", Content {contentStatus} ({viewResult.SampleRowsCompared} rows compared)"
                    : "";
                sb.AppendLine($"  {viewResult.ViewName}: Schema {schemaStatus} ({viewResult.LegacyColumnCount} cols), Data {dataStatus} ({rowInfo}){contentInfo}");

                // Show content differences if any (already formatted with indentation)
                foreach (var diff in viewResult.ContentDifferences)
                {
                    sb.AppendLine($"    {diff}");
                }

                // Show warnings if any
                foreach (var warning in viewResult.Warnings)
                {
                    sb.AppendLine($"    ⚠ {warning}");
                }
            }
            sb.AppendLine();

            // Needs Investigation Section (both failed - environmental issues)
            if (report.NeedsInvestigation > 0)
            {
                sb.AppendLine("NEEDS INVESTIGATION");
                sb.AppendLine("================================================================================");
                sb.AppendLine("These procedures failed in BOTH legacy and shadow schemas.");
                sb.AppendLine("This indicates environmental issues (missing permissions, external database");
                sb.AppendLine("dependencies, data issues) rather than shadow schema defects.");
                sb.AppendLine();
                sb.AppendLine("⚠ ACTION REQUIRED: Investigate and resolve these environmental issues.");
                sb.AppendLine();

                foreach (var result in report.Results.Where(r => r.Status == TestStatus.NeedsInvestigation))
                {
                    sb.AppendLine($"Procedure: {result.ProcedureName}");
                    foreach (var warning in result.Warnings)
                    {
                        sb.AppendLine($"  {warning}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                }

                sb.AppendLine();
            }

            // Other Warnings Section (CRUD rollback, legacy-only failures, etc.)
            if (proceduresWithOtherWarnings > 0)
            {
                sb.AppendLine("OTHER WARNINGS");
                sb.AppendLine("================================================================================");
                sb.AppendLine("These procedures passed but have informational warnings.");
                sb.AppendLine("Common reasons: CRUD rollback tests, legacy procedure failures (shadow improved),");
                sb.AppendLine("or data quality differences.");
                sb.AppendLine();

                foreach (var result in report.Results.Where(r => r.Status == TestStatus.Passed && r.Warnings.Count > 0))
                {
                    sb.AppendLine($"Procedure: {result.ProcedureName}");
                    sb.AppendLine("Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        sb.AppendLine($"  {warning}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                }

                sb.AppendLine();
            }

            if (report.FailedProcedures > 0)
            {
                sb.AppendLine("SHADOW SCHEMA DEFECTS");
                sb.AppendLine("================================================================================");
                sb.AppendLine("These procedures have TRUE shadow schema defects (shadow fails, legacy works).");
                sb.AppendLine();
                sb.AppendLine("⚠ ACTION REQUIRED: Fix these shadow schema issues before ColdFusion migration.");
                sb.AppendLine();

                foreach (var result in report.Results.Where(r => r.Status == TestStatus.Failed))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Procedure: {result.ProcedureName}");
                    sb.AppendLine($"Status: FAILED ({result.Differences.Count} differences)");
                    sb.AppendLine();
                    sb.AppendLine("Differences:");
                    foreach (var diff in result.Differences)
                    {
                        sb.AppendLine($"  - {diff}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                }
            }
            else if (report.NeedsInvestigation == 0)
            {
                sb.AppendLine("RESULT: ✓ ALL SHADOW PROCEDURES WORKING");
                sb.AppendLine("================================================================================");
                sb.AppendLine("All tested shadow procedures work correctly (identical to legacy or better).");
                sb.AppendLine("(or have documented environmental issues - not shadow schema defects).");
                sb.AppendLine("The shadow schema compatibility layer is working correctly.");
                sb.AppendLine();
                sb.AppendLine("Next Steps:");
                sb.AppendLine("  1. Update ColdFusion datasource to VIPER database with [EffortShadow] schema (TEST environment only!)");
                sb.AppendLine("  2. Test all ColdFusion CRUD operations");
                sb.AppendLine("  3. Verify authorization queries work correctly");
                sb.AppendLine("  4. Test all reports generation");
                sb.AppendLine("  5. Performance validation (should be < 20% slower than baseline)");
            }

            sb.AppendLine();
            sb.AppendLine("DETAILED RESULTS");
            sb.AppendLine("================================================================================");

            foreach (var result in report.Results)
            {
                sb.AppendLine();
                sb.AppendLine($"Procedure: {result.ProcedureName}");
                sb.AppendLine($"Status: {(result.Passed ? "✓ PASSED" : "✗ FAILED")}");
                sb.AppendLine($"Row Count: Legacy={result.LegacyRowCount}, Shadow={result.ShadowRowCount}");
                sb.AppendLine($"Execution Time: Legacy={result.LegacyExecutionMs}ms, Shadow={result.ShadowExecutionMs}ms");

                if (result.Warnings.Count > 0)
                {
                    sb.AppendLine("Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        sb.AppendLine($"  ⚠ {warning}");
                    }
                }

                if (!result.Passed && result.Differences.Count > 0)
                {
                    sb.AppendLine("Differences:");
                    foreach (var diff in result.Differences)
                    {
                        sb.AppendLine($"  - {diff}");
                    }
                }

                sb.AppendLine();
            }

            // tblPercent Migration Verification Details
            sb.AppendLine();
            sb.AppendLine("tblPercent MIGRATION VERIFICATION");
            sb.AppendLine("================================================================================");
            sb.AppendLine("Compares percentage records for top 25 users between Legacy and Shadow.");
            sb.AppendLine("Orphaned records (no corresponding tblPerson entry) are excluded from comparison.");
            sb.AppendLine();

            if (percentReport.UsersWithDiscrepancies == 0)
            {
                sb.AppendLine($"✓ All {percentReport.TotalUsersChecked} users verified - records match between Legacy and Shadow");
            }
            else
            {
                sb.AppendLine($"✗ {percentReport.UsersWithDiscrepancies} of {percentReport.TotalUsersChecked} users have discrepancies");
                sb.AppendLine();

                foreach (var userResult in percentReport.Results.Where(r => !r.Matched))
                {
                    sb.AppendLine($"MothraID: {userResult.MothraId}");
                    sb.AppendLine($"  Legacy Count: {userResult.LegacyCount}, Shadow Count: {userResult.ShadowCount}");

                    if (userResult.MissingInShadow.Count > 0)
                    {
                        sb.AppendLine($"  Missing in Shadow: {userResult.MissingInShadow.Count}");
                        foreach (var key in userResult.MissingInShadow.Take(5))
                        {
                            sb.AppendLine($"    - {key}");
                        }
                        if (userResult.MissingInShadow.Count > 5)
                            sb.AppendLine($"    ... and {userResult.MissingInShadow.Count - 5} more");
                    }

                    if (userResult.ExtraInShadow.Count > 0)
                    {
                        sb.AppendLine($"  Extra in Shadow: {userResult.ExtraInShadow.Count}");
                        foreach (var key in userResult.ExtraInShadow.Take(5))
                        {
                            sb.AppendLine($"    - {key}");
                        }
                        if (userResult.ExtraInShadow.Count > 5)
                            sb.AppendLine($"    ... and {userResult.ExtraInShadow.Count - 5} more");
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("================================================================================");
            sb.AppendLine("END OF REPORT");
            sb.AppendLine("================================================================================");

            File.WriteAllText(reportFile, sb.ToString());

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("✓ Report generated successfully");
            Console.ResetColor();
            Console.WriteLine("================================================================================");

            // Fail if any views failed, procedures failed OR need investigation, OR percent migration has discrepancies
            // NeedsInvestigation indicates environmental issues that prevent validation
            bool hasViewFailures = viewReport.FailedViews > 0;
            bool hasProcedureFailures = report.FailedProcedures > 0 || report.NeedsInvestigation > 0;
            bool hasPercentDiscrepancies = percentReport.UsersWithDiscrepancies > 0;
            return (hasViewFailures || hasProcedureFailures || hasPercentDiscrepancies) ? 1 : 0;
        }

        // ============================================================================
        // Procedure Discovery
        // ============================================================================

        private static List<ProcedureTest> DiscoverProcedures(string connectionString, bool verbose, string? testMothraId)
        {
            var procedures = new List<ProcedureTest>();
            int skippedCount = 0;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            // Query to get all procedures in EffortShadow schema with their parameters
            string query = @"
                SELECT
                    p.name AS ProcedureName,
                    ISNULL(par.name, '') AS ParameterName,
                    ISNULL(TYPE_NAME(par.user_type_id), '') AS DataType,
                    ISNULL(par.max_length, 0) AS MaxLength,
                    ISNULL(par.is_nullable, 0) AS IsNullable,
                    ISNULL(par.has_default_value, 0) AS HasDefault,
                    ISNULL(par.is_output, 0) AS IsOutput
                FROM sys.procedures p
                LEFT JOIN sys.parameters par ON p.object_id = par.object_id
                WHERE p.schema_id = SCHEMA_ID('EffortShadow')
                ORDER BY p.name, par.parameter_id";

            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            string? currentProcName = null;
            var currentParams = new List<SqlParameter>();

            while (reader.Read())
            {
                string procName = reader.GetString(0);
                string paramName = reader.GetString(1);

                // New procedure found
                if (currentProcName != procName && currentProcName != null)
                {
                    // Check if procedure should be excluded from verification
                    // (these procedures were not migrated to EffortShadow, so they shouldn't exist)
                    if (EffortSchemaConfig.ShouldExcludeProcedure(currentProcName))
                    {
                        if (verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"  ⚠ Warning: Excluded procedure found in shadow schema: {currentProcName}");
                            Console.WriteLine($"    This procedure should not exist (marked as: {EffortSchemaConfig.GetExclusionReason(currentProcName)})");
                            Console.ResetColor();
                        }
                        skippedCount++;
                        currentParams = new List<SqlParameter>();
                    }
                    else
                    {
                        // Add previous procedure to list
                        procedures.Add(new ProcedureTest
                        {
                            Name = currentProcName,
                            Parameters = currentParams.ToArray()
                        });
                        currentParams = new List<SqlParameter>();
                    }
                }

                currentProcName = procName;

                // Add parameter if it exists (not all procedures have parameters)
                if (!string.IsNullOrEmpty(paramName))
                {
                    string dataType = reader.GetString(2);
                    int maxLength = reader.GetInt16(3);
                    bool isNullable = reader.GetBoolean(4);
                    bool hasDefault = reader.GetBoolean(5);
                    bool isOutput = reader.GetBoolean(6);

                    // Generate test value based on parameter name and type
                    // For OUTPUT parameters, we use DBNull.Value since we don't provide input
                    object testValue = isOutput
                        ? DBNull.Value
                        : GenerateTestValue(paramName, dataType, isNullable, hasDefault, testMothraId, currentProcName);

                    // Create parameter with explicit SqlDbType to avoid type inference issues
                    // (e.g., C# int being mapped to SQL INT instead of BIT)
                    var param = new SqlParameter(paramName, testValue)
                    {
                        SqlDbType = GetSqlDbType(dataType),
                        Direction = isOutput ? ParameterDirection.Output : ParameterDirection.Input
                    };

                    // For OUTPUT parameters, set Size for varchar types
                    if (isOutput && (dataType.ToLowerInvariant().Contains("varchar") || dataType.ToLowerInvariant().Contains("char")))
                    {
                        param.Size = maxLength > 0 ? maxLength : 255;
                    }

                    // Debug logging for problematic procedure
                    if (verbose && currentProcName == "usp_getEffortDeptActivityTotalWithExcludeTerms")
                    {
                        Console.WriteLine($"    Param: {paramName}, SQL Type: {dataType}, C# Type: {testValue?.GetType().Name ?? "null"}, Value: {testValue}, SqlDbType: {param.SqlDbType}");
                    }

                    currentParams.Add(param);
                }
            }

            // Add last procedure (if not excluded)
            if (currentProcName != null && !EffortSchemaConfig.ShouldExcludeProcedure(currentProcName))
            {
                procedures.Add(new ProcedureTest
                {
                    Name = currentProcName,
                    Parameters = currentParams.ToArray()
                });
            }
            else if (currentProcName != null)
            {
                skippedCount++;
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠ Warning: Excluded procedure found in shadow schema: {currentProcName}");
                    Console.WriteLine($"    This procedure should not exist (marked as: {EffortSchemaConfig.GetExclusionReason(currentProcName)})");
                    Console.ResetColor();
                }
            }

            reader.Close();

            if (verbose)
            {
                Console.WriteLine($"Discovered {procedures.Count} procedures to verify in EffortShadow schema:");
                if (skippedCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  (Excluded {skippedCount} unused/obsolete procedures from verification)");
                    Console.ResetColor();
                }
                foreach (var proc in procedures.Take(10))
                {
                    Console.WriteLine($"  - {proc.Name} ({proc.Parameters.Length} parameters)");
                }
                if (procedures.Count > 10)
                    Console.WriteLine($"  ... and {procedures.Count - 10} more");
                Console.WriteLine();
            }
            else if (skippedCount > 0)
            {
                Console.WriteLine($"Note: Excluded {skippedCount} unused/obsolete procedures from verification");
            }

            return procedures;
        }

        private static object GenerateTestValue(string paramName, string dataType, bool isNullable, bool hasDefault, string? testMothraId, string? procedureName = null)
        {
            // For non-nullable parameters, we must provide valid test data
            // For nullable/default parameters, provide NULL unless it's a key field

            // Generate test values based on common parameter naming conventions
            string paramLower = paramName.ToLowerInvariant();

            // Boolean flags - handle FIRST to avoid matching substrings like "dept" in "allDepts"
            if ((paramLower.Contains("alldepts") || paramLower.Contains("useacademicyear") ||
                paramLower.Contains("includeall") || paramLower.Contains("showdetails")) &&
                dataType.ToLowerInvariant() == "bit")
            {
                return false;  // Default to false for boolean flags
            }

            // ModBy (modification user) - Always required for CRUD operations
            if (paramLower.Contains("modby"))
            {
                return "testuser";  // 8 char user ID
            }

            // TermCode: Use a term from the migrated data range (2023-2024 academic year)
            // Using 202309 (Fall Semester 2023) which is the start of the academic year and likely has the most data
            // UC Davis SVM uses semesters for DVM students: Fall (09), Spring (02), Summer (04)
            // Academic year runs from Fall Semester (09) through Summer Semester (04)
            // Some procedures accept both int (202309) and varchar ("202309")
            if (paramLower.Contains("termcode") || paramLower.Contains("term") && !paramLower.Contains("excluded"))
            {
                // Return as string if varchar type, otherwise int
                if (dataType.ToLowerInvariant().Contains("varchar") || dataType.ToLowerInvariant().Contains("char"))
                {
                    return "202309";  // Fall Semester 2023 as string (start of migrated data range)
                }
                return 202309;  // Fall Semester 2023 as int
            }

            // Year parameters
            // NOTE: Check useAcademicYear BEFORE academicyear to avoid matching both
            if (paramLower.Contains("useacademicyear"))
            {
                // This is a BIT flag, not a year string
                return 0;  // 0 = use calendar year, 1 = use academic year
            }

            if (paramLower.Contains("academicyear"))
            {
                // Special case: usp_getInstructorsWithClinicalEffort has no data in 2023-2024
                // Use 2019-2020 academic year which has 127 rows
                if (procedureName != null && procedureName.Equals("usp_getInstructorsWithClinicalEffort", StringComparison.OrdinalIgnoreCase))
                {
                    return "2019-2020";
                }
                return "2023-2024";
            }

            if (paramLower.Contains("year") && (paramLower.Contains("start") || paramLower.Contains("begin")))
            {
                return 2023;
            }

            if (paramLower.Contains("year") && paramLower.Contains("end"))
            {
                return 2024;
            }

            // Standalone Year parameter (could be academic year or calendar year)
            if (paramLower.Equals("year") || paramLower.Equals("@year"))
            {
                return "2024";  // Calendar year
            }

            // Department
            if (paramLower.Contains("dept"))
            {
                return "VMTH";  // Vet Medicine Teaching Hospital - commonly used dept
            }

            // MothraID - 8-digit ID with leading zeros (e.g., "00162858")
            // This is the primary identifier used by all Effort stored procedures
            if (paramLower.Contains("mothraid"))
            {
                // testMothraId should always have a value from auto-selection or manual parameter
                // If it's null here, the script would have already exited with an error
                return testMothraId ?? throw new InvalidOperationException("MothraID is required but was not provided");
            }

            // EmployeeID/ClientID - These are NOT used by legacy Effort procedures
            // The ColdFusion application only uses MothraID parameters
            // If we encounter these parameters, use fallback values
            if (paramLower.Contains("employeeid") || paramLower.Contains("clientid"))
            {
                // Return reasonable fallback values
                return "000000001";
            }

            // FirstName, LastName, MiddleIni
            if (paramLower.Contains("firstname"))
            {
                return "Test";
            }
            if (paramLower.Contains("lastname"))
            {
                return "User";
            }
            if (paramLower.Contains("middleini"))
            {
                return "T";
            }

            // CRN (Course Reference Number)
            if (paramLower.Contains("crn"))
            {
                return "99999";  // Use high number to avoid conflicts
            }

            // SubjCode, CrseNumb, SeqNumb (course identifiers)
            if (paramLower.Contains("subjcode") || paramLower.Contains("subject"))
            {
                return "TST";  // Test subject code
            }
            if (paramLower.Contains("crsenumb") || paramLower.Contains("coursenumber"))
            {
                return "999";
            }
            if (paramLower.Contains("seqnumb") || paramLower.Contains("sequence"))
            {
                return "001";
            }

            // CourseID (parent/child for relationships)
            if (paramLower.Contains("courseid") || paramLower.Contains("parentcourseid") || paramLower.Contains("childcourseid"))
            {
                return 999999;  // Use high number to avoid conflicts
            }

            // EffortID
            if (paramLower.Contains("effortid") || paramLower.Contains("percentid"))
            {
                return 999999;
            }

            // Enrollment, Units, Hours, Weeks
            if (paramLower.Contains("enrollment"))
            {
                return 10;
            }
            if (paramLower.Contains("units"))
            {
                return 3.0;
            }
            if (paramLower.Contains("hours"))
            {
                return 40;
            }
            if (paramLower.Contains("weeks"))
            {
                return 10;
            }

            // Percent
            if (paramLower.Contains("percent") && !paramLower.Contains("id"))
            {
                return 50.0;
            }

            // TypeID
            if (paramLower.Contains("typeid"))
            {
                return 1;
            }

            // Unit (for percent tracking)
            if (paramLower.Contains("unit") && !paramLower.Contains("units"))
            {
                return 1;
            }

            // SessionType
            if (paramLower.Contains("sessiontype"))
            {
                return "LEC";  // Lecture
            }

            // Relationship (for course relationships)
            // Valid values: 'Parent', 'Child', 'CrossList', 'Section'
            if (paramLower.Contains("relationship"))
            {
                return "Section";  // Use valid CHECK constraint value
            }

            // TypeClass (for percent tracking)
            // Valid values: 'Clinical', 'Admin', 'Other'
            if (paramLower.Contains("typeclass"))
            {
                return "Clinical";  // Use most common value
            }

            // Action (for audit)
            if (paramLower.Contains("action"))
            {
                return "CREATE";
            }

            // Audit message
            if (paramLower.Contains("audit") && !paramLower.Contains("academic"))
            {
                return "Test audit entry";
            }

            // Comment, Modifier
            if (paramLower.Contains("comment") || paramLower.Contains("modifier"))
            {
                return isNullable ? DBNull.Value : (object)"Test";
            }

            // TitleCode (must check before "title" to match more specifically)
            if (paramLower.Contains("titlecode"))
            {
                return "999999";  // Test title code (char(6) format)
            }

            // Title, Dept
            if (paramLower.Contains("title"))
            {
                return isNullable ? DBNull.Value : (object)"Test Title";
            }
            if (paramLower.Contains("custdept") || paramLower.Contains("effortdept"))
            {
                return isNullable ? DBNull.Value : (object)"VMTH";
            }

            // Role
            if (paramLower.Contains("role"))
            {
                return "1";  // Role ID: 1 = Instructor of Record (IOR)
            }

            // JobGroupID
            if (paramLower.Contains("jobgr") || paramLower.Contains("jobgroup"))
            {
                return "335";  // Common job group (ADJUNCT PROFESSOR)
            }

            // Activity
            if (paramLower.Contains("activity"))
            {
                return "DID";  // Didactic
            }

            // ExcludedTerms (list of term codes to exclude, '0' means none)
            // Some procedures use varchar (comma-separated list), others use bit (include/exclude flag)
            if (paramLower.Contains("excludedterms") || paramLower.Contains("excluded"))
            {
                // Return appropriate type based on SQL type
                if (dataType.ToLowerInvariant() == "bit")
                {
                    return false;  // bit: false = don't exclude
                }
                return "0";  // varchar: '0' means no exclusions
            }

            // Default values based on data type
            switch (dataType.ToLowerInvariant())
            {
                case "int":
                case "bigint":
                case "smallint":
                case "tinyint":
                    return 1;

                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                    return "TEST";

                case "bit":
                    return false;  // Use C# bool for BIT type

                case "float":
                case "real":
                case "decimal":
                case "numeric":
                    return 0.0;

                case "datetime":
                case "datetime2":
                case "date":
                    return DateTime.Now;

                default:
                    // If we can't determine a good value, use NULL
                    return DBNull.Value;
            }
        }

        /// <summary>
        /// Maps SQL Server data type names to SqlDbType enum values.
        /// This ensures parameters are created with the correct SQL type, avoiding
        /// implicit type conversions (e.g., C# int -> SQL INT instead of SQL BIT).
        /// </summary>
        private static SqlDbType GetSqlDbType(string sqlTypeName)
        {
            return sqlTypeName.ToLowerInvariant() switch
            {
                "bit" => SqlDbType.Bit,
                "tinyint" => SqlDbType.TinyInt,
                "smallint" => SqlDbType.SmallInt,
                "int" => SqlDbType.Int,
                "bigint" => SqlDbType.BigInt,
                "decimal" => SqlDbType.Decimal,
                "numeric" => SqlDbType.Decimal,
                "money" => SqlDbType.Money,
                "smallmoney" => SqlDbType.SmallMoney,
                "float" => SqlDbType.Float,
                "real" => SqlDbType.Real,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "time" => SqlDbType.Time,
                "char" => SqlDbType.Char,
                "varchar" => SqlDbType.VarChar,
                "text" => SqlDbType.Text,
                "nchar" => SqlDbType.NChar,
                "nvarchar" => SqlDbType.NVarChar,
                "ntext" => SqlDbType.NText,
                "binary" => SqlDbType.Binary,
                "varbinary" => SqlDbType.VarBinary,
                "image" => SqlDbType.Image,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "xml" => SqlDbType.Xml,
                _ => SqlDbType.Variant  // Default to Variant for unknown types
            };
        }

        // ============================================================================
        // CRUD Procedure Testing
        // ============================================================================

        private static bool IsCrudProcedure(string procedureName)
        {
            string nameLower = procedureName.ToLowerInvariant();
            return nameLower.Contains("create") ||
                   nameLower.Contains("update") ||
                   nameLower.Contains("delete") ||
                   nameLower.StartsWith("usp_del") ||
                   nameLower.Contains("close") ||
                   nameLower.Contains("open") ||
                   nameLower.Contains("reopen") ||
                   nameLower.Contains("unopen") ||
                   nameLower.Contains("verify");
        }

        private static bool TestCrudProcedure(SqlConnection connection, ProcedureTest test, ComparisonResult result)
        {
            // Test CRUD procedures using transactions with rollback
            // This allows us to test write operations without leaving test data
            // AND verify that the procedure actually performs the expected data changes

            using var transaction = connection.BeginTransaction();

            try
            {
                // Special handling for usp_delCourse - verify actual deletion
                if (test.Name.Equals("usp_delCourse", StringComparison.OrdinalIgnoreCase))
                {
                    return TestDeleteCourse(connection, transaction, test, result);
                }

                // Special handling for usp_delInstructorEffort - verify actual deletion
                if (test.Name.Equals("usp_delInstructorEffort", StringComparison.OrdinalIgnoreCase))
                {
                    return TestDeleteEffort(connection, transaction, test, result);
                }

                // Special handling for procedures that need pre-existing test data
                if (test.Name.Equals("usp_createCourseRelationship", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to use existing courses from migrated data first
                    using var findCmd = new SqlCommand(@"
                        SELECT TOP 2 Id FROM [effort].[Courses]
                        ORDER BY Id;", connection, transaction);

                    int? parentCourseId = null;
                    int? childCourseId = null;

                    using (var reader = findCmd.ExecuteReader())
                    {
                        if (reader.Read())
                            parentCourseId = reader.GetInt32(0);
                        if (reader.Read())
                            childCourseId = reader.GetInt32(0);
                    }

                    // If we don't have existing courses, create test courses
                    if (parentCourseId == null || childCourseId == null)
                    {
                        using var setupCmd = new SqlCommand(@"
                            INSERT INTO [effort].[Courses] (Crn, TermCode, SubjCode, CrseNumb, SeqNumb, Enrollment, Units, CustDept)
                            VALUES
                                ('99991', 202410, 'TEST', '001', '001', 0, 1, 'TEST'),
                                ('99992', 202410, 'TEST', '001', '002', 0, 1, 'TEST');

                            SELECT CAST(SCOPE_IDENTITY() AS int) as LastCourseId;", connection, transaction);

                        childCourseId = (int)setupCmd.ExecuteScalar();
                        parentCourseId = childCourseId - 1;
                    }

                    // Update the procedure parameters to use valid IDs
                    foreach (var param in test.Parameters)
                    {
                        if (param.ParameterName.Contains("ParentCourseId", StringComparison.OrdinalIgnoreCase))
                            param.Value = parentCourseId;
                        else if (param.ParameterName.Contains("ChildCourseId", StringComparison.OrdinalIgnoreCase) ||
                                 param.ParameterName.Contains("CourseId", StringComparison.OrdinalIgnoreCase))
                            param.Value = childCourseId;
                    }
                }

                // Special handling for usp_createInstructorEffort - needs real CourseID and MothraID
                // Must satisfy: FK_Records_Persons (PersonId+TermCode must exist in Persons)
                // and CK_Records_HoursOrWeeks (Hours XOR Weeks, not both)
                if (test.Name.Equals("usp_createInstructorEffort", StringComparison.OrdinalIgnoreCase))
                {
                    // Get a real CourseID and matching MothraID/TermCode from migrated data
                    using var findCmd = new SqlCommand(@"
                        SELECT TOP 1 c.Id as CourseId, u.MothraId
                        FROM [effort].[Courses] c
                        INNER JOIN [effort].[Persons] p ON c.TermCode = p.TermCode
                        INNER JOIN [users].[Person] u ON p.PersonId = u.PersonId
                        WHERE u.MothraId IS NOT NULL
                        ORDER BY c.Id;", connection, transaction);

                    using (var reader = findCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int courseId = reader.GetInt32(0);
                            string mothraId = reader.GetString(1);

                            foreach (var param in test.Parameters)
                            {
                                if (param.ParameterName.Equals("@CourseID", StringComparison.OrdinalIgnoreCase))
                                    param.Value = courseId;
                                else if (param.ParameterName.Equals("@MothraID", StringComparison.OrdinalIgnoreCase))
                                    param.Value = mothraId;
                                // Set Weeks to NULL (Hours XOR Weeks constraint)
                                else if (param.ParameterName.Equals("@Weeks", StringComparison.OrdinalIgnoreCase))
                                    param.Value = DBNull.Value;
                            }
                        }
                    }
                }

                // Special handling for usp_createInstructorPercent - needs real MothraID from Persons table
                // Must satisfy FK_Percentages_Persons (PersonId+TermCode must exist in Persons)
                // The trigger uses vwTerms to lookup TermCode from AcademicYear, so we need to ensure
                // the person exists in Persons for a TermCode that matches vwTerms for that academic year
                if (test.Name.Equals("usp_createInstructorPercent", StringComparison.OrdinalIgnoreCase))
                {
                    // Get a MothraID and AcademicYear where the person actually exists in Persons
                    // for a TermCode that vwTerms will return for that academic year
                    using var findCmd = new SqlCommand(@"
                        SELECT TOP 1
                            u.MothraId,
                            CAST(t.AcademicYear - 1 AS varchar) + '-' + CAST(t.AcademicYear AS varchar) as AcademicYear
                        FROM [effort].[Persons] p
                        INNER JOIN [users].[Person] u ON p.PersonId = u.PersonId
                        INNER JOIN [dbo].[vwTerms] t ON p.TermCode = t.TermCode
                        WHERE u.MothraId IS NOT NULL
                        ORDER BY p.TermCode DESC;", connection, transaction);

                    using (var reader = findCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string mothraId = reader.GetString(0);
                            string academicYear = reader.GetString(1);

                            foreach (var param in test.Parameters)
                            {
                                if (param.ParameterName.Equals("@MothraID", StringComparison.OrdinalIgnoreCase))
                                    param.Value = mothraId;
                                else if (param.ParameterName.Equals("@AcademicYear", StringComparison.OrdinalIgnoreCase))
                                    param.Value = academicYear;
                            }
                        }
                    }
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string shadowProcName = $"[EffortShadow].{test.Name}";

                using var cmd = new SqlCommand(shadowProcName, connection, transaction);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                // Add parameters
                foreach (var param in test.Parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }

                // Execute the procedure
                cmd.ExecuteNonQuery();

                stopwatch.Stop();

                // Rollback the transaction (undo all changes)
                transaction.Rollback();

                result.ShadowExecutionMs = stopwatch.ElapsedMilliseconds;
                result.ShadowRowCount = 0; // CRUD procedures don't return rows
                result.Warnings.Add("CRUD procedure tested with transaction rollback");

                return true; // Success - procedure executed without errors
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                result.Status = TestStatus.Failed;
                result.Differences.Add($"CRUD test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests usp_delCourse by verifying actual data deletion.
        /// Finds a real course with effort records, deletes it, verifies deletion, then rolls back.
        /// </summary>
        private static bool TestDeleteCourse(SqlConnection connection, SqlTransaction transaction, ProcedureTest test, ComparisonResult result)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Find a course that has effort records (so we can verify cascade deletion)
            using var findCmd = new SqlCommand(@"
                SELECT TOP 1 c.Id as CourseId, c.Crn, u.MothraId, COUNT(r.Id) as EffortCount
                FROM [effort].[Courses] c
                INNER JOIN [effort].[Records] r ON c.Id = r.CourseId
                INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
                INNER JOIN [users].[Person] u ON p.PersonId = u.PersonId
                WHERE u.MothraId IS NOT NULL
                GROUP BY c.Id, c.Crn, u.MothraId
                HAVING COUNT(r.Id) > 0
                ORDER BY COUNT(r.Id);", connection, transaction);

            int courseId;
            string mothraId;
            int effortCountBefore;

            using (var reader = findCmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    result.Warnings.Add("No course with effort records found for delete test - executing without verification");
                    transaction.Rollback();
                    return TestCrudProcedureFallback(connection, test, result);
                }
                courseId = reader.GetInt32(0);
                mothraId = reader.GetString(2);
                effortCountBefore = reader.GetInt32(3);
            }

            // Verify course exists before deletion
            using var courseCountBeforeCmd = new SqlCommand(
                "SELECT COUNT(*) FROM [EffortShadow].[tblCourses] WHERE course_ID = @CourseID",
                connection, transaction);
            courseCountBeforeCmd.Parameters.AddWithValue("@CourseID", courseId);
            int courseCountBefore = (int)courseCountBeforeCmd.ExecuteScalar();

            if (courseCountBefore == 0)
            {
                result.Differences.Add($"Course {courseId} not visible in shadow view before deletion");
                result.Status = TestStatus.Failed;
                result.Passed = false;
                transaction.Rollback();
                return false;
            }

            // Execute usp_delCourse through the shadow schema
            using var delCmd = new SqlCommand("[EffortShadow].[usp_delCourse]", connection, transaction);
            delCmd.CommandType = CommandType.StoredProcedure;
            delCmd.CommandTimeout = 120;
            delCmd.Parameters.AddWithValue("@CourseID", courseId);
            delCmd.Parameters.AddWithValue("@ModBy", mothraId);
            delCmd.ExecuteNonQuery();

            // Verify effort records were deleted
            using var effortCountAfterCmd = new SqlCommand(
                "SELECT COUNT(*) FROM [effort].[Records] WHERE CourseId = @CourseID",
                connection, transaction);
            effortCountAfterCmd.Parameters.AddWithValue("@CourseID", courseId);
            int effortCountAfter = (int)effortCountAfterCmd.ExecuteScalar();

            // Verify course was deleted
            using var courseCountAfterCmd = new SqlCommand(
                "SELECT COUNT(*) FROM [effort].[Courses] WHERE Id = @CourseID",
                connection, transaction);
            courseCountAfterCmd.Parameters.AddWithValue("@CourseID", courseId);
            int courseCountAfter = (int)courseCountAfterCmd.ExecuteScalar();

            stopwatch.Stop();
            transaction.Rollback();

            result.ShadowExecutionMs = stopwatch.ElapsedMilliseconds;
            result.ShadowRowCount = effortCountBefore + 1; // Effort records + 1 course

            // Verify deletions occurred
            bool success = true;

            if (effortCountAfter != 0)
            {
                result.Differences.Add($"Expected 0 effort records after deletion, found {effortCountAfter}");
                result.Status = TestStatus.Failed;
                result.Passed = false;
                success = false;
            }

            if (courseCountAfter != 0)
            {
                result.Differences.Add($"Expected 0 courses after deletion, found {courseCountAfter}");
                result.Status = TestStatus.Failed;
                result.Passed = false;
                success = false;
            }

            if (success)
            {
                result.Status = TestStatus.Passed;
                result.Passed = true;
                result.Warnings.Add($"Verified: Deleted {effortCountBefore} effort record(s) + 1 course (rolled back)");
            }

            return success;
        }

        /// <summary>
        /// Tests usp_delInstructorEffort by verifying actual data deletion.
        /// </summary>
        private static bool TestDeleteEffort(SqlConnection connection, SqlTransaction transaction, ProcedureTest test, ComparisonResult result)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Find an effort record to delete
            using var findCmd = new SqlCommand(@"
                SELECT TOP 1 r.Id as EffortId, u.MothraId
                FROM [effort].[Records] r
                INNER JOIN [effort].[Persons] p ON r.PersonId = p.PersonId AND r.TermCode = p.TermCode
                INNER JOIN [users].[Person] u ON p.PersonId = u.PersonId
                WHERE u.MothraId IS NOT NULL
                ORDER BY r.Id;", connection, transaction);

            int effortId;
            string mothraId;

            using (var reader = findCmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    result.Warnings.Add("No effort record found for delete test - executing without verification");
                    transaction.Rollback();
                    return TestCrudProcedureFallback(connection, test, result);
                }
                effortId = reader.GetInt32(0);
                mothraId = reader.GetString(1);
            }

            // Verify record exists before deletion
            using var countBeforeCmd = new SqlCommand(
                "SELECT COUNT(*) FROM [effort].[Records] WHERE Id = @EffortID",
                connection, transaction);
            countBeforeCmd.Parameters.AddWithValue("@EffortID", effortId);
            int countBefore = (int)countBeforeCmd.ExecuteScalar();

            if (countBefore == 0)
            {
                result.Differences.Add($"Effort record {effortId} not found before deletion");
                result.Status = TestStatus.Failed;
                result.Passed = false;
                transaction.Rollback();
                return false;
            }

            // Execute usp_delInstructorEffort through the shadow schema
            using var delCmd = new SqlCommand("[EffortShadow].[usp_delInstructorEffort]", connection, transaction);
            delCmd.CommandType = CommandType.StoredProcedure;
            delCmd.CommandTimeout = 120;
            delCmd.Parameters.AddWithValue("@EffortID", effortId);
            delCmd.Parameters.AddWithValue("@ModBy", mothraId);
            delCmd.ExecuteNonQuery();

            // Verify record was deleted
            using var countAfterCmd = new SqlCommand(
                "SELECT COUNT(*) FROM [effort].[Records] WHERE Id = @EffortID",
                connection, transaction);
            countAfterCmd.Parameters.AddWithValue("@EffortID", effortId);
            int countAfter = (int)countAfterCmd.ExecuteScalar();

            stopwatch.Stop();
            transaction.Rollback();

            result.ShadowExecutionMs = stopwatch.ElapsedMilliseconds;
            result.ShadowRowCount = 1; // 1 effort record deleted

            if (countAfter != 0)
            {
                result.Differences.Add($"Expected 0 effort records after deletion, found {countAfter}");
                result.Status = TestStatus.Failed;
                result.Passed = false;
                return false;
            }

            result.Status = TestStatus.Passed;
            result.Passed = true;
            result.Warnings.Add("Verified: Deleted 1 effort record (rolled back)");
            return true;
        }

        /// <summary>
        /// Fallback for CRUD procedures when we can't find test data - just verify it executes without error.
        /// </summary>
        private static bool TestCrudProcedureFallback(SqlConnection connection, ProcedureTest test, ComparisonResult result)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string shadowProcName = $"[EffortShadow].{test.Name}";

                using var cmd = new SqlCommand(shadowProcName, connection, transaction);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120;

                foreach (var param in test.Parameters)
                {
                    cmd.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                }

                cmd.ExecuteNonQuery();
                stopwatch.Stop();
                transaction.Rollback();

                result.ShadowExecutionMs = stopwatch.ElapsedMilliseconds;
                result.ShadowRowCount = 0;
                result.Warnings.Add("CRUD procedure tested with transaction rollback (no verification)");

                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                result.Status = TestStatus.Failed;
                result.Differences.Add($"CRUD test failed: {ex.Message}");
                return false;
            }
        }

        // ============================================================================
        // Data Models
        // ============================================================================

        private class ProcedureTest
        {
            public string Name { get; set; } = string.Empty;
            public SqlParameter[] Parameters { get; set; } = Array.Empty<SqlParameter>();
        }

        private enum TestStatus
        {
            Passed,              // Shadow works correctly (matches legacy or better)
            Failed,              // Shadow has defect (shadow fails, legacy works)
            NeedsInvestigation   // Both fail (environmental issue)
        }

        private class ComparisonResult
        {
            public string ProcedureName { get; set; } = string.Empty;
            public bool Passed { get; set; }  // Keep for backwards compatibility
            public TestStatus Status { get; set; } = TestStatus.Passed;
            public int LegacyRowCount { get; set; }
            public int ShadowRowCount { get; set; }
            public long LegacyExecutionMs { get; set; }
            public long ShadowExecutionMs { get; set; }
            public List<string> Differences { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        private class VerificationReport
        {
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public int TotalProcedures { get; set; }
            public int PassedProcedures { get; set; }
            public int FailedProcedures { get; set; }
            public int NeedsInvestigation { get; set; }  // Both legacy and shadow failed - environmental issues
            public List<ComparisonResult> Results { get; set; } = new List<ComparisonResult>();
        }

        private class PercentVerificationReport
        {
            public int TotalUsersChecked { get; set; }
            public int UsersWithDiscrepancies { get; set; }
            public List<PercentUserResult> Results { get; set; } = new List<PercentUserResult>();
        }

        private class PercentUserResult
        {
            public string MothraId { get; set; } = string.Empty;
            public int LegacyCount { get; set; }
            public int ShadowCount { get; set; }
            public bool Matched { get; set; }
            public List<string> MissingInShadow { get; set; } = new List<string>();
            public List<string> ExtraInShadow { get; set; } = new List<string>();
        }

        // View/Table Verification Data Models

        private class ViewVerificationReport
        {
            public int TotalViews { get; set; }
            public int PassedViews { get; set; }
            public int FailedViews { get; set; }
            public List<ViewVerificationResult> Results { get; set; } = new List<ViewVerificationResult>();
        }

        private class ViewVerificationResult
        {
            public string ViewName { get; set; } = string.Empty;
            public string LegacyTable { get; set; } = string.Empty;
            public bool SchemaPassed { get; set; }
            public bool DataPassed { get; set; }
            public bool ContentPassed { get; set; } = true;  // Data content comparison
            public int LegacyColumnCount { get; set; }
            public int ShadowColumnCount { get; set; }
            public int LegacyRowCount { get; set; }
            public int ShadowRowCount { get; set; }
            public int SampleRowsCompared { get; set; }
            public int ContentMismatches { get; set; }
            public List<string> MissingColumns { get; set; } = new List<string>();
            public List<string> ExtraColumns { get; set; } = new List<string>();
            public List<string> ContentDifferences { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
        }

        private class ColumnInfo
        {
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public int MaxLength { get; set; }
            public bool IsNullable { get; set; }
        }

        // ============================================================================
        // Helper Functions
        // ============================================================================

        private static string GetDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return $"{builder.DataSource}/{builder.InitialCatalog}";
        }

        private static DataTable ExecuteProcedure(SqlConnection conn, string procedureName, List<SqlParameter> parameters)
        {
            var dataTable = new DataTable();

            using var cmd = new SqlCommand(procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;  // 2 minutes

            // Clone parameters to avoid issues with reusing them
            // Important: Must preserve SqlDbType and Direction to avoid type inference issues and OUTPUT parameter issues
            foreach (var param in parameters)
            {
                var clonedParam = new SqlParameter(param.ParameterName, param.Value)
                {
                    SqlDbType = param.SqlDbType,
                    Direction = param.Direction
                };

                // Preserve Size for OUTPUT parameters (required for varchar OUTPUT params)
                if (param.Direction == ParameterDirection.Output && param.Size > 0)
                {
                    clonedParam.Size = param.Size;
                }

                cmd.Parameters.Add(clonedParam);
            }

            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dataTable);

            return dataTable;
        }

        private static void CompareResults(DataTable legacy, DataTable shadow, ComparisonResult result, string procedureName, bool orderInsensitive = false, SqlConnection? legacyConn = null)
        {
            // Row count comparison
            if (legacy.Rows.Count != shadow.Rows.Count)
            {
                // Check if this is a known discrepancy (Shadow returns more due to legacy NULL academic year bug)
                // Only downgrade to warning if the NULL academic year condition is actually present in legacy data
                if (ProceduresAllowingMoreShadowRows.Contains(procedureName)
                    && shadow.Rows.Count > legacy.Rows.Count)
                {
                    bool legacyHasNullAcademicYears = legacyConn != null && LegacyHasNullAcademicYears(legacyConn);
                    if (legacyHasNullAcademicYears)
                    {
                        result.Warnings.Add($"Row count differs (KNOWN ISSUE): Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count}");
                        result.Warnings.Add($"  Reason: {NullAcademicYearReason}");
                        // Don't return - continue with comparison of available rows
                    }
                    else
                    {
                        // No NULL academic years in legacy - this is a genuine mismatch
                        result.Differences.Add($"Row count mismatch: Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count}");
                        return;
                    }
                }
                else if (ProceduresWithPotentialUnmappedLegacyRows.Contains(procedureName)
                    && legacy.Rows.Count > shadow.Rows.Count)
                {
                    // Legacy has more rows - will check if they're from unmapped MothraIds during detailed comparison
                    result.Warnings.Add($"Row count differs: Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count}");
                    result.Warnings.Add($"  Will check if extra Legacy rows have unmapped MothraId (not in VIPER.users.Person)");
                    // Don't return - continue with comparison to check for unmapped records
                }
                else
                {
                    result.Differences.Add($"Row count mismatch: Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count}");
                    return; // Can't meaningfully compare data if row counts differ
                }
            }

            // Column count comparison
            if (legacy.Columns.Count != shadow.Columns.Count)
            {
                result.Warnings.Add($"Column count differs: Legacy={legacy.Columns.Count}, Shadow={shadow.Columns.Count}");
            }

            // Build column mapping (should be 1:1 for shadow schema)
            var columnMapping = BuildColumnMapping(legacy.Columns, shadow.Columns);

            if (columnMapping.UnmappedLegacyColumns.Any())
            {
                result.Warnings.Add($"Unmapped legacy columns: {string.Join(", ", columnMapping.UnmappedLegacyColumns)}");
            }

            if (columnMapping.UnmappedShadowColumns.Any())
            {
                result.Warnings.Add($"Unmapped shadow columns: {string.Join(", ", columnMapping.UnmappedShadowColumns)}");
            }

            if (orderInsensitive)
            {
                // For order-insensitive procedures, compare row sets instead of row-by-row
                result.Warnings.Add("Order-insensitive comparison: checking for matching data sets (ORDER BY differences ignored)");
                CompareResultSetsUnordered(legacy, shadow, columnMapping, result, procedureName, legacyConn);
            }
            else
            {
                // Data value comparison (sample first 100 rows to avoid huge reports)
                int rowsToCheck = Math.Min(100, Math.Min(legacy.Rows.Count, shadow.Rows.Count));

                for (int i = 0; i < rowsToCheck && result.Differences.Count < 50; i++)
                {
                    CompareRows(legacy.Rows[i], shadow.Rows[i], columnMapping, result, i, procedureName);
                }

                if (legacy.Rows.Count > 100 || shadow.Rows.Count > 100)
                {
                    result.Warnings.Add($"Only compared first 100 rows (total rows: Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count})");
                }
            }
        }

        private static (Dictionary<string, string> Mapping, List<string> UnmappedLegacyColumns, List<string> UnmappedShadowColumns) BuildColumnMapping(
            DataColumnCollection legacyColumns,
            DataColumnCollection shadowColumns)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var unmappedLegacy = new List<string>();
            var unmappedShadow = new List<string>();

            // For shadow schema, columns should match exactly
            foreach (DataColumn legacyCol in legacyColumns)
            {
                if (shadowColumns.Contains(legacyCol.ColumnName))
                {
                    mapping[legacyCol.ColumnName] = legacyCol.ColumnName;
                }
                else
                {
                    unmappedLegacy.Add(legacyCol.ColumnName);
                }
            }

            // Find shadow columns without legacy counterpart
            foreach (DataColumn shadowCol in shadowColumns)
            {
                if (!mapping.ContainsValue(shadowCol.ColumnName))
                    unmappedShadow.Add(shadowCol.ColumnName);
            }

            return (mapping, unmappedLegacy, unmappedShadow);
        }

        private static void CompareResultSetsUnordered(DataTable legacy, DataTable shadow,
            (Dictionary<string, string> Mapping, List<string> UnmappedLegacyColumns, List<string> UnmappedShadowColumns) columnMapping,
            ComparisonResult result, string procedureName, SqlConnection? legacyConn = null)
        {
            // Build sets of row "signatures" to compare data sets without caring about order
            var legacyRowSignatures = new HashSet<string>();
            var shadowRowSignatures = new HashSet<string>();

            // Also track MothraID -> signature mapping for orphan checking
            var legacyMothraIdToSignatures = new Dictionary<string, List<string>>();

            // Create signatures for legacy rows
            foreach (DataRow row in legacy.Rows)
            {
                var sig = GetRowSignature(row, columnMapping.Mapping);
                legacyRowSignatures.Add(sig);

                // Track MothraID if this column exists (for orphan detection)
                if (legacy.Columns.Contains("percent_MothraID") || legacy.Columns.Contains("mothraid"))
                {
                    var mothraIdCol = legacy.Columns.Contains("percent_MothraID") ? "percent_MothraID" : "mothraid";
                    var mothraId = row[mothraIdCol]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(mothraId))
                    {
                        if (!legacyMothraIdToSignatures.ContainsKey(mothraId))
                            legacyMothraIdToSignatures[mothraId] = new List<string>();
                        legacyMothraIdToSignatures[mothraId].Add(sig);
                    }
                }
            }

            // Create signatures for shadow rows
            foreach (DataRow row in shadow.Rows)
            {
                shadowRowSignatures.Add(GetRowSignature(row, columnMapping.Mapping));
            }

            // Find rows in legacy but not in shadow
            var missingInShadow = legacyRowSignatures.Except(shadowRowSignatures).ToList();
            // Find rows in shadow but not in legacy
            var extraInShadow = shadowRowSignatures.Except(legacyRowSignatures).ToList();

            if (missingInShadow.Any())
            {
                // Check if this procedure can have records with unmapped MothraIds
                if (ProceduresWithPotentialUnmappedLegacyRows.Contains(procedureName) && legacyConn != null)
                {
                    // For each missing row, check if it's from an orphaned record
                    int orphanedCount = 0;
                    int realMissingCount = 0;
                    var orphanedRows = new List<string>();
                    var realMissingRows = new List<string>();

                    // Find which MothraIDs have missing rows
                    var mothraIdsWithMissingRows = new HashSet<string>();
                    foreach (var sig in missingInShadow)
                    {
                        foreach (var kvp in legacyMothraIdToSignatures.Where(kvp => kvp.Value.Contains(sig)))
                        {
                            mothraIdsWithMissingRows.Add(kvp.Key);
                        }
                    }

                    // Check each MothraID for orphan status
                    var orphanedMothraIds = CheckForOrphanedMothraIds(legacyConn, mothraIdsWithMissingRows);

                    foreach (var sig in missingInShadow)
                    {
                        bool isOrphaned = legacyMothraIdToSignatures
                            .Any(kvp => kvp.Value.Contains(sig) && orphanedMothraIds.Contains(kvp.Key));

                        if (isOrphaned)
                        {
                            orphanedCount++;
                            orphanedRows.Add(sig);
                        }
                        else
                        {
                            realMissingCount++;
                            realMissingRows.Add(sig);
                        }
                    }

                    // Report orphaned rows as warnings (expected behavior)
                    if (orphanedCount > 0)
                    {
                        result.Warnings.Add($"Orphaned Legacy rows (KNOWN ISSUE - no tblPerson for that academic year): {orphanedCount}");
                        foreach (var sig in orphanedRows.Take(5))
                        {
                            result.Warnings.Add($"  Orphaned row: {sig}");
                        }
                        if (orphanedRows.Count > 5)
                            result.Warnings.Add($"  ... and {orphanedRows.Count - 5} more orphaned rows");
                    }

                    // Report real missing rows as errors
                    if (realMissingCount > 0)
                    {
                        result.Differences.Add($"Rows in Legacy but NOT in Shadow (with valid tblPerson): {realMissingCount}");
                        foreach (var sig in realMissingRows.Take(5))
                        {
                            result.Differences.Add($"  Missing row: {sig}");
                        }
                        if (realMissingRows.Count > 5)
                            result.Differences.Add($"  ... and {realMissingRows.Count - 5} more missing rows");
                    }
                }
                else
                {
                    // Standard handling - all missing rows are errors
                    result.Differences.Add($"Rows in Legacy but NOT in Shadow: {missingInShadow.Count}");
                    // Show first few examples
                    foreach (var sig in missingInShadow.Take(5))
                    {
                        result.Differences.Add($"  Missing row: {sig}");
                    }
                }
            }

            if (extraInShadow.Any())
            {
                // Check if extra rows in Shadow are expected for this procedure (known legacy bug)
                // Only downgrade to warning if the NULL academic year condition is actually present
                if (ProceduresAllowingMoreShadowRows.Contains(procedureName))
                {
                    bool legacyHasNullAcademicYears = legacyConn != null && LegacyHasNullAcademicYears(legacyConn);
                    if (legacyHasNullAcademicYears)
                    {
                        result.Warnings.Add($"Extra rows in Shadow (KNOWN ISSUE): {extraInShadow.Count}");
                        result.Warnings.Add($"  Reason: {NullAcademicYearReason}");
                        // Show first few examples as warnings
                        foreach (var sig in extraInShadow.Take(5))
                        {
                            result.Warnings.Add($"  Extra row: {sig}");
                        }
                    }
                    else
                    {
                        // No NULL academic years in legacy - this is a genuine mismatch
                        result.Differences.Add($"Rows in Shadow but NOT in Legacy: {extraInShadow.Count}");
                        foreach (var sig in extraInShadow.Take(5))
                        {
                            result.Differences.Add($"  Extra row: {sig}");
                        }
                    }
                }
                else
                {
                    result.Differences.Add($"Rows in Shadow but NOT in Legacy: {extraInShadow.Count}");
                    // Show first few examples
                    foreach (var sig in extraInShadow.Take(5))
                    {
                        result.Differences.Add($"  Extra row: {sig}");
                    }
                }
            }
        }

        /// <summary>
        /// Checks which MothraIDs have orphaned tblPercent records (no corresponding tblPerson for that academic year).
        /// An orphaned record is a tblPercent entry where no tblPerson record exists for the same MothraID
        /// in the academic year derived from the percent's start date.
        /// </summary>
        private static HashSet<string> CheckForOrphanedMothraIds(SqlConnection legacyConn, HashSet<string> mothraIds)
        {
            var orphanedIds = new HashSet<string>();
            if (mothraIds.Count == 0) return orphanedIds;

            // Use shared helper for academic year derivation
            var academicYearExpr = EffortScriptHelper.GetAcademicYearFromDateSql("p.percent_start");

            // For each MothraID, check if there's any percent record without a corresponding tblPerson entry
            // for that academic year. If so, the MothraID has orphaned records.
            var query = $@"
                SELECT TOP 1 1
                FROM tblPercent p
                WHERE p.percent_MothraID = @MothraId
                  AND p.percent_start IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM tblPerson per
                    INNER JOIN tblStatus stat ON per.person_TermCode = stat.status_TermCode
                    WHERE per.person_MothraID = p.percent_MothraID
                      AND stat.status_AcademicYear = {academicYearExpr}
                  )";

            using var cmd = new SqlCommand(query, legacyConn);
            cmd.Parameters.Add("@MothraId", SqlDbType.VarChar, 20);

            foreach (var mothraId in mothraIds)
            {
                cmd.Parameters["@MothraId"].Value = mothraId;
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    orphanedIds.Add(mothraId);
                }
            }

            return orphanedIds;
        }

        /// <summary>
        /// Checks if the legacy Efforts database has any tblPercent records with NULL percent_AcademicYear.
        /// Used to gate the "known issue" downgrade for row count mismatches - only allow downgrade
        /// when the legacy NULL academic year bug is actually present in the data.
        /// </summary>
        private static bool LegacyHasNullAcademicYears(SqlConnection legacyConn)
        {
            const string query = @"
                SELECT TOP 1 1
                FROM tblPercent
                WHERE percent_AcademicYear IS NULL
                  AND percent_start IS NOT NULL";

            using var cmd = new SqlCommand(query, legacyConn);
            var result = cmd.ExecuteScalar();
            return result != null;
        }

        private static string GetRowSignature(DataRow row, Dictionary<string, string> columnMapping)
        {
            // Create a normalized string representation of the row for comparison
            // Skip columns that have known acceptable differences (e.g., person_ClientID)
            var values = columnMapping.Keys
                .OrderBy(k => k)
                .Where(col => !KnownValueMappings.ContainsKey(col))  // Skip columns with known differences
                .Select(col =>
                {
                    var value = row[col];
                    if (value == DBNull.Value)
                        return "NULL";
                    else if (value is string str)
                        return str.Trim();
                    else if (value is DateTime dt)
                        return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    else if (IsNumeric(value))
                        return Convert.ToDecimal(value).ToString("F2");
                    else
                        return value.ToString() ?? "";
                })
                .ToList();
            return string.Join("|", values);
        }

        private static void CompareRows(DataRow legacyRow, DataRow shadowRow,
            (Dictionary<string, string> Mapping, List<string> UnmappedLegacyColumns, List<string> UnmappedShadowColumns) columnMapping,
            ComparisonResult result, int rowIndex, string procedureName)
        {
            foreach (var mapping in columnMapping.Mapping)
            {
                string legacyCol = mapping.Key;
                string shadowCol = mapping.Value;

                var legacyValue = legacyRow[legacyCol];
                var shadowValue = shadowRow[shadowCol];

                // Detect whitespace padding differences (char vs varchar type mismatch)
                // This catches cases where strings are semantically equal but have different lengths
                // due to CHAR column padding with trailing spaces
                if (legacyValue is string legacyStr && shadowValue is string shadowStr &&
                    legacyStr != shadowStr && legacyStr.Trim() == shadowStr.Trim())
                {
                    result.Warnings.Add(
                        $"Row {rowIndex}, Column '{legacyCol}': WHITESPACE PADDING DIFFERENCE - " +
                        $"Legacy='{legacyStr}' ({legacyStr.Length} chars), " +
                        $"Shadow='{shadowStr}' ({shadowStr.Length} chars). " +
                        "This indicates a char/varchar type mismatch in the schema.");
                    // Compare trimmed strings for actual difference
                    if (legacyStr.Trim() != shadowStr.Trim())
                    {
                        result.Differences.Add($"Row {rowIndex}, Column '{legacyCol}': Legacy='{legacyStr}' vs Shadow='{shadowStr}'");
                    }
                }
                else if (!ValuesEqual(legacyValue, shadowValue))
                {
                    // Check if this is a known acceptable column difference (e.g., person_ClientID)
                    string legacyNormalized = NormalizeValue(legacyValue);
                    string shadowNormalized = NormalizeValue(shadowValue);
                    if (IsKnownValueDifference(legacyCol, legacyNormalized, shadowNormalized, procedureName))
                    {
                        continue;  // Skip known differences
                    }

                    // Check if this is a known issue procedure where Shadow returns more/higher values
                    // due to legacy NULL percent_AcademicYear bug
                    if (ProceduresAllowingMoreShadowRows.Contains(procedureName) &&
                        IsNumeric(legacyValue) && IsNumeric(shadowValue))
                    {
                        decimal legacyNum = Convert.ToDecimal(legacyValue);
                        decimal shadowNum = Convert.ToDecimal(shadowValue);

                        // Shadow returning higher numeric value is expected for these procedures
                        // (Legacy misses records with NULL percent_AcademicYear)
                        if (shadowNum >= legacyNum)
                        {
                            result.Warnings.Add($"Row {rowIndex}, Column '{legacyCol}': Legacy='{legacyValue}' vs Shadow='{shadowValue}' (KNOWN ISSUE - Shadow includes records with NULL percent_AcademicYear)");
                            continue;
                        }
                    }

                    // Compare non-string values
                    result.Differences.Add($"Row {rowIndex}, Column '{legacyCol}': Legacy='{legacyValue}' vs Shadow='{shadowValue}'");
                }
            }
        }

        private static bool ValuesEqual(object value1, object value2)
        {
            if (value1 == DBNull.Value && value2 == DBNull.Value) return true;
            if (value1 == DBNull.Value || value2 == DBNull.Value) return false;

            // DateTime comparison with tolerance (handles datetime vs datetime2 precision)
            if (value1 is DateTime dt1 && value2 is DateTime dt2)
            {
                // Allow up to 1 second difference to account for different SQL datetime precisions
                // datetime has ~3.33ms precision, datetime2 has up to 100ns precision
                return Math.Abs((dt1 - dt2).TotalMilliseconds) < 1000;
            }

            // Normalize strings (trim whitespace)
            if (value1 is string str1 && value2 is string str2)
                return str1.Trim().Equals(str2.Trim(), StringComparison.OrdinalIgnoreCase);

            // Numeric comparison with tolerance
            if (IsNumeric(value1) && IsNumeric(value2))
            {
                decimal dec1 = Convert.ToDecimal(value1);
                decimal dec2 = Convert.ToDecimal(value2);
                return Math.Abs(dec1 - dec2) < 0.01m;  // Tolerance for floating point
            }

            return value1.Equals(value2);
        }

        private static bool IsNumeric(object value)
        {
            return value is int || value is decimal || value is double || value is float || value is long;
        }

        /// <summary>
        /// Verifies tblPercent migration by comparing the top 25 users with most records.
        /// This method filters out orphaned records (those without a corresponding tblPerson entry)
        /// since these are intentionally not migrated per tblPercent-tblPerson-relationship.md.
        /// </summary>
        /// <param name="legacyConnectionString">Connection string for legacy Effort database</param>
        /// <param name="shadowConnectionString">Connection string for VIPER database with EffortShadow schema</param>
        /// <param name="verbose">Enable verbose output</param>
        /// <returns>Report containing verification results</returns>
        private static PercentVerificationReport VerifyTblPercentByTopUsers(string legacyConnectionString, string shadowConnectionString, bool verbose = false)
        {
            var percentReport = new PercentVerificationReport();

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("tblPercent Migration Verification (Top 25 Users)");
            Console.WriteLine("================================================================================");

            // Step 1: Find top 25 people with most records in legacy
            var topUsersQuery = @"
                SELECT TOP 25 p.percent_MothraID, COUNT(*) as cnt
                FROM tblPercent p
                WHERE p.percent_start IS NOT NULL
                GROUP BY p.percent_MothraID
                ORDER BY cnt DESC";

            var topUsers = new List<(string MothraId, int LegacyCount)>();

            using (var legacyConn = new SqlConnection(legacyConnectionString))
            {
                legacyConn.Open();
                using var cmd = new SqlCommand(topUsersQuery, legacyConn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    topUsers.Add((reader.GetString(0), reader.GetInt32(1)));
                }
            }

            percentReport.TotalUsersChecked = topUsers.Count;
            Console.WriteLine($"Found {topUsers.Count} users with the most percentage records.");
            Console.WriteLine();

            // Step 2: For each user, compare legacy vs shadow
            foreach (var user in topUsers)
            {
                Console.Write($"Checking MothraID {user.MothraId} ({user.LegacyCount} records in legacy)... ");

                var legacyRecords = GetLegacyPercentRecords(legacyConnectionString, user.MothraId);

                // Get shadow records
                var shadowRecords = GetShadowPercentRecords(shadowConnectionString, user.MothraId);

                // Compare using composite key: typeID + start + unit + modifier
                var legacySet = legacyRecords.Select(r => GetPercentRecordKey(r)).ToHashSet();
                var shadowSet = shadowRecords.Select(r => GetPercentRecordKey(r)).ToHashSet();

                var missingInShadow = legacySet.Except(shadowSet).ToList();
                var extraInShadow = shadowSet.Except(legacySet).ToList();

                var userResult = new PercentUserResult
                {
                    MothraId = user.MothraId,
                    LegacyCount = legacyRecords.Count,
                    ShadowCount = shadowRecords.Count,
                    Matched = missingInShadow.Count == 0 && extraInShadow.Count == 0,
                    MissingInShadow = missingInShadow,
                    ExtraInShadow = extraInShadow
                };
                percentReport.Results.Add(userResult);

                if (userResult.Matched)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ MATCH ({shadowRecords.Count} records)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ MISMATCH");
                    Console.ResetColor();
                    percentReport.UsersWithDiscrepancies++;

                    if (missingInShadow.Count > 0)
                    {
                        Console.WriteLine($"    Missing in shadow: {missingInShadow.Count}");
                        if (verbose)
                        {
                            foreach (var key in missingInShadow.Take(5))
                            {
                                Console.WriteLine($"      - {key}");
                            }
                            if (missingInShadow.Count > 5)
                                Console.WriteLine($"      ... and {missingInShadow.Count - 5} more");
                        }
                    }

                    if (extraInShadow.Count > 0)
                    {
                        Console.WriteLine($"    Extra in shadow: {extraInShadow.Count}");
                        if (verbose)
                        {
                            foreach (var key in extraInShadow.Take(5))
                            {
                                Console.WriteLine($"      - {key}");
                            }
                            if (extraInShadow.Count > 5)
                                Console.WriteLine($"      ... and {extraInShadow.Count - 5} more");
                        }
                    }
                }
            }

            Console.WriteLine();
            if (percentReport.UsersWithDiscrepancies == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All top users' percentage records verified successfully");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {percentReport.UsersWithDiscrepancies} of {topUsers.Count} users have discrepancies");
                Console.ResetColor();
            }
            Console.WriteLine("================================================================================");

            return percentReport;
        }

        private static List<PercentRecord> GetLegacyPercentRecords(string connectionString, string mothraId)
        {
            var records = new List<PercentRecord>();

            var query = @"
                SELECT p.percent_ID, p.percent_TypeID, p.percent_start, p.percent_end,
                       p.percent_Percent, p.percent_Unit, p.percent_Modifier
                FROM tblPercent p
                WHERE p.percent_MothraID = @MothraId
                  AND p.percent_start IS NOT NULL
                ORDER BY p.percent_start, p.percent_TypeID";

            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MothraId", mothraId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new PercentRecord
                {
                    Id = reader.GetInt32(0),
                    TypeId = reader.GetInt32(1),
                    StartDate = reader.GetDateTime(2),
                    EndDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    Percentage = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetDouble(4)),
                    Unit = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Modifier = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return records;
        }

        private static List<PercentRecord> GetShadowPercentRecords(string connectionString, string mothraId)
        {
            var records = new List<PercentRecord>();

            // Query shadow view (tblPercent in EffortShadow schema)
            var query = @"
                SELECT s.percent_ID, s.percent_TypeID, s.percent_start, s.percent_end,
                       s.percent_Percent, s.percent_Unit, s.percent_Modifier
                FROM [EffortShadow].[tblPercent] s
                WHERE s.percent_MothraID = @MothraId
                ORDER BY s.percent_start, s.percent_TypeID";

            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MothraId", mothraId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new PercentRecord
                {
                    Id = reader.GetInt32(0),
                    TypeId = reader.GetInt32(1),
                    StartDate = reader.GetDateTime(2),
                    EndDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    Percentage = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetDouble(4)),
                    Unit = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Modifier = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return records;
        }

        private static string GetPercentRecordKey(PercentRecord record)
        {
            // Composite key: typeID + startDate + unit + modifier
            // (start date formatted as ISO 8601 for reliable comparison)
            var unit = string.IsNullOrWhiteSpace(record.Unit) ? "NULL" : record.Unit.Trim();
            var modifier = string.IsNullOrWhiteSpace(record.Modifier) ? "NULL" : record.Modifier.Trim();
            return $"Type:{record.TypeId}|Start:{record.StartDate:yyyy-MM-dd}|Unit:{unit}|Mod:{modifier}";
        }

        // ============================================================================
        // View/Table Schema Verification
        // ============================================================================

        /// <summary>
        /// Verifies that shadow views match legacy table schemas and data.
        /// This runs BEFORE stored procedure verification since views are prerequisites for SPs.
        /// </summary>
        private static ViewVerificationReport VerifyShadowViews(string legacyConnectionString, string shadowConnectionString, bool verbose)
        {
            var report = new ViewVerificationReport { TotalViews = ViewToLegacyTable.Count };

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("Shadow View/Table Verification");
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            using var legacyConn = new SqlConnection(legacyConnectionString);
            using var shadowConn = new SqlConnection(shadowConnectionString);
            legacyConn.Open();
            shadowConn.Open();

            foreach (var viewMapping in ViewToLegacyTable)
            {
                string viewName = viewMapping.Key;
                string legacyTable = viewMapping.Value;

                Console.Write($"Checking {viewName}... ");

                var result = new ViewVerificationResult
                {
                    ViewName = viewName,
                    LegacyTable = legacyTable
                };

                try
                {
                    // Step 1: Schema verification
                    VerifyViewSchema(legacyConn, shadowConn, viewName, legacyTable, result, verbose);

                    // Step 2: Data verification (row counts with expected difference handling)
                    VerifyViewData(legacyConn, shadowConn, viewName, legacyTable, result, verbose);

                    // Step 3: Content verification (compare actual data values)
                    VerifyViewContent(legacyConn, shadowConn, viewName, legacyTable, result, verbose);

                    // Determine pass/fail
                    bool passed = result.SchemaPassed && result.DataPassed && result.ContentPassed;
                    if (passed)
                    {
                        report.PassedViews++;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ OK");
                        Console.ResetColor();

                        if (verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"    Schema: {result.LegacyColumnCount} columns OK");
                            if (result.LegacyRowCount == result.ShadowRowCount)
                            {
                                Console.WriteLine($"    Data: {result.LegacyRowCount} rows OK");
                            }
                            else
                            {
                                Console.WriteLine($"    Data: {result.LegacyRowCount} legacy, {result.ShadowRowCount} shadow (expected difference) OK");
                            }
                            if (result.SampleRowsCompared > 0)
                            {
                                Console.WriteLine($"    Content: {result.SampleRowsCompared} rows compared OK");
                            }
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        report.FailedViews++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ FAIL");
                        Console.ResetColor();

                        foreach (var error in result.Errors)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"    {error}");
                            Console.ResetColor();
                        }

                        // Show content differences in verbose mode (up to 5 rows)
                        if (verbose && result.ContentDifferences.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"    Data differences (up to 5 rows):");
                            foreach (var diff in result.ContentDifferences)
                            {
                                Console.WriteLine($"    {diff}");
                            }
                            Console.ResetColor();
                        }
                    }

                    // Show warnings in verbose mode
                    if (verbose && result.Warnings.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        foreach (var warning in result.Warnings)
                        {
                            Console.WriteLine($"    ⚠ {warning}");
                        }
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    report.FailedViews++;
                    result.Errors.Add($"Exception: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ ERROR: {ex.Message}");
                    Console.ResetColor();
                }

                report.Results.Add(result);
            }

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.Write($"View Verification Summary: {report.TotalViews} views, ");
            if (report.FailedViews == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"0 failures");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{report.FailedViews} failures");
            }
            Console.ResetColor();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            return report;
        }

        /// <summary>
        /// Compares column schema between legacy table and shadow view.
        /// </summary>
        private static void VerifyViewSchema(SqlConnection legacyConn, SqlConnection shadowConn,
            string viewName, string legacyTable, ViewVerificationResult result, bool verbose)
        {
            // Get legacy table columns
            var legacyColumns = GetTableColumns(legacyConn, legacyTable, isLegacy: true);
            result.LegacyColumnCount = legacyColumns.Count;

            // Get shadow view columns
            var shadowColumns = GetTableColumns(shadowConn, viewName, isLegacy: false);
            result.ShadowColumnCount = shadowColumns.Count;

            // Compare column names (case-insensitive)
            var legacyColNames = legacyColumns.Select(c => c.ColumnName.ToLowerInvariant()).ToHashSet();
            var shadowColNames = shadowColumns.Select(c => c.ColumnName.ToLowerInvariant()).ToHashSet();

            // Find missing columns (in legacy but not in shadow)
            var missingInShadow = legacyColNames.Except(shadowColNames).ToList();
            foreach (var col in missingInShadow)
            {
                result.MissingColumns.Add(col);
            }

            // Find extra columns (in shadow but not in legacy)
            var extraInShadow = shadowColNames.Except(legacyColNames).ToList();
            foreach (var col in extraInShadow)
            {
                result.ExtraColumns.Add(col);
            }

            // Schema passes if all legacy columns exist in shadow
            // Extra columns in shadow are OK (may be derived/computed columns)
            if (missingInShadow.Count == 0)
            {
                result.SchemaPassed = true;
            }
            else
            {
                result.SchemaPassed = false;
                result.Errors.Add($"Missing columns in shadow: {string.Join(", ", missingInShadow)}");
            }

            // Warn about extra columns
            if (extraInShadow.Count > 0)
            {
                result.Warnings.Add($"Extra columns in shadow (OK): {string.Join(", ", extraInShadow)}");
            }
        }

        /// <summary>
        /// Compares row counts between legacy table and shadow view.
        /// Accounts for expected differences due to skipped migration records.
        /// </summary>
        private static void VerifyViewData(SqlConnection legacyConn, SqlConnection shadowConn,
            string viewName, string legacyTable, ViewVerificationResult result, bool verbose)
        {
            // Get row counts
            result.LegacyRowCount = GetRowCount(legacyConn, legacyTable, isLegacy: true);
            result.ShadowRowCount = GetRowCount(shadowConn, viewName, isLegacy: false);

            // Check if row counts match
            if (result.LegacyRowCount == result.ShadowRowCount)
            {
                result.DataPassed = true;
                return;
            }

            // Check if this view has expected row differences
            if (ViewsWithExpectedRowDifferences.TryGetValue(viewName, out string? reason))
            {
                // Shadow should have FEWER rows than legacy (skipped records)
                if (result.ShadowRowCount < result.LegacyRowCount)
                {
                    result.DataPassed = true;
                    int skipped = result.LegacyRowCount - result.ShadowRowCount;
                    result.Warnings.Add($"Row count difference: {skipped} records skipped ({reason})");
                }
                else
                {
                    // Shadow has MORE rows than legacy - unexpected
                    result.DataPassed = false;
                    result.Errors.Add($"Shadow has MORE rows than legacy: Legacy={result.LegacyRowCount}, Shadow={result.ShadowRowCount}");
                }
            }
            else
            {
                // No expected difference - this is a failure
                result.DataPassed = false;
                result.Errors.Add($"Row count mismatch: Legacy={result.LegacyRowCount}, Shadow={result.ShadowRowCount}");
            }
        }

        /// <summary>
        /// Compares actual data content between legacy table and shadow view.
        /// Compares a sample of rows using only legacy columns (ignores extra shadow columns).
        /// </summary>
        private static void VerifyViewContent(SqlConnection legacyConn, SqlConnection shadowConn,
            string viewName, string legacyTable, ViewVerificationResult result, bool verbose)
        {
            const int sampleSize = 100;

            // Skip if schema failed (can't compare content without matching columns)
            if (!result.SchemaPassed)
            {
                return;
            }

            // Get primary key column(s) for ordering - may be comma-separated for composite keys
            if (!ViewPrimaryKeys.TryGetValue(viewName, out string? primaryKeySpec))
            {
                result.Warnings.Add($"Content comparison skipped (no primary key defined)");
                return;
            }

            // Parse composite keys
            var primaryKeys = primaryKeySpec.Split(',').Select(k => k.Trim()).ToList();
            string orderByClause = string.Join(", ", primaryKeys.Select(k => $"[{k}]"));

            // Get legacy columns (these are the columns we'll compare)
            var legacyColumns = GetTableColumns(legacyConn, legacyTable, isLegacy: true);
            if (legacyColumns.Count == 0)
            {
                result.Warnings.Add($"Content comparison skipped (no columns found)");
                return;
            }

            // Build column list for SELECT (only legacy columns, case-insensitive match)
            var columnList = string.Join(", ", legacyColumns.Select(c => $"[{c.ColumnName}]"));

            // Determine sampling strategy
            bool useRandomSampling = LargeViewsForRandomSampling.Contains(viewName);
            string samplingNote = useRandomSampling ? " (random sample)" : "";

            // Query legacy data - use random sampling for large tables
            string shadowQuery;
            string legacyQuery = useRandomSampling
                ? $@"
                    SELECT TOP {sampleSize} {columnList}
                    FROM [{legacyTable}]
                    ORDER BY NEWID()"
                : $"SELECT TOP {sampleSize} {columnList} FROM [{legacyTable}] ORDER BY {orderByClause}";

            var legacyData = ExecuteQuery(legacyConn, legacyQuery);

            // For random sampling, we need to get the same rows from shadow by matching primary keys
            if (useRandomSampling && legacyData.Rows.Count > 0)
            {
                // Build WHERE clause with composite key matching
                var whereConditions = new List<string>();
                foreach (DataRow row in legacyData.Rows)
                {
                    var keyParts = new List<string>();
                    foreach (var pk in primaryKeys)
                    {
                        var pkVal = row[pk];
                        if (pkVal == null || pkVal == DBNull.Value)
                            keyParts.Add($"[{pk}] IS NULL");
                        else if (pkVal is string s)
                            keyParts.Add($"[{pk}] = '{s.Replace("'", "''")}'");
                        else
                            keyParts.Add($"[{pk}] = {pkVal}");
                    }
                    whereConditions.Add($"({string.Join(" AND ", keyParts)})");
                }

                shadowQuery = $@"
                    SELECT {columnList}
                    FROM [EffortShadow].[{viewName}]
                    WHERE {string.Join(" OR ", whereConditions)}
                    ORDER BY {orderByClause}";

                // Re-sort legacy data by primary key to match
                legacyData.DefaultView.Sort = orderByClause.Replace("[", "").Replace("]", "");
                legacyData = legacyData.DefaultView.ToTable();
            }
            else
            {
                shadowQuery = $"SELECT TOP {sampleSize} {columnList} FROM [EffortShadow].[{viewName}] ORDER BY {orderByClause}";
            }

            var shadowData = ExecuteQuery(shadowConn, shadowQuery);

            // Compare row by row
            int minRows = Math.Min(legacyData.Rows.Count, shadowData.Rows.Count);
            result.SampleRowsCompared = minRows;

            // Track row-level differences for better reporting
            var rowDifferences = new Dictionary<string, List<(string Column, string Legacy, string Shadow)>>();

            for (int rowIdx = 0; rowIdx < minRows; rowIdx++)
            {
                var legacyRow = legacyData.Rows[rowIdx];
                var shadowRow = shadowData.Rows[rowIdx];

                // Build composite primary key display value
                var pkParts = primaryKeys.Select(pk => $"{pk}={legacyRow[pk]?.ToString() ?? "NULL"}");
                var pkDisplay = string.Join(", ", pkParts);

                for (int colIdx = 0; colIdx < legacyColumns.Count; colIdx++)
                {
                    var colName = legacyColumns[colIdx].ColumnName;
                    var legacyValue = legacyRow[colIdx];
                    var shadowValue = shadowRow[colIdx];

                    // Normalize values for comparison
                    string legacyStr = NormalizeValue(legacyValue);
                    string shadowStr = NormalizeValue(shadowValue);

                    // Check if this is a known acceptable difference
                    if (IsKnownValueDifference(colName, legacyStr, shadowStr, viewName))
                    {
                        continue;  // Skip known differences
                    }

                    if (!string.Equals(legacyStr, shadowStr, StringComparison.OrdinalIgnoreCase))
                    {
                        result.ContentMismatches++;

                        // Group differences by row (up to 5 rows)
                        if (!rowDifferences.ContainsKey(pkDisplay) && rowDifferences.Count < 5)
                        {
                            rowDifferences[pkDisplay] = new List<(string, string, string)>();
                        }

                        if (rowDifferences.TryGetValue(pkDisplay, out var diffs))
                        {
                            diffs.Add((colName, Truncate(legacyStr, 40), Truncate(shadowStr, 40)));
                        }
                    }
                }
            }

            // Format row differences with highlighting
            foreach (var (pkDisplay, diffs) in rowDifferences)
            {
                result.ContentDifferences.Add($"  [{pkDisplay}]{samplingNote}");
                foreach (var (col, legacy, shadow) in diffs)
                {
                    result.ContentDifferences.Add($"    [{col}]: '{legacy}' → '{shadow}'");
                }
            }

            // Determine pass/fail
            if (result.ContentMismatches > 0)
            {
                result.ContentPassed = false;
                result.Errors.Add($"Content mismatch: {result.ContentMismatches} differences found in {result.SampleRowsCompared} rows{samplingNote}");
            }
        }

        /// <summary>
        /// Checks if a value difference is a known acceptable difference.
        /// </summary>
        private static bool IsKnownValueDifference(string columnName, string legacyValue, string shadowValue, string viewName)
        {
            // Check column-specific known mappings
            // "*" means any non-null value is acceptable
            if (KnownValueMappings.TryGetValue(columnName, out var mappings) &&
                mappings.TryGetValue(legacyValue, out var expectedShadow) &&
                ((expectedShadow == "*" && shadowValue != "NULL") ||
                 string.Equals(expectedShadow, shadowValue, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check view-specific known differences
            if (ViewsWithTermNameDifferences.Contains(viewName) &&
                columnName.EndsWith("_TermName", StringComparison.OrdinalIgnoreCase))
            {
                // Term name format differences are acceptable (e.g., "Summer Session II 2002" vs "Summer 2002")
                return true;
            }

            return false;
        }

        /// <summary>
        /// Normalizes a database value for comparison.
        /// </summary>
        private static string NormalizeValue(object? value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            // Handle floating point precision differences
            if (value is double d)
                return Math.Round(d, 6).ToString("G");
            if (value is float f)
                return Math.Round(f, 6).ToString("G");
            if (value is decimal dec)
                return Math.Round(dec, 6).ToString("G");

            // Handle datetime (ignore milliseconds)
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");

            // Trim strings
            if (value is string s)
                return s.Trim();

            return value.ToString() ?? "NULL";
        }

        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Executes a query and returns the results as a DataTable.
        /// </summary>
        private static DataTable ExecuteQuery(SqlConnection conn, string query)
        {
            var dataTable = new DataTable();
            using var cmd = new SqlCommand(query, conn);
            cmd.CommandTimeout = 60;
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dataTable);
            return dataTable;
        }

        /// <summary>
        /// Gets column metadata for a table or view.
        /// </summary>
        private static List<ColumnInfo> GetTableColumns(SqlConnection conn, string tableName, bool isLegacy)
        {
            var columns = new List<ColumnInfo>();

            // For shadow views, use [EffortShadow] schema
            string schemaPrefix = isLegacy ? "" : "[EffortShadow].";
            _ = isLegacy ? tableName : $"{schemaPrefix}{tableName}";

            // Query INFORMATION_SCHEMA for column metadata
            string query = isLegacy
                ? @"
                    SELECT COLUMN_NAME, DATA_TYPE, ISNULL(CHARACTER_MAXIMUM_LENGTH, 0), IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION"
                : @"
                    SELECT COLUMN_NAME, DATA_TYPE, ISNULL(CHARACTER_MAXIMUM_LENGTH, 0), IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'EffortShadow' AND TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(new ColumnInfo
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    MaxLength = reader.GetInt32(2),
                    IsNullable = reader.GetString(3).Equals("YES", StringComparison.OrdinalIgnoreCase)
                });
            }

            return columns;
        }

        /// <summary>
        /// Gets the row count for a table or view.
        /// </summary>
        private static int GetRowCount(SqlConnection conn, string tableName, bool isLegacy)
        {
            string schemaPrefix = isLegacy ? "" : "[EffortShadow].";
            string fullTableName = isLegacy ? tableName : $"{schemaPrefix}{tableName}";

            string query = $"SELECT COUNT(*) FROM {fullTableName}";

            using var cmd = new SqlCommand(query, conn);
            return (int)cmd.ExecuteScalar();
        }

        private class PercentRecord
        {
            public int Id { get; set; }
            public int TypeId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal? Percentage { get; set; }
            public string? Unit { get; set; }
            public string? Modifier { get; set; }
        }
    }
}
