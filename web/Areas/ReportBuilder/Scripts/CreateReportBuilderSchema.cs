// Description: Create the Report Builder's [reportbuilder] tables and the empty [reports] schema
// Usage: dotnet run -- create-schema [--apply]
// Without --apply the changes run in a transaction that is rolled back, so the script can be
// checked against an environment first. Every step is skipped if it already exists, so running
// it again is safe.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.ReportBuilder.Scripts
{
    public static class CreateReportBuilderSchema
    {
        /// <summary>The builder's own tables. Kept out of [reports] so they are never reportable.</summary>
        private const string BuilderSchema = "reportbuilder";

        /// <summary>The allow-list: report views and procedures go here, written as reviewed scripts.</summary>
        private const string SourceSchema = "reports";

        private static readonly (string Name, string Sql)[] Tables =
        [
            ("ReportDefinition", """
                CREATE TABLE [reportbuilder].[ReportDefinition] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReportDefinition] PRIMARY KEY,
                    [Slug] nvarchar(100) NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(2000) NULL,
                    [Area] nvarchar(50) NULL,
                    [SourceName] nvarchar(128) NOT NULL,
                    [SourceKind] nvarchar(30) NOT NULL,
                    [Paginate] bit NOT NULL CONSTRAINT [DF_ReportDefinition_Paginate] DEFAULT (1),
                    [PageSize] int NOT NULL CONSTRAINT [DF_ReportDefinition_PageSize] DEFAULT (25),
                    [AllowSearch] bit NOT NULL CONSTRAINT [DF_ReportDefinition_AllowSearch] DEFAULT (0),
                    [AllowExcel] bit NOT NULL CONSTRAINT [DF_ReportDefinition_AllowExcel] DEFAULT (0),
                    [AllowPdf] bit NOT NULL CONSTRAINT [DF_ReportDefinition_AllowPdf] DEFAULT (0),
                    [PdfLandscape] bit NOT NULL CONSTRAINT [DF_ReportDefinition_PdfLandscape] DEFAULT (0),
                    [DefaultSortColumn] nvarchar(128) NULL,
                    [DefaultSortDescending] bit NOT NULL CONSTRAINT [DF_ReportDefinition_DefaultSortDescending] DEFAULT (0),
                    [SensitiveNotice] nvarchar(1000) NULL,
                    [IsActive] bit NOT NULL CONSTRAINT [DF_ReportDefinition_IsActive] DEFAULT (1),
                    [DeletedOn] datetime2 NULL,
                    [CreatedBy] nvarchar(50) NOT NULL,
                    [CreatedOn] datetime2 NOT NULL,
                    [ModifiedBy] nvarchar(50) NOT NULL,
                    [ModifiedOn] datetime2 NOT NULL,
                    CONSTRAINT [UQ_ReportDefinition_Slug] UNIQUE ([Slug])
                )
                """),
            ("ReportPermission", """
                CREATE TABLE [reportbuilder].[ReportPermission] (
                    [ReportDefinitionId] int NOT NULL,
                    [Permission] nvarchar(100) NOT NULL,
                    CONSTRAINT [PK_ReportPermission] PRIMARY KEY ([ReportDefinitionId], [Permission]),
                    CONSTRAINT [FK_ReportPermission_ReportDefinition] FOREIGN KEY ([ReportDefinitionId])
                        REFERENCES [reportbuilder].[ReportDefinition] ([Id]) ON DELETE CASCADE
                )
                """),
            ("ReportColumn", """
                CREATE TABLE [reportbuilder].[ReportColumn] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReportColumn] PRIMARY KEY,
                    [ReportDefinitionId] int NOT NULL,
                    [SourceColumn] nvarchar(128) NOT NULL,
                    [Label] nvarchar(200) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [Visible] bit NOT NULL CONSTRAINT [DF_ReportColumn_Visible] DEFAULT (1),
                    [Format] nvarchar(30) NOT NULL,
                    [Sortable] bit NOT NULL CONSTRAINT [DF_ReportColumn_Sortable] DEFAULT (1),
                    CONSTRAINT [FK_ReportColumn_ReportDefinition] FOREIGN KEY ([ReportDefinitionId])
                        REFERENCES [reportbuilder].[ReportDefinition] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [UQ_ReportColumn_Source] UNIQUE ([ReportDefinitionId], [SourceColumn])
                )
                """),
            ("ReportParameter", """
                CREATE TABLE [reportbuilder].[ReportParameter] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReportParameter] PRIMARY KEY,
                    [ReportDefinitionId] int NOT NULL,
                    [Name] nvarchar(128) NOT NULL,
                    [Label] nvarchar(200) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [Type] nvarchar(30) NOT NULL,
                    [Operator] nvarchar(30) NULL,
                    [Required] bit NOT NULL CONSTRAINT [DF_ReportParameter_Required] DEFAULT (0),
                    [DefaultValue] nvarchar(500) NULL,
                    [ChoicesSource] nvarchar(128) NULL,
                    [ChoicesList] nvarchar(max) NULL,
                    [Binding] nvarchar(30) NOT NULL,
                    CONSTRAINT [FK_ReportParameter_ReportDefinition] FOREIGN KEY ([ReportDefinitionId])
                        REFERENCES [reportbuilder].[ReportDefinition] ([Id]) ON DELETE CASCADE
                )
                """),
            ("ReportDefinitionHistory", """
                CREATE TABLE [reportbuilder].[ReportDefinitionHistory] (
                    [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReportDefinitionHistory] PRIMARY KEY,
                    [ReportDefinitionId] int NOT NULL,
                    [Action] nvarchar(30) NOT NULL,
                    [Snapshot] nvarchar(max) NOT NULL,
                    [ChangedBy] nvarchar(50) NOT NULL,
                    [ChangedOn] datetime2 NOT NULL,
                    CONSTRAINT [FK_ReportDefinitionHistory_ReportDefinition] FOREIGN KEY ([ReportDefinitionId])
                        REFERENCES [reportbuilder].[ReportDefinition] ([Id]) ON DELETE CASCADE
                )
                """),
            ("ReportRunLog", """
                CREATE TABLE [reportbuilder].[ReportRunLog] (
                    [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ReportRunLog] PRIMARY KEY,
                    [ReportDefinitionId] int NOT NULL,
                    [Action] nvarchar(30) NOT NULL,
                    [LoginId] nvarchar(50) NOT NULL,
                    [RanOn] datetime2 NOT NULL,
                    [Parameters] nvarchar(max) NULL,
                    [RowsReturned] int NOT NULL,
                    [DurationMs] int NOT NULL,
                    CONSTRAINT [FK_ReportRunLog_ReportDefinition] FOREIGN KEY ([ReportDefinitionId])
                        REFERENCES [reportbuilder].[ReportDefinition] ([Id]) ON DELETE CASCADE
                )
                """),
        ];

        private static readonly (string Table, string Name, string Sql)[] Indexes =
        [
            ("ReportRunLog", "IX_ReportRunLog_Report_RanOn",
                "CREATE INDEX [IX_ReportRunLog_Report_RanOn] ON [reportbuilder].[ReportRunLog] ([ReportDefinitionId], [RanOn])"),
            ("ReportDefinitionHistory", "IX_ReportDefinitionHistory_Report",
                "CREATE INDEX [IX_ReportDefinitionHistory_Report] ON [reportbuilder].[ReportDefinitionHistory] ([ReportDefinitionId], [ChangedOn])"),
        ];

        public static int Run(string[] args)
        {
            bool apply = args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase));
            IConfiguration configuration = LoadConfiguration();
            string? connectionString = configuration.GetConnectionString("VIPER");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("ERROR: the VIPER connection string is not configured.");
                return 1;
            }

            var target = new SqlConnectionStringBuilder(connectionString);
            Console.WriteLine($"Target: {target.DataSource}/{target.InitialCatalog}");
            Console.WriteLine(apply ? "Mode: APPLY" : "Mode: dry run (rolled back)");
            Console.WriteLine();

            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using SqlTransaction transaction = connection.BeginTransaction();
            try
            {
                EnsureSchema(connection, transaction, BuilderSchema);
                EnsureSchema(connection, transaction, SourceSchema);
                foreach ((string name, string sql) in Tables)
                {
                    EnsureObject(connection, transaction, $"table [{BuilderSchema}].[{name}]",
                        $"SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('{BuilderSchema}') AND name = '{name}'", sql);
                }

                foreach ((string table, string name, string sql) in Indexes)
                {
                    EnsureObject(connection, transaction, $"index {name}",
                        $"SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('[{BuilderSchema}].[{table}]') AND name = '{name}'", sql);
                }

                if (apply)
                {
                    transaction.Commit();
                    Console.WriteLine();
                    Console.WriteLine("Committed.");
                }
                else
                {
                    transaction.Rollback();
                    Console.WriteLine();
                    Console.WriteLine("Dry run complete; rolled back. Run with --apply to keep the changes.");
                }

                return 0;
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine("Rolled back; nothing was changed.");
                return 1;
            }
        }

        private static void EnsureSchema(SqlConnection connection, SqlTransaction transaction, string schema)
        {
            // CREATE SCHEMA must be alone in its batch, so it runs through EXEC.
            EnsureObject(connection, transaction, $"schema [{schema}]",
                $"SELECT COUNT(*) FROM sys.schemas WHERE name = '{schema}'",
                $"EXEC('CREATE SCHEMA [{schema}]')");
        }

        private static void EnsureObject(SqlConnection connection, SqlTransaction transaction, string label, string existsSql, string createSql)
        {
            using var check = new SqlCommand(existsSql, connection, transaction);
            if (Convert.ToInt32(check.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
            {
                Console.WriteLine($"  exists   {label}");
                return;
            }

            using var create = new SqlCommand(createSql, connection, transaction);
            create.ExecuteNonQuery();
            Console.WriteLine($"  created  {label}");
        }

        /// <summary>appsettings files plus AWS Parameter Store, as the other area scripts load them.</summary>
        private static IConfiguration LoadConfiguration()
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            string appRoot = GetApplicationRoot();
            Console.WriteLine($"Environment: {environment}");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(appRoot)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            try
            {
                AWSOptions awsOptions = new() { Region = RegionEndpoint.USWest1 };
                builder.AddSystemsManager("/" + environment, awsOptions)
                       .AddSystemsManager("/Shared", awsOptions);
            }
            catch (Amazon.Runtime.AmazonServiceException ex)
            {
                Console.WriteLine($"Warning: could not reach AWS Parameter Store: {ex.Message}");
            }
            catch (Amazon.Runtime.AmazonClientException ex)
            {
                Console.WriteLine($"Warning: could not reach AWS Parameter Store: {ex.Message}");
            }

            return builder.Build();
        }

        private static string GetApplicationRoot()
        {
            string currentDir = Directory.GetCurrentDirectory();
            if (currentDir.Contains("Scripts"))
            {
                currentDir = Path.GetFullPath(Path.Join(currentDir, "..", "..", ".."));
            }

            return currentDir;
        }
    }
}
