using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Data Remediation Script for Effort System Migration
    /// Purpose: Fix all critical data quality issues identified in migration analysis
    /// Date: Nov 2025
    ///
    /// Usage:
    ///   EffortDataRemediation.exe                    # Full remediation with backup
    ///   EffortDataRemediation.exe --dry-run          # Preview changes without applying
    ///   EffortDataRemediation.exe --skip-backup      # Skip backup creation (faster)
    /// </summary>
    public class EffortDataRemediation
    {
        private readonly string _effortsConnectionString;
        private readonly string _viperConnectionString;
        private readonly string _outputPath;
        private readonly DateTime _remediationDate;
        private readonly IConfiguration _configuration;
        private readonly bool _dryRun;
        private readonly bool _skipBackup;
        private readonly RemediationReport _report;

        public EffortDataRemediation(IConfiguration? configuration = null, bool dryRun = true, bool skipBackup = false, string? outputPath = null)
        {
            _configuration = configuration ?? EffortScriptHelper.LoadConfiguration();
            _dryRun = dryRun;
            _skipBackup = skipBackup;
            _viperConnectionString = EffortScriptHelper.GetConnectionString(_configuration, "VIPER");
            // Remediation script needs WRITE access to fix data quality issues in legacy database
            _effortsConnectionString = EffortScriptHelper.GetConnectionString(_configuration, "Effort", readOnly: false);
            _outputPath = EffortScriptHelper.ValidateOutputPath(outputPath, "RemediationOutput");
            _remediationDate = DateTime.Now;
            _report = new RemediationReport { RemediationDate = _remediationDate };

            Directory.CreateDirectory(_outputPath);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("EFFORT DATA REMEDIATION SCRIPT");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            // Default to dry-run (safe mode) unless --apply flag is provided
            bool applyChanges = args.Contains("--apply") || args.Contains("-a");
            bool dryRun = !applyChanges;
            bool skipBackup = args.Contains("--skip-backup") || args.Contains("-s");

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🔍 DRY-RUN MODE (Default): No changes will be applied");
                Console.WriteLine("   Use --apply flag to actually perform remediation");
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("⚠️  APPLY MODE: Changes will be applied to the database");
                Console.ResetColor();
                Console.WriteLine();
            }

            try
            {
                var remediation = new EffortDataRemediation(null, dryRun, skipBackup);

                Console.WriteLine("Connection Configuration:");
                Console.WriteLine($"  VIPER Database: {EffortScriptHelper.GetServerAndDatabase(remediation._viperConnectionString)}");
                Console.WriteLine($"  Efforts Database: {EffortScriptHelper.GetServerAndDatabase(remediation._effortsConnectionString)}");
                Console.WriteLine($"  Backup Enabled: {!skipBackup}");
                Console.WriteLine();

                remediation.RunFullRemediation();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        public void RunFullRemediation()
        {
            Console.WriteLine("Starting data remediation process...");
            Console.WriteLine();

            // Task 1: Fix duplicate courses
            Console.WriteLine("Task 1: Fixing duplicate course records...");
            FixDuplicateCourses();

            // Task 2: Fix missing department codes
            Console.WriteLine("\nTask 2: Fixing missing department codes...");
            FixMissingDepartmentCodes();

            // Task 3: Generate placeholder CRNs
            Console.WriteLine("\nTask 3: Generating placeholder CRNs for blank courses...");
            GeneratePlaceholderCRNs();

            // Task 4: Delete negative hours record
            Console.WriteLine("\nTask 4: Deleting effort record with negative hours...");
            DeleteNegativeHoursRecord();

            // Task 5: Delete invalid MothraIds
            Console.WriteLine("\nTask 5: Deleting invalid MothraId records...");
            DeleteInvalidMothraIds();

            // Task 6: Consolidate guest instructors
            Console.WriteLine("\nTask 6: Consolidating guest instructor accounts...");
            ConsolidateGuestInstructors();

            // Task 7: Add missing person records
            Console.WriteLine("\nTask 7: Adding missing person records to VIPER...");
            AddMissingPersonRecords();

            // Task 8: Remap Karina Snapp
            Console.WriteLine("\nTask 8: Remapping Karina Snapp to correct PersonId...");
            RemapKarinaSnapp();

            // Task 9: Fix courses with invalid units
            Console.WriteLine("\nTask 9: Fixing courses with invalid units (<=0)...");
            FixCoursesWithInvalidUnits();

            // Task 10: Delete orphaned course relationships
            Console.WriteLine("\nTask 10: Deleting course relationships with invalid references...");
            DeleteOrphanedCourseRelationships();

            // Task 11: Fix course relationships with invalid types
            Console.WriteLine("\nTask 11: Fixing course relationships with invalid types...");
            FixCourseRelationshipTypes();

            // Generate final report
            Console.WriteLine("\nGenerating remediation report...");
            GenerateReport();

            DisplaySummary();
        }

        #region Task 1: Fix Duplicate Courses

        private void FixDuplicateCourses()
        {
            var task = new RemediationTask { TaskName = "Fix Duplicate Courses" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Find duplicate courses (CRN + Term + Units)
                var duplicatesQuery = @"
                    SELECT course_CRN, course_TermCode, course_Units, COUNT(*) as DupeCount
                    FROM tblCourses
                    GROUP BY course_CRN, course_TermCode, course_Units
                    HAVING COUNT(*) > 1";

                var duplicates = new List<(string? crn, int term, double? units)>();
                using (var cmd = new SqlCommand(duplicatesQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        duplicates.Add((
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.GetInt32(1),
                            reader.IsDBNull(2) ? (double?)null : Convert.ToDouble(reader.GetValue(2))
                        ));
                    }
                }

                if (duplicates.Count == 0)
                {
                    task.Status = "Skipped - No duplicates found";
                    Console.WriteLine("  ✓ No duplicate courses found");
                    return;
                }

                Console.WriteLine($"  Found {duplicates.Count} sets of duplicate courses");

                foreach (var (crn, term, units) in duplicates)
                {
                    FixDuplicateCourse(conn, crn, term, units, task);
                }

                task.Status = "Completed";
            }
        }

        private void FixDuplicateCourse(SqlConnection conn, string? crn, int term, double? units, RemediationTask task)
        {
            // Get all course IDs for this duplicate set with effort counts
            var coursesQuery = @"
                SELECT c.course_id, COUNT(e.effort_ID) as EffortCount
                FROM tblCourses c
                LEFT JOIN tblEffort e ON c.course_id = e.effort_CourseID
                WHERE ((@CRN IS NULL AND c.course_CRN IS NULL) OR c.course_CRN = @CRN)
                  AND c.course_TermCode = @Term
                  AND ((@Units IS NULL AND c.course_Units IS NULL) OR c.course_Units = @Units)
                GROUP BY c.course_id
                ORDER BY COUNT(e.effort_ID) DESC, c.course_id ASC";

            var courseIds = new List<(int id, int effortCount)>();
            using (var cmd = new SqlCommand(coursesQuery, conn))
            {
                cmd.Parameters.AddWithValue("@CRN", (object?)crn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Term", term);
                cmd.Parameters.AddWithValue("@Units", units.HasValue ? (object)units.Value : DBNull.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        courseIds.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }
            }

            if (courseIds.Count <= 1)
            {
                Console.WriteLine($"    ✓ CRN {crn ?? "NULL"} Term {term} Units {units?.ToString() ?? "NULL"} - Already resolved");
                return;
            }

            // Keep the one with most effort records, delete the rest
            var keepId = courseIds[0].id;
            var deleteIds = courseIds.Skip(1).Select(x => x.id).ToList();

            Console.WriteLine($"    CRN {crn ?? "NULL"} Term {term} Units {units?.ToString() ?? "NULL"}:");
            Console.WriteLine($"      Keeping course_id {keepId} ({courseIds[0].effortCount} effort records)");
            Console.WriteLine($"      Deleting course_id(s): {string.Join(", ", deleteIds)}");

            // Backup first (only in apply mode)
            if (!_dryRun && !_skipBackup)
            {
                // Build parameterized IN clause
                var paramNames = new List<string>();
                var parameters = new List<SqlParameter>();
                for (int i = 0; i < deleteIds.Count; i++)
                {
                    var paramName = $"@CourseId{i}";
                    paramNames.Add(paramName);
                    parameters.Add(new SqlParameter(paramName, deleteIds[i]));
                }
                var whereClause = $"course_id IN ({string.Join(",", paramNames)})";
                BackupRecords(conn, "tblCourses", whereClause, parameters.ToArray());
            }

            // Execute in transaction - commit if applying, rollback if dry-run
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Update any effort records pointing to deleted courses to point to kept course
                    foreach (var deleteId in deleteIds)
                    {
                        var updateEffortQuery = @"
                            UPDATE tblEffort
                            SET effort_CourseID = @KeepId
                            WHERE effort_CourseID = @DeleteId";

                        using (var cmd = new SqlCommand(updateEffortQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@KeepId", keepId);
                            cmd.Parameters.AddWithValue("@DeleteId", deleteId);
                            int updated = cmd.ExecuteNonQuery();
                            if (updated > 0)
                            {
                                Console.WriteLine($"        {(_dryRun ? "Would remap" : "Remapped")} {updated} effort records from course {deleteId} to {keepId}");
                            }
                        }
                    }

                    // Delete the duplicate courses
                    var deleteQuery = "DELETE FROM tblCourses WHERE course_id = @CourseId";
                    foreach (var deleteId in deleteIds)
                    {
                        using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CourseId", deleteId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"        [DRY-RUN] Transaction rolled back - no changes applied");
                    }
                    else
                    {
                        transaction.Commit();
                        task.RecordsAffected += deleteIds.Count;
                        task.Details.Add($"Removed {deleteIds.Count} duplicate course(s) for CRN {crn} Term {term}");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 2: Fix Missing Department Codes

        private void FixMissingDepartmentCodes()
        {
            var task = new RemediationTask { TaskName = "Fix Missing Department Codes" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Find persons with NULL/empty EffortDept
                var missingDeptQuery = @"
                    SELECT DISTINCT RTRIM(person_MothraID) as person_MothraID
                    FROM tblPerson
                    WHERE person_EffortDept IS NULL OR LTRIM(RTRIM(person_EffortDept)) = ''";

                var mothraIds = new List<string>();
                using (var cmd = new SqlCommand(missingDeptQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        mothraIds.Add(reader.GetString(0));
                    }
                }

                if (mothraIds.Count == 0)
                {
                    task.Status = "Skipped - No missing departments found";
                    Console.WriteLine("  ✓ No persons with missing departments");
                    return;
                }

                Console.WriteLine($"  Found {mothraIds.Count} persons with missing department codes");

                foreach (var mothraId in mothraIds)
                {
                    FixPersonDepartment(conn, mothraId, task);
                }

                task.Status = "Completed";
            }
        }

        private void FixPersonDepartment(SqlConnection conn, string mothraId, RemediationTask task)
        {
            var mothraIdWhere = EffortScriptHelper.BuildMothraIdWhereClause("person_MothraID", "@MothraId");

            // Find most recent valid department for this person
            var findDeptQuery = $@"
                SELECT TOP 1 person_EffortDept
                FROM tblPerson
                WHERE {mothraIdWhere}
                  AND person_EffortDept IS NOT NULL
                  AND LTRIM(RTRIM(person_EffortDept)) != ''
                ORDER BY person_TermCode DESC";

            string? foundDept = null;
            using (var cmd = new SqlCommand(findDeptQuery, conn))
            {
                cmd.Parameters.AddWithValue("@MothraId", mothraId);
                var result = cmd.ExecuteScalar();
                foundDept = result?.ToString();
            }

            // Use found dept or default to "UNK"
            string deptToUse = !string.IsNullOrWhiteSpace(foundDept) ? foundDept : "UNK";

            // Backup first (only in apply mode)
            if (!_dryRun && !_skipBackup)
            {
                var backupWhere = $"{mothraIdWhere} AND (person_EffortDept IS NULL OR LTRIM(RTRIM(person_EffortDept)) = '')";
                BackupRecords(conn, "tblPerson", backupWhere, new SqlParameter("@MothraId", mothraId));
            }

            // Execute in transaction - commit if applying, rollback if dry-run
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var updateQuery = $@"
                        UPDATE tblPerson
                        SET person_EffortDept = @Dept
                        WHERE {mothraIdWhere}
                          AND (person_EffortDept IS NULL OR LTRIM(RTRIM(person_EffortDept)) = '')";

                    int updated;
                    using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Dept", deptToUse);
                        cmd.Parameters.AddWithValue("@MothraId", mothraId);
                        updated = cmd.ExecuteNonQuery();
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                        if (updated > 0)
                        {
                            Console.WriteLine($"    Would update {updated} record(s) for {mothraId} to dept: {deptToUse}");
                        }
                    }
                    else
                    {
                        transaction.Commit();
                        if (updated > 0)
                        {
                            task.RecordsAffected += updated;
                            Console.WriteLine($"    ✓ Updated {updated} record(s) for {mothraId} to dept: {deptToUse}");
                        }
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 3: Generate Placeholder CRNs

        private void GeneratePlaceholderCRNs()
        {
            var task = new RemediationTask { TaskName = "Generate Placeholder CRNs" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Find ALL courses with blank CRN and check which have effort records
                var blankCrnQuery = @"
                    SELECT c.course_id, c.course_TermCode, c.course_SubjCode, c.course_CrseNumb,
                           ISNULL((SELECT COUNT(*) FROM tblEffort e WHERE e.effort_CourseID = c.course_id), 0) as EffortCount
                    FROM tblCourses c
                    WHERE c.course_CRN IS NULL OR LTRIM(RTRIM(c.course_CRN)) = ''
                    ORDER BY c.course_id";

                var blankCourses = new List<(int id, int term, string subj, string num, int effortCount)>();
                using (var cmd = new SqlCommand(blankCrnQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        blankCourses.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetInt32(4)
                        ));
                    }
                }

                if (blankCourses.Count == 0)
                {
                    task.Status = "Skipped - No blank CRNs found";
                    Console.WriteLine("  ✓ No courses with blank CRNs");
                    return;
                }

                var coursesWithEffort = blankCourses.Where(c => c.effortCount > 0).ToList();
                var coursesWithoutEffort = blankCourses.Where(c => c.effortCount == 0).ToList();

                Console.WriteLine($"  Found {blankCourses.Count} total courses with blank CRNs");
                Console.WriteLine($"    - {coursesWithEffort.Count} used in effort records (CRN range: 90001+)");
                Console.WriteLine($"    - {coursesWithoutEffort.Count} not used in effort records (CRN range: 99001+)");

                // Assign CRNs 90001+ to courses WITH effort records
                int nextCrn = 90001;
                foreach (var (id, term, subj, num, effortCount) in coursesWithEffort)
                {
                    AssignPlaceholderCRN(conn, id, term, subj, num, effortCount, nextCrn, task);
                    nextCrn++;
                }

                // Assign CRNs 99001+ to courses WITHOUT effort records
                int nextUnusedCrn = 99001;
                foreach (var (id, term, subj, num, effortCount) in coursesWithoutEffort)
                {
                    AssignPlaceholderCRN(conn, id, term, subj, num, effortCount, nextUnusedCrn, task);
                    nextUnusedCrn++;
                }

                task.Status = "Completed";
            }
        }

        private void AssignPlaceholderCRN(SqlConnection conn, int courseId, int term, string subj, string num, int effortCount, int crn, RemediationTask task)
        {
            string crnString = crn.ToString();

            Console.WriteLine($"    Course {courseId} ({subj} {num}, Term {term}, {effortCount} effort records):");
            Console.WriteLine($"      {(_dryRun ? "Would assign" : "Assigning")} CRN: {crnString}");

            // Backup first (only in apply mode)
            if (!_dryRun && !_skipBackup)
            {
                BackupRecords(conn, "tblCourses", "course_id = @CourseId",
                    new SqlParameter("@CourseId", courseId));
            }

            // Execute in transaction - commit if applying, rollback if dry-run
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    var updateQuery = @"
                        UPDATE tblCourses
                        SET course_CRN = @CRN
                        WHERE course_id = @CourseId";

                    using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CRN", crnString);
                        cmd.Parameters.AddWithValue("@CourseId", courseId);
                        cmd.ExecuteNonQuery();
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                        task.RecordsAffected++;
                        task.Details.Add($"Assigned CRN {crnString} to course {courseId}");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 4: Delete Negative Hours Record

        private void DeleteNegativeHoursRecord()
        {
            var task = new RemediationTask { TaskName = "Delete Negative Hours Record" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Check if the specific record still exists
                var checkQuery = "SELECT COUNT(*) FROM tblEffort WHERE effort_ID = 87673";
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        task.Status = "Skipped - Record already deleted";
                        Console.WriteLine("  ✓ Negative hours record (effort_ID 87673) already removed");
                        return;
                    }
                }

                Console.WriteLine($"  Found effort record with negative hours (effort_ID: 87673)");

                // Backup first (only in apply mode)
                if (!_dryRun && !_skipBackup)
                {
                    BackupRecords(conn, "tblEffort", "effort_ID = @EffortId",
                        new SqlParameter("@EffortId", 87673));
                }

                // Execute in transaction - commit if applying, rollback if dry-run
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var deleteQuery = "DELETE FROM tblEffort WHERE effort_ID = 87673";
                        using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        if (_dryRun)
                        {
                            transaction.Rollback();
                            Console.WriteLine("    Would delete effort_ID 87673");
                            task.Status = "Dry-run";
                        }
                        else
                        {
                            transaction.Commit();
                            task.RecordsAffected = 1;
                            task.Details.Add("Deleted effort_ID 87673 (negative hours)");
                            task.Status = "Completed";
                            Console.WriteLine("    ✓ Deleted negative hours record");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Task 5: Delete Invalid MothraIds

        private void DeleteInvalidMothraIds()
        {
            var task = new RemediationTask { TaskName = "Delete Invalid MothraIds" };
            _report.Tasks.Add(task);

            var invalidIds = new[] { "#URL.Mot", "00476734" };

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                foreach (var mothraId in invalidIds)
                {
                    DeleteMothraIdFromAllTables(conn, mothraId, task);
                }

                task.Status = task.RecordsAffected > 0 ? "Completed" : "Skipped - No records found";
            }
        }

        private void DeleteMothraIdFromAllTables(SqlConnection conn, string mothraId, RemediationTask task)
        {
            var tables = new[]
            {
                ("tblEffort", "effort_MothraID"),
                ("tblPercent", "percent_MothraID"),
                ("tblPerson", "person_MothraID"),
                ("tblSabbatic", "sab_MothraID")
            };

            int totalDeleted = 0;

            // Check counts first (outside transaction)
            var tablesToDelete = new List<(string table, string column, int count)>();
            foreach (var (table, column) in tables)
            {
                var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@MothraId");
                var checkQuery = $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
                int count;
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MothraId", mothraId);
                    count = (int)cmd.ExecuteScalar();
                }

                if (count > 0)
                {
                    tablesToDelete.Add((table, column, count));
                }
            }

            if (tablesToDelete.Count == 0)
            {
                Console.WriteLine($"  ✓ No records found for MothraId {mothraId}");
                return;
            }

            Console.WriteLine($"  {(_dryRun ? "Would delete" : "Deleting")} MothraId: {mothraId}");
            foreach (var (table, column, count) in tablesToDelete)
            {
                Console.WriteLine($"    Found {count} record(s) in {table}");
            }

            // Backup first (only in apply mode)
            if (!_dryRun && !_skipBackup)
            {
                foreach (var (table, column, _) in tablesToDelete)
                {
                    var backupWhere = EffortScriptHelper.BuildMothraIdWhereClause(column, "@MothraId");
                    BackupRecords(conn, table, backupWhere, new SqlParameter("@MothraId", mothraId));
                }
            }

            // Execute in transaction - commit if applying, rollback if dry-run
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var (table, column, _) in tablesToDelete)
                    {
                        var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@MothraId");
                        var deleteQuery = $"DELETE FROM {table} WHERE {whereClause}";
                        using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MothraId", mothraId);
                            int deleted = cmd.ExecuteNonQuery();
                            totalDeleted += deleted;
                            Console.WriteLine($"      {(_dryRun ? "Would delete" : "✓ Deleted")} {deleted} record(s)");
                        }
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                        task.RecordsAffected += totalDeleted;
                        task.Details.Add($"Deleted {totalDeleted} records for MothraId {mothraId}");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 6: Consolidate Guest Instructors

        private void ConsolidateGuestInstructors()
        {
            var task = new RemediationTask { TaskName = "Consolidate Guest Instructors" };
            _report.Tasks.Add(task);

            var guestIds = new[] { "UNKGuest", "VETGuest", "VMDOGues" };

            using (var viperConn = new SqlConnection(_viperConnectionString))
            using (var effortConn = new SqlConnection(_effortsConnectionString))
            {
                viperConn.Open();
                effortConn.Open();

                // Step 1: Check if VETGUEST person exists in VIPER
                int vetguestPersonId = EnsureVetGuestPerson(viperConn, task);

                if (vetguestPersonId == 0)
                {
                    task.Status = "Failed - Could not create VETGUEST person";
                    Console.WriteLine("  ✗ Failed to create VETGUEST person in VIPER");
                    return;
                }

                // Step 2: Update all guest instructor references to VETGUEST
                foreach (var guestId in guestIds)
                {
                    RemapGuestToVetGuest(effortConn, guestId, "VETGUEST", task);
                }

                task.Status = "Completed";
            }
        }

        private int EnsureVetGuestPerson(SqlConnection viperConn, RemediationTask task)
        {
            // Check if VETGUEST already exists
            var checkQuery = "SELECT PersonId FROM users.Person WHERE MothraId = 'VETGUEST'";
            using (var cmd = new SqlCommand(checkQuery, viperConn))
            {
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    int existingId = (int)result;
                    Console.WriteLine($"  ✓ VETGUEST person already exists (PersonId: {existingId})");
                    return existingId;
                }
            }

            Console.WriteLine($"  {(_dryRun ? "Would create" : "Creating")} VETGUEST person in VIPER...");

            // Execute in transaction with serializable isolation to prevent race conditions
            using (var transaction = viperConn.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Get next PersonId inside transaction to prevent race conditions
                    // PersonId is NOT an IDENTITY column, must be manually assigned
                    int nextPersonId;
                    var maxIdQuery = "SELECT ISNULL(MAX(PersonId), 0) + 1 FROM users.Person WITH (TABLOCKX)";
                    using (var cmd = new SqlCommand(maxIdQuery, viperConn, transaction))
                    {
                        nextPersonId = (int)cmd.ExecuteScalar();
                    }

                    // Create VETGUEST person with explicit PersonId
                    const int CLIENT_ID_LENGTH = 11;
                    var vetGuestClientId = "VETGUEST".PadRight(CLIENT_ID_LENGTH);
                    var insertQuery = @"
                        INSERT INTO users.Person (
                            PersonId, ClientId, MothraId, FirstName, LastName, MailId,
                            CurrentStudent, FutureStudent, CurrentEmployee, FutureEmployee
                        )
                        VALUES (
                            @PersonId, @ClientId, 'VETGUEST', 'Guest', 'Instructor', 'vetguest',
                            0, 0, 0, 0
                        );";

                    using (var cmd = new SqlCommand(insertQuery, viperConn, transaction))
                    {
                        cmd.Parameters.Add(new SqlParameter("@PersonId", SqlDbType.Int) { Value = nextPersonId });
                        cmd.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.VarChar, CLIENT_ID_LENGTH) { Value = vetGuestClientId });
                        cmd.ExecuteNonQuery();
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"    Would create VETGUEST person (PersonId: {nextPersonId})");
                        return -1; // Return dummy ID for dry-run
                    }
                    else
                    {
                        transaction.Commit();
                        Console.WriteLine($"    ✓ Created VETGUEST person (PersonId: {nextPersonId})");
                        task.Details.Add($"Created VETGUEST person with PersonId {nextPersonId}");
                        return nextPersonId;
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private void RemapGuestToVetGuest(SqlConnection conn, string oldMothraId, string newMothraId, RemediationTask task)
        {
            var tables = new[]
            {
                ("tblEffort", "effort_MothraID"),
                ("tblPercent", "percent_MothraID"),
                ("tblPerson", "person_MothraID")
            };

            int totalRemapped = 0;

            var tablesToUpdate = new List<(string table, string column, int count)>();
            foreach (var (table, column) in tables)
            {
                var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldId", excludeIfAlreadyNewValue: true, "@NewId");
                var checkQuery = $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
                int count;
                using (var cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@OldId", oldMothraId);
                    cmd.Parameters.AddWithValue("@NewId", newMothraId);
                    count = (int)cmd.ExecuteScalar();
                }

                if (count > 0)
                {
                    tablesToUpdate.Add((table, column, count));
                }
            }

            if (tablesToUpdate.Count == 0)
            {
                bool hasRecordsAlready = false;
                foreach (var (table, column) in tables)
                {
                    var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldId");
                    var checkQuery = $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
                    using (var cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OldId", oldMothraId);
                        int existingCount = (int)cmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            hasRecordsAlready = true;
                            break;
                        }
                    }
                }

                if (!hasRecordsAlready)
                {
                    Console.WriteLine($"  ✓ No records found for {oldMothraId}");
                }

                return;
            }

            Console.WriteLine($"  {(_dryRun ? "Would remap" : "Remapping")} {oldMothraId} → {newMothraId}");
            foreach (var (table, column, count) in tablesToUpdate)
            {
                Console.WriteLine($"    Found {count} record(s) in {table}");
            }

            // Backup first (only in apply mode)
            if (!_dryRun && !_skipBackup)
            {
                foreach (var (table, column, _) in tablesToUpdate)
                {
                    var backupWhere = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldMothraId");
                    BackupRecords(conn, table, backupWhere, new SqlParameter("@OldMothraId", oldMothraId));
                }
            }

            // Execute in transaction - commit if applying, rollback if dry-run
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    foreach (var (table, column, _) in tablesToUpdate)
                    {
                        var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldId", excludeIfAlreadyNewValue: true, "@NewId");
                        var updateQuery = $"UPDATE {table} SET {column} = @NewId WHERE {whereClause}";
                        using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@NewId", newMothraId);
                            cmd.Parameters.AddWithValue("@OldId", oldMothraId);
                            int updated = cmd.ExecuteNonQuery();
                            totalRemapped += updated;
                            Console.WriteLine($"      {(_dryRun ? "Would remap" : "✓ Remapped")} {updated} record(s)");
                        }
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                        task.RecordsAffected += totalRemapped;
                        task.Details.Add($"Remapped {totalRemapped} records from {oldMothraId} to {newMothraId}");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 7: Add Missing Person Records

        private void AddMissingPersonRecords()
        {
            var task = new RemediationTask { TaskName = "Add Missing Person Records" };
            _report.Tasks.Add(task);

            var missingPersons = new[]
            {
                new { MothraId = "00297319", FirstName = "IAIN", MiddleName = "A", LastName = "GRANT" },
                new { MothraId = "00352428", FirstName = "REGINA", MiddleName = "M", LastName = "SCHROEDER" }
            };

            using (var viperConn = new SqlConnection(_viperConnectionString))
            {
                viperConn.Open();

                foreach (var person in missingPersons)
                {
                    AddPersonToViper(viperConn, person.MothraId, person.FirstName, person.MiddleName, person.LastName, task);
                }

                task.Status = task.RecordsAffected > 0 ? "Completed" : "Skipped - Already exist";
            }
        }

        private void AddPersonToViper(SqlConnection conn, string mothraId, string firstName, string middleName, string lastName, RemediationTask task)
        {
            // Check if person already exists
            var checkQuery = "SELECT PersonId FROM users.Person WHERE MothraId = @MothraId";
            using (var cmd = new SqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@MothraId", mothraId);
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    Console.WriteLine($"  ✓ Person {mothraId} ({firstName} {lastName}) already exists");
                    return;
                }
            }

            Console.WriteLine($"  {(_dryRun ? "Would add" : "Adding")} person {mothraId} ({firstName} {middleName} {lastName})");

            // Execute in transaction with serializable isolation to prevent race conditions
            using (var transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    // Get next PersonId inside transaction to prevent race conditions
                    // PersonId is NOT an IDENTITY column, must be manually assigned
                    int nextPersonId;
                    var maxIdQuery = "SELECT ISNULL(MAX(PersonId), 0) + 1 FROM users.Person WITH (TABLOCKX)";
                    using (var cmd = new SqlCommand(maxIdQuery, conn, transaction))
                    {
                        nextPersonId = (int)cmd.ExecuteScalar();
                    }

                    // Insert person with explicit PersonId
                    var insertQuery = @"
                        INSERT INTO users.Person (
                            PersonId, ClientId, MothraId, FirstName, LastName, MailId,
                            CurrentStudent, FutureStudent, CurrentEmployee, FutureEmployee
                        )
                        VALUES (
                            @PersonId, @ClientId, @MothraId, @FirstName, @LastName, @MailId,
                            0, 0, 0, 0
                        );";

                    using (var cmd = new SqlCommand(insertQuery, conn, transaction))
                    {
                        cmd.Parameters.Add(new SqlParameter("@PersonId", SqlDbType.Int) { Value = nextPersonId });
                        cmd.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Char, 11) { Value = mothraId.PadRight(11) });
                        cmd.Parameters.Add(new SqlParameter("@MothraId", SqlDbType.VarChar, 50) { Value = mothraId });
                        cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.VarChar, 50) { Value = firstName });
                        cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.VarChar, 50) { Value = lastName });
                        cmd.Parameters.Add(new SqlParameter("@MailId", SqlDbType.VarChar, 50) { Value = mothraId.ToLower() });

                        cmd.ExecuteNonQuery();
                    }

                    if (_dryRun)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"    Would create person (PersonId: {nextPersonId})");
                    }
                    else
                    {
                        transaction.Commit();
                        Console.WriteLine($"    ✓ Created person (PersonId: {nextPersonId})");
                        task.RecordsAffected++;
                        task.Details.Add($"Created person {mothraId} - {firstName} {lastName} (PersonId {nextPersonId})");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Task 8: Remap Karina Snapp

        private void RemapKarinaSnapp()
        {
            var task = new RemediationTask { TaskName = "Remap Karina Snapp" };
            _report.Tasks.Add(task);

            string oldMothraId = "01655578";
            string newMothraId = "00421817";

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                var tables = new[]
                {
                    ("tblEffort", "effort_MothraID"),
                    ("tblPercent", "percent_MothraID"),
                    ("tblPerson", "person_MothraID")
                };

                // Check counts first (outside transaction)
                var tablesToUpdate = new List<(string table, string column, int count)>();
                foreach (var (table, column) in tables)
                {
                    var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldId");
                    var checkQuery = $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
                    int count;
                    using (var cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@OldId", oldMothraId);
                        count = (int)cmd.ExecuteScalar();
                    }

                    if (count > 0)
                    {
                        Console.WriteLine($"  Found {count} record(s) in {table} for MothraId {oldMothraId}");
                        tablesToUpdate.Add((table, column, count));
                    }
                }

                if (tablesToUpdate.Count == 0)
                {
                    task.Status = "Skipped - No records found with old MothraId";
                    Console.WriteLine($"  ✓ No records found for {oldMothraId} (already remapped)");
                    return;
                }

                // Backup first (only in apply mode)
                if (!_dryRun && !_skipBackup)
                {
                    foreach (var (table, column, _) in tablesToUpdate)
                    {
                        var backupWhere = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldMothraId");
                        BackupRecords(conn, table, backupWhere, new SqlParameter("@OldMothraId", oldMothraId));
                    }
                }

                // Execute in transaction - commit if applying, rollback if dry-run
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var (table, column, _) in tablesToUpdate)
                        {
                            var whereClause = EffortScriptHelper.BuildMothraIdWhereClause(column, "@OldId", excludeIfAlreadyNewValue: true, "@NewId");
                            var updateQuery = $"UPDATE {table} SET {column} = @NewId WHERE {whereClause}";
                            using (var cmd = new SqlCommand(updateQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NewId", newMothraId);
                                cmd.Parameters.AddWithValue("@OldId", oldMothraId);
                                int updated = cmd.ExecuteNonQuery();
                                if (!_dryRun)
                                {
                                    task.RecordsAffected += updated;
                                }
                                Console.WriteLine($"    {(_dryRun ? "Would remap" : "✓ Remapped")} {updated} record(s) to {newMothraId}");
                            }
                        }

                        if (_dryRun)
                        {
                            transaction.Rollback();
                            task.Status = "Dry-run";
                        }
                        else
                        {
                            transaction.Commit();
                            task.Status = "Completed";
                            task.Details.Add($"Remapped {task.RecordsAffected} records from {oldMothraId} to {newMothraId}");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Task 9: Fix Courses with Invalid Units

        private void FixCoursesWithInvalidUnits()
        {
            var task = new RemediationTask { TaskName = "Fix Courses with Invalid Units" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                var query = "SELECT course_ID, course_CRN, course_TermCode, course_Units FROM tblCourses WHERE course_Units < 0";
                var coursesToFix = new List<(int courseId, string? crn, int term, double? units)>();

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coursesToFix.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.GetInt32(2),
                            reader.IsDBNull(3) ? (double?)null : Convert.ToDouble(reader.GetValue(3))
                        ));
                    }
                }

                if (coursesToFix.Count == 0)
                {
                    task.Status = "Skipped - No courses with invalid units found";
                    Console.WriteLine("  ✓ No courses with invalid units found");
                    return;
                }

                Console.WriteLine($"  Found {coursesToFix.Count} courses with Units < 0");

                if (!_dryRun && !_skipBackup)
                {
                    BackupRecords(conn, "tblCourses", "course_Units < 0");
                }

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var (courseId, crn, term, units) in coursesToFix)
                        {
                            // Deletion preferred over default value to avoid data integrity issues
                            var deleteQuery = "DELETE FROM tblCourses WHERE course_ID = @CourseId";
                            using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CourseId", courseId);
                                int deleted = cmd.ExecuteNonQuery();
                                if (!_dryRun)
                                {
                                    task.RecordsAffected += deleted;
                                }
                                Console.WriteLine($"    {(_dryRun ? "Would delete" : "✓ Deleted")} course {courseId} (CRN: {crn ?? "NULL"}, Term: {term}, Units: {units})");
                            }
                        }

                        if (_dryRun)
                        {
                            transaction.Rollback();
                            task.Status = "Dry-run";
                        }
                        else
                        {
                            transaction.Commit();
                            task.Status = "Completed";
                            task.Details.Add($"Deleted {task.RecordsAffected} courses with invalid units");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Task 10: Delete Orphaned Course Relationships

        private void DeleteOrphanedCourseRelationships()
        {
            var task = new RemediationTask { TaskName = "Delete Orphaned Course Relationships" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Table is optional in legacy schema - skip if not present
                var tableExistsQuery = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = 'tblCourseRelationships'";
                using (var cmd = new SqlCommand(tableExistsQuery, conn))
                {
                    int tableExists = (int)cmd.ExecuteScalar();
                    if (tableExists == 0)
                    {
                        task.Status = "Skipped - tblCourseRelationships table does not exist";
                        Console.WriteLine("  ✓ tblCourseRelationships table does not exist - skipping");
                        return;
                    }
                }

                var query = @"
                    SELECT cr_ParentID, cr_ChildID
                    FROM tblCourseRelationships r
                    WHERE NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = r.cr_ParentID)
                       OR NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = r.cr_ChildID)";

                var orphanedRelationships = new List<(int parentId, int childId)>();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orphanedRelationships.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1)
                        ));
                    }
                }

                if (orphanedRelationships.Count == 0)
                {
                    task.Status = "Skipped - No orphaned relationships found";
                    Console.WriteLine("  ✓ No orphaned course relationships found");
                    return;
                }

                Console.WriteLine($"  Found {orphanedRelationships.Count} orphaned relationships");

                if (!_dryRun && !_skipBackup)
                {
                    BackupRecords(conn, "tblCourseRelationships",
                        "NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = cr_ParentID) OR NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = cr_ChildId)");
                }

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var (parentId, childId) in orphanedRelationships)
                        {
                            // Composite key required because table has no ID column
                            var deleteQuery = "DELETE FROM tblCourseRelationships WHERE cr_ParentID = @ParentId AND cr_ChildID = @ChildId";
                            using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ParentId", parentId);
                                cmd.Parameters.AddWithValue("@ChildId", childId);
                                int deleted = cmd.ExecuteNonQuery();
                                if (!_dryRun)
                                {
                                    task.RecordsAffected += deleted;
                                }
                                Console.WriteLine($"    {(_dryRun ? "Would delete" : "✓ Deleted")} relationship (Parent: {parentId}, Child: {childId})");
                            }
                        }

                        if (_dryRun)
                        {
                            transaction.Rollback();
                            task.Status = "Dry-run";
                        }
                        else
                        {
                            transaction.Commit();
                            task.Status = "Completed";
                            task.Details.Add($"Deleted {task.RecordsAffected} orphaned course relationships");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Task 11: Fix Course Relationship Types

        private void FixCourseRelationshipTypes()
        {
            var task = new RemediationTask { TaskName = "Fix Course Relationship Types" };
            _report.Tasks.Add(task);

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Table is optional in legacy schema - skip if not present
                var tableExistsQuery = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = 'tblCourseRelationships'";
                using (var cmd = new SqlCommand(tableExistsQuery, conn))
                {
                    int tableExists = (int)cmd.ExecuteScalar();
                    if (tableExists == 0)
                    {
                        task.Status = "Skipped - tblCourseRelationships table does not exist";
                        Console.WriteLine("  ✓ tblCourseRelationships table does not exist - skipping");
                        return;
                    }
                }

                var query = @"
                    SELECT cr_ParentID, cr_ChildID, cr_Relationship
                    FROM tblCourseRelationships
                    WHERE LOWER(cr_Relationship) NOT IN ('parent', 'child', 'crosslist', 'section')
                      AND LOWER(cr_Relationship) NOT LIKE '%cross%'
                      AND LOWER(cr_Relationship) NOT LIKE '%list%'";

                var invalidRelationships = new List<(int parentId, int childId, string type)>();
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        invalidRelationships.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetString(2)
                        ));
                    }
                }

                if (invalidRelationships.Count == 0)
                {
                    task.Status = "Skipped - No invalid relationship types found";
                    Console.WriteLine("  ✓ No invalid relationship types found");
                    return;
                }

                Console.WriteLine($"  Found {invalidRelationships.Count} relationships with invalid types");

                if (!_dryRun && !_skipBackup)
                {
                    BackupRecords(conn, "tblCourseRelationships",
                        "LOWER(cr_Relationship) NOT IN ('parent', 'child', 'crosslist', 'section') AND LOWER(cr_Relationship) NOT LIKE '%cross%' AND LOWER(cr_Relationship) NOT LIKE '%list%'");
                }

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var (parentId, childId, type) in invalidRelationships)
                        {
                            // Modern schema CHECK constraint only allows Parent, Child, CrossList, or Section types
                            var deleteQuery = "DELETE FROM tblCourseRelationships WHERE cr_ParentID = @ParentId AND cr_ChildID = @ChildId";
                            using (var cmd = new SqlCommand(deleteQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@ParentId", parentId);
                                cmd.Parameters.AddWithValue("@ChildId", childId);
                                int deleted = cmd.ExecuteNonQuery();
                                if (!_dryRun)
                                {
                                    task.RecordsAffected += deleted;
                                }
                                Console.WriteLine($"    {(_dryRun ? "Would delete" : "✓ Deleted")} relationship (Parent: {parentId}, Child: {childId}) with invalid type: {type}");
                            }
                        }

                        if (_dryRun)
                        {
                            transaction.Rollback();
                            task.Status = "Dry-run";
                        }
                        else
                        {
                            transaction.Commit();
                            task.Status = "Completed";
                            task.Details.Add($"Deleted {task.RecordsAffected} relationships with invalid types");
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Helper Methods
        private static readonly HashSet<string> AllowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tblCourses",
            "tblEffort",
            "tblPerson",
            "tblPercent",
            "tblSabbatic",
            "tblCourseRelationships"  // Legacy table name is plural
        };

        /// <summary>
        /// Backs up records from a table before modification.
        /// Uses parameterized queries to prevent SQL injection.
        /// </summary>
        /// <param name="conn">Active SQL connection</param>
        /// <param name="tableName">Table name (must be in whitelist)</param>
        /// <param name="whereClause">WHERE clause with parameter placeholders (e.g., "column = @Param")</param>
        /// <param name="parameters">SQL parameters for the WHERE clause</param>
        private void BackupRecords(SqlConnection conn, string tableName, string whereClause, params SqlParameter[] parameters)
        {
            try
            {
                // Validate table name against whitelist to prevent SQL injection
                if (!AllowedTables.Contains(tableName))
                {
                    throw new InvalidOperationException($"Table '{tableName}' is not in the allowed backup table list. Allowed tables: {string.Join(", ", AllowedTables)}");
                }

                // Create backup directory if it doesn't exist
                var backupDir = Path.Join(_outputPath, "Backups");
                Directory.CreateDirectory(backupDir);

                // Generate unique backup filename
                var timestamp = _remediationDate.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{tableName}_{timestamp}_{Guid.NewGuid().ToString()[..8]}.sql";
                var backupFilePath = Path.Join(backupDir, backupFileName);

                // Query records to backup - table name is validated, parameters are used for values
                var selectQuery = $"SELECT * FROM {tableName} WHERE {whereClause}";

                using (var cmd = new SqlCommand(selectQuery, conn))
                {
                    // Add parameters if provided
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            Console.WriteLine($"      ℹ No records to backup");
                            return;
                        }

                        var sql = new StringBuilder();

                        // Write header comment
                        sql.AppendLine("-- Backup created by EffortDataRemediation");
                        sql.AppendLine($"-- Table: {tableName}");
                        sql.AppendLine($"-- Date: {_remediationDate:yyyy-MM-dd HH:mm:ss}");
                        sql.AppendLine($"-- Condition: {whereClause}");
                        sql.AppendLine();

                        // Get column names
                        var columnNames = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames.Add(reader.GetName(i));
                        }

                        // Write INSERT statements
                        int recordCount = 0;
                        while (reader.Read())
                        {
                            var values = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader.IsDBNull(i))
                                {
                                    values.Add("NULL");
                                }
                                else
                                {
                                    var value = reader[i];
                                    values.Add(FormatSqlValue(value));
                                }
                            }

                            sql.AppendLine($"INSERT INTO {tableName} ([{string.Join("], [", columnNames)}])");
                            sql.AppendLine($"VALUES ({string.Join(", ", values)});");
                            sql.AppendLine();
                            recordCount++;
                        }

                        // Write to file
                        File.WriteAllText(backupFilePath, sql.ToString());
                        Console.WriteLine($"      📦 Backed up {recordCount} record(s) to {backupFileName}");
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"      ⚠ Backup SQL error: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"      ⚠ Backup file I/O error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"      ⚠ Backup access denied: {ex.Message}");
            }
        }

        private string FormatSqlValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            // Handle different data types
            switch (value)
            {
                case string str:
                    // Escape single quotes and wrap in quotes
                    return "'" + str.Replace("'", "''") + "'";

                case DateTime dt:
                    return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";

                case bool b:
                    return b ? "1" : "0";

                case byte[] bytes:
                    // Convert binary data to hex string
                    return "0x" + BitConverter.ToString(bytes).Replace("-", "");

                case decimal _:
                case double _:
                case float _:
                case int _:
                case long _:
                case short _:
                case byte _:
                    return value.ToString() ?? "NULL";

                default:
                    // For any other type, treat as string
                    return "'" + value.ToString()?.Replace("'", "''") + "'";
            }
        }

        private void GenerateReport()
        {
            var reportPath = Path.Join(_outputPath, $"RemediationReport_{_remediationDate:yyyyMMdd_HHmmss}.txt");

            // Generate text report
            var sb = new StringBuilder();
            sb.AppendLine("===========================================");
            sb.AppendLine("EFFORT DATA REMEDIATION REPORT");
            sb.AppendLine("===========================================");
            sb.AppendLine($"Date: {_remediationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            sb.AppendLine($"Mode: {(_dryRun ? "DRY-RUN (executed with rollback)" : "FULL REMEDIATION")}");
            sb.AppendLine($"Backup: {(_dryRun ? "Not needed (rolled back)" : (_skipBackup ? "Disabled" : "Enabled"))}");
            sb.AppendLine();

            sb.AppendLine("TASK SUMMARY");
            sb.AppendLine("------------");
            foreach (var task in _report.Tasks)
            {
                sb.AppendLine($"{task.TaskName}:");
                sb.AppendLine($"  Status: {task.Status}");
                sb.AppendLine($"  Records Affected: {task.RecordsAffected}");
                if (task.Details.Count > 0)
                {
                    sb.AppendLine("  Details:");
                    foreach (var detail in task.Details)
                    {
                        sb.AppendLine($"    - {detail}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("TOTAL SUMMARY");
            sb.AppendLine("-------------");
            sb.AppendLine($"Total Records Affected: {_report.TotalRecordsAffected}");
            sb.AppendLine($"Tasks Completed: {_report.Tasks.Count(t => t.Status == "Completed")}");
            sb.AppendLine($"Tasks Skipped: {_report.Tasks.Count(t => t.Status.StartsWith("Skipped"))}");

            File.WriteAllText(reportPath, sb.ToString());

            Console.WriteLine($"  ✓ Report saved to: {reportPath}");
        }

        private void DisplaySummary()
        {
            Console.WriteLine("\n===========================================");
            Console.WriteLine("REMEDIATION SUMMARY");
            Console.WriteLine("===========================================");

            if (_dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🔍 DRY-RUN MODE - No changes were applied");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Remediation Completed");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine($"Total Records Affected: {_report.TotalRecordsAffected}");
            Console.WriteLine($"Tasks Completed: {_report.Tasks.Count(t => t.Status == "Completed")}");
            Console.WriteLine($"Tasks Skipped: {_report.Tasks.Count(t => t.Status.StartsWith("Skipped"))}");

            if (!_dryRun && _report.TotalRecordsAffected > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Next Steps:");
                Console.WriteLine("1. Review the remediation report for details");
                Console.WriteLine("2. Run EffortDataAnalysis again to verify all issues resolved");
                Console.WriteLine("3. Proceed with database migration if analysis shows 0 critical issues");
                Console.ResetColor();
            }

            Console.WriteLine($"\nReports saved to: {_outputPath}");
            Console.WriteLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        #endregion

        #region Data Models

        public class RemediationReport
        {
            public DateTime RemediationDate { get; set; }
            public List<RemediationTask> Tasks { get; set; } = new List<RemediationTask>();

            public int TotalRecordsAffected => Tasks.Sum(t => t.RecordsAffected);
        }

        public class RemediationTask
        {
            public string TaskName { get; set; } = null!;
            public string Status { get; set; } = "In Progress";
            public int RecordsAffected { get; set; }
            public List<string> Details { get; set; } = new List<string>();
        }

        #endregion
    }
}

