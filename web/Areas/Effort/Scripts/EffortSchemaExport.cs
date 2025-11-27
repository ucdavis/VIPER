using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Schema Export Script for Effort Database
    /// Purpose: Export complete database schema and sample data for documentation and migration planning
    /// </summary>
    public class EffortSchemaExport
    {
        private readonly string _connectionString;
        private readonly string _outputPath;
        private readonly IConfiguration _configuration;
        private readonly StringBuilder _output;

        public EffortSchemaExport(IConfiguration? configuration = null, string? outputPath = null)
        {
            _configuration = configuration ?? EffortScriptHelper.LoadConfiguration();
            _connectionString = EffortScriptHelper.GetConnectionString(_configuration, "Effort");
            _outputPath = outputPath ?? Path.Join(Directory.GetCurrentDirectory(), "Effort_Database_Schema_And_Data_LEGACY.txt");
            _output = new StringBuilder();
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("EFFORT DATABASE SCHEMA AND SAMPLE DATA EXPORT");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Export Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                IConfiguration? configuration = null;
                string? outputPath = null;

                // Check if custom config file was provided
                if (args.Length > 0 && args[0].EndsWith(".json"))
                {
                    Console.WriteLine($"Loading configuration from: {args[0]}");
                    var builder = new ConfigurationBuilder()
                        .AddJsonFile(args[0], optional: false)
                        .AddEnvironmentVariables();
                    configuration = builder.Build();
                }
                else
                {
                    Console.WriteLine($"Using application configuration from: {EffortScriptHelper.GetApplicationRoot()}");
                    Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
                }

                // Check if custom output path was provided via switch or positional arg
                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg.StartsWith("--output", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = arg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                        outputPath = parts.Length == 2 ? parts[1] : i + 1 < args.Length ? args[i + 1] : outputPath;
                    }
                    else if (!arg.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && outputPath is null)
                    {
                        outputPath = arg;
                    }
                }

                // Validate output path to prevent path traversal attacks
                // Only validate if user provided a path; otherwise let constructor use its default
                var sanitizedOutputPath = !string.IsNullOrWhiteSpace(outputPath)
                    ? EffortScriptHelper.ValidateOutputPath(outputPath, "Exports")
                    : null;

                var exporter = new EffortSchemaExport(configuration, sanitizedOutputPath);

                // Display connection info (without passwords)
                Console.WriteLine("\nConnection Configuration:");
                Console.WriteLine($"  Effort Database: {EffortScriptHelper.GetServerAndDatabase(exporter._connectionString)}");
                Console.WriteLine($"  Output File: {exporter._outputPath}");
                Console.WriteLine();

                exporter.ExportSchema();

                Console.WriteLine();
                Console.WriteLine("================================================================================");
                Console.WriteLine("Export completed successfully!");
                Console.WriteLine($"Output file: {exporter._outputPath}");
                Console.WriteLine("================================================================================");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("connection string not found"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine("\nTo fix this, add the Effort connection string to appsettings.json:");
                Console.WriteLine("  \"ConnectionStrings\": {");
                Console.WriteLine("    \"Effort\": \"Server=YOUR_SERVER;Database=Effort;Trusted_Connection=true;\"");
                Console.WriteLine("  }");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (SqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nDATABASE ERROR: {ex.Message}");
                Console.WriteLine($"\nError Number: {ex.Number}");
                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFILE I/O ERROR: {ex.Message}");
                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nACCESS DENIED: {ex.Message}");
                Console.WriteLine("\nEnsure you have permission to write to the output directory.");
                Console.ResetColor();
                Environment.Exit(1);
            }
            catch (InvalidOperationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nCONFIGURATION ERROR: {ex.Message}");
                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        public void ExportSchema()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            WriteHeader(conn);
            ExportTables(conn);
            ExportViews(conn);
            ExportFunctions(conn);
            ExportStoredProcedures(conn);

            // Write to file
            File.WriteAllText(_outputPath, _output.ToString());
        }

        private void WriteHeader(SqlConnection conn)
        {
            var serverName = conn.DataSource;
            var databaseName = conn.Database;

            _output.AppendLine("================================================================================");
            _output.AppendLine("EFFORT DATABASE SCHEMA AND SAMPLE DATA");
            _output.AppendLine("================================================================================");
            _output.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _output.AppendLine($"Server: {serverName}");
            _output.AppendLine($"Database: {databaseName}");
            _output.AppendLine("================================================================================");
            _output.AppendLine();
        }

        private void ExportTables(SqlConnection conn)
        {
            Console.WriteLine("Exporting tables...");

            var tables = GetTables(conn);

            foreach (var (schemaName, tableName) in tables)
            {
                Console.WriteLine($"  - [{schemaName}].[{tableName}]");

                _output.AppendLine();
                _output.AppendLine("################################################################################");
                _output.AppendLine($"TABLE: [{schemaName}].[{tableName}]");
                _output.AppendLine("################################################################################");
                _output.AppendLine();

                ExportTableColumns(conn, schemaName, tableName);
                ExportPrimaryKeys(conn, schemaName, tableName);
                ExportForeignKeys(conn, schemaName, tableName);
                ExportIndexes(conn, schemaName, tableName);
                ExportRowCount(conn, schemaName, tableName);
                ExportSampleData(conn, schemaName, tableName);
            }
        }

        private List<(string Schema, string Table)> GetTables(SqlConnection conn)
        {
            var tables = new List<(string, string)>();

            var sql = @"
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }

            return tables;
        }

        private void ExportTableColumns(SqlConnection conn, string schemaName, string tableName)
        {
            _output.AppendLine("SCHEMA:");
            _output.AppendLine("-------");

            var sql = @"
                SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    NUMERIC_PRECISION,
                    NUMERIC_SCALE,
                    IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table
                ORDER BY ORDINAL_POSITION";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Schema", schemaName);
            cmd.Parameters.AddWithValue("@Table", tableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var columnName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                var precision = reader.IsDBNull(3) ? (byte?)null : reader.GetByte(3);
                var scale = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                var isNullable = reader.GetString(5);

                var typeSpec = dataType;
                if (maxLength.HasValue)
                {
                    typeSpec += maxLength.Value == -1 ? "(MAX)" : $"({maxLength.Value})";
                }
                else if (precision.HasValue && scale.HasValue)
                {
                    typeSpec += $"({precision.Value},{scale.Value})";
                }
                else if (precision.HasValue)
                {
                    typeSpec += $"({precision.Value})";
                }

                var nullable = isNullable == "NO" ? " NOT NULL" : " NULL";

                _output.AppendLine($"  {columnName} : {typeSpec}{nullable}");
            }

            _output.AppendLine();
        }

        private void ExportPrimaryKeys(SqlConnection conn, string schemaName, string tableName)
        {
            _output.AppendLine("PRIMARY KEYS:");
            _output.AppendLine("-------------");

            var sql = @"
                SELECT
                    TC.CONSTRAINT_NAME,
                    STUFF((
                        SELECT ', ' + KCU.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU
                        WHERE KCU.CONSTRAINT_SCHEMA = TC.CONSTRAINT_SCHEMA
                            AND KCU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
                        ORDER BY KCU.ORDINAL_POSITION
                        FOR XML PATH('')
                    ), 1, 2, '') AS PKColumns
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
                WHERE TC.TABLE_SCHEMA = @Schema
                    AND TC.TABLE_NAME = @Table
                    AND TC.CONSTRAINT_TYPE = 'PRIMARY KEY'";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Schema", schemaName);
            cmd.Parameters.AddWithValue("@Table", tableName);

            using var reader = cmd.ExecuteReader();
            var hasPrimaryKey = false;

            while (reader.Read())
            {
                hasPrimaryKey = true;
                var constraintName = reader.GetString(0);
                var columns = reader.GetString(1);
                _output.AppendLine($"  {constraintName}: {columns}");
            }

            if (!hasPrimaryKey)
            {
                _output.AppendLine("  (No primary key defined)");
            }

            _output.AppendLine();
        }

        private void ExportForeignKeys(SqlConnection conn, string schemaName, string tableName)
        {
            _output.AppendLine("FOREIGN KEYS:");
            _output.AppendLine("-------------");

            var sql = @"
                SELECT
                    FK.name AS ConstraintName,
                    STUFF((
                        SELECT ', ' + COL_NAME(FKC.parent_object_id, FKC.parent_column_id)
                        FROM sys.foreign_key_columns FKC
                        WHERE FKC.constraint_object_id = FK.object_id
                        ORDER BY FKC.constraint_column_id
                        FOR XML PATH('')
                    ), 1, 2, '') AS SourceColumns,
                    SCHEMA_NAME(RefT.schema_id) AS RefSchema,
                    RefT.name AS RefTable,
                    STUFF((
                        SELECT ', ' + COL_NAME(FKC.referenced_object_id, FKC.referenced_column_id)
                        FROM sys.foreign_key_columns FKC
                        WHERE FKC.constraint_object_id = FK.object_id
                        ORDER BY FKC.constraint_column_id
                        FOR XML PATH('')
                    ), 1, 2, '') AS RefColumns
                FROM sys.foreign_keys FK
                INNER JOIN sys.tables T ON FK.parent_object_id = T.object_id
                INNER JOIN sys.tables RefT ON FK.referenced_object_id = RefT.object_id
                WHERE SCHEMA_NAME(T.schema_id) = @Schema AND T.name = @Table";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Schema", schemaName);
            cmd.Parameters.AddWithValue("@Table", tableName);

            using var reader = cmd.ExecuteReader();
            var hasForeignKey = false;

            while (reader.Read())
            {
                hasForeignKey = true;
                var constraintName = reader.GetString(0);
                var sourceColumns = reader.GetString(1);
                var refSchema = reader.GetString(2);
                var refTable = reader.GetString(3);
                var refColumns = reader.GetString(4);

                _output.AppendLine($"  {constraintName}: {sourceColumns} REFERENCES [{refSchema}].[{refTable}]({refColumns})");
            }

            if (!hasForeignKey)
            {
                _output.AppendLine("  (No foreign keys defined)");
            }

            _output.AppendLine();
        }

        private void ExportIndexes(SqlConnection conn, string schemaName, string tableName)
        {
            _output.AppendLine("INDEXES:");
            _output.AppendLine("--------");

            var sql = @"
                SELECT
                    I.name AS IndexName,
                    I.type_desc AS IndexType,
                    I.is_unique AS IsUnique,
                    I.is_primary_key AS IsPrimaryKey,
                    I.is_unique_constraint AS IsUniqueConstraint,
                    STUFF((
                        SELECT ', ' + COL_NAME(IC.object_id, IC.column_id) +
                            CASE WHEN IC.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
                        FROM sys.index_columns IC
                        WHERE IC.object_id = I.object_id
                            AND IC.index_id = I.index_id
                            AND IC.is_included_column = 0
                        ORDER BY IC.key_ordinal
                        FOR XML PATH('')
                    ), 1, 2, '') AS KeyColumns,
                    STUFF((
                        SELECT ', ' + COL_NAME(IC.object_id, IC.column_id)
                        FROM sys.index_columns IC
                        WHERE IC.object_id = I.object_id
                            AND IC.index_id = I.index_id
                            AND IC.is_included_column = 1
                        ORDER BY IC.index_column_id
                        FOR XML PATH('')
                    ), 1, 2, '') AS IncludedColumns
                FROM sys.indexes I
                INNER JOIN sys.tables T ON I.object_id = T.object_id
                WHERE SCHEMA_NAME(T.schema_id) = @Schema
                    AND T.name = @Table
                    AND I.name IS NOT NULL
                ORDER BY I.name";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Schema", schemaName);
            cmd.Parameters.AddWithValue("@Table", tableName);

            using var reader = cmd.ExecuteReader();
            var hasIndex = false;
            var description = new StringBuilder();

            while (reader.Read())
            {
                hasIndex = true;
                var indexName = reader.GetString(0);
                var indexType = reader.GetString(1);
                var isUnique = reader.GetBoolean(2);
                var isPrimaryKey = reader.GetBoolean(3);
                var isUniqueConstraint = reader.GetBoolean(4);
                var keyColumns = reader.GetString(5);
                var includedColumns = reader.IsDBNull(6) ? null : reader.GetString(6);

                description.Clear();
                description.Append($"  {indexName}: {indexType}");

                if (isUnique)
                {
                    description.Append(" UNIQUE");
                }

                if (isPrimaryKey)
                {
                    description.Append(" (PRIMARY KEY)");
                }
                else if (isUniqueConstraint)
                {
                    description.Append(" (UNIQUE CONSTRAINT)");
                }

                description.Append($" ON ({keyColumns})");

                if (!string.IsNullOrEmpty(includedColumns))
                {
                    description.Append($" INCLUDE ({includedColumns})");
                }

                _output.AppendLine(description.ToString());
            }

            if (!hasIndex)
            {
                _output.AppendLine("  (No indexes defined)");
            }

            _output.AppendLine();
        }

        private void ExportRowCount(SqlConnection conn, string schemaName, string tableName)
        {
            var sql = $"SELECT COUNT(*) FROM [{schemaName}].[{tableName}]";

            using var cmd = new SqlCommand(sql, conn);
            var count = (int)cmd.ExecuteScalar();

            _output.AppendLine($"ROW COUNT: {count:N0}");
            _output.AppendLine();
        }

        private void ExportSampleData(SqlConnection conn, string schemaName, string tableName)
        {
            var countSql = $"SELECT COUNT(*) FROM [{schemaName}].[{tableName}]";
            using var countCmd = new SqlCommand(countSql, conn);
            var count = (int)countCmd.ExecuteScalar();

            if (count == 0)
            {
                _output.AppendLine("SAMPLE DATA:");
                _output.AppendLine("(No data in table)");
                _output.AppendLine();
                return;
            }

            _output.AppendLine("SAMPLE DATA (10 random rows):");
            _output.AppendLine("----------------------------------------");

            var sql = $"SELECT TOP 10 * FROM [{schemaName}].[{tableName}] ORDER BY NEWID()";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            // Write column headers
            var columnNames = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                columnNames.Add(columnName);
            }

            _output.AppendLine(string.Join(" | ", columnNames));
            _output.AppendLine(new string('-', columnNames.Sum(n => n.Length) + (columnNames.Count - 1) * 3));

            // Write data rows
            while (reader.Read())
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "NULL";
                    if (value.Length > 50)
                    {
                        value = value.Substring(0, 47) + "...";
                    }
                    values.Add(value);
                }
                _output.AppendLine(string.Join(" | ", values));
            }

            _output.AppendLine();
        }

        private void ExportViews(SqlConnection conn)
        {
            Console.WriteLine("Exporting views...");

            _output.AppendLine();
            _output.AppendLine("================================================================================");
            _output.AppendLine("VIEWS");
            _output.AppendLine("================================================================================");
            _output.AppendLine();

            var sql = @"
                SELECT
                    s.name AS VIEW_SCHEMA,
                    v.name AS VIEW_NAME,
                    m.definition AS VIEW_DEFINITION
                FROM sys.views AS v
                INNER JOIN sys.schemas AS s ON v.schema_id = s.schema_id
                INNER JOIN sys.sql_modules AS m ON v.object_id = m.object_id
                ORDER BY s.name, v.name";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var schema = reader.GetString(0);
                var name = reader.GetString(1);
                var definition = reader.IsDBNull(2) ? "(Definition not available)" : reader.GetString(2);

                Console.WriteLine($"  - [{schema}].[{name}]");

                _output.AppendLine("################################################################################");
                _output.AppendLine($"VIEW: [{schema}].[{name}]");
                _output.AppendLine("################################################################################");
                _output.AppendLine(definition);
                _output.AppendLine();
            }
        }

        private void ExportFunctions(SqlConnection conn)
        {
            Console.WriteLine("Exporting functions...");

            _output.AppendLine();
            _output.AppendLine("================================================================================");
            _output.AppendLine("FUNCTIONS");
            _output.AppendLine("================================================================================");
            _output.AppendLine();

            var sql = @"
                SELECT
                    s.name AS ROUTINE_SCHEMA,
                    o.name AS ROUTINE_NAME,
                    o.type_desc AS FUNCTION_TYPE,
                    m.definition AS ROUTINE_DEFINITION
                FROM sys.objects AS o
                INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id
                INNER JOIN sys.sql_modules AS m ON o.object_id = m.object_id
                WHERE o.type IN ('FN', 'IF', 'TF')  -- FN=Scalar, IF=Inline Table, TF=Table-Valued
                ORDER BY s.name, o.name";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var schema = reader.GetString(0);
                var name = reader.GetString(1);
                var functionType = reader.GetString(2);
                var definition = reader.IsDBNull(3) ? "(Definition not available)" : reader.GetString(3);

                Console.WriteLine($"  - [{schema}].[{name}] ({functionType})");

                _output.AppendLine("################################################################################");
                _output.AppendLine($"FUNCTION: [{schema}].[{name}] ({functionType})");
                _output.AppendLine("################################################################################");
                _output.AppendLine(definition);
                _output.AppendLine();
            }
        }

        private void ExportStoredProcedures(SqlConnection conn)
        {
            Console.WriteLine("Exporting stored procedures...");

            _output.AppendLine();
            _output.AppendLine("================================================================================");
            _output.AppendLine("STORED PROCEDURES");
            _output.AppendLine("================================================================================");
            _output.AppendLine();

            var sql = @"
                SELECT
                    s.name AS ROUTINE_SCHEMA,
                    p.name AS ROUTINE_NAME,
                    m.definition AS ROUTINE_DEFINITION
                FROM sys.procedures AS p
                INNER JOIN sys.schemas AS s ON p.schema_id = s.schema_id
                INNER JOIN sys.sql_modules AS m ON p.object_id = m.object_id
                ORDER BY s.name, p.name";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var schema = reader.GetString(0);
                var name = reader.GetString(1);
                var definition = reader.IsDBNull(2) ? "(Definition not available)" : reader.GetString(2);

                Console.WriteLine($"  - [{schema}].[{name}]");

                _output.AppendLine("################################################################################");
                _output.AppendLine($"STORED PROCEDURE: [{schema}].[{name}]");
                _output.AppendLine("################################################################################");
                _output.AppendLine(definition);
                _output.AppendLine();
            }
        }
    }
}
