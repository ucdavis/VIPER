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
// ============================================
// USAGE:
// dotnet run -- post-deployment              (dry-run mode - shows what would be changed)
// dotnet run -- post-deployment --apply      (actually apply changes)
// dotnet run -- post-deployment --force      (force recreate permission even if it exists)
// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
// ============================================

using System;
using System.Collections.Generic;
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
