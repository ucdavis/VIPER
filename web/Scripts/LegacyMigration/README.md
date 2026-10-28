# Legacy Migration Toolkit

Shared tools for migrating a legacy database into VIPER. They work against any SQL Server database you have a connection string for, so a new migration does not need its own copy of these scripts.

Available today:

| Launcher | What it does | Connects to a database? |
| --- | --- | --- |
| `RunSchemaExport.bat` | Exports a database's schema and object definitions to files | Yes, read-only, catalog metadata only |
| `RunSchemaCompare.bat` | Compares two exports of the same database (e.g. Test vs Production) | No, reads the exported files only |

Neither reads table rows.

## Prerequisites

- .NET 10 SDK
- AWS credentials that can read Parameter Store (the same access the other migration scripts use), when the connection string comes from there. A connection string in `appsettings` or an environment variable needs none (see [Connection names](#connection-names)).
- Network or VPN access to the SQL Server, for the export
- A login with `VIEW DEFINITION` on the database and `SELECT` on `sys.sql_expression_dependencies` (which by default only `db_owner` has). Without them the export still runs, but warns and is incomplete (see [Permissions](#permissions)).

## Quick start

The launchers work from any directory. These examples run from this folder:

```powershell
cd web\Scripts\LegacyMigration

# 1. Export each environment
.\RunSchemaExport.bat ClinicalScheduler Test
.\RunSchemaExport.bat ClinicalScheduler Production

# 2. Compare the two exports
.\RunSchemaCompare.bat ClinicalScheduler Test Production
```

The first run restores packages and builds the tool, so it takes longer.

## Exporting a schema

```
RunSchemaExport.bat <ConnectionName> <Development|Test|Production>
```

Both arguments are required. The environment decides which connection string is used and which output folder is written.

### Connection names

`<ConnectionName>` is a key under `ConnectionStrings`, resolved from these sources in order (later sources win):

1. `appsettings.json`, then `appsettings.<Environment>.json`
2. Environment variables, e.g. `ConnectionStrings__PhoneLists`
3. AWS Parameter Store: `/<Environment>/ConnectionStrings/<ConnectionName>`, then `/Shared/ConnectionStrings/<ConnectionName>`

Because Parameter Store is read last, it overrides an environment variable of the same name. To point at a database that is not in Parameter Store, set an environment variable under an unused name and pass that name.

The two Parameter Store paths are read separately. If one cannot be read (no permission, or Parameter Store cannot be reached), the tool warns, naming the path, and continues with the other path and the local settings. The export then fails with "connection string not found" if the name was defined only in the path it could not read.

### Output

Each export writes to `Output\<ConnectionName>\<Environment>\` in this folder. A **successful export clears the folder** before writing, so it reflects that export alone; it is gitignored. An export that fails before writing, such as on a failed connection, leaves the previous export in place; the comparison report shows each export's date. `summary.json` is written last, so an export that fails while writing is reported as missing rather than compared.

```
Output\ClinicalScheduler\Test\
  Tables\            one CREATE TABLE script per table, with its constraints and indexes
  Views\
  StoredProcedures\
  Functions\
  Triggers\
  Types\             user-defined alias and table types
  Synonyms\
  summary.json
```

Files are named `<schema>.<object>.sql`. Within each name, a dot and any character a Windows file name cannot hold are written as `%XX` (`[a].[b.c]` is `a.b%2Ec.sql`), so every object's file name is the same in every export. Two names differing only in case get a `~2` suffix on the second, since Windows treats them as one file. Some details of the scripts:

- **Untrusted and disabled constraints** are scripted `WITH NOCHECK`, with a comment above them. Existing rows were never checked against these constraints. A disabled primary key or unique constraint is marked `/* DISABLED index */` in its line.
- **System-generated constraint names** (e.g. `DF__Rotation__Active__1A2B3C`) are left out of the scripts, because they differ between environments and would show up as false differences. `summary.json` still records them. For the same reason, constraints with system-generated names come after the named ones, ordered by what they enforce rather than by name.
- **Column collations** are shown only where they differ from the database default.

`summary.json` holds the facts that later steps read:

| Field | Contents |
| --- | --- |
| `server`, `database`, `productVersion`, `compatibilityLevel`, `databaseCollation` | Environment facts |
| `hasViewDefinition` | Whether the login had `VIEW DEFINITION` |
| `objectCounts` | Objects exported per type |
| `tables` | Each table's approximate row count (from metadata, not `COUNT(*)`), whether it has a primary key or any enforced, unfiltered unique key (a disabled one enforces nothing, and a filtered one guarantees uniqueness only within its filter), its trigger count, its primary key and enforced, unfiltered unique key columns, and its columns: type (and alias type), length in characters for text, nullability, computed or not, collation, and identity seed, increment, and current value |
| `views` | Each view's columns, in the same form as table columns. They are the columns SQL Server recorded when the view was created or last refreshed: a view written as `SELECT *` keeps that list even after its tables change |
| `personIdColumns` | Columns that may hold a person's ID, from names and foreign keys only (no data read). Category `idType`: the name contains a word for an ID type (`mothra`, `iam`, `pidm`), or a foreign key references such a column. Category `review`: the name says who did something (`ModBy`, `CreatedBy`, `UpdatedBy`, `addUser`, `Who_Mod`) but not with which ID. Each comes with the reason it matched. Names are split into words, and only whole words count, so `iam` does not flag `William` and `SortBy` is not flagged |
| `tablesWithoutPrimaryKey`, `tablesWithoutUniqueKey` | Candidates for duplicate-row checks |
| `foreignKeys`, `checkConstraints` | Every constraint, with `isNotTrusted` and `isDisabled` |
| `externalReferences` | Objects in this database that reference another database or a linked server |
| `linkedServerMentions` | Objects whose code uses `OPENQUERY`, `OPENROWSET`, or `OPENDATASOURCE` |
| `unresolvedReferences` | References to objects that may not exist |
| `referencesError` | Present only when reading `sys.sql_expression_dependencies` failed (most often for lack of permission, but any SQL error counts, such as a timeout): why, so the empty `externalReferences` and `unresolvedReferences` are not taken to mean there are none |
| `moduleDependencies` | Each view, procedure, function, and trigger, database DDL triggers included: the tables, views, and other objects it uses, whether it reads or writes each (and which columns it writes), as SQL Server binds it (`sys.dm_sql_referenced_entities`); whether its code runs dynamic SQL; and the error, instead, when SQL Server could not bind it (such as one naming a column that no longer exists; a missing table is listed under `unresolvedReferences` instead) |
| `missingDefinitions` | Objects exported without a definition, and why |
| `unexportedObjects` | Objects of a type the export does not script (e.g. old-style rules) |
| `excludedToolObjects` | `dt_*` procedures and diagram-support objects that were skipped |

The console prints a summary of the same counts at the end, plus warnings for anything that needs a look, and how long reading the catalog and writing the files took. Every command except the compare ends with a `Finished` line giving the time and how long the run took.

## Comparing environments

```
RunSchemaCompare.bat <ConnectionName> <EnvironmentA> <EnvironmentB>
```

Both environments must already be exported. The compare reads only those files, so it needs no database or AWS access. The report is printed and saved as `Output\<ConnectionName>\compare-<EnvironmentA>-<EnvironmentB>.txt`.

The launcher runs `dotnet run`, which restores the tool's packages first, so offline it works only once a launcher has run online on that machine. Offline, to be sure no restore is attempted, run the tool directly from this folder, skipping the restore:

```powershell
dotnet run --no-restore --project LegacyMigration.csproj --configuration Release -- schema-compare --connection ClinicalScheduler --left Test --right Production
```

The report lists:

- **Environment facts** side by side, with differences starred. A collation difference matters most, since it changes how keys compare and join.
- **Possible permission gaps**: a definition visible in one environment only, or `VIEW DEFINITION` differing.
- **Objects only in one environment.**
- **Definitions that differ**, each with a `git diff --no-index` command for a line-by-line view.
- **Whitespace-only differences**, listed apart so they do not hide real ones. Only whitespace between T-SQL tokens counts: a change inside a string, a bracketed name, or a comment is a difference.
- **View columns that differ**: a view's columns, from `summary.json`, compared in order by name and type. SQL Server fixes a view's columns when it is created or refreshed, so a `SELECT *` view refreshed in only one environment can differ here while its definition is identical; such a view is counted under "Same definition, different view columns" rather than as identical. Views whose names differ only in case, possible under a case-sensitive collation, are left out of this check, since the second one's file name gets a `~2` suffix.

### Viewing line-by-line differences

The `git diff --no-index` commands in the report use paths relative to the **repository root**, so run them from there. `--no-index` compares any two paths on disk, so it works even though `Output\` is gitignored.

Useful variants (run from the repository root):

```powershell
$out = "web/Scripts/LegacyMigration/Output/ClinicalScheduler"
git diff --no-index --stat        -- "$out/Test" "$out/Production"   # files changed, with counts
git diff --no-index --name-status -- "$out/Test" "$out/Production"   # A/D/M per file
git diff --no-index -w            -- "$out/Test" "$out/Production"   # ignore whitespace
```

VS Code's compare view (select two files, then **Compare Selected**) works too.

## Permissions

The catalog only shows a login the objects it has some permission on. So:

- **Without `VIEW DEFINITION`**, an object's definition can come back empty. The export still writes a file for that object, saying the definition is not available, and lists it under `missingDefinitions`.
- **Objects the login cannot see at all** are absent from the catalog. The export cannot know they exist. In a comparison they show up as "only in" the other environment.
- **Without `VIEW DEFINITION` on the database and `SELECT` on `sys.sql_expression_dependencies`** (by default only `db_owner` has the `SELECT`), `externalReferences` and `unresolvedReferences` cannot be read. The export carries on with both empty, records why in `referencesError`, and warns.

If two environments' exports disagree, check whether the logins' permissions differ before assuming the schemas do.

## What these tools do not do

- **Read table data.** They query catalog views only, and row counts come from partition metadata.
- **Find inbound references.** `externalReferences` is outbound only: what this database uses elsewhere. Other databases, SQL Agent jobs, and applications that use this database are not visible from inside it.
- **See application queries.** References made from application code (e.g. inline queries in a ColdFusion app) never reach the database catalog.
- **See dynamic SQL.** Names built into strings and run with `EXEC` or `sp_executesql`, or passed through `OPENQUERY`, are not tracked as dependencies. `moduleDependencies` marks the objects that run dynamic SQL, and `linkedServerMentions` lists those that use `OPENQUERY` and similar, but not what they query.
- **Write to any database.** Every connection is opened with `ApplicationIntent=ReadOnly`, and the tools only run `SELECT`s. On a standalone server `ApplicationIntent` does not itself block writes, so this read-only guarantee comes from the code.
- **Fix anything.** They only report. Deciding what to do about each finding stays with the developers and the people who sign off on the migration.

## For developers

- `Program.cs` routes each command (`schema-export`, `schema-compare`) to its class. Each `.bat` launcher is a thin wrapper that calls `dotnet run -- <command>`.
- `LegacyMigrationScriptHelper.cs` holds the shared configuration, connection, and argument handling.
- `PersonIdRules.cs` holds the word lists behind `personIdColumns`. Adding an ID type or a "who did it" verb is one line.
- The project is excluded from the web build (`Viper.csproj`) and linted at pre-commit by `scripts/lint-staged-dotnet.js`.
- **Tests** are in `test/LegacyMigration/`, in the main test project, which references this one. Run them with `npm run test:backend -- LegacyMigration`. This is a tool meant to be run again and again, so add tests with every change.
  - **What they cover:** everything that runs without a database: the export's scripts and `summary.json`, written from a catalog the tests build in memory (`SchemaExport.Write`); which columns count as person columns; how object names are quoted; the Parameter Store fallback; and the schema comparison (against two export folders the tests write).
  - **Culture:** `Program.Main` runs the toolkit under the invariant culture, so numbers and dates format the same on every machine (`1,234`, `10.6%`); test classes carry `[InvariantCulture]` to match, and a new one should too.
  - **Not covered:** whether the SQL itself is right for SQL Server, including the catalog queries in `Catalog.Load`, which running the tool verifies.
- If pre-commit reports `IDE0005: Using directive is unnecessary` on a `using` of one of the project's packages (such as `Microsoft.Data.SqlClient`), the project has not been restored since the package was added. Run any launcher once, or `dotnet restore`, and commit again.
