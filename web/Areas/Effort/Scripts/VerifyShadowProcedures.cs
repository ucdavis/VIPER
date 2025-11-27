using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
        // Procedures where ORDER BY differences are acceptable because the application re-sorts results
        private static readonly HashSet<string> OrderInsensitiveProcedures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "usp_getEffortReportMeritSummaryForLairmore",  // ColdFusion re-sorts by jgddesc,instructor in yearStats.cfc:276
            "usp_getInstructorsWithClinicalEffort"         // No explicit ORDER BY; ColdFusion handles display order
        };

        // Columns to ignore during comparison per procedure (identity columns that differ between Legacy and Shadow)
        // Key: procedure name, Value: set of column names to ignore (case-insensitive)
        private static readonly Dictionary<string, HashSet<string>> ColumnsToIgnore = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // percent_ID is an identity column - Legacy has original IDs (59903), Shadow has new IDs (12079)
            // The ColdFusion code only uses mothraid from this procedure (valueList(clin_instructors.mothraid))
            ["usp_getInstructorsWithClinicalEffort"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "percent_ID" }
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
                        else if (legacyException != null && shadowException == null)
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
                        else if (legacyException == null && shadowException != null)
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
                            var ignoredColumns = ColumnsToIgnore.TryGetValue(test.Name, out var cols) ? cols : null;
                            CompareResults(legacyData!, shadowData!, result, orderInsensitive, ignoredColumns);
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
            // Generate Report
            // ============================================================================

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("Verification Summary");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Total Procedures Tested: {report.TotalProcedures}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed: {report.PassedProcedures}");
            Console.ResetColor();

            if (report.FailedProcedures > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed (Shadow Defects): {report.FailedProcedures}");
                Console.ResetColor();
            }

            if (report.NeedsInvestigation > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Needs Investigation (Both Failed): {report.NeedsInvestigation}");
                Console.ResetColor();
            }

            // Count and display other warnings (e.g., CRUD rollback warnings, legacy-only failures)
            int proceduresWithOtherWarnings = report.Results.Count(r =>
                r.Status == TestStatus.Passed && r.Warnings.Count > 0);
            if (proceduresWithOtherWarnings > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Other Warnings (CRUD rollback, etc.): {proceduresWithOtherWarnings}");
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
            sb.AppendLine($"Total Procedures Tested: {report.TotalProcedures} (representative sample)");
            sb.AppendLine($"Passed: {report.PassedProcedures} (shadow works correctly)");
            sb.AppendLine($"Failed: {report.FailedProcedures} (shadow defects - shadow fails, legacy works)");
            sb.AppendLine($"Needs Investigation: {report.NeedsInvestigation} (both legacy and shadow fail)");

            if (proceduresWithOtherWarnings > 0)
            {
                sb.AppendLine($"Other Warnings: {proceduresWithOtherWarnings} (CRUD rollback tests, legacy-only failures, etc.)");
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

            sb.AppendLine("================================================================================");
            sb.AppendLine("END OF REPORT");
            sb.AppendLine("================================================================================");

            File.WriteAllText(reportFile, sb.ToString());

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("✓ Report generated successfully");
            Console.ResetColor();
            Console.WriteLine("================================================================================");

            // Fail if any procedures failed OR need investigation (both legacy and shadow failed)
            // NeedsInvestigation indicates environmental issues that prevent validation
            return (report.FailedProcedures == 0 && report.NeedsInvestigation == 0) ? 0 : 1;
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
            if (paramLower.Contains("alldepts") || paramLower.Contains("useacademicyear") ||
                paramLower.Contains("includeall") || paramLower.Contains("showdetails"))
            {
                if (dataType.ToLowerInvariant() == "bit")
                {
                    return false;  // Default to false for boolean flags
                }
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

            using var transaction = connection.BeginTransaction();

            try
            {
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

        private static void CompareResults(DataTable legacy, DataTable shadow, ComparisonResult result, bool orderInsensitive = false, HashSet<string>? ignoredColumns = null)
        {
            // Row count comparison
            if (legacy.Rows.Count != shadow.Rows.Count)
            {
                result.Differences.Add($"Row count mismatch: Legacy={legacy.Rows.Count}, Shadow={shadow.Rows.Count}");
                return; // Can't meaningfully compare data if row counts differ
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

            // Note if any columns are being ignored
            if (ignoredColumns != null && ignoredColumns.Any())
            {
                result.Warnings.Add($"Ignoring columns during comparison: {string.Join(", ", ignoredColumns)}");
            }

            if (orderInsensitive)
            {
                // For order-insensitive procedures, compare row sets instead of row-by-row
                result.Warnings.Add("Order-insensitive comparison: checking for matching data sets (ORDER BY differences ignored)");
                CompareResultSetsUnordered(legacy, shadow, columnMapping, result, ignoredColumns);
            }
            else
            {
                // Data value comparison (sample first 100 rows to avoid huge reports)
                int rowsToCheck = Math.Min(100, Math.Min(legacy.Rows.Count, shadow.Rows.Count));

                for (int i = 0; i < rowsToCheck && result.Differences.Count < 50; i++)
                {
                    CompareRows(legacy.Rows[i], shadow.Rows[i], columnMapping, result, i);
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
            ComparisonResult result, HashSet<string>? ignoredColumns = null)
        {
            // Build sets of row "signatures" to compare data sets without caring about order
            var legacyRowSignatures = new HashSet<string>();
            var shadowRowSignatures = new HashSet<string>();

            // Create signatures for legacy rows
            foreach (DataRow row in legacy.Rows)
            {
                legacyRowSignatures.Add(GetRowSignature(row, columnMapping.Mapping, ignoredColumns));
            }

            // Create signatures for shadow rows
            foreach (DataRow row in shadow.Rows)
            {
                shadowRowSignatures.Add(GetRowSignature(row, columnMapping.Mapping, ignoredColumns));
            }

            // Find rows in legacy but not in shadow
            var missingInShadow = legacyRowSignatures.Except(shadowRowSignatures).ToList();
            // Find rows in shadow but not in legacy
            var extraInShadow = shadowRowSignatures.Except(legacyRowSignatures).ToList();

            if (missingInShadow.Any())
            {
                result.Differences.Add($"Rows in Legacy but NOT in Shadow: {missingInShadow.Count}");
                // Show first few examples
                foreach (var sig in missingInShadow.Take(5))
                {
                    result.Differences.Add($"  Missing row: {sig}");
                }
            }

            if (extraInShadow.Any())
            {
                result.Differences.Add($"Rows in Shadow but NOT in Legacy: {extraInShadow.Count}");
                // Show first few examples
                foreach (var sig in extraInShadow.Take(5))
                {
                    result.Differences.Add($"  Extra row: {sig}");
                }
            }
        }

        private static string GetRowSignature(DataRow row, Dictionary<string, string> columnMapping, HashSet<string>? ignoredColumns = null)
        {
            // Create a normalized string representation of the row for comparison
            var values = new List<string>();
            foreach (var col in columnMapping.Keys.OrderBy(k => k))
            {
                // Skip ignored columns (e.g., identity columns that differ between Legacy and Shadow)
                if (ignoredColumns != null && ignoredColumns.Contains(col))
                    continue;

                var value = row[col];
                if (value == DBNull.Value)
                    values.Add("NULL");
                else if (value is string str)
                    values.Add(str.Trim());
                else if (value is DateTime dt)
                    values.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                else if (IsNumeric(value))
                    values.Add(Convert.ToDecimal(value).ToString("F2"));
                else
                    values.Add(value.ToString() ?? "");
            }
            return string.Join("|", values);
        }

        private static void CompareRows(DataRow legacyRow, DataRow shadowRow,
            (Dictionary<string, string> Mapping, List<string> UnmappedLegacyColumns, List<string> UnmappedShadowColumns) columnMapping,
            ComparisonResult result, int rowIndex)
        {
            foreach (var mapping in columnMapping.Mapping)
            {
                string legacyCol = mapping.Key;
                string shadowCol = mapping.Value;

                var legacyValue = legacyRow[legacyCol];
                var shadowValue = shadowRow[shadowCol];

                // Compare values
                if (!ValuesEqual(legacyValue, shadowValue))
                {
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
    }
}
