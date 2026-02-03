using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Creates the modernized Effort schema in VIPER database with all 19 tables
    /// Run this BEFORE MigrateEffortData.cs
    ///
    /// Architecture: Creates [VIPER].[effort] schema (NOT a separate database)
    /// Shadow Schema: [VIPER].[EffortShadow] schema with views pointing to VIPER.effort tables
    ///
    /// Usage:
    ///   dotnet script CreateEffortDatabase.cs              (dry-run mode - tests but doesn't commit)
    ///   dotnet script CreateEffortDatabase.cs --apply      (actually create tables - requires confirmation)
    ///   dotnet script CreateEffortDatabase.cs --force      (drop and recreate with confirmation)
    ///   dotnet script CreateEffortDatabase.cs --recreate   (alias for --force)
    ///   dotnet script CreateEffortDatabase.cs --drop       (drop without recreating)
    ///   dotnet script CreateEffortDatabase.cs --cleanup    (alias for --drop)
    ///
    /// Set environment: $env:ASPNETCORE_ENVIRONMENT="Test" (PowerShell) or set ASPNETCORE_ENVIRONMENT=Test (CMD)
    /// </summary>
    public class CreateEffortDatabase
    {
        public static int Run(string[] args)
        {
            try
            {
                // Parse command-line arguments
                bool executeMode = args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase));
                bool forceRecreate = args.Any(a => a.Equals("--force", StringComparison.OrdinalIgnoreCase) ||
                                                  a.Equals("--recreate", StringComparison.OrdinalIgnoreCase));
                bool dropOnly = args.Any(a => a.Equals("--drop", StringComparison.OrdinalIgnoreCase) ||
                                             a.Equals("--cleanup", StringComparison.OrdinalIgnoreCase));

                Console.WriteLine("============================================");
                if (dropOnly)
                {
                    Console.WriteLine("Dropping Effort Schema from VIPER Database");
                }
                else if (forceRecreate)
                {
                    Console.WriteLine("Recreating Effort Schema in VIPER Database (with confirmation)");
                }
                else if (executeMode)
                {
                    Console.WriteLine("Creating Effort Schema in VIPER Database (EXECUTE MODE)");
                }
                else
                {
                    Console.WriteLine("Creating Effort Schema in VIPER Database (DRY-RUN MODE)");
                }
                Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("============================================");
                Console.WriteLine();

                // Display mode information
                if (!dropOnly && !forceRecreate)
                {
                    if (executeMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠ APPLY MODE: Tables will be created and changes committed.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("ℹ DRY-RUN MODE: Schema will be created, tables tested, then rolled back.");
                        Console.WriteLine("  No permanent changes to tables will be made.");
                        Console.WriteLine("  To actually create tables, use: dotnet script CreateEffortDatabase.cs --apply");
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }

                var configuration = EffortScriptHelper.LoadConfiguration();
                string connectionString = EffortScriptHelper.GetConnectionString(configuration, "VIPER");

                Console.WriteLine($"Target server: {EffortScriptHelper.GetServerAndDatabase(connectionString)}");
                Console.WriteLine();

                // Check if effort schema and tables exist in VIPER
                bool schemaExists = SchemaExists(connectionString);
                bool tablesExist = TablesExist(connectionString);

                // Handle --drop or --force flags
                if ((dropOnly || forceRecreate) && (schemaExists || tablesExist))
                {
                    // Check for data in the schema
                    var dataCounts = CheckSchemaData(connectionString);
                    int totalRecords = dataCounts.Values.Sum();

                    if (totalRecords > 0)
                    {
                        // Data exists - require confirmation
                        if (!ConfirmDeletion(dataCounts, totalRecords))
                        {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Operation cancelled by user.");
                            Console.ResetColor();
                            return 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Schema exists but is empty. Proceeding with drop...");
                        Console.WriteLine();
                    }

                    // Drop schema and EffortShadow schema
                    if (!DropEffortSchema(connectionString))
                    {
                        return 1;
                    }

                    // If --drop only, exit here
                    if (dropOnly)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ Effort schema dropped successfully!");
                        Console.ResetColor();
                        Console.WriteLine("============================================");
                        Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine("============================================");
                        return 0;
                    }
                }
                else if (dropOnly)
                {
                    // --drop requested but nothing exists to drop
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ Effort schema does not exist. Nothing to drop.");
                    Console.ResetColor();
                    return 0;
                }
                else if (tablesExist && !forceRecreate)
                {
                    // Tables exist but user didn't specify --force
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ Effort tables already exist in VIPER database. Skipping creation.");
                    Console.WriteLine();
                    Console.WriteLine("To recreate the schema and tables, use:");
                    Console.WriteLine("  dotnet script CreateEffortDatabase.cs --force");
                    Console.WriteLine();
                    Console.WriteLine("To drop the schema and tables without recreating, use:");
                    Console.WriteLine("  dotnet script CreateEffortDatabase.cs --drop");
                    Console.ResetColor();
                    return 0;
                }

                // Create schema
                if (!CreateSchema(connectionString))
                {
                    return 1;
                }

                // Create all tables (with transaction-based dry-run support)
                bool tablesCreated = CreateTables(connectionString, executeMode);

                if (!tablesCreated && executeMode)
                {
                    // Only fail if in execute mode and tables weren't created
                    return 1;
                }

                // Validate schema creation (only in execute mode, since tables are rolled back in dry-run)
                if (executeMode)
                {
                    Console.WriteLine();
                    if (!ValidateSchemaCreation(connectionString))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("✗ Schema validation failed!");
                        Console.ResetColor();
                        return 1;
                    }
                }

                Console.WriteLine();
                if (executeMode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Effort schema created successfully in VIPER database!");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Next Steps:");
                    Console.WriteLine("  1. Run CreateEffortReportingProcedures.cs to create reporting stored procedures");
                    Console.WriteLine("  2. Run MigrateEffortData.cs to migrate data from legacy database");
                    Console.WriteLine("  3. Run CreateEffortShadow.cs to create shadow schema for ColdFusion");
                    Console.WriteLine("  4. DBA will configure database permissions for applications");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ DRY-RUN SUCCESSFUL: All table creation SQL validated!");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Schema created (permanent), but tables were rolled back (no permanent changes).");
                    Console.WriteLine();
                    Console.WriteLine("To actually create the tables, run:");
                    Console.WriteLine("  dotnet script CreateEffortDatabase.cs --apply");
                    Console.ResetColor();
                }
                Console.WriteLine("============================================");
                Console.WriteLine($"End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine("============================================");

                return 0;
            }
            catch (SqlException sqlEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DATABASE ERROR: {sqlEx.Message}");
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine(sqlEx.StackTrace);
                Console.ResetColor();
                return 1;
            }
            catch (Exception ex)
            {
                // Rethrow critical exceptions that should never be handled
                if (ex is OutOfMemoryException || ex is StackOverflowException || ex is System.Threading.ThreadAbortException)
                    throw;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return 1;
            }
        }

        static bool SchemaExists(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sys.schemas WHERE name = 'effort'";
            int count = (int)cmd.ExecuteScalar();

            return count > 0;
        }

        static bool TablesExist(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            // Check if at least one of the key tables exists in the effort schema
            cmd.CommandText = @"
            SELECT COUNT(*)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'effort' AND t.name IN ('Records', 'Persons', 'Courses')";
            int count = (int)cmd.ExecuteScalar();

            return count > 0;
        }

        static Dictionary<string, int> CheckSchemaData(string connectionString)
        {
            var dataCounts = new Dictionary<string, int>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Check all tables defined in EffortScriptHelper.EffortTables array
                foreach (var table in EffortScriptHelper.EffortTables)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM [effort].[{table}]";
                    try
                    {
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            dataCounts[table] = count;
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Skip missing or inaccessible tables
                        Console.Error.WriteLine($"Warning: Could not query table '{table}': {ex.Message}");
                    }
                }
            }
            catch (SqlException ex)
            {
                // Return empty counts if schema is inaccessible
                Console.Error.WriteLine($"Warning: Could not access schema: {ex.Message}");
            }

            return dataCounts;
        }

        static bool ConfirmDeletion(Dictionary<string, int> dataCounts, int totalRecords)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ⚠ DESTRUCTIVE OPERATION ⚠                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("The Effort schema contains data that will be PERMANENTLY DELETED:");
            Console.WriteLine();

            // Display data counts by table
            foreach (var kvp in dataCounts.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"  • {kvp.Key,-25} {kvp.Value,8:N0} rows");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  TOTAL: {totalRecords:N0} records will be PERMANENTLY DELETED");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("This action cannot be undone.");
            Console.WriteLine();
            Console.Write("Type 'DELETE' (in capital letters) to confirm: ");

            string confirmation = Console.ReadLine()?.Trim() ?? "";

            if (confirmation == "DELETE")
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Confirmation received. Proceeding with deletion...");
                Console.ResetColor();
                return true;
            }

            return false;
        }

        static bool DropEffortSchema(string connectionString)
        {
            Console.WriteLine();
            Console.WriteLine("Dropping Effort schema and related objects...");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Drop EffortShadow schema first (it depends on VIPER.effort schema)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sys.schemas WHERE name = 'EffortShadow'";
                try
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        Console.WriteLine("  Dropping EffortShadow schema (dependency)...");
                        cmd.CommandText = @"
                        DECLARE @sql NVARCHAR(MAX) = '';
                        SELECT @sql += 'DROP VIEW [EffortShadow].[' + name + '];'
                        FROM sys.views WHERE schema_id = SCHEMA_ID('EffortShadow');
                        EXEC sp_executesql @sql;
                        SET @sql = '';
                        SELECT @sql += 'DROP PROCEDURE [EffortShadow].[' + name + '];'
                        FROM sys.procedures WHERE schema_id = SCHEMA_ID('EffortShadow');
                        EXEC sp_executesql @sql;
                        SET @sql = '';
                        SELECT @sql += 'DROP FUNCTION [EffortShadow].[' + name + '];'
                        FROM sys.objects WHERE schema_id = SCHEMA_ID('EffortShadow') AND type IN ('FN','IF','TF');
                        EXEC sp_executesql @sql;
                        DROP SCHEMA [EffortShadow];";
                        cmd.ExecuteNonQuery();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✓ EffortShadow schema dropped");
                        Console.ResetColor();
                    }
                }
                catch (SqlException)
                {
                    // Ignore if database doesn't exist
                }
            }

            // Drop all tables in effort schema
            using (var cmd = connection.CreateCommand())
            {
                Console.WriteLine("  Dropping effort schema tables...");
                cmd.CommandText = @"
                DECLARE @sql NVARCHAR(MAX) = '';

                -- Drop all foreign keys first
                SELECT @sql += 'ALTER TABLE [effort].[' + t.name + '] DROP CONSTRAINT [' + fk.name + '];'
                FROM sys.foreign_keys fk
                INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
                WHERE t.schema_id = SCHEMA_ID('effort');

                EXEC sp_executesql @sql;

                SET @sql = '';

                -- Drop all tables
                SELECT @sql += 'DROP TABLE [effort].[' + name + '];'
                FROM sys.tables
                WHERE schema_id = SCHEMA_ID('effort');

                EXEC sp_executesql @sql;
            ";
                cmd.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ All tables dropped");
                Console.ResetColor();
            }

            // Drop programmable objects (stored procedures, functions) in effort schema
            using (var cmd = connection.CreateCommand())
            {
                Console.WriteLine("  Dropping programmable objects in effort schema...");
                cmd.CommandText = @"
                DECLARE @sql NVARCHAR(MAX) = '';

                -- Drop all stored procedures
                SELECT @sql += 'DROP PROCEDURE [effort].[' + name + '];'
                FROM sys.procedures
                WHERE schema_id = SCHEMA_ID('effort');

                EXEC sp_executesql @sql;

                SET @sql = '';

                -- Drop all functions
                SELECT @sql += 'DROP FUNCTION [effort].[' + name + '];'
                FROM sys.objects
                WHERE schema_id = SCHEMA_ID('effort') AND type IN ('FN', 'IF', 'TF');

                EXEC sp_executesql @sql;
            ";
                cmd.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ All programmable objects dropped");
                Console.ResetColor();
            }

            // Drop effort schema
            using (var cmd = connection.CreateCommand())
            {
                Console.WriteLine("  Dropping effort schema...");
                cmd.CommandText = "DROP SCHEMA IF EXISTS [effort]";
                cmd.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Effort schema dropped");
                Console.ResetColor();
            }

            return true;
        }

        static bool CreateSchema(string connectionString)
        {
            Console.WriteLine("Step 1: Creating effort schema in VIPER database...");

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Check if schema already exists
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM sys.schemas WHERE name = 'effort'";
                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("  ⚠ Effort schema already exists, skipping schema creation");
                    Console.ResetColor();
                    return true;
                }
            }

            // Create schema if it doesn't exist
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE SCHEMA [effort]";
                cmd.ExecuteNonQuery();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ Effort schema created");
            Console.ResetColor();
            return true;
        }

        static bool CreateTables(string connectionString, bool executeMode)
        {
            Console.WriteLine();
            Console.WriteLine("Step 2: Creating tables in effort schema...");

            if (!executeMode)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  (Transaction will be rolled back after validation)");
                Console.ResetColor();
            }
            Console.WriteLine();

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Begin transaction for dry-run support
            // Note: Transaction will be committed only in execute mode
            using var transaction = connection.BeginTransaction();

            try
            {
                // Execute each table creation in dependency order
                // 1. Lookup tables (no dependencies)
                CreateRolesTable(connection, transaction);
                CreateEffortTypesTable(connection, transaction);
                CreateSessionTypesTable(connection, transaction);
                CreateUnitsTable(connection, transaction);
                CreateJobCodesTable(connection, transaction);
                CreateReportUnitsTable(connection, transaction);

                // 2. Term and course tables
                CreateTermStatusTable(connection, transaction);
                CreateCoursesTable(connection, transaction);

                // 3. Person tables
                CreatePersonsTable(connection, transaction);
                CreateAlternateTitlesTable(connection, transaction);
                CreateUserAccessTable(connection, transaction);

                // 4. Main data tables (depend on above)
                CreateRecordsTable(connection, transaction);
                CreatePercentagesTable(connection, transaction);
                CreateSabbatic​alsTable(connection, transaction);

                // 5. Relationship and audit tables
                CreateCourseRelationshipsTable(connection, transaction);
                CreateAuditsTable(connection, transaction);

                // 6. Alert states table (for data hygiene dashboard)
                CreateAlertStatesTable(connection, transaction);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ All {EffortScriptHelper.ExpectedTableCount} tables created successfully in transaction");
                Console.ResetColor();

                // Commit or rollback based on mode
                if (executeMode)
                {
                    transaction.Commit();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ Transaction committed - tables are permanent");
                    Console.ResetColor();
                }
                else
                {
                    transaction.Rollback();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("  ↶ Transaction rolled back - no permanent changes to tables");
                    Console.ResetColor();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Error creating tables: {ex.Message}");
                Console.ResetColor();

                transaction.Rollback();
                Console.WriteLine("  ↶ Transaction rolled back");

                throw; // Re-throw to be caught by main error handler
            }
        }

        static void CreateRolesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Roles')
BEGIN
    CREATE TABLE [effort].[Roles] (
        Id int NOT NULL,  -- Matches legacy tblRoles.Role_ID
        Description varchar(50) NOT NULL,  -- Matches legacy tblRoles.Role_Desc
        IsActive bit NOT NULL DEFAULT 1,
        SortOrder int NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
    );
    -- Data migrated from legacy tblRoles by MigrateEffortData.cs
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Roles table created");
        }

        static void CreateEffortTypesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'EffortTypes')
BEGIN
    CREATE TABLE [effort].[EffortTypes] (
        Id int IDENTITY(1,1) NOT NULL,
        Class varchar(20) NOT NULL,
        Name varchar(50) NOT NULL,
        ShowOnTemplate bit NOT NULL DEFAULT 1,
        IsActive bit NOT NULL DEFAULT 1,
        CONSTRAINT PK_EffortTypes PRIMARY KEY CLUSTERED (Id)
    );
    -- Data migrated from legacy tblEffortType_LU by MigrateEffortData.cs
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ EffortTypes table created");
        }

        static void CreateSessionTypesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'SessionTypes')
BEGIN
    CREATE TABLE [effort].[SessionTypes] (
        Id varchar(3) NOT NULL,
        Description varchar(50) NOT NULL,
        UsesWeeks bit NOT NULL DEFAULT 0,
        IsActive bit NOT NULL DEFAULT 1,
        FacultyCanEnter bit NOT NULL DEFAULT 1,   -- Faculty/instructors can add this type themselves
        AllowedOnDvm bit NOT NULL DEFAULT 1,      -- Allowed on DVM courses
        AllowedOn199299 bit NOT NULL DEFAULT 1,   -- Allowed on 199/299 courses
        AllowedOnRCourses bit NOT NULL DEFAULT 1, -- Allowed on R courses
        CONSTRAINT PK_SessionTypes PRIMARY KEY CLUSTERED (Id)
    );

    -- Seed with 36 distinct session types from legacy database analysis
    -- IMPORTANT: Keep in sync with EffortScriptHelper.ValidSessionTypes
    -- CLI is the ONLY session type that uses weeks instead of hours
    INSERT INTO [effort].[SessionTypes] (Id, Description, UsesWeeks) VALUES
    ('ACT', 'Activity', 0),
    ('AUT', 'Autopsy', 0),
    ('CBL', 'Case-Based Learning', 0),
    ('CLI', 'Clinical', 1),   -- ONLY CLI uses weeks
    ('CON', 'Conference', 0),
    ('D/L', 'Distance Learning', 0),
    ('DIS', 'Discussion', 0),
    ('DSL', 'Distance Learning', 0),
    ('EXM', 'Examination', 0),
    ('FAS', 'Faculty Assessment', 0),
    ('FWK', 'Fieldwork', 0),
    ('IND', 'Independent Study', 0),
    ('INT', 'Internship', 0),
    ('JLC', 'Journal Club', 0),
    ('L/D', 'Lab/Discussion', 0),
    ('LAB', 'Laboratory', 0),
    ('LEC', 'Lecture', 0),
    ('LED', 'Lab/Discussion', 0),
    ('LIS', 'Listening', 0),
    ('LLA', 'Lab/Lecture', 0),
    ('PBL', 'Problem-Based Learning', 0),
    ('PER', 'Performance', 0),
    ('PRA', 'Practicum', 0),
    ('PRB', 'Problem', 0),
    ('PRJ', 'Project', 0),
    ('PRS', 'Presentation', 0),
    ('SEM', 'Seminar', 0),
    ('STD', 'Studio', 0),
    ('T-D', 'Team-Discussion', 0),
    ('TBL', 'Team-Based Learning', 0),
    ('TMP', 'Temporary', 0),
    ('TUT', 'Tutorial', 0),
    ('VAR', 'Variable', 0),
    ('WED', 'Wednesday', 0),
    ('WRK', 'Workshop', 0),
    ('WVL', 'Work-Variable Learning', 0);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine($"  ✓ SessionTypes table created and seeded ({EffortScriptHelper.ValidSessionTypes.Length} rows)");
        }

        static void CreateTermStatusTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'TermStatus')
BEGIN
    -- Term data comes from VIPER.dbo.vwTerms
    -- This table tracks Effort-specific workflow status only
    -- Status is computed from dates (not stored) to match legacy ColdFusion logic
    CREATE TABLE [effort].[TermStatus] (
        TermCode int NOT NULL,
        HarvestedDate datetime2(7) NULL,
        OpenedDate datetime2(7) NULL,
        ClosedDate datetime2(7) NULL,
        CONSTRAINT PK_TermStatus PRIMARY KEY CLUSTERED (TermCode)
    );
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ TermStatus table created (workflow tracking)");
        }

        static void CreateCoursesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Courses')
BEGIN
    -- NOTE: Title field not included - course titles should be fetched from VIPER course catalog
    -- To get course title: JOIN [courses].[Catalog] ON SubjCode + CrseNumb
    CREATE TABLE [effort].[Courses] (
        Id int IDENTITY(1,1) NOT NULL,
        Crn char(5) NOT NULL,
        TermCode int NOT NULL,
        SubjCode char(3) NOT NULL,
        CrseNumb char(5) NOT NULL,
        SeqNumb char(3) NOT NULL,
        Enrollment int NOT NULL DEFAULT 0,
        Units decimal(4,2) NOT NULL,
        CustDept varchar(6) NOT NULL,  -- Must be varchar to match legacy schema (char pads with spaces)
        CONSTRAINT PK_Courses PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Courses_CRN_Term_Units UNIQUE (Crn, TermCode, Units),
        CONSTRAINT FK_Courses_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
        CONSTRAINT CK_Courses_Enrollment CHECK (Enrollment >= 0),
        CONSTRAINT CK_Courses_Units CHECK (Units >= 0)
    );

    CREATE NONCLUSTERED INDEX IX_Courses_TermCode ON [effort].[Courses](TermCode);
    CREATE NONCLUSTERED INDEX IX_Courses_CRN ON [effort].[Courses](Crn);
    CREATE NONCLUSTERED INDEX IX_Courses_CustDept ON [effort].[Courses](CustDept);
    CREATE NONCLUSTERED INDEX IX_Courses_SubjCode_CrseNumb ON [effort].[Courses](SubjCode, CrseNumb);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Courses table created");
        }

        static void CreatePersonsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Persons')
BEGIN
    CREATE TABLE [effort].[Persons] (
        PersonId int NOT NULL,
        TermCode int NOT NULL,
        FirstName varchar(50) NOT NULL,
        LastName varchar(50) NOT NULL,
        MiddleInitial varchar(1) NULL,
        EffortTitleCode varchar(6) NOT NULL,  -- Must be varchar to match legacy schema (char pads with spaces)
        EffortDept varchar(6) NOT NULL,
        PercentAdmin float NOT NULL DEFAULT 0,  -- Match legacy float(53)
        JobGroupId char(3) NULL,
        Title varchar(50) NULL,
        AdminUnit varchar(25) NULL,
        EffortVerified datetime2(7) NULL,
        ReportUnit varchar(50) NULL,
        VolunteerWos tinyint NULL,
        PercentClinical float NULL,  -- Match legacy float(53)
        LastEmailed datetime2(7) NULL,
        LastEmailedBy int NULL,
        CONSTRAINT PK_Persons PRIMARY KEY CLUSTERED (PersonId, TermCode),
        CONSTRAINT FK_Persons_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_Persons_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
        CONSTRAINT FK_Persons_LastEmailedBy FOREIGN KEY (LastEmailedBy) REFERENCES [users].[Person](PersonId),
        CONSTRAINT CK_Persons_PercentAdmin CHECK (PercentAdmin BETWEEN 0 AND 100),
        CONSTRAINT CK_Persons_PercentClinical CHECK (PercentClinical IS NULL OR PercentClinical BETWEEN 0 AND 100)
    );

    CREATE NONCLUSTERED INDEX IX_Persons_LastName_FirstName ON [effort].[Persons](LastName, FirstName);
    CREATE NONCLUSTERED INDEX IX_Persons_EffortDept ON [effort].[Persons](EffortDept);
    CREATE NONCLUSTERED INDEX IX_Persons_TermCode ON [effort].[Persons](TermCode);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Persons table created");
        }

        static void CreateRecordsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Records')
BEGIN
    CREATE TABLE [effort].[Records] (
        Id int IDENTITY(1,1) NOT NULL,
        CourseId int NOT NULL,
        PersonId int NOT NULL,
        TermCode int NOT NULL,
        EffortTypeId varchar(3) NOT NULL,
        RoleId int NOT NULL,  -- Matches legacy tblRoles.Role_ID (int)
        Hours int NULL,
        Weeks int NULL,
        Crn varchar(5) NOT NULL,
        ModifiedDate datetime2(7) NULL,
        ModifiedBy int NULL,
        CONSTRAINT PK_Records PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Records_Courses FOREIGN KEY (CourseId) REFERENCES [effort].[Courses](Id),
        CONSTRAINT FK_Records_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_Records_Persons FOREIGN KEY (PersonId, TermCode) REFERENCES [effort].[Persons](PersonId, TermCode),
        CONSTRAINT FK_Records_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
        CONSTRAINT FK_Records_Roles FOREIGN KEY (RoleId) REFERENCES [effort].[Roles](Id),
        CONSTRAINT FK_Records_EffortTypes FOREIGN KEY (EffortTypeId) REFERENCES [effort].[EffortTypes](Id),
        CONSTRAINT FK_Records_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
        CONSTRAINT CK_Records_HoursOrWeeks CHECK ((Hours IS NOT NULL AND Weeks IS NULL) OR (Hours IS NULL AND Weeks IS NOT NULL)),
        CONSTRAINT CK_Records_Hours CHECK (Hours IS NULL OR (Hours >= 0 AND Hours <= 2500)),
        CONSTRAINT CK_Records_Weeks CHECK (Weeks IS NULL OR (Weeks > 0 AND Weeks <= 52))
    );

    CREATE NONCLUSTERED INDEX IX_Records_PersonId_TermCode ON [effort].[Records](PersonId, TermCode);
    CREATE NONCLUSTERED INDEX IX_Records_CourseId ON [effort].[Records](CourseId);
    CREATE NONCLUSTERED INDEX IX_Records_TermCode ON [effort].[Records](TermCode);
    CREATE NONCLUSTERED INDEX IX_Records_ModifiedDate ON [effort].[Records](ModifiedDate);

    -- Prevent duplicate effort records per course/person/effort type
    CREATE UNIQUE INDEX [UQ_Records_Course_Person_EffortType]
    ON [effort].[Records] ([CourseId], [PersonId], [EffortTypeId]);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Records table created");
        }

        static void CreatePercentagesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Percentages')
BEGIN
    CREATE TABLE [effort].[Percentages] (
        Id int IDENTITY(1,1) NOT NULL,
        PersonId int NOT NULL,
        AcademicYear char(9) NOT NULL,  -- Format: 'YYYY-YYYY' (e.g., '2019-2020'), derived from StartDate if missing
        Percentage float NOT NULL,  -- Match legacy float(53)
        PercentAssignTypeId int NOT NULL,
        UnitId int NULL,  -- FK to Units table
        Modifier varchar(50) NULL,
        Comment varchar(100) NULL,
        StartDate datetime2(7) NOT NULL,
        EndDate datetime2(7) NULL,
        ModifiedDate datetime2(7) NULL,
        ModifiedBy int NULL,
        Compensated bit NOT NULL DEFAULT 0,
        CONSTRAINT PK_Percentages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Percentages_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_Percentages_PercentAssignTypes FOREIGN KEY (PercentAssignTypeId) REFERENCES [effort].[PercentAssignTypes](Id),
        CONSTRAINT FK_Percentages_Units FOREIGN KEY (UnitId) REFERENCES [effort].[Units](Id),
        CONSTRAINT FK_Percentages_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
        CONSTRAINT CK_Percentages_Percentage CHECK (Percentage BETWEEN 0 AND 1),
        CONSTRAINT CK_Percentages_DateRange CHECK (EndDate IS NULL OR EndDate >= StartDate)
    );

    CREATE NONCLUSTERED INDEX IX_Percentages_PersonId ON [effort].[Percentages](PersonId);
    CREATE NONCLUSTERED INDEX IX_Percentages_AcademicYear ON [effort].[Percentages](AcademicYear);
    CREATE NONCLUSTERED INDEX IX_Percentages_StartDate ON [effort].[Percentages](StartDate);
    CREATE NONCLUSTERED INDEX IX_Percentages_EndDate ON [effort].[Percentages](EndDate) WHERE EndDate IS NOT NULL;
    CREATE NONCLUSTERED INDEX IX_Percentages_UnitId ON [effort].[Percentages](UnitId) WHERE UnitId IS NOT NULL;
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Percentages table created");
        }

        static void CreateSabbatic​alsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Sabbaticals')
BEGIN
    CREATE TABLE [effort].[Sabbaticals] (
        Id int IDENTITY(1,1) NOT NULL,
        PersonId int NOT NULL,
        ExcludeClinicalTerms varchar(2000) NULL,
        ExcludeDidacticTerms varchar(2000) NULL,
        ModifiedDate datetime2(7) NULL,
        ModifiedBy int NULL,
        CONSTRAINT PK_Sabbaticals PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Sabbaticals_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_Sabbaticals_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId)
    );

    CREATE NONCLUSTERED INDEX IX_Sabbaticals_PersonId ON [effort].[Sabbaticals](PersonId);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Sabbaticals table created");
        }

        static void CreateUserAccessTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'UserAccess')
BEGIN
    CREATE TABLE [effort].[UserAccess] (
        Id int IDENTITY(1,1) NOT NULL,
        PersonId int NOT NULL,
        DepartmentCode varchar(6) NOT NULL,  -- Must be varchar to match legacy schema (char pads with spaces)
        ModifiedDate datetime2(7) NULL,
        ModifiedBy int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CONSTRAINT PK_UserAccess PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_UserAccess_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_UserAccess_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
        CONSTRAINT UQ_UserAccess_Person_Dept UNIQUE (PersonId, DepartmentCode)
    );

    CREATE NONCLUSTERED INDEX IX_UserAccess_PersonId ON [effort].[UserAccess](PersonId) WHERE IsActive = 1;
    CREATE NONCLUSTERED INDEX IX_UserAccess_DeptCode ON [effort].[UserAccess](DepartmentCode) WHERE IsActive = 1;
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ UserAccess table created (CRITICAL)");
        }

        static void CreateUnitsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Units')
BEGIN
    CREATE TABLE [effort].[Units] (
        Id int IDENTITY(1,1) NOT NULL,
        Name varchar(20) NOT NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CONSTRAINT PK_Units PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Units_Name UNIQUE (Name)
    );
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Units table created (CRITICAL)");
        }

        static void CreateJobCodesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'JobCodes')
BEGIN
    -- Legacy tblJobCode has: jobcode, faculty (unused), include_clinschedule
    -- No description/title exists in legacy - it's just a code-to-flags mapping
    CREATE TABLE [effort].[JobCodes] (
        Id int IDENTITY(1,1) NOT NULL,
        Code varchar(6) NOT NULL,                     -- Legacy: jobcode varchar(6)
        IncludeClinSchedule bit NOT NULL DEFAULT 1,   -- Legacy: include_clinschedule
        IsActive bit NOT NULL DEFAULT 1,              -- Soft delete (new)
        CONSTRAINT PK_JobCodes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_JobCodes_Code UNIQUE (Code)
    );
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ JobCodes table created");
        }

        static void CreateReportUnitsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'ReportUnits')
BEGIN
    CREATE TABLE [effort].[ReportUnits] (
        Id int IDENTITY(1,1) NOT NULL,
        UnitCode varchar(10) NOT NULL,
        UnitName varchar(100) NOT NULL,
        ParentUnitId int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        SortOrder int NULL,
        CONSTRAINT PK_ReportUnits PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ReportUnits_Parent FOREIGN KEY (ParentUnitId) REFERENCES [effort].[ReportUnits](Id),
        CONSTRAINT UQ_ReportUnits_Code UNIQUE (UnitCode)
    );
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ ReportUnits table created");
        }

        static void CreateAlternateTitlesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'AlternateTitles')
BEGIN
    CREATE TABLE [effort].[AlternateTitles] (
        Id int IDENTITY(1,1) NOT NULL,
        PersonId int NOT NULL,
        AlternateTitle varchar(100) NOT NULL,
        EffectiveDate date NOT NULL,
        ExpirationDate date NULL,
        ModifiedDate datetime2(7) NULL,
        ModifiedBy int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CONSTRAINT PK_AlternateTitles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AlternateTitles_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
        CONSTRAINT FK_AlternateTitles_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
        CONSTRAINT CK_AlternateTitles_DateRange CHECK (ExpirationDate IS NULL OR ExpirationDate >= EffectiveDate)
    );

    CREATE NONCLUSTERED INDEX IX_AlternateTitles_PersonId ON [effort].[AlternateTitles](PersonId) WHERE IsActive = 1;
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ AlternateTitles table created");
        }


        static void CreateCourseRelationshipsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'CourseRelationships')
BEGIN
    CREATE TABLE [effort].[CourseRelationships] (
        Id int IDENTITY(1,1) NOT NULL,
        ParentCourseId int NOT NULL,
        ChildCourseId int NOT NULL,
        RelationshipType varchar(20) NOT NULL,
        CONSTRAINT PK_CourseRelationships PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CourseRelationships_Parent FOREIGN KEY (ParentCourseId) REFERENCES [effort].[Courses](Id),
        CONSTRAINT FK_CourseRelationships_Child FOREIGN KEY (ChildCourseId) REFERENCES [effort].[Courses](Id),
        CONSTRAINT UQ_CourseRelationships UNIQUE (ParentCourseId, ChildCourseId),
        CONSTRAINT CK_CourseRelationships_Type CHECK (RelationshipType IN ('CrossList', 'Section'))
    );

    -- Unique index to ensure each child course can only have one parent
    CREATE UNIQUE NONCLUSTERED INDEX IX_CourseRelationships_ChildCourseId
    ON [effort].[CourseRelationships](ChildCourseId);
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ CourseRelationships table created");
        }

        static void CreateAuditsTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Audits')
BEGIN
    CREATE TABLE [effort].[Audits] (
        Id int IDENTITY(1,1) NOT NULL,
        TableName varchar(50) NOT NULL,
        RecordId int NOT NULL,
        Action varchar(50) NOT NULL,           -- Granular action names (e.g., CreateEffort, UpdateCourse)
        ChangedBy int NOT NULL,
        ChangedDate datetime2(7) NOT NULL DEFAULT GETDATE(),
        Changes nvarchar(MAX) NULL,            -- Audit text (legacy plain-text or JSON)
        MigratedDate datetime2(7) NULL,
        UserAgent varchar(500) NULL,
        IpAddress varchar(50) NULL,

        -- Legacy preservation columns (for 1:1 verification against legacy tblAudit)
        -- Can be dropped after ColdFusion is decommissioned
        LegacyAction varchar(100) NULL,        -- Original action text (e.g., 'CreateCourse')
        LegacyCRN varchar(20) NULL,            -- Original audit_CRN
        LegacyMothraID varchar(20) NULL,       -- Original audit_MothraID

        -- Term context for audit record
        TermCode int NULL,

        CONSTRAINT PK_Audits PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Audits_ChangedBy FOREIGN KEY (ChangedBy) REFERENCES [users].[Person](PersonId)
    );

    CREATE NONCLUSTERED INDEX IX_Audits_TableName_RecordId ON [effort].[Audits](TableName, RecordId);
    CREATE NONCLUSTERED INDEX IX_Audits_ChangedDate ON [effort].[Audits](ChangedDate DESC);
    CREATE NONCLUSTERED INDEX IX_Audits_ChangedBy ON [effort].[Audits](ChangedBy);
    CREATE NONCLUSTERED INDEX IX_Audits_TermCode ON [effort].[Audits](TermCode) WHERE TermCode IS NOT NULL;
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ Audits table created");
        }

        static void CreateAlertStatesTable(SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'AlertStates')
BEGIN
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
    CREATE NONCLUSTERED INDEX IX_AlertStates_Status ON [effort].[AlertStates](Status) WHERE Status != 'Active';
END";
            cmd.ExecuteNonQuery();
            Console.WriteLine("  ✓ AlertStates table created");
        }

        // Helper method to validate and report results (DRY principle)
        static bool ValidateCount(SqlConnection connection, string sql, int expected, string successMessage, string errorMessage)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            int actual = (int)cmd.ExecuteScalar();

            if (actual == expected)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ {successMessage}");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ {errorMessage.Replace("{expected}", expected.ToString()).Replace("{actual}", actual.ToString())}");
                Console.ResetColor();
                return false;
            }
        }

        static bool ValidateSchemaCreation(string connectionString)
        {
            Console.WriteLine("Validating schema creation...");
            Console.WriteLine();

            bool allValidationsPassed = true;

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // 1. Validate all tables exist
            allValidationsPassed &= ValidateCount(
                connection,
                "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('effort')",
                EffortScriptHelper.ExpectedTableCount,
                $"All {EffortScriptHelper.ExpectedTableCount} tables created",
                "Expected {expected} tables, found {actual}"
            );

            // 2. Validate Roles table exists (data migrated by MigrateEffortData.cs)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'Roles'";
                int tableExists = (int)cmd.ExecuteScalar();
                if (tableExists == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ Roles table created (data populated by MigrateEffortData.cs)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ✗ Roles table not found");
                    Console.ResetColor();
                    allValidationsPassed = false;
                }
            }

            // 3. Validate SessionTypes table seeded data
            // Use count from EffortScriptHelper.ValidSessionTypes to ensure consistency
            allValidationsPassed &= ValidateCount(
                connection,
                "SELECT COUNT(*) FROM [effort].[SessionTypes]",
                EffortScriptHelper.ValidSessionTypes.Length,
                $"SessionTypes table seeded ({EffortScriptHelper.ValidSessionTypes.Length} rows)",
                "SessionTypes table: expected {expected} rows, found {actual}"
            );

            // 4. Validate EffortTypes table exists (data migrated by MigrateEffortData.cs)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('effort') AND name = 'EffortTypes'";
                int tableExists = (int)cmd.ExecuteScalar();
                if (tableExists == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ EffortTypes table created (data populated by MigrateEffortData.cs)");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ✗ EffortTypes table not found");
                    Console.ResetColor();
                    allValidationsPassed = false;
                }
            }

            // 5. Validate Role column type in Records table
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'effort'
                  AND TABLE_NAME = 'Records'
                  AND COLUMN_NAME = 'Role'";

                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    string dataType = (string)result;
                    if (dataType == "int")
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ Records.Role column type correct (int)");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ Records.Role column: expected int, found {dataType}");
                        Console.ResetColor();
                        allValidationsPassed = false;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ Records.Role column not found");
                    Console.ResetColor();
                    allValidationsPassed = false;
                }
            }

            Console.WriteLine();
            if (allValidationsPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All validations passed!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Some validations failed!");
                Console.ResetColor();
            }

            return allValidationsPassed;
        }
    }
}
