// ============================================
// Script: EffortPostDeployment.cs
// Description: Post-deployment tasks for Effort system
// Author: VIPER2 Development Team
// Date: 2026-01-07
// ============================================
// This script performs post-deployment tasks:
// 1. UnitsMigration - Simplify Units table and add UnitId FK to Percentages
// 2. AddManageEffortTypesPermission - Add SVMSecure.Effort.ManageEffortTypes to RAPS (cloned from ManageUnits)
// 3. RenameTablesForStandardization - Rename EffortTypes→PercentAssignTypes, SessionTypes→EffortTypes
// 4. AddEffortTypeFlags - Add FacultyCanEnter, AllowedOnDvm, AllowedOn199299, AllowedOnRCourses columns to EffortTypes
// 5. RenameRecordsFKColumns - Rename Records.SessionType→Records.EffortTypeId and Records.Role→Records.RoleId
// 6. RenamePercentagesEffortTypeIdColumn - Rename Percentages.EffortTypeId→Percentages.PercentAssignTypeId
// 7. DuplicateRecordsCleanup - Remove duplicate (CourseId, PersonId, EffortTypeId) records and add unique constraint
// 8. AddLastEmailedColumns - Add LastEmailed/LastEmailedBy columns to Persons table (performance optimization)
// 9. SimplifyTermStatus - Remove redundant columns from TermStatus table
// 10. FixPercentageConstraint - Update CK_Percentages_Percentage constraint from 0-100 to 0-1
// 11. AddViewDeptAuditPermission - Add SVMSecure.Effort.ViewDeptAudit permission for department chairs to view audit trail
// 12. FixEffortTypeDescriptions - Fix incorrect EffortType descriptions to match legacy CREST tbl_sessiontype
// 13. BackfillHarvestAuditActions - Update audit entries from harvest to use new Harvest* action types
// 14. AddAlertStatesTable - Create AlertStates table for persisting data hygiene alert states
// ============================================
// USAGE:
// dotnet run -- post-deployment              (dry-run mode - shows what would be changed)
// dotnet run -- post-deployment --apply      (actually apply changes)
// dotnet run -- post-deployment --force      (force recreate permission even if it exists)
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    public class EffortPostDeployment
    {
        // Permission cloning constants
        private const string SourcePermission = "SVMSecure.Effort.ManageUnits";
        private const string TargetPermission = "SVMSecure.Effort.ManageEffortTypes";
        private const string TargetDescription = "Manage Effort Types in the Effort system";
        private const string ScriptModBy = "SCRIPT"; // Used for ModBy field in audit logs

        // EffortType flag columns (added after table rename from SessionTypes to EffortTypes)
        private static readonly string[] EffortTypeFlagColumns = { "FacultyCanEnter", "AllowedOnDvm", "AllowedOn199299", "AllowedOnRCourses" };

        public static int Run(string[] args)
        {
            // Parse command-line arguments
            bool executeMode = Array.Exists(args, arg => arg.Equals("--apply", StringComparison.OrdinalIgnoreCase));
            bool forceMode = Array.Exists(args, arg => arg.Equals("--force", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("============================================");
            Console.WriteLine("Effort Post-Deployment Script");
            Console.WriteLine($"Mode: {(executeMode ? "APPLY (permanent changes)" : "DRY-RUN (shows what would be changed)")}");
            Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("============================================");
            Console.WriteLine();

            var configuration = EffortScriptHelper.LoadConfiguration();

            int overallResult = 0;
            var taskResults = new Dictionary<string, (bool Success, string Message)>();

            // Task 1: Units Migration
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 1: Units Migration");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunUnitsMigrationTask(viperConnectionString, executeMode);
                taskResults["UnitsMigration"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 2: Add ManageEffortTypes permission to RAPS
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 2: Add ManageEffortTypes Permission to RAPS");
                Console.WriteLine("----------------------------------------");

                string rapsConnectionString = EffortScriptHelper.GetConnectionString(configuration, "RAPS", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(rapsConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunAddPermissionTask(rapsConnectionString, executeMode, forceMode);
                taskResults["ManageEffortTypesPermission"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 3: Rename tables for standardization (EffortTypes→PercentAssignTypes, SessionTypes→EffortTypes)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 3: Rename Tables for Standardization");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunRenameTablesTask(viperConnectionString, executeMode);
                taskResults["RenameTablesForStandardization"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 4: Add EffortType flag columns (runs after table rename)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 4: Add EffortType Flag Columns");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunAddEffortTypeFlagsTask(viperConnectionString, executeMode);
                taskResults["EffortTypeFlags"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 5: Rename Records FK columns (SessionType→EffortTypeId, Role→RoleId)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 5: Rename Records FK Columns");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunRenameRecordsFKColumnsTask(viperConnectionString, executeMode);
                taskResults["RenameRecordsFKColumns"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 6: Rename Percentages.EffortTypeId column to Percentages.PercentAssignTypeId
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 6: Rename Percentages.EffortTypeId Column");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunRenamePercentagesEffortTypeIdTask(viperConnectionString, executeMode);
                taskResults["RenamePercentagesEffortTypeIdColumn"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 7: Duplicate Records Cleanup
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 7: Duplicate Records Cleanup");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunDuplicateRecordsCleanupTask(viperConnectionString, executeMode);
                taskResults["DuplicateRecordsCleanup"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 8: Add LastEmailed columns to Persons table
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 8: Add LastEmailed Columns to Persons");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunAddLastEmailedColumnsTask(viperConnectionString, executeMode);
                taskResults["AddLastEmailedColumns"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 9: Simplify TermStatus table (remove redundant columns)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 9: Simplify TermStatus Table");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunSimplifyTermStatusTask(viperConnectionString, executeMode);
                taskResults["SimplifyTermStatus"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 10: Fix Percentage Constraint (0-100 to 0-1)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 10: Fix Percentage Constraint");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunFixPercentageConstraintTask(viperConnectionString, executeMode);
                taskResults["FixPercentageConstraint"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

// Task 11: Add ViewDeptAudit permission for department chairs
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 11: Add ViewDeptAudit Permission to RAPS");
                Console.WriteLine("----------------------------------------");

                string rapsConnectionString = EffortScriptHelper.GetConnectionString(configuration, "RAPS", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(rapsConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunAddViewDeptAuditPermissionTask(rapsConnectionString, executeMode);
                taskResults["ViewDeptAuditPermission"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

// Task 12: Fix EffortType descriptions to match legacy CREST tbl_sessiontype
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 12: Fix EffortType Descriptions");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunFixEffortTypeDescriptionsTask(viperConnectionString, executeMode);
                taskResults["FixEffortTypeDescriptions"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 13: Backfill harvest audit action types
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 13: Backfill Harvest Audit Actions");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunBackfillHarvestAuditActionsTask(viperConnectionString, executeMode);
                taskResults["BackfillHarvestAuditActions"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Task 14: Add AlertStates Table
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Task 14: Add AlertStates Table");
                Console.WriteLine("----------------------------------------");

                string viperConnectionString = EffortScriptHelper.GetConnectionString(configuration, "Viper", readOnly: false);
                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(viperConnectionString)}");
                Console.WriteLine();

                var (success, message) = RunAddAlertStatesTableTask(viperConnectionString, executeMode);
                taskResults["AddAlertStatesTable"] = (success, message);
                if (!success) overallResult = 1;

                Console.WriteLine();
            }

            // Summary
            Console.WriteLine("============================================");
            Console.WriteLine("Post-Deployment Summary");
            Console.WriteLine("============================================");
            foreach (var (taskName, result) in taskResults)
            {
                var statusIcon = result.Success ? "✓" : "✗";
                var statusColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = statusColor;
                Console.Write($"  [{statusIcon}] ");
                Console.ResetColor();
                Console.WriteLine($"{taskName}: {result.Message}");
            }
            Console.WriteLine();

            if (!executeMode)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY-RUN COMPLETE - No changes were made");
                Console.WriteLine("Run with --apply to apply changes");
                Console.ResetColor();
            }
            else if (overallResult == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All tasks completed successfully!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Some tasks had issues. Review output above.");
                Console.ResetColor();
            }

            return overallResult;
        }

        #region Task 4: Add EffortType Flag Columns

        private static (bool Success, string Message) RunAddEffortTypeFlagsTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check which columns already exist (table is now EffortTypes after Task 3 rename)
                var existingColumns = GetExistingEffortTypeColumns(connection);
                var missingColumns = new List<string>();

                Console.WriteLine("Checking for EffortType flag columns...");
                foreach (var column in EffortTypeFlagColumns)
                {
                    if (existingColumns.Contains(column))
                    {
                        Console.WriteLine($"  ✓ Column '{column}' already exists");
                    }
                    else
                    {
                        Console.WriteLine($"  ○ Column '{column}' is missing - will be added");
                        missingColumns.Add(column);
                    }
                }

                if (missingColumns.Count == 0)
                {
                    return (true, "All columns already exist");
                }

                Console.WriteLine();

                if (!executeMode)
                {
                    return (true, $"Would add {missingColumns.Count} columns: {string.Join(", ", missingColumns)}");
                }

                // Add missing columns
                foreach (var column in missingColumns)
                {
                    AddEffortTypeFlagColumn(connection, column);
                    Console.WriteLine($"  Added column '{column}'");
                }

                return (true, $"Added {missingColumns.Count} columns: {string.Join(", ", missingColumns)}");
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static HashSet<string> GetExistingEffortTypeColumns(SqlConnection connection)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = new SqlCommand(@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = 'EffortTypes'",
                connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }
            return columns;
        }

        private static void AddEffortTypeFlagColumn(SqlConnection connection, string columnName)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[EffortTypes]
                ADD [{columnName}] bit NOT NULL DEFAULT 1",
                connection);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Task 2: Add ManageSessionTypes Permission

        private static (bool Success, string Message) RunAddPermissionTask(string connectionString, bool executeMode, bool forceMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                Console.WriteLine($"Source Permission: {SourcePermission}");
                Console.WriteLine($"Target Permission: {TargetPermission}");
                Console.WriteLine();

                // Check if source permission exists
                int? sourcePermissionId = GetPermissionId(connection, SourcePermission);
                if (sourcePermissionId == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR: Source permission '{SourcePermission}' not found in RAPS.");
                    Console.ResetColor();
                    return (false, $"Source permission '{SourcePermission}' not found");
                }
                Console.WriteLine($"  Source permission found: PermissionID = {sourcePermissionId}");

                // Check if target permission already exists
                int? existingTargetId = GetPermissionId(connection, TargetPermission);
                if (existingTargetId != null)
                {
                    if (forceMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Target permission already exists (PermissionID = {existingTargetId}). --force mode: will delete and recreate.");
                        Console.ResetColor();

                        if (executeMode)
                        {
                            DeletePermissionAndMappings(connection, existingTargetId.Value);
                            Console.WriteLine($"  Deleted existing permission and all its mappings.");
                        }
                        else
                        {
                            Console.WriteLine($"  [DRY-RUN] Would delete existing permission and all its mappings.");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Target permission already exists (PermissionID = {existingTargetId}). Use --force to recreate.");
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine("Checking current mappings...");
                        ShowCurrentMappings(connection, existingTargetId.Value);
                        return (true, "Permission already exists");
                    }
                }
                else
                {
                    Console.WriteLine($"  Target permission does not exist yet.");
                }

                Console.WriteLine();

                // Gather what we'll clone
                var rolePermissions = GetRolePermissions(connection, sourcePermissionId.Value);
                var memberPermissions = GetMemberPermissions(connection, sourcePermissionId.Value);

                Console.WriteLine("Access to clone from source permission:");
                Console.WriteLine($"  - {rolePermissions.Count} role-permission mappings");
                Console.WriteLine($"  - {memberPermissions.Count} member-permission mappings");
                Console.WriteLine();

                if (rolePermissions.Count > 0)
                {
                    Console.WriteLine("Roles with access:");
                    foreach (var rp in rolePermissions)
                    {
                        string accessType = rp.Access == 1 ? "Allow" : "Deny";
                        Console.WriteLine($"    - RoleID {rp.RoleId}: {rp.RoleName} ({accessType})");
                    }
                    Console.WriteLine();
                }

                if (memberPermissions.Count > 0)
                {
                    Console.WriteLine("Members with direct access:");
                    foreach (var mp in memberPermissions)
                    {
                        string accessType = mp.Access == 1 ? "Allow" : "Deny";
                        string dateRange = "";
                        if (mp.StartDate.HasValue || mp.EndDate.HasValue)
                        {
                            dateRange = $" ({mp.StartDate?.ToString("yyyy-MM-dd") ?? "no start"} to {mp.EndDate?.ToString("yyyy-MM-dd") ?? "no end"})";
                        }
                        Console.WriteLine($"    - MemberID {mp.MemberId}: {mp.DisplayName} ({accessType}){dateRange}");
                    }
                    Console.WriteLine();
                }

                // Create the permission and clone mappings in a transaction
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Create the new permission
                    int newPermissionId = CreatePermission(connection, transaction, TargetPermission, TargetDescription);
                    Console.WriteLine($"Created permission '{TargetPermission}' with PermissionID = {newPermissionId}");

                    // Clone role permissions
                    int roleCount = CloneRolePermissions(connection, transaction, sourcePermissionId.Value, newPermissionId);
                    Console.WriteLine($"Cloned {roleCount} role-permission mappings");

                    // Clone member permissions
                    int memberCount = CloneMemberPermissions(connection, transaction, sourcePermissionId.Value, newPermissionId);
                    Console.WriteLine($"Cloned {memberCount} member-permission mappings");

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Transaction committed - changes are permanent");
                        Console.ResetColor();
                        return (true, $"Created permission (ID={newPermissionId}) with {roleCount} role mappings and {memberCount} member mappings");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified permission creation with {roleCount} role mappings and {memberCount} member mappings (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static int? GetPermissionId(SqlConnection connection, string permission)
        {
            using var cmd = new SqlCommand(
                "SELECT PermissionID FROM tblPermissions WHERE Permission = @Permission",
                connection);
            cmd.Parameters.AddWithValue("@Permission", permission);
            var result = cmd.ExecuteScalar();
            return result == null ? null : Convert.ToInt32(result);
        }

        private static int CreatePermission(SqlConnection connection, SqlTransaction transaction, string permission, string description)
        {
            // Insert the permission
            using var cmd = new SqlCommand(@"
                INSERT INTO tblPermissions (Permission, Description)
                OUTPUT INSERTED.PermissionID
                VALUES (@Permission, @Description)",
                connection, transaction);
            cmd.Parameters.AddWithValue("@Permission", permission);
            cmd.Parameters.AddWithValue("@Description", description);
            int permissionId = Convert.ToInt32(cmd.ExecuteScalar());

            // Create audit entry (matches RAPSAuditService.AuditPermissionChange with AuditActionType.Create)
            using var auditCmd = new SqlCommand(@"
                INSERT INTO tblLog (PermissionID, Audit, Detail, ModTime, ModBy)
                VALUES (@PermissionID, 'CreatePermission', @Detail, GETDATE(), @ModBy)",
                connection, transaction);
            auditCmd.Parameters.AddWithValue("@PermissionID", permissionId);
            auditCmd.Parameters.AddWithValue("@Detail", permission);
            auditCmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            auditCmd.ExecuteNonQuery();

            return permissionId;
        }

        private static void DeletePermissionAndMappings(SqlConnection connection, int permissionId)
        {
            // Delete member permissions first (FK constraint)
            using (var cmd = new SqlCommand("DELETE FROM tblMemberPermissions WHERE PermissionID = @PermissionID", connection))
            {
                cmd.Parameters.AddWithValue("@PermissionID", permissionId);
                cmd.ExecuteNonQuery();
            }

            // Delete role permissions
            using (var cmd = new SqlCommand("DELETE FROM tblRolePermissions WHERE PermissionID = @PermissionID", connection))
            {
                cmd.Parameters.AddWithValue("@PermissionID", permissionId);
                cmd.ExecuteNonQuery();
            }

            // Delete the permission
            using (var cmd = new SqlCommand("DELETE FROM tblPermissions WHERE PermissionID = @PermissionID", connection))
            {
                cmd.Parameters.AddWithValue("@PermissionID", permissionId);
                cmd.ExecuteNonQuery();
            }
        }

        private static List<RolePermissionInfo> GetRolePermissions(SqlConnection connection, int permissionId)
        {
            var result = new List<RolePermissionInfo>();
            using var cmd = new SqlCommand(@"
                SELECT rp.RoleID, rp.Access, r.Role
                FROM tblRolePermissions rp
                JOIN tblRoles r ON rp.RoleID = r.RoleID
                WHERE rp.PermissionID = @PermissionID
                ORDER BY r.Role",
                connection);
            cmd.Parameters.AddWithValue("@PermissionID", permissionId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new RolePermissionInfo
                {
                    RoleId = reader.GetInt32(0),
                    Access = reader.IsDBNull(1) ? 1 : Convert.ToInt32(reader.GetValue(1)),
                    RoleName = reader.GetString(2)
                });
            }
            return result;
        }

        private static List<MemberPermissionInfo> GetMemberPermissions(SqlConnection connection, int permissionId)
        {
            var result = new List<MemberPermissionInfo>();
            using var cmd = new SqlCommand(@"
                SELECT mp.MemberID, mp.Access, mp.AddDate, mp.StartDate, mp.EndDate, mp.ModBy, mp.ModTime
                FROM tblMemberPermissions mp
                WHERE mp.PermissionID = @PermissionID
                ORDER BY mp.MemberID",
                connection);
            cmd.Parameters.AddWithValue("@PermissionID", permissionId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var memberId = reader.GetString(0).Trim();
                result.Add(new MemberPermissionInfo
                {
                    MemberId = memberId,
                    Access = reader.IsDBNull(1) ? 1 : Convert.ToInt32(reader.GetValue(1)),
                    AddDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    StartDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    EndDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    ModBy = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ModTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    DisplayName = memberId // Use MemberID as display name for simplicity
                });
            }
            return result;
        }

        private static int CloneRolePermissions(SqlConnection connection, SqlTransaction transaction, int sourcePermissionId, int targetPermissionId)
        {
            // Insert role permissions
            using var cmd = new SqlCommand(@"
                INSERT INTO tblRolePermissions (RoleID, PermissionID, Access, ModBy, ModTime)
                SELECT RoleID, @TargetPermissionID, Access, @ModBy, GETDATE()
                FROM tblRolePermissions
                WHERE PermissionID = @SourcePermissionID",
                connection, transaction);
            cmd.Parameters.AddWithValue("@SourcePermissionID", sourcePermissionId);
            cmd.Parameters.AddWithValue("@TargetPermissionID", targetPermissionId);
            cmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            int count = cmd.ExecuteNonQuery();

            // Create audit entries for each role-permission mapping
            // (matches RAPSAuditService.AuditRolePermissionChange with AuditActionType.Create)
            // Note: The API uses "UpdateRolePermission" for both Create and Update actions
            using var auditCmd = new SqlCommand(@"
                INSERT INTO tblLog (RoleID, PermissionID, Audit, Detail, ModTime, ModBy)
                SELECT RoleID, @TargetPermissionID, 'UpdateRolePermission', CAST(Access AS varchar), GETDATE(), @ModBy
                FROM tblRolePermissions
                WHERE PermissionID = @TargetPermissionID",
                connection, transaction);
            auditCmd.Parameters.AddWithValue("@TargetPermissionID", targetPermissionId);
            auditCmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            auditCmd.ExecuteNonQuery();

            return count;
        }

        private static int CloneMemberPermissions(SqlConnection connection, SqlTransaction transaction, int sourcePermissionId, int targetPermissionId)
        {
            // Insert member permissions
            using var cmd = new SqlCommand(@"
                INSERT INTO tblMemberPermissions (MemberID, PermissionID, Access, AddDate, StartDate, EndDate, ModBy, ModTime)
                SELECT MemberID, @TargetPermissionID, Access, AddDate, StartDate, EndDate, @ModBy, GETDATE()
                FROM tblMemberPermissions
                WHERE PermissionID = @SourcePermissionID",
                connection, transaction);
            cmd.Parameters.AddWithValue("@SourcePermissionID", sourcePermissionId);
            cmd.Parameters.AddWithValue("@TargetPermissionID", targetPermissionId);
            cmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            int count = cmd.ExecuteNonQuery();

            // Create audit entries for each member-permission mapping
            // (matches RAPSAuditService.AuditPermissionMemberChange with AuditActionType.Create)
            // Detail format: {Access:<access>,"StartDate":"<date>","EndDate":"<date>"}
            using var auditCmd = new SqlCommand(@"
                INSERT INTO tblLog (MemberID, PermissionID, Audit, Detail, ModTime, ModBy)
                SELECT
                    MemberID,
                    @TargetPermissionID,
                    'CreateMemberPermission',
                    '{Access:' + CAST(Access AS varchar)
                        + CASE WHEN StartDate IS NOT NULL THEN ',""StartDate"":""' + CONVERT(varchar, StartDate, 112) + '""' ELSE '' END
                        + CASE WHEN EndDate IS NOT NULL THEN ',""EndDate"":""' + CONVERT(varchar, EndDate, 112) + '""' ELSE '' END
                        + '}',
                    GETDATE(),
                    @ModBy
                FROM tblMemberPermissions
                WHERE PermissionID = @TargetPermissionID",
                connection, transaction);
            auditCmd.Parameters.AddWithValue("@TargetPermissionID", targetPermissionId);
            auditCmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            auditCmd.ExecuteNonQuery();

            return count;
        }

        private static void ShowCurrentMappings(SqlConnection connection, int permissionId)
        {
            var rolePermissions = GetRolePermissions(connection, permissionId);
            var memberPermissions = GetMemberPermissions(connection, permissionId);

            Console.WriteLine($"  - {rolePermissions.Count} role-permission mappings");
            Console.WriteLine($"  - {memberPermissions.Count} member-permission mappings");

            if (rolePermissions.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Current roles with access:");
                foreach (var rp in rolePermissions)
                {
                    string accessType = rp.Access == 1 ? "Allow" : "Deny";
                    Console.WriteLine($"    - {rp.RoleName} ({accessType})");
                }
            }

            if (memberPermissions.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Current members with direct access:");
                foreach (var mp in memberPermissions)
                {
                    string accessType = mp.Access == 1 ? "Allow" : "Deny";
                    Console.WriteLine($"    - {mp.DisplayName} ({accessType})");
                }
            }
        }

        #endregion

        #region Task 1: Units Migration

        // Columns to remove from Units table
        private static readonly string[] UnitsColumnsToRemove = { "Code", "Description", "SortOrder" };
        // Indexes/constraints to remove from Units table
        private static readonly string[] UnitsConstraintsToRemove = { "UQ_Units_Code", "IX_Units_IsActive" };

        private static (bool Success, string Message) RunUnitsMigrationTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Begin transaction for dry-run support
                using var transaction = connection.BeginTransaction();

                try
                {
                    var completedSteps = new List<string>();

                    Console.WriteLine("Executing Units migration steps...");
                    Console.WriteLine();

                    // Step 1: Remove constraints/indexes from Units
                    Console.WriteLine("Step 1: Remove constraints/indexes from Units...");
                    foreach (var constraint in UnitsConstraintsToRemove)
                    {
                        if (ConstraintOrIndexExists(connection, transaction, "Units", constraint))
                        {
                            DropConstraintOrIndex(connection, transaction, "Units", constraint);
                            Console.WriteLine($"  ✓ Removed {constraint}");
                            completedSteps.Add($"Removed {constraint}");
                        }
                        else
                        {
                            Console.WriteLine($"  - {constraint} already removed");
                        }
                    }

                    // Step 2: Remove unused columns from Units
                    Console.WriteLine();
                    Console.WriteLine("Step 2: Remove unused columns from Units...");
                    var existingUnitsColumns = GetTableColumns(connection, transaction, "Units");
                    foreach (var column in UnitsColumnsToRemove)
                    {
                        if (existingUnitsColumns.Contains(column))
                        {
                            DropColumn(connection, transaction, "Units", column);
                            Console.WriteLine($"  ✓ Removed Units.{column}");
                            completedSteps.Add($"Removed Units.{column}");
                        }
                        else
                        {
                            Console.WriteLine($"  - Column '{column}' already removed");
                        }
                    }

                    // Step 3: Alter Units.Name to varchar(20)
                    Console.WriteLine();
                    Console.WriteLine("Step 3: Check/alter Units.Name column type...");
                    var nameColumnInfo = GetColumnInfo(connection, transaction, "Units", "Name");
                    if (nameColumnInfo.MaxLength != 20 || nameColumnInfo.DataType != "varchar")
                    {
                        AlterColumnType(connection, transaction, "Units", "Name", "varchar(20) NOT NULL");
                        Console.WriteLine($"  ✓ Altered Units.Name from {nameColumnInfo.DataType}({nameColumnInfo.MaxLength}) to varchar(20)");
                        completedSteps.Add("Altered Units.Name to varchar(20)");
                    }
                    else
                    {
                        Console.WriteLine($"  - Name is already varchar(20)");
                    }

                    // Step 4: Add UnitId column to Percentages
                    Console.WriteLine();
                    Console.WriteLine("Step 4: Add Percentages.UnitId column...");
                    var existingPercentagesColumns = GetTableColumns(connection, transaction, "Percentages");
                    bool unitIdExists = existingPercentagesColumns.Contains("UnitId");
                    bool oldUnitExists = existingPercentagesColumns.Contains("Unit");

                    if (!unitIdExists)
                    {
                        AddColumn(connection, transaction, "Percentages", "UnitId", "int NULL");
                        Console.WriteLine($"  ✓ Added Percentages.UnitId column");
                        completedSteps.Add("Added Percentages.UnitId");
                        unitIdExists = true;
                    }
                    else
                    {
                        Console.WriteLine($"  - UnitId column already exists");
                    }

                    // Step 5: Populate UnitId from Unit
                    Console.WriteLine();
                    Console.WriteLine("Step 5: Populate UnitId from Unit column...");
                    if (unitIdExists && oldUnitExists)
                    {
                        int updated = PopulateUnitIdFromUnit(connection, transaction);
                        if (updated > 0)
                        {
                            Console.WriteLine($"  ✓ Populated {updated} UnitId values from Unit");
                            completedSteps.Add($"Populated {updated} UnitId values");
                        }
                        else
                        {
                            Console.WriteLine($"  - All UnitId values already populated");
                        }
                    }
                    else if (!oldUnitExists)
                    {
                        Console.WriteLine($"  - Old Unit column doesn't exist, skipping population");
                    }

                    // Verify migration before adding FK
                    int unmapped = oldUnitExists ? GetUnmappedUnitIdCount(connection, transaction) : 0;
                    if (unmapped > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  WARNING: {unmapped} rows still have NULL UnitId but non-NULL Unit");
                        Console.ResetColor();
                    }

                    // Step 6: Add FK constraint
                    Console.WriteLine();
                    Console.WriteLine("Step 6: Add FK_Percentages_Units constraint...");
                    if (!ForeignKeyExists(connection, transaction, "FK_Percentages_Units"))
                    {
                        AddForeignKey(connection, transaction, "Percentages", "FK_Percentages_Units", "UnitId", "Units", "Id");
                        Console.WriteLine($"  ✓ Added FK_Percentages_Units");
                        completedSteps.Add("Added FK_Percentages_Units");
                    }
                    else
                    {
                        Console.WriteLine($"  - FK_Percentages_Units already exists");
                    }

                    // Step 7: Add index on UnitId
                    Console.WriteLine();
                    Console.WriteLine("Step 7: Add IX_Percentages_UnitId index...");
                    if (!ConstraintOrIndexExists(connection, transaction, "Percentages", "IX_Percentages_UnitId"))
                    {
                        AddFilteredIndex(connection, transaction, "Percentages", "IX_Percentages_UnitId", "UnitId", "UnitId IS NOT NULL");
                        Console.WriteLine($"  ✓ Added IX_Percentages_UnitId");
                        completedSteps.Add("Added IX_Percentages_UnitId");
                    }
                    else
                    {
                        Console.WriteLine($"  - IX_Percentages_UnitId already exists");
                    }

                    // Step 8: Drop old Unit column (only if UnitId is populated)
                    Console.WriteLine();
                    Console.WriteLine("Step 8: Drop old Percentages.Unit column...");
                    existingPercentagesColumns = GetTableColumns(connection, transaction, "Percentages");
                    oldUnitExists = existingPercentagesColumns.Contains("Unit");
                    if (oldUnitExists && unmapped == 0)
                    {
                        DropColumn(connection, transaction, "Percentages", "Unit");
                        Console.WriteLine($"  ✓ Dropped Percentages.Unit column");
                        completedSteps.Add("Dropped Percentages.Unit");
                    }
                    else if (oldUnitExists)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  - Skipped dropping Unit column - {unmapped} rows still need migration");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  - Old Unit column already removed");
                    }

                    // Step 9: Add unique constraint on Units.Name
                    Console.WriteLine();
                    Console.WriteLine("Step 9: Add UQ_Units_Name constraint...");
                    if (!ConstraintOrIndexExists(connection, transaction, "Units", "UQ_Units_Name"))
                    {
                        AddUniqueConstraint(connection, transaction, "Units", "UQ_Units_Name", "Name");
                        Console.WriteLine($"  ✓ Added UQ_Units_Name");
                        completedSteps.Add("Added UQ_Units_Name");
                    }
                    else
                    {
                        Console.WriteLine($"  - UQ_Units_Name already exists");
                    }

                    Console.WriteLine();

                    // If no steps were needed, skip transaction handling
                    if (completedSteps.Count == 0)
                    {
                        transaction.Rollback(); // Clean up empty transaction
                        return (true, "All migration steps already complete");
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Transaction committed - changes are permanent");
                        Console.ResetColor();
                        return (true, $"Completed {completedSteps.Count} migration steps");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified {completedSteps.Count} migration steps (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        // Transaction-aware overloads for Units Migration
        private static HashSet<string> GetTableColumns(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = new SqlCommand(@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = @TableName",
                connection, transaction);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }
            return columns;
        }

        private static (string DataType, int MaxLength) GetColumnInfo(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using var cmd = new SqlCommand(@"
                SELECT DATA_TYPE, ISNULL(CHARACTER_MAXIMUM_LENGTH, 0)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName",
                connection, transaction);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@ColumnName", columnName);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetString(0), reader.GetInt32(1));
            }
            return ("unknown", 0);
        }

        private static bool ConstraintOrIndexExists(SqlConnection connection, SqlTransaction transaction, string tableName, string constraintName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.indexes
                WHERE name = @ConstraintName AND object_id = OBJECT_ID('[effort].[' + @TableName + ']')
                UNION
                SELECT 1 FROM sys.key_constraints
                WHERE name = @ConstraintName AND parent_object_id = OBJECT_ID('[effort].[' + @TableName + ']')",
                connection, transaction);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@ConstraintName", constraintName);
            return cmd.ExecuteScalar() != null;
        }

        private static bool ForeignKeyExists(SqlConnection connection, SqlTransaction transaction, string fkName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.foreign_keys WHERE name = @FKName",
                connection, transaction);
            cmd.Parameters.AddWithValue("@FKName", fkName);
            return cmd.ExecuteScalar() != null;
        }

        private static int GetUnmappedUnitIdCount(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM [effort].[Percentages]
                WHERE Unit IS NOT NULL AND UnitId IS NULL",
                connection, transaction);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void DropConstraintOrIndex(SqlConnection connection, SqlTransaction transaction, string tableName, string constraintName)
        {
            // Check if it's a constraint or index
            using var checkCmd = new SqlCommand(@"
                SELECT CASE
                    WHEN EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = @ConstraintName) THEN 'CONSTRAINT'
                    WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = @ConstraintName) THEN 'INDEX'
                    ELSE 'UNKNOWN'
                END",
                connection, transaction);
            checkCmd.Parameters.AddWithValue("@ConstraintName", constraintName);
            var type = checkCmd.ExecuteScalar()?.ToString();

            string sql = type == "CONSTRAINT"
                ? $"ALTER TABLE [effort].[{tableName}] DROP CONSTRAINT [{constraintName}]"
                : $"DROP INDEX [{constraintName}] ON [effort].[{tableName}]";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void DropColumn(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[{tableName}] DROP COLUMN [{columnName}]",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void AlterColumnType(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string newType)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[{tableName}] ALTER COLUMN [{columnName}] {newType}",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void AddColumn(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string columnType)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[{tableName}] ADD [{columnName}] {columnType}",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static int PopulateUnitIdFromUnit(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand(@"
                UPDATE [effort].[Percentages]
                SET UnitId = TRY_CAST(Unit AS int)
                WHERE Unit IS NOT NULL AND UnitId IS NULL",
                connection, transaction);
            return cmd.ExecuteNonQuery();
        }

        private static void AddForeignKey(SqlConnection connection, SqlTransaction transaction, string tableName, string fkName, string columnName, string refTable, string refColumn)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[{tableName}]
                ADD CONSTRAINT [{fkName}]
                FOREIGN KEY ([{columnName}]) REFERENCES [effort].[{refTable}]([{refColumn}])",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void AddFilteredIndex(SqlConnection connection, SqlTransaction transaction, string tableName, string indexName, string columnName, string filter)
        {
            using var cmd = new SqlCommand($@"
                CREATE NONCLUSTERED INDEX [{indexName}]
                ON [effort].[{tableName}]([{columnName}])
                WHERE {filter}",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void AddUniqueConstraint(SqlConnection connection, SqlTransaction transaction, string tableName, string constraintName, string columnName)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[{tableName}] ADD CONSTRAINT [{constraintName}] UNIQUE ([{columnName}])",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Task 4: Rename Tables for Standardization

        private static (bool Success, string Message) RunRenameTablesTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Detect current state
                bool effortTypesExists = TableExists(connection, "EffortTypes");
                bool sessionTypesExists = TableExists(connection, "SessionTypes");
                bool percentAssignTypesExists = TableExists(connection, "PercentAssignTypes");

                string migrationState;
                if (effortTypesExists && sessionTypesExists && !percentAssignTypesExists)
                {
                    migrationState = "PRE_MIGRATION";
                }
                else if (percentAssignTypesExists && sessionTypesExists && !effortTypesExists)
                {
                    migrationState = "PARTIAL_MIGRATION";
                }
                else if (percentAssignTypesExists && effortTypesExists && !sessionTypesExists)
                {
                    migrationState = "COMPLETE";
                }
                else
                {
                    migrationState = "UNKNOWN";
                }

                Console.WriteLine($"Current migration state: {migrationState}");
                Console.WriteLine($"  EffortTypes exists: {effortTypesExists}");
                Console.WriteLine($"  SessionTypes exists: {sessionTypesExists}");
                Console.WriteLine($"  PercentAssignTypes exists: {percentAssignTypesExists}");
                Console.WriteLine();

                if (migrationState == "COMPLETE")
                {
                    return (true, "Tables already renamed");
                }

                if (migrationState == "UNKNOWN")
                {
                    return (false, "Unknown migration state - manual intervention required");
                }

                var completedSteps = new List<string>();

                // Step 1: Rename EffortTypes to PercentAssignTypes (if needed)
                if (migrationState == "PRE_MIGRATION")
                {
                    Console.WriteLine("Step 1: Renaming [effort].[EffortTypes] to [effort].[PercentAssignTypes]...");
                    if (executeMode)
                    {
                        RenameTable(connection, "EffortTypes", "PercentAssignTypes");
                        Console.WriteLine("  ✓ Table renamed");
                        completedSteps.Add("EffortTypes → PercentAssignTypes");
                    }
                    else
                    {
                        Console.WriteLine("  [DRY-RUN] Would rename EffortTypes to PercentAssignTypes");
                        completedSteps.Add("Would rename EffortTypes → PercentAssignTypes");
                    }
                }
                else
                {
                    Console.WriteLine("Step 1: EffortTypes already renamed to PercentAssignTypes - skipping");
                }

                Console.WriteLine();

                // Step 2: Rename SessionTypes to EffortTypes
                if (sessionTypesExists)
                {
                    Console.WriteLine("Step 2: Renaming [effort].[SessionTypes] to [effort].[EffortTypes]...");
                    if (executeMode)
                    {
                        RenameTable(connection, "SessionTypes", "EffortTypes");
                        Console.WriteLine("  ✓ Table renamed");
                        completedSteps.Add("SessionTypes → EffortTypes");
                    }
                    else
                    {
                        Console.WriteLine("  [DRY-RUN] Would rename SessionTypes to EffortTypes");
                        completedSteps.Add("Would rename SessionTypes → EffortTypes");
                    }
                }
                else
                {
                    Console.WriteLine("Step 2: SessionTypes already renamed to EffortTypes - skipping");
                }

                return (true, string.Join(", ", completedSteps));
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = @TableName",
                connection);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            return cmd.ExecuteScalar() != null;
        }

        private static void RenameTable(SqlConnection connection, string oldName, string newName)
        {
            using var cmd = new SqlCommand($"EXEC sp_rename 'effort.{oldName}', '{newName}'", connection);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Task 5: Rename Records FK Columns

        private static (bool Success, string Message) RunRenameRecordsFKColumnsTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var completedSteps = new List<string>();

                // Step 1: Rename SessionType → EffortTypeId
                Console.WriteLine("Step 1: Checking SessionType → EffortTypeId rename...");
                bool sessionTypeExists = ColumnExists(connection, "Records", "SessionType");
                bool effortTypeIdExists = ColumnExists(connection, "Records", "EffortTypeId");

                Console.WriteLine($"  Column 'SessionType' exists: {sessionTypeExists}");
                Console.WriteLine($"  Column 'EffortTypeId' exists: {effortTypeIdExists}");

                if (sessionTypeExists && effortTypeIdExists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Both SessionType and EffortTypeId exist - manual intervention required");
                    Console.ResetColor();
                }
                else if (!sessionTypeExists && !effortTypeIdExists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Neither SessionType nor EffortTypeId exists - manual intervention required");
                    Console.ResetColor();
                }
                else if (effortTypeIdExists)
                {
                    // Only new exists - already renamed
                    Console.WriteLine("  ✓ Already renamed to EffortTypeId");
                }
                else
                {
                    // Only old exists - proceed with rename
                    if (executeMode)
                    {
                        using var cmd = new SqlCommand("EXEC sp_rename 'effort.Records.SessionType', 'EffortTypeId', 'COLUMN'", connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("  ✓ Renamed SessionType → EffortTypeId");
                        completedSteps.Add("SessionType → EffortTypeId");
                    }
                    else
                    {
                        Console.WriteLine("  [DRY-RUN] Would rename SessionType → EffortTypeId");
                        completedSteps.Add("Would rename SessionType → EffortTypeId");
                    }
                }

                Console.WriteLine();

                // Step 2: Rename Role → RoleId
                Console.WriteLine("Step 2: Checking Role → RoleId rename...");
                bool roleExists = ColumnExists(connection, "Records", "Role");
                bool roleIdExists = ColumnExists(connection, "Records", "RoleId");

                Console.WriteLine($"  Column 'Role' exists: {roleExists}");
                Console.WriteLine($"  Column 'RoleId' exists: {roleIdExists}");

                if (roleExists && roleIdExists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Both Role and RoleId exist - manual intervention required");
                    Console.ResetColor();
                }
                else if (!roleExists && !roleIdExists)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Neither Role nor RoleId exists - manual intervention required");
                    Console.ResetColor();
                }
                else if (roleIdExists)
                {
                    // Only new exists - already renamed
                    Console.WriteLine("  ✓ Already renamed to RoleId");
                }
                else
                {
                    // Only old exists - proceed with rename
                    if (executeMode)
                    {
                        using var cmd = new SqlCommand("EXEC sp_rename 'effort.Records.Role', 'RoleId', 'COLUMN'", connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("  ✓ Renamed Role → RoleId");
                        completedSteps.Add("Role → RoleId");
                    }
                    else
                    {
                        Console.WriteLine("  [DRY-RUN] Would rename Role → RoleId");
                        completedSteps.Add("Would rename Role → RoleId");
                    }
                }

                if (completedSteps.Count == 0)
                {
                    return (true, "All columns already renamed");
                }

                return (true, string.Join(", ", completedSteps));
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName",
                connection);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@ColumnName", columnName);
            return cmd.ExecuteScalar() != null;
        }

        #endregion

        #region Task 6: Rename Percentages.EffortTypeId Column

        private static (bool Success, string Message) RunRenamePercentagesEffortTypeIdTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check if column exists with old name
                bool effortTypeIdExists = ColumnExists(connection, "Percentages", "EffortTypeId");
                bool percentAssignTypeIdExists = ColumnExists(connection, "Percentages", "PercentAssignTypeId");

                Console.WriteLine($"Column 'EffortTypeId' exists: {effortTypeIdExists}");
                Console.WriteLine($"Column 'PercentAssignTypeId' exists: {percentAssignTypeIdExists}");
                Console.WriteLine();

                if (effortTypeIdExists && percentAssignTypeIdExists)
                {
                    return (false, "Both EffortTypeId and PercentAssignTypeId columns exist - manual intervention required");
                }
                if (!effortTypeIdExists && !percentAssignTypeIdExists)
                {
                    return (false, "Neither EffortTypeId nor PercentAssignTypeId column exists - manual intervention required");
                }
                if (percentAssignTypeIdExists)
                {
                    // Only new exists - already renamed
                    return (true, "Column already renamed to PercentAssignTypeId");
                }

                // Only old exists - proceed with rename
                Console.WriteLine("Renaming column [effort].[Percentages].[EffortTypeId] to [PercentAssignTypeId]...");

                if (!executeMode)
                {
                    Console.WriteLine("  [DRY-RUN] Would rename column");
                    return (true, "Would rename EffortTypeId → PercentAssignTypeId");
                }

                using var cmd = new SqlCommand("EXEC sp_rename 'effort.Percentages.EffortTypeId', 'PercentAssignTypeId', 'COLUMN'", connection);
                cmd.ExecuteNonQuery();
                Console.WriteLine("  ✓ Column renamed");

                return (true, "Renamed EffortTypeId → PercentAssignTypeId");
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Task 7: Duplicate Records Cleanup

        private static (bool Success, string Message) RunDuplicateRecordsCleanupTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check if unique constraint already exists
                bool constraintExists = UniqueIndexExists(connection, "Records", "UQ_Records_Course_Person_EffortType");

                // Count duplicate records
                var (duplicateGroups, recordsToDelete) = CountDuplicateRecords(connection);

                Console.WriteLine("Analyzing duplicate effort records...");
                Console.WriteLine($"  - Duplicate groups: {duplicateGroups}");
                Console.WriteLine($"  - Records to delete: {recordsToDelete}");
                Console.WriteLine($"  - Unique constraint exists: {constraintExists}");
                Console.WriteLine();

                if (duplicateGroups == 0)
                {
                    if (constraintExists)
                    {
                        return (true, "No duplicates found, unique constraint already exists");
                    }

                    // No duplicates but constraint missing - just add the constraint
                    // Note: CREATE INDEX is DDL and typically auto-commits, so we can't truly dry-run it
                    if (!executeMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  [DRY-RUN] Would add unique constraint (no duplicates to clean)");
                        Console.ResetColor();
                        return (true, "Would add unique constraint (no duplicates to clean)");
                    }

                    AddRecordsUniqueConstraint(connection, null);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ Added unique constraint [UQ_Records_Course_Person_EffortType]");
                    Console.ResetColor();
                    return (true, "Added unique constraint (no duplicates to clean)");
                }

                // Execute cleanup with transaction (DELETE is DML, can be rolled back)
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Step 1: Delete duplicates (keep highest Id per group)
                    Console.Write("Deleting duplicate records... ");
                    int deleted = DeleteDuplicateRecords(connection, transaction);
                    Console.WriteLine($"✓ ({deleted} records)");

                    // Step 2: Verify no duplicates remain
                    Console.Write("Verifying no duplicates remain... ");
                    var (remainingGroups, _) = CountDuplicateRecords(connection, transaction);
                    if (remainingGroups > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ ({remainingGroups} groups remain)");
                        Console.ResetColor();
                        transaction.Rollback();
                        return (false, $"Cleanup incomplete: {remainingGroups} duplicate groups remain");
                    }
                    Console.WriteLine("✓");

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        // Step 3: Add unique constraint (only in execute mode, after commit)
                        transaction.Commit();

                        if (!constraintExists)
                        {
                            Console.Write("Adding unique constraint... ");
                            AddRecordsUniqueConstraint(connection, null);
                            Console.WriteLine("✓");
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Changes are permanent");
                        Console.ResetColor();
                        return (true, $"Deleted {deleted} duplicates from {duplicateGroups} groups; added unique constraint");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        if (!constraintExists)
                        {
                            Console.WriteLine("  [DRY-RUN] Would add unique constraint after delete");
                        }
                        Console.ResetColor();
                        return (true, $"Verified deletion of {deleted} duplicates from {duplicateGroups} groups (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static bool UniqueIndexExists(SqlConnection connection, string tableName, string indexName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.indexes
                WHERE name = @IndexName AND object_id = OBJECT_ID('[effort].[' + @TableName + ']')",
                connection);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@IndexName", indexName);
            return cmd.ExecuteScalar() != null;
        }

        private static (int DuplicateGroups, int RecordsToDelete) CountDuplicateRecords(SqlConnection connection, SqlTransaction? transaction = null)
        {
            using var cmd = new SqlCommand(@"
                WITH DuplicateGroups AS (
                    SELECT CourseId, PersonId, EffortTypeId, COUNT(*) AS RecordCount
                    FROM [effort].[Records]
                    GROUP BY CourseId, PersonId, EffortTypeId
                    HAVING COUNT(*) > 1
                )
                SELECT COUNT(*) AS GroupCount, ISNULL(SUM(RecordCount - 1), 0) AS RecordsToDelete
                FROM DuplicateGroups",
                connection, transaction);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetInt32(0), reader.GetInt32(1));
            }
            return (0, 0);
        }

        private static int DeleteDuplicateRecords(SqlConnection connection, SqlTransaction transaction)
        {
            // Delete all duplicate records, keeping the one with the highest Id per group
            using var cmd = new SqlCommand(@"
                WITH DuplicateGroups AS (
                    SELECT CourseId, PersonId, EffortTypeId
                    FROM [effort].[Records]
                    GROUP BY CourseId, PersonId, EffortTypeId
                    HAVING COUNT(*) > 1
                ),
                RecordsToKeep AS (
                    SELECT MAX(r.Id) AS MaxId
                    FROM [effort].[Records] r
                    INNER JOIN DuplicateGroups d
                        ON r.CourseId = d.CourseId
                        AND r.PersonId = d.PersonId
                        AND r.EffortTypeId = d.EffortTypeId
                    GROUP BY r.CourseId, r.PersonId, r.EffortTypeId
                )
                DELETE r
                FROM [effort].[Records] r
                INNER JOIN DuplicateGroups d
                    ON r.CourseId = d.CourseId
                    AND r.PersonId = d.PersonId
                    AND r.EffortTypeId = d.EffortTypeId
                WHERE r.Id NOT IN (SELECT MaxId FROM RecordsToKeep)",
                connection, transaction);

            return cmd.ExecuteNonQuery();
        }

        private static void AddRecordsUniqueConstraint(SqlConnection connection, SqlTransaction? transaction)
        {
            using var cmd = new SqlCommand(@"
                CREATE UNIQUE INDEX [UQ_Records_Course_Person_EffortType]
                ON [effort].[Records] ([CourseId], [PersonId], [EffortTypeId])",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Task 8: Add LastEmailed Columns to Persons

        private static (bool Success, string Message) RunAddLastEmailedColumnsTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Begin transaction for dry-run support
                using var transaction = connection.BeginTransaction();

                try
                {
                    var completedSteps = new List<string>();

                    // Check which columns already exist
                    var existingColumns = GetTableColumns(connection, transaction, "Persons");

                    // Step 1: Add LastEmailed column
                    Console.WriteLine("Step 1: Add LastEmailed column...");
                    if (!existingColumns.Contains("LastEmailed"))
                    {
                        AddColumn(connection, transaction, "Persons", "LastEmailed", "datetime2(7) NULL");
                        Console.WriteLine("  ✓ Added Persons.LastEmailed column");
                        completedSteps.Add("Added LastEmailed column");
                    }
                    else
                    {
                        Console.WriteLine("  - LastEmailed column already exists");
                    }

                    // Step 2: Add LastEmailedBy column
                    Console.WriteLine();
                    Console.WriteLine("Step 2: Add LastEmailedBy column...");
                    if (!existingColumns.Contains("LastEmailedBy"))
                    {
                        AddColumn(connection, transaction, "Persons", "LastEmailedBy", "int NULL");
                        Console.WriteLine("  ✓ Added Persons.LastEmailedBy column");
                        completedSteps.Add("Added LastEmailedBy column");
                    }
                    else
                    {
                        Console.WriteLine("  - LastEmailedBy column already exists");
                    }

                    // Step 3: Add FK constraint
                    Console.WriteLine();
                    Console.WriteLine("Step 3: Add FK_Persons_LastEmailedBy constraint...");
                    if (!ForeignKeyExists(connection, transaction, "FK_Persons_LastEmailedBy"))
                    {
                        using var fkCmd = new SqlCommand(@"
                            ALTER TABLE [effort].[Persons]
                            ADD CONSTRAINT [FK_Persons_LastEmailedBy]
                            FOREIGN KEY ([LastEmailedBy]) REFERENCES [users].[Person]([PersonId])",
                            connection, transaction);
                        fkCmd.ExecuteNonQuery();
                        Console.WriteLine("  ✓ Added FK_Persons_LastEmailedBy constraint");
                        completedSteps.Add("Added FK constraint");
                    }
                    else
                    {
                        Console.WriteLine("  - FK_Persons_LastEmailedBy already exists");
                    }

                    // Step 4: Backfill from audit data
                    Console.WriteLine();
                    Console.WriteLine("Step 4: Backfill LastEmailed from audit data...");
                    int backfilled = BackfillLastEmailedFromAudit(connection, transaction);
                    if (backfilled > 0)
                    {
                        Console.WriteLine($"  ✓ Backfilled {backfilled} rows from audit data");
                        completedSteps.Add($"Backfilled {backfilled} rows");
                    }
                    else
                    {
                        Console.WriteLine("  - No rows needed backfill (all NULL or already populated)");
                    }

                    Console.WriteLine();

                    // If no steps were needed, skip transaction handling
                    if (completedSteps.Count == 0)
                    {
                        transaction.Rollback();
                        return (true, "All steps already complete");
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Transaction committed - changes are permanent");
                        Console.ResetColor();
                        return (true, $"Completed {completedSteps.Count} steps: {string.Join(", ", completedSteps)}");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified {completedSteps.Count} steps (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static int BackfillLastEmailedFromAudit(SqlConnection connection, SqlTransaction transaction)
        {
            // Get the most recent successful VerifyEmail audit for each person/term
            // and update Persons.LastEmailed/LastEmailedBy where NULL.
            // LEFT JOIN to users.Person sets LastEmailedBy to NULL when the sender
            // no longer exists (deleted user or ChangedBy=0), which satisfies the FK.
            using var cmd = new SqlCommand(@"
                WITH LatestEmails AS (
                    SELECT RecordId, TermCode, ChangedDate, ChangedBy,
                           ROW_NUMBER() OVER (PARTITION BY RecordId, TermCode ORDER BY ChangedDate DESC) as rn
                    FROM [effort].[Audits]
                    WHERE TableName = 'Persons'
                      AND Action = 'VerifyEmail'
                      AND Changes LIKE '%""NewValue"":""Success""%'
                )
                UPDATE p SET
                    p.LastEmailed = e.ChangedDate,
                    p.LastEmailedBy = u.PersonId
                FROM [effort].[Persons] p
                INNER JOIN LatestEmails e
                    ON p.PersonId = e.RecordId
                    AND p.TermCode = e.TermCode
                    AND e.rn = 1
                LEFT JOIN [users].[Person] u
                    ON u.PersonId = e.ChangedBy
                WHERE p.LastEmailed IS NULL",
                connection, transaction);

            return cmd.ExecuteNonQuery();
        }

        #endregion

        #region Task 9: Simplify TermStatus Table

        // Columns and constraints to remove from TermStatus table
        private static readonly string[] TermStatusColumnsToRemove = { "Status", "CreatedDate", "ModifiedDate", "ModifiedBy" };
        private static readonly string[] TermStatusConstraintsToRemove = { "FK_TermStatus_ModifiedBy", "CK_TermStatus_Status" };
        private static readonly string[] TermStatusIndexesToRemove = { "IX_TermStatus_Status" };

        private static (bool Success, string Message) RunSimplifyTermStatusTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Begin transaction for dry-run support
                using var transaction = connection.BeginTransaction();

                try
                {
                    var completedSteps = new List<string>();

                    Console.WriteLine("Simplifying TermStatus table (removing redundant columns)...");
                    Console.WriteLine("  Status is now computed from dates to match legacy ColdFusion logic.");
                    Console.WriteLine();

                    // Step 1: Drop FK constraint
                    Console.WriteLine("Step 1: Drop FK_TermStatus_ModifiedBy constraint...");
                    if (TermStatusForeignKeyExists(connection, transaction, "FK_TermStatus_ModifiedBy"))
                    {
                        DropTermStatusConstraint(connection, transaction, "FK_TermStatus_ModifiedBy");
                        Console.WriteLine("  ✓ Dropped FK_TermStatus_ModifiedBy");
                        completedSteps.Add("Dropped FK_TermStatus_ModifiedBy");
                    }
                    else
                    {
                        Console.WriteLine("  - FK_TermStatus_ModifiedBy already removed");
                    }

                    // Step 2: Drop check constraint
                    Console.WriteLine();
                    Console.WriteLine("Step 2: Drop CK_TermStatus_Status constraint...");
                    if (TermStatusCheckConstraintExists(connection, transaction, "CK_TermStatus_Status"))
                    {
                        DropTermStatusConstraint(connection, transaction, "CK_TermStatus_Status");
                        Console.WriteLine("  ✓ Dropped CK_TermStatus_Status");
                        completedSteps.Add("Dropped CK_TermStatus_Status");
                    }
                    else
                    {
                        Console.WriteLine("  - CK_TermStatus_Status already removed");
                    }

                    // Step 3: Drop index on Status
                    Console.WriteLine();
                    Console.WriteLine("Step 3: Drop IX_TermStatus_Status index...");
                    if (TermStatusIndexExists(connection, transaction, "IX_TermStatus_Status"))
                    {
                        DropTermStatusIndex(connection, transaction, "IX_TermStatus_Status");
                        Console.WriteLine("  ✓ Dropped IX_TermStatus_Status");
                        completedSteps.Add("Dropped IX_TermStatus_Status");
                    }
                    else
                    {
                        Console.WriteLine("  - IX_TermStatus_Status already removed");
                    }

                    // Step 4: Drop columns
                    Console.WriteLine();
                    Console.WriteLine("Step 4: Drop redundant columns...");
                    var existingColumns = GetTermStatusColumns(connection, transaction);
                    foreach (var column in TermStatusColumnsToRemove)
                    {
                        if (existingColumns.Contains(column))
                        {
                            DropTermStatusColumn(connection, transaction, column);
                            Console.WriteLine($"  ✓ Dropped TermStatus.{column}");
                            completedSteps.Add($"Dropped {column}");
                        }
                        else
                        {
                            Console.WriteLine($"  - Column '{column}' already removed");
                        }
                    }

                    Console.WriteLine();

                    // If no steps were needed, skip transaction handling
                    if (completedSteps.Count == 0)
                    {
                        transaction.Rollback();
                        return (true, "All simplification steps already complete");
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Transaction committed - changes are permanent");
                        Console.ResetColor();
                        return (true, $"Completed {completedSteps.Count} steps: {string.Join(", ", completedSteps)}");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified {completedSteps.Count} steps (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static HashSet<string> GetTermStatusColumns(SqlConnection connection, SqlTransaction transaction)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = new SqlCommand(@"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort' AND TABLE_NAME = 'TermStatus'",
                connection, transaction);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }
            return columns;
        }

        private static bool TermStatusForeignKeyExists(SqlConnection connection, SqlTransaction transaction, string fkName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.foreign_keys
                WHERE name = @FKName AND parent_object_id = OBJECT_ID('[effort].[TermStatus]')",
                connection, transaction);
            cmd.Parameters.AddWithValue("@FKName", fkName);
            return cmd.ExecuteScalar() != null;
        }

        private static bool TermStatusCheckConstraintExists(SqlConnection connection, SqlTransaction transaction, string constraintName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.check_constraints
                WHERE name = @ConstraintName AND parent_object_id = OBJECT_ID('[effort].[TermStatus]')",
                connection, transaction);
            cmd.Parameters.AddWithValue("@ConstraintName", constraintName);
            return cmd.ExecuteScalar() != null;
        }

        private static bool TermStatusIndexExists(SqlConnection connection, SqlTransaction transaction, string indexName)
        {
            using var cmd = new SqlCommand(@"
                SELECT 1 FROM sys.indexes
                WHERE name = @IndexName AND object_id = OBJECT_ID('[effort].[TermStatus]')",
                connection, transaction);
            cmd.Parameters.AddWithValue("@IndexName", indexName);
            return cmd.ExecuteScalar() != null;
        }

        private static void DropTermStatusConstraint(SqlConnection connection, SqlTransaction transaction, string constraintName)
        {
            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[TermStatus] DROP CONSTRAINT [{constraintName}]",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void DropTermStatusIndex(SqlConnection connection, SqlTransaction transaction, string indexName)
        {
            using var cmd = new SqlCommand($@"
                DROP INDEX [{indexName}] ON [effort].[TermStatus]",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static void DropTermStatusColumn(SqlConnection connection, SqlTransaction transaction, string columnName)
        {
            // First, drop any default constraints on the column (auto-generated names like DF__TermStatu__...)
            var defaultConstraintName = GetDefaultConstraintName(connection, transaction, "effort", "TermStatus", columnName);
            if (defaultConstraintName != null)
            {
                using var dropDefaultCmd = new SqlCommand($@"
                    ALTER TABLE [effort].[TermStatus] DROP CONSTRAINT [{defaultConstraintName}]",
                    connection, transaction);
                dropDefaultCmd.ExecuteNonQuery();
                Console.WriteLine($"    ✓ Dropped default constraint {defaultConstraintName}");
            }

            using var cmd = new SqlCommand($@"
                ALTER TABLE [effort].[TermStatus] DROP COLUMN [{columnName}]",
                connection, transaction);
            cmd.ExecuteNonQuery();
        }

        private static string? GetDefaultConstraintName(SqlConnection connection, SqlTransaction transaction, string schemaName, string tableName, string columnName)
        {
            using var cmd = new SqlCommand(@"
                SELECT dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                WHERE dc.parent_object_id = OBJECT_ID(@TableName)
                  AND c.name = @ColumnName",
                connection, transaction);
            cmd.Parameters.AddWithValue("@TableName", $"[{schemaName}].[{tableName}]");
            cmd.Parameters.AddWithValue("@ColumnName", columnName);
            return cmd.ExecuteScalar() as string;
        }

        #endregion

        #region Task 10: Fix Percentage Constraint

        /// <summary>
        /// Updates CK_Percentages_Percentage constraint from BETWEEN 0 AND 100 to BETWEEN 0 AND 1.
        /// The service stores percentages as decimals (0-1) but the constraint was incorrectly defined as 0-100.
        /// </summary>
        private static (bool Success, string Message) RunFixPercentageConstraintTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                Console.WriteLine("Checking CK_Percentages_Percentage constraint...");
                Console.WriteLine("  Service stores percentages as decimals (0-1), constraint should match.");
                Console.WriteLine();

                // Check current constraint definition
                string? currentDefinition = GetPercentageConstraintDefinition(connection);

                if (currentDefinition == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Constraint CK_Percentages_Percentage not found");
                    Console.ResetColor();
                    return (true, "Constraint not found - may need manual review");
                }

                Console.WriteLine($"  Current constraint: {currentDefinition}");

                // Check if already correct (0-1 range)
                // Use regex with word boundary to avoid false positive: "AND (100)" contains "AND (1)"
                bool isCorrect =
                    Regex.IsMatch(currentDefinition, @"\bBETWEEN\s+0\s+AND\s+1(\.0+)?\b", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(currentDefinition, @"<=\s*\(?1(\.0+)?\)?\b", RegexOptions.IgnoreCase);
                if (isCorrect)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ Constraint already uses 0-1 range");
                    Console.ResetColor();
                    return (true, "Constraint already correct (0-1 range)");
                }

                Console.WriteLine("  ○ Constraint uses 0-100 range - needs update to 0-1");
                Console.WriteLine();

                if (!executeMode)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("  [DRY-RUN] Would update constraint to BETWEEN 0 AND 1");
                    Console.ResetColor();
                    return (true, "Would update constraint from 0-100 to 0-1");
                }

                // Drop old constraint and add new one
                using var transaction = connection.BeginTransaction();
                try
                {
                    Console.WriteLine("  Dropping old constraint...");
                    using (var dropCmd = new SqlCommand(
                        "ALTER TABLE [effort].[Percentages] DROP CONSTRAINT [CK_Percentages_Percentage]",
                        connection, transaction))
                    {
                        dropCmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("  Adding new constraint (0-1 range)...");
                    using (var addCmd = new SqlCommand(
                        "ALTER TABLE [effort].[Percentages] ADD CONSTRAINT [CK_Percentages_Percentage] CHECK ([Percentage] >= 0 AND [Percentage] <= 1)",
                        connection, transaction))
                    {
                        addCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ Constraint updated successfully");
                    Console.ResetColor();
                    return (true, "Updated constraint from 0-100 to 0-1");
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Failed to update constraint: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Failed to update constraint: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        private static string? GetPercentageConstraintDefinition(SqlConnection connection)
        {
            using var cmd = new SqlCommand(@"
                SELECT cc.definition
                FROM sys.check_constraints cc
                JOIN sys.tables t ON cc.parent_object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = 'effort'
                  AND t.name = 'Percentages'
                  AND cc.name = 'CK_Percentages_Percentage'",
                connection);
            return cmd.ExecuteScalar() as string;
        }

        #endregion

        #region Task 11: Add ViewDeptAudit Permission

        /// <summary>
        /// Creates SVMSecure.Effort.ViewDeptAudit permission and assigns it to Chairperson and Vice-Chairperson roles.
        /// This permission allows department chairs to view the audit trail for their own department only.
        /// </summary>
        private static (bool Success, string Message) RunAddViewDeptAuditPermissionTask(string connectionString, bool executeMode)
        {
            const string ViewDeptAuditPermission = "SVMSecure.Effort.ViewDeptAudit";
            const string ViewDeptAuditDescription = "View audit trail for own department only (department chairs)";

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                Console.WriteLine($"Target Permission: {ViewDeptAuditPermission}");
                Console.WriteLine();

                // Find Chairpersons and Vice-Chairpersons roles
                var chairRoleId = GetRoleIdByName(connection, "Chairpersons");
                var viceChairRoleId = GetRoleIdByName(connection, "Chairpersons(Vice)");

                if (chairRoleId == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Warning: 'Chairpersons' role not found in RAPS");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  Found 'Chairpersons' role (RoleID = {chairRoleId})");
                }

                if (viceChairRoleId == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Warning: 'Chairpersons(Vice)' role not found in RAPS");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  Found 'Chairpersons(Vice)' role (RoleID = {viceChairRoleId})");
                }

                if (chairRoleId == null && viceChairRoleId == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ERROR: Neither chair role found. Cannot proceed.");
                    Console.ResetColor();
                    return (false, "Required roles not found");
                }

                Console.WriteLine();

                // Check if target permission already exists
                int? existingPermissionId = GetPermissionId(connection, ViewDeptAuditPermission);
                if (existingPermissionId != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Permission already exists (PermissionID = {existingPermissionId}).");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Current role mappings:");
                    ShowCurrentMappings(connection, existingPermissionId.Value);

                    // Check if any roles are missing the permission (before starting transaction)
                    Console.WriteLine();
                    Console.WriteLine("Checking for missing role assignments...");

                    bool chairNeedsPermission = chairRoleId != null && !RoleHasPermission(connection, chairRoleId.Value, existingPermissionId.Value);
                    bool viceChairNeedsPermission = viceChairRoleId != null && !RoleHasPermission(connection, viceChairRoleId.Value, existingPermissionId.Value);

                    if (chairRoleId != null && !chairNeedsPermission)
                    {
                        Console.WriteLine($"  ✓ 'Chairpersons' role already has permission");
                    }
                    if (viceChairRoleId != null && !viceChairNeedsPermission)
                    {
                        Console.WriteLine($"  ✓ 'Chairpersons(Vice)' role already has permission");
                    }

                    if (!chairNeedsPermission && !viceChairNeedsPermission)
                    {
                        Console.WriteLine("  No roles need updating.");
                        return (true, "All roles already have permission");
                    }

                    // Add missing permissions in a transaction
                    using var updateTransaction = connection.BeginTransaction();
                    try
                    {
                        int addedRoles = 0;

                        if (chairNeedsPermission)
                        {
                            if (executeMode)
                            {
                                AssignPermissionToRole(connection, updateTransaction, chairRoleId!.Value, existingPermissionId.Value, "Chairpersons");
                            }
                            Console.WriteLine($"  + Adding permission to 'Chairpersons' role");
                            addedRoles++;
                        }

                        if (viceChairNeedsPermission)
                        {
                            if (executeMode)
                            {
                                AssignPermissionToRole(connection, updateTransaction, viceChairRoleId!.Value, existingPermissionId.Value, "Chairpersons(Vice)");
                            }
                            Console.WriteLine($"  + Adding permission to 'Chairpersons(Vice)' role");
                            addedRoles++;
                        }

                        if (executeMode)
                        {
                            updateTransaction.Commit();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ Added permission to {addedRoles} role(s)");
                            Console.ResetColor();
                            return (true, $"Added permission to {addedRoles} role(s)");
                        }
                        else
                        {
                            updateTransaction.Rollback();
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"  ↶ Would add permission to {addedRoles} role(s) (dry-run)");
                            Console.ResetColor();
                            return (true, $"Would add permission to {addedRoles} role(s) (dry-run)");
                        }
                    }
                    catch (SqlException ex)
                    {
                        updateTransaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                        Console.ResetColor();
                        return (false, $"Database error: {ex.Message}");
                    }
                }

                Console.WriteLine($"  Permission does not exist yet.");

                Console.WriteLine();

                // Create the permission and assign to roles in a transaction
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Create the new permission
                    int newPermissionId = CreatePermission(connection, transaction, ViewDeptAuditPermission, ViewDeptAuditDescription);
                    Console.WriteLine($"Created permission '{ViewDeptAuditPermission}' with PermissionID = {newPermissionId}");

                    int assignedRoles = 0;

                    // Assign to Chairpersons role
                    if (chairRoleId != null)
                    {
                        AssignPermissionToRole(connection, transaction, chairRoleId.Value, newPermissionId, "Chairpersons");
                        assignedRoles++;
                        Console.WriteLine($"Assigned permission to 'Chairpersons' role");
                    }

                    // Assign to Chairpersons(Vice) role
                    if (viceChairRoleId != null)
                    {
                        AssignPermissionToRole(connection, transaction, viceChairRoleId.Value, newPermissionId, "Chairpersons(Vice)");
                        assignedRoles++;
                        Console.WriteLine($"Assigned permission to 'Chairpersons(Vice)' role");
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ Transaction committed - changes are permanent");
                        Console.ResetColor();
                        return (true, $"Created permission (ID={newPermissionId}) and assigned to {assignedRoles} role(s)");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified permission creation with {assignedRoles} role assignment(s) (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a role ID by role name.
        /// </summary>
        private static int? GetRoleIdByName(SqlConnection connection, string roleName)
        {
            using var cmd = new SqlCommand(
                "SELECT RoleID FROM tblRoles WHERE Role = @RoleName",
                connection);
            cmd.Parameters.AddWithValue("@RoleName", roleName);
            var result = cmd.ExecuteScalar();
            return result == null ? null : Convert.ToInt32(result);
        }

        /// <summary>
        /// Check if a role already has a specific permission.
        /// </summary>
        private static bool RoleHasPermission(SqlConnection connection, int roleId, int permissionId)
        {
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM tblRolePermissions WHERE RoleID = @RoleId AND PermissionID = @PermissionId",
                connection);
            cmd.Parameters.AddWithValue("@RoleId", roleId);
            cmd.Parameters.AddWithValue("@PermissionId", permissionId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        /// <summary>
        /// Assign a permission to a role.
        /// </summary>
        private static void AssignPermissionToRole(SqlConnection connection, SqlTransaction transaction, int roleId, int permissionId, string roleName)
        {
            // Insert role permission with Allow access (1)
            using var cmd = new SqlCommand(@"
                INSERT INTO tblRolePermissions (RoleID, PermissionID, Access, ModBy, ModTime)
                VALUES (@RoleID, @PermissionID, 1, @ModBy, GETDATE())",
                connection, transaction);
            cmd.Parameters.AddWithValue("@RoleID", roleId);
            cmd.Parameters.AddWithValue("@PermissionID", permissionId);
            cmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            cmd.ExecuteNonQuery();

            // Create audit entry
            using var auditCmd = new SqlCommand(@"
                INSERT INTO tblLog (RoleID, PermissionID, Audit, Detail, ModTime, ModBy)
                VALUES (@RoleID, @PermissionID, 'UpdateRolePermission', '1', GETDATE(), @ModBy)",
                connection, transaction);
            auditCmd.Parameters.AddWithValue("@RoleID", roleId);
            auditCmd.Parameters.AddWithValue("@PermissionID", permissionId);
            auditCmd.Parameters.AddWithValue("@ModBy", ScriptModBy);
            auditCmd.ExecuteNonQuery();
        }

        #endregion

#region Task 12: Fix EffortType Descriptions

        /// <summary>
        /// Correct descriptions from CREST tbl_sessiontype for the 36 used session types
        /// </summary>
        private static readonly Dictionary<string, string> CorrectEffortTypeDescriptions = new()
        {
            { "ACT", "Activity" },
            { "AUT", "Autotutorial" },
            { "CBL", "Case Based Learning" },
            { "CLI", "Clinical Activity" },
            { "CON", "Conference" },
            { "D/L", "Discussion/Laboratory" },
            { "DIS", "Discussion" },
            { "DSL", "Directed Self Learning" },
            { "EXM", "Examination" },
            { "FAS", "Formative Assessment" },
            { "FWK", "Fieldwork" },
            { "IND", "Independent Study" },
            { "INT", "Internship" },
            { "JLC", "Journal Club" },
            { "L/D", "Laboratory/Discussion" },
            { "LAB", "Laboratory" },
            { "LEC", "Lecture" },
            { "LED", "Lecture/Discussion" },
            { "LIS", "Listening" },
            { "LLA", "Lecture/Laboratory" },
            { "PBL", "Problem Based Learning" },
            { "PER", "Performance Instruction" },
            { "PRA", "Practice" },
            { "PRB", "Extensive Problem Solving" },
            { "PRJ", "Project" },
            { "PRS", "Presentation" },
            { "SEM", "Seminar" },
            { "STD", "Studio" },
            { "T-D", "Term Paper or Discussion" },
            { "TBL", "Team Based Learning" },
            { "TMP", "Term Paper" },
            { "TUT", "Tutorial" },
            { "VAR", "Variable" },
            { "WED", "Web Electronic Discussion" },
            { "WRK", "Workshop" },
            { "WVL", "Web Virtual Lecture" }
        };

        private static (bool Success, string Message) RunFixEffortTypeDescriptionsTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check if EffortTypes table exists (it should after Task 3 rename)
                using var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'EffortTypes'",
                    connection);
                int tableExists = (int)checkCmd.ExecuteScalar();

                if (tableExists == 0)
                {
                    Console.WriteLine("  ⚠ EffortTypes table does not exist - skipping");
                    return (true, "Skipped - EffortTypes table does not exist");
                }

                // Find which descriptions need updating
                var typesToUpdate = new List<(string Id, string CurrentDesc, string CorrectDesc)>();

                using (var selectCmd = new SqlCommand(
                    "SELECT Id, Description FROM [effort].[EffortTypes]", connection))
                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = reader.GetString(0).Trim();
                        string currentDesc = reader.GetString(1);

                        if (CorrectEffortTypeDescriptions.TryGetValue(id, out string? correctDesc)
                            && !string.Equals(currentDesc, correctDesc, StringComparison.Ordinal))
                        {
                            typesToUpdate.Add((id, currentDesc, correctDesc));
                        }
                    }
                }

                if (typesToUpdate.Count == 0)
                {
                    Console.WriteLine("  ✓ All EffortType descriptions are already correct");
                    return (true, "No changes needed - all descriptions correct");
                }

                Console.WriteLine($"  Found {typesToUpdate.Count} EffortType(s) with incorrect descriptions:");
                foreach (var (id, currentDesc, correctDesc) in typesToUpdate)
                {
                    Console.WriteLine($"    {id}: \"{currentDesc}\" → \"{correctDesc}\"");
                }
                Console.WriteLine();

                // Execute the updates within a transaction (rollback in dry-run mode)
                int updatedCount = 0;
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var (id, _, correctDesc) in typesToUpdate)
                    {
                        using var updateCmd = new SqlCommand(
                            "UPDATE [effort].[EffortTypes] SET Description = @Description WHERE Id = @Id",
                            connection, transaction);
                        updateCmd.Parameters.AddWithValue("@Description", correctDesc);
                        updateCmd.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = updateCmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            updatedCount++;
                        }
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Updated {updatedCount} EffortType description(s)");
                        Console.ResetColor();
                        return (true, $"Updated {updatedCount} description(s)");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  ✓ Verified {updatedCount} update(s) would succeed");
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified {updatedCount} update(s) (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Database error: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Task 13: Backfill Harvest Audit Actions

        /// <summary>
        /// Maps old action names to new harvest-specific action names.
        /// These actions are created during the harvest process and should be
        /// hidden from department-level users (chairs) who view the audit trail.
        /// </summary>
        private static readonly Dictionary<string, string> HarvestActionMappings = new()
        {
            { "CreatePerson", "HarvestCreatePerson" },
            { "CreateCourse", "HarvestCreateCourse" },
            { "CreateEffort", "HarvestCreateEffort" }
        };

        /// <summary>
        /// Backfills historical audit entries with harvest-specific action names.
        ///
        /// This task identifies Create* entries that were logged during harvest sessions
        /// and renames them to Harvest* variants so they can be filtered from chairs.
        ///
        /// IMPORTANT: We filter by ChangedBy (the user who ran the harvest) to preserve
        /// legitimate manual data entry. Analysis of early terms (2008) revealed that
        /// some Create* entries were from staff manually entering effort data over extended
        /// periods (e.g., Jan Ilkiw: 10,000+ entries over 2 years). These should remain
        /// visible to chairs as CreatePerson/CreateCourse/CreateEffort.
        ///
        /// The sanity check warnings about "entries from different users" are expected
        /// for these early terms - they indicate legitimate manual work being preserved.
        /// </summary>
        private static (bool Success, string Message) RunBackfillHarvestAuditActionsTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check if Audits table exists
                using var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Audits'",
                    connection);
                int tableExists = (int)checkCmd.ExecuteScalar();

                if (tableExists == 0)
                {
                    Console.WriteLine("  ⚠ Audits table does not exist - skipping");
                    return (true, "Skipped - Audits table does not exist");
                }

                // Check if any entries already use the new action names (migration already done)
                using var alreadyMigratedCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM [effort].[Audits] WHERE Action IN ('HarvestCreatePerson', 'HarvestCreateCourse', 'HarvestCreateEffort')",
                    connection);
                int alreadyMigrated = (int)alreadyMigratedCmd.ExecuteScalar();

                if (alreadyMigrated > 0)
                {
                    Console.WriteLine($"  ✓ Found {alreadyMigrated} entries already using new Harvest* action names");
                    Console.WriteLine("    Migration appears to have been run previously");
                }

                // Find all ImportEffort entries to identify harvest sessions
                // ImportEffort is logged at the END of harvest with a summary, making it the reliable marker
                // Since harvest deletes all records for a term, ALL Create* entries before ImportEffort are from that harvest
                // We also track ChangedBy as a defensive filter - all entries should match the harvest initiator
                var harvestSessions = new List<(int TermCode, DateTime ImportEffortDate, int ChangedBy)>();

                using (var harvestCmd = new SqlCommand(@"
                    SELECT TermCode, ChangedDate, ChangedBy
                    FROM [effort].[Audits]
                    WHERE Action = 'ImportEffort'
                      AND TermCode IS NOT NULL
                    ORDER BY TermCode, ChangedDate",
                    connection))
                using (var reader = harvestCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        harvestSessions.Add((
                            reader.GetInt32(0),
                            reader.GetDateTime(1),
                            reader.GetInt32(2)
                        ));
                    }
                }

                if (harvestSessions.Count == 0)
                {
                    Console.WriteLine("  ⚠ No ImportEffort entries found - nothing to backfill");
                    return (true, "No ImportEffort entries found");
                }

                Console.WriteLine($"  Found {harvestSessions.Count} harvest session(s)");

                // Count entries that would be updated for each action type
                var updateCounts = new Dictionary<string, int>();
                foreach (var (oldAction, _) in HarvestActionMappings)
                {
                    updateCounts[oldAction] = 0;
                }

                // For each harvest session, find and count Create* entries
                // Harvest deletes all previous records for a term before re-importing,
                // so ALL Create* entries before ImportEffort for that term are from the harvest.
                // We filter by ChangedBy as a defensive measure and run sanity checks.
                foreach (var (termCode, importEffortDate, changedBy) in harvestSessions)
                {
                    // Sanity check 1: count entries older than 1 hour before ImportEffort
                    var sanityWindow = importEffortDate.AddHours(-1);
                    using var timeCheckCmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM [effort].[Audits]
                        WHERE TermCode = @TermCode
                          AND ChangedBy = @ChangedBy
                          AND ChangedDate < @SanityWindow
                          AND Action IN ('CreatePerson', 'CreateCourse', 'CreateEffort')",
                        connection);
                    timeCheckCmd.Parameters.AddWithValue("@TermCode", termCode);
                    timeCheckCmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                    timeCheckCmd.Parameters.AddWithValue("@SanityWindow", sanityWindow);
                    int oldEntries = (int)timeCheckCmd.ExecuteScalar();
                    if (oldEntries > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠ Term {termCode}: {oldEntries} entries are >1 hour before ImportEffort");
                        Console.ResetColor();
                    }

                    // Sanity check 2: compare count with and without ChangedBy filter
                    // If different, there are entries from other users - likely manual data entry
                    // that should remain visible to chairs (expected for early 2008 terms)
                    using var totalCountCmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM [effort].[Audits]
                        WHERE TermCode = @TermCode
                          AND ChangedDate <= @ImportEffortDate
                          AND Action IN ('CreatePerson', 'CreateCourse', 'CreateEffort')",
                        connection);
                    totalCountCmd.Parameters.AddWithValue("@TermCode", termCode);
                    totalCountCmd.Parameters.AddWithValue("@ImportEffortDate", importEffortDate);
                    int totalWithoutUserFilter = (int)totalCountCmd.ExecuteScalar();

                    using var userCountCmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM [effort].[Audits]
                        WHERE TermCode = @TermCode
                          AND ChangedBy = @ChangedBy
                          AND ChangedDate <= @ImportEffortDate
                          AND Action IN ('CreatePerson', 'CreateCourse', 'CreateEffort')",
                        connection);
                    userCountCmd.Parameters.AddWithValue("@TermCode", termCode);
                    userCountCmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                    userCountCmd.Parameters.AddWithValue("@ImportEffortDate", importEffortDate);
                    int totalWithUserFilter = (int)userCountCmd.ExecuteScalar();

                    if (totalWithoutUserFilter != totalWithUserFilter)
                    {
                        int diff = totalWithoutUserFilter - totalWithUserFilter;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠ Term {termCode}: {diff} manual entries from other users preserved (not converted to Harvest*)");
                        Console.ResetColor();
                    }

                    // Count Create* entries matching this harvest session (with ChangedBy filter)
                    using var countCmd = new SqlCommand(@"
                        SELECT Action, COUNT(*) as Count
                        FROM [effort].[Audits]
                        WHERE TermCode = @TermCode
                          AND ChangedBy = @ChangedBy
                          AND ChangedDate <= @ImportEffortDate
                          AND Action IN ('CreatePerson', 'CreateCourse', 'CreateEffort')
                        GROUP BY Action",
                        connection);
                    countCmd.Parameters.AddWithValue("@TermCode", termCode);
                    countCmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                    countCmd.Parameters.AddWithValue("@ImportEffortDate", importEffortDate);

                    using var reader = countCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string action = reader.GetString(0);
                        int count = reader.GetInt32(1);
                        updateCounts[action] += count;
                    }
                }

                int totalToUpdate = updateCounts.Values.Sum();
                if (totalToUpdate == 0)
                {
                    Console.WriteLine("  ✓ No entries need to be updated");
                    return (true, "No entries to update");
                }

                Console.WriteLine($"  Found {totalToUpdate} audit entries to update:");
                foreach (var (action, count) in updateCounts.Where(kv => kv.Value > 0))
                {
                    Console.WriteLine($"    {action} → {HarvestActionMappings[action]}: {count} entries");
                }
                Console.WriteLine();

                // Execute the updates within a transaction (rollback in dry-run mode)
                int updatedCount = 0;
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var (termCode, importEffortDate, changedBy) in harvestSessions)
                    {
                        foreach (var (oldAction, newAction) in HarvestActionMappings)
                        {
                            using var updateCmd = new SqlCommand(@"
                                UPDATE [effort].[Audits]
                                SET Action = @NewAction
                                WHERE TermCode = @TermCode
                                  AND ChangedBy = @ChangedBy
                                  AND ChangedDate <= @ImportEffortDate
                                  AND Action = @OldAction",
                                connection, transaction);
                            updateCmd.Parameters.AddWithValue("@NewAction", newAction);
                            updateCmd.Parameters.AddWithValue("@TermCode", termCode);
                            updateCmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                            updateCmd.Parameters.AddWithValue("@ImportEffortDate", importEffortDate);
                            updateCmd.Parameters.AddWithValue("@OldAction", oldAction);

                            updatedCount += updateCmd.ExecuteNonQuery();
                        }
                    }

                    // Commit or rollback based on execute mode
                    if (executeMode)
                    {
                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Updated {updatedCount} audit entries");
                        Console.ResetColor();
                        return (true, $"Updated {updatedCount} entries");
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  ✓ Verified {updatedCount} update(s) would succeed");
                        Console.WriteLine("  ↶ Transaction rolled back - no permanent changes");
                        Console.ResetColor();
                        return (true, $"Verified {updatedCount} update(s) (rolled back)");
                    }
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Database error: {ex.Message}");
                    Console.WriteLine("  ↶ Transaction rolled back");
                    Console.ResetColor();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {ex.Message}");
                Console.ResetColor();
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or ArgumentException
                                       or FormatException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"UNEXPECTED ERROR: {ex}");
                Console.ResetColor();
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Task 14: Add AlertStates Table

        /// <summary>
        /// Creates the AlertStates table for persisting data hygiene alert states.
        /// This table stores the review/ignore status of alerts so they persist across server restarts.
        /// </summary>
        private static (bool Success, string Message) RunAddAlertStatesTableTask(string connectionString, bool executeMode)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                Console.WriteLine("Checking AlertStates table...");
                Console.WriteLine();

                // Check if table already exists
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT COUNT(*) FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = 'effort' AND t.name = 'AlertStates'";
                int exists = (int)checkCmd.ExecuteScalar();

                if (exists > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ AlertStates table already exists");
                    Console.ResetColor();
                    return (true, "Table already exists");
                }

                if (!executeMode)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("  ○ AlertStates table would be created");
                    Console.ResetColor();
                    return (true, "Would create AlertStates table");
                }

                // Create the table
                Console.WriteLine("  Creating AlertStates table...");
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE [effort].[AlertStates] (
                        Id int IDENTITY(1,1) NOT NULL,
                        TermCode int NOT NULL,
                        AlertType varchar(20) NOT NULL,
                        EntityType varchar(20) NOT NULL,
                        EntityId varchar(50) NOT NULL,
                        Status varchar(20) NOT NULL DEFAULT 'Active',
                        IgnoredBy int NULL,
                        IgnoredDate datetime2(7) NULL,
                        ResolvedDate datetime2(7) NULL,
                        ModifiedDate datetime2(7) NOT NULL DEFAULT GETDATE(),
                        ModifiedBy int NULL,
                        CONSTRAINT PK_AlertStates PRIMARY KEY CLUSTERED (Id),
                        CONSTRAINT UQ_AlertStates UNIQUE (TermCode, AlertType, EntityId),
                        CONSTRAINT FK_AlertStates_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
                        CONSTRAINT FK_AlertStates_IgnoredBy FOREIGN KEY (IgnoredBy) REFERENCES [users].[Person](PersonId),
                        CONSTRAINT FK_AlertStates_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
                        CONSTRAINT CK_AlertStates_Status CHECK (Status IN ('Active', 'Resolved', 'Ignored')),
                        CONSTRAINT CK_AlertStates_AlertType CHECK (AlertType IN ('NoRecords', 'NoInstructors', 'NoDepartment', 'ZeroHours', 'NotVerified'))
                    );

                    CREATE NONCLUSTERED INDEX IX_AlertStates_TermCode ON [effort].[AlertStates](TermCode);
                    CREATE NONCLUSTERED INDEX IX_AlertStates_Status ON [effort].[AlertStates](Status) WHERE Status != 'Active';";
                createCmd.ExecuteNonQuery();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ AlertStates table created successfully");
                Console.ResetColor();

                return (true, "Created AlertStates table");
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ SQL Error: {ex.Message}");
                Console.ResetColor();
                return (false, $"SQL Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        private class RolePermissionInfo
        {
            public int RoleId { get; set; }
            public int Access { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }

        private class MemberPermissionInfo
        {
            public string MemberId { get; set; } = string.Empty;
            public int Access { get; set; }
            public DateTime? AddDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string? ModBy { get; set; }
            public DateTime? ModTime { get; set; }
            public string DisplayName { get; set; } = string.Empty;
        }

        #endregion
    }
}
