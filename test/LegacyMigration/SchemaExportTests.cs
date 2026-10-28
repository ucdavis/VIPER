using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The export's scripts and summary.json, written from a catalog built in memory as Catalog.Load would
/// read it. Two environments' exports must match wherever their schemas do, including constraints whose
/// system-generated names differ, so the comparison reports only real differences.
/// </summary>
[InvariantCulture]
public sealed class SchemaExportTests : IDisposable
{
    private const int Table = 1;
    private const int Other = 2;

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("schema-export-tests-");

    public void Dispose() => _root.Delete(recursive: true);

    private static DbObject Object(int id, string name, string type, string typeDesc, int parent = 0) =>
        new(id, "dbo", name, type, typeDesc, parent, false);

    private static ColumnInfo Column(int objectId, string name, string type = "int") =>
        new(objectId, name, type, "sys", false, type == "int" ? (short)4 : (short)20, 10, 0, false, false, null, null, false,
            null, false, type == "int" ? null : "SQL_Latin1_General_CP1_CI_AS", false, false, null, null, false, type, null);

    private static IndexInfo Index(int id, string name, bool primaryKey = false, bool uniqueConstraint = false,
        bool unique = false, bool systemNamed = false, string? filter = null, bool disabled = false) =>
        new(Table, id, name, (byte)(primaryKey ? 1 : 2), primaryKey ? "CLUSTERED" : "NONCLUSTERED",
            primaryKey || uniqueConstraint || unique, primaryKey, uniqueConstraint, systemNamed, filter, disabled, false);

    private static CheckInfo Check(string name, string definition, bool systemNamed = true, bool untrusted = false, bool disabled = false) =>
        new(Table, name, definition, untrusted, disabled, false, systemNamed);

    private static ForeignKeyInfo ForeignKey(int id, string name, bool systemNamed = true, bool untrusted = false) =>
        new(id, Table, name, "dbo", "tblOther", untrusted, false, false, systemNamed, "NO_ACTION", "NO_ACTION");

    /// <summary>
    /// A catalog of dbo.tblAudit (id, code, otherId) and dbo.tblOther (id), plus the given parts.
    /// </summary>
    private static Catalog NewCatalog(List<IndexInfo>? indexes = null, Dictionary<(int, int), List<IndexColumn>>? indexColumns = null,
        List<CheckInfo>? checks = null, List<ForeignKeyInfo>? foreignKeys = null,
        Dictionary<int, List<ForeignKeyColumn>>? foreignKeyColumns = null, List<DbObject>? modules = null,
        Dictionary<int, ModuleInfo>? definitions = null, HashSet<int>? disabledTriggers = null,
        List<DatabaseTrigger>? databaseTriggers = null, string? referencesError = null, string otherName = "tblOther") => new()
        {
            Info = new DatabaseInfo("test-sql", "Legacy", "13.0", 130, "SQL_Latin1_General_CP1_CI_AS", true),
            Objects = [Object(Table, "tblAudit", "U", "USER_TABLE"), Object(Other, otherName, "U", "USER_TABLE"), .. modules ?? []],
            ColumnsByObject = new()
            {
                [Table] = [Column(Table, "id"), Column(Table, "code", "varchar"), Column(Table, "otherId")],
                [Other] = [Column(Other, "id")]
            },
            IndexesByObject = (indexes ?? []).GroupBy(i => i.ObjectId).ToDictionary(g => g.Key, g => g.ToList()),
            IndexColumns = indexColumns ?? [],
            ChecksByParent = (checks ?? []).GroupBy(c => c.ParentObjectId).ToDictionary(g => g.Key, g => g.ToList()),
            ForeignKeysByParent = (foreignKeys ?? []).GroupBy(f => f.ParentObjectId).ToDictionary(g => g.Key, g => g.ToList()),
            ForeignKeyColumns = foreignKeyColumns ?? [],
            RowCounts = [],
            Modules = definitions ?? [],
            DisabledTriggers = disabledTriggers ?? [],
            TriggerCountByParent = [],
            DatabaseTriggers = databaseTriggers ?? [],
            Types = [],
            Synonyms = [],
            ExternalReferences = [],
            UnresolvedReferences = [],
            ReferencesError = referencesError,
            ModuleReferences = []
        };

    private SchemaSummary Write(Catalog catalog, string environment = "Test") =>
        SchemaExport.Write(catalog, "Legacy", environment, Path.Join(_root.FullName, environment));

    private string Read(string environment, string file) =>
        File.ReadAllText(Path.Join(_root.FullName, environment, file)).ReplaceLineEndings("\n");

    [Theory]
    [InlineData(null, true)]
    [InlineData("([code] IS NOT NULL)", false)]
    public void UniqueIndex_CountsAsAKeyOnlyWhenUnfiltered(string? filter, bool isKey)
    {
        var summary = Write(NewCatalog(
            indexes: [Index(2, "UX_code", unique: true, filter: filter)],
            indexColumns: new() { [(Table, 2)] = [new IndexColumn("code", false, false)] }));

        var table = summary.Tables.Single(t => t.Name == "[dbo].[tblAudit]");
        List<List<string>> keys = isKey ? [["code"]] : [];
        Assert.Equal(isKey, table.HasUniqueKey);
        Assert.Equal(keys, table.UniqueKeys);
        Assert.Equal(!isKey, summary.TablesWithoutUniqueKey.Contains("[dbo].[tblAudit]"));
        Assert.Contains("[dbo].[tblAudit]", summary.TablesWithoutPrimaryKey);
    }

    /// <summary>
    /// The same constraints as two environments list them. The catalog lists constraints by name, and the
    /// system-generated suffixes (1111, 9999) land on different constraints in each, so the order differs.
    /// </summary>
    private static Catalog SystemNamedConstraints(bool swapped)
    {
        string[] keyColumns = swapped ? ["otherId", "code"] : ["code", "otherId"];
        string[] definitions = swapped ? ["([otherId]>(0))", "([code]<>'')"] : ["([code]<>'')", "([otherId]>(0))"];
        string[] referencing = swapped ? ["otherId", "id"] : ["id", "otherId"];
        return NewCatalog(
            indexes:
            [
                Index(1, "PK_tblAudit", primaryKey: true),
                Index(2, "UQ__tblAudit__1111", uniqueConstraint: true, systemNamed: true),
                Index(3, "UQ__tblAudit__9999", uniqueConstraint: true, systemNamed: true)
            ],
            indexColumns: new()
            {
                [(Table, 1)] = [new IndexColumn("id", false, false)],
                [(Table, 2)] = [new IndexColumn(keyColumns[0], false, false)],
                [(Table, 3)] = [new IndexColumn(keyColumns[1], false, false)]
            },
            checks:
            [
                Check("CK_named", "([id]>(0))", systemNamed: false),
                Check("CK__tblAudit__1111", definitions[0]),
                Check("CK__tblAudit__9999", definitions[1])
            ],
            foreignKeys: [ForeignKey(10, "FK__tblAudit__1111"), ForeignKey(11, "FK__tblAudit__9999")],
            foreignKeyColumns: new()
            {
                [10] = [new ForeignKeyColumn(referencing[0], "id")],
                [11] = [new ForeignKeyColumn(referencing[1], "id")]
            });
    }

    [Fact]
    public void SystemNamedConstraints_ComeOutInTheSameOrderWhateverTheirNames()
    {
        var test = Write(SystemNamedConstraints(swapped: false));
        var production = Write(SystemNamedConstraints(swapped: true), "Production");

        Assert.Equal(Read("Test", "Tables/dbo.tblAudit.sql"), Read("Production", "Tables/dbo.tblAudit.sql"));
        Assert.Equal(test.CheckConstraints.Select(c => c.Definition), production.CheckConstraints.Select(c => c.Definition));
        Assert.Equal(test.ForeignKeys.Select(f => f.Columns[0]), production.ForeignKeys.Select(f => f.Columns[0]));
        Assert.Equal(test.Tables[0].UniqueKeys, production.Tables[0].UniqueKeys);
    }

    [Fact]
    public void SystemNamedConstraints_FollowNamedOnes_OrderedByWhatTheyEnforce()
    {
        Write(SystemNamedConstraints(swapped: true));

        var script = Read("Test", "Tables/dbo.tblAudit.sql");
        string[] inOrder =
        [
            "CONSTRAINT [PK_tblAudit] PRIMARY KEY CLUSTERED ([id] ASC)",
            "UNIQUE NONCLUSTERED ([code] ASC)",
            "UNIQUE NONCLUSTERED ([otherId] ASC)",
            "ADD CONSTRAINT [CK_named] CHECK ([id]>(0));",
            "ADD CHECK ([code]<>'');",
            "ADD CHECK ([otherId]>(0));",
            "FOREIGN KEY ([id]) REFERENCES",
            "FOREIGN KEY ([otherId]) REFERENCES"
        ];
        var positions = inOrder.Select(s => script.IndexOf(s, StringComparison.Ordinal)).ToList();
        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.Order(), positions);
    }

    [Fact]
    public void TableScript_MarksUntrustedAndDisabledConstraints()
    {
        var summary = Write(NewCatalog(
            checks: [Check("CK__tblAudit__1111", "([id]>(0))", disabled: true, untrusted: true)],
            foreignKeys: [ForeignKey(10, "FK_tblAudit_tblOther", systemNamed: false, untrusted: true)],
            foreignKeyColumns: new() { [10] = [new ForeignKeyColumn("otherId", "id")] }));

        var script = Read("Test", "Tables/dbo.tblAudit.sql");
        Assert.Contains("-- No primary key\nCREATE TABLE [dbo].[tblAudit] (\n    [id] int NOT NULL,\n    [code] varchar(20) NOT NULL,\n", script, StringComparison.Ordinal);
        Assert.Contains("-- DISABLED: not enforced for new rows, and existing rows were never validated\n" +
                        "ALTER TABLE [dbo].[tblAudit] WITH NOCHECK ADD CHECK ([id]>(0));\n" +
                        "-- (system-named constraint; NOCHECK CONSTRAINT omitted)\n", script, StringComparison.Ordinal);
        Assert.Contains("-- UNTRUSTED: enforced for new rows, but existing rows were never validated\n" +
                        "ALTER TABLE [dbo].[tblAudit] WITH NOCHECK ADD CONSTRAINT [FK_tblAudit_tblOther] FOREIGN KEY ([otherId]) REFERENCES [dbo].[tblOther] ([id]);\n",
            script, StringComparison.Ordinal);

        var fk = Assert.Single(summary.ForeignKeys);
        Assert.Equal(("[dbo].[tblAudit]", "[dbo].[tblOther]", true), (fk.Table, fk.ReferencedTable, fk.IsNotTrusted));
        Assert.Equal(2, summary.ObjectCounts["Tables"]);
    }

    [Fact]
    public void Modules_GetAHeaderAndTheirSettings_OrSayWhyTheDefinitionIsMissing()
    {
        var summary = Write(NewCatalog(
            modules: [Object(3, "trgAudit", "TR", "SQL_TRIGGER", parent: Table), Object(4, "vHidden", "V", "VIEW")],
            definitions: new() { [3] = new ModuleInfo("  CREATE TRIGGER dbo.trgAudit ON dbo.tblAudit AFTER INSERT AS SELECT 1  ", true, false, false) },
            disabledTriggers: [3]));

        Assert.Equal("-- SQL_TRIGGER: [dbo].[trgAudit]\n-- On table: [dbo].[tblAudit]\n-- DISABLED trigger\n" +
                     "SET ANSI_NULLS ON\nGO\nSET QUOTED_IDENTIFIER OFF\nGO\n" +
                     "CREATE TRIGGER dbo.trgAudit ON dbo.tblAudit AFTER INSERT AS SELECT 1\nGO\n",
            Read("Test", "Triggers/dbo.trgAudit.sql"));
        Assert.StartsWith($"-- VIEW: [dbo].[vHidden]\n{SchemaExport.MissingDefinitionMarker}", Read("Test", "Views/dbo.vHidden.sql"), StringComparison.Ordinal);
        Assert.Equal(("[dbo].[vHidden]", "VIEW"), (Assert.Single(summary.MissingDefinitions).Object, summary.MissingDefinitions[0].Type));
    }

    [Fact]
    public void DisabledKey_IsMarkedInItsLine()
    {
        Write(NewCatalog(
            indexes: [Index(1, "PK_tblAudit", primaryKey: true, disabled: true)],
            indexColumns: new() { [(Table, 1)] = [new IndexColumn("id", false, false)] }));

        Assert.Contains("    /* DISABLED index */ CONSTRAINT [PK_tblAudit] PRIMARY KEY CLUSTERED ([id] ASC)", Read("Test", "Tables/dbo.tblAudit.sql"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void DatabaseDdlTrigger_IsInTheModuleDependencies()
    {
        var trigger = new DatabaseTrigger("trgSchemaChanges", false,
            new ModuleInfo("CREATE TRIGGER trgSchemaChanges ON DATABASE FOR CREATE_TABLE AS EXEC ('SELECT 1')", true, true, false),
            new ModuleReferences([new EntityReference(null, null, "dbo", "tblAudit", null, false, true, false, false)], null));

        var summary = Write(NewCatalog(databaseTriggers: [trigger]));

        var dependency = Assert.Single(summary.ModuleDependencies);
        Assert.Equal(("[trgSchemaChanges]", "DATABASE_DDL_TRIGGER", true), (dependency.Object, dependency.Type, dependency.UsesDynamicSql));
        var reference = Assert.Single(dependency.References);
        Assert.Equal(("[dbo].[tblAudit]", true), (reference.Name, reference.Writes));
        Assert.True(File.Exists(Path.Join(_root.FullName, "Test", "Triggers", "Database.trgSchemaChanges.sql")));
    }

    [Fact]
    public void ReferencesThatCouldNotBeRead_AreRecorded()
    {
        var summary = Write(NewCatalog(referencesError: "The SELECT permission was denied. (error 229)"));

        Assert.Equal("The SELECT permission was denied. (error 229)", summary.ReferencesError);
        Assert.Empty(summary.ExternalReferences);
    }

    [Theory]
    [InlineData("tblAudit", "tblAudit")]
    [InlineData("b.c", "b%2Ec")]
    [InlineData("a:b*c", "a%3Ab%2Ac")]
    [InlineData("100%", "100%25")]
    [InlineData("trailing ", "trailing%20")]
    [InlineData(" leading", " leading")]
    public void FileNamePart_EscapesTheSeparatorAndWhatAFileNameCannotHold(string part, string expected)
    {
        Assert.Equal(expected, SchemaExport.FileNamePart(part));
    }

    [Fact]
    public void DottedNames_GetTheSameFileWhetherOrNotTheOtherExists()
    {
        Write(NewCatalog(otherName: "b.c"));

        Assert.True(File.Exists(Path.Join(_root.FullName, "Test", "Tables", "dbo.b%2Ec.sql")));
        Assert.True(File.Exists(Path.Join(_root.FullName, "Test", "Tables", "dbo.tblAudit.sql")));
        Assert.NotEqual(
            $"{SchemaExport.FileNamePart("a")}.{SchemaExport.FileNamePart("b.c")}",
            $"{SchemaExport.FileNamePart("a.b")}.{SchemaExport.FileNamePart("c")}");
    }
}
