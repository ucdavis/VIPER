# Legacy Migration Toolkit

Shared tools for migrating a legacy database into VIPER. They work against any SQL Server database you have a connection string for, so a new migration does not need its own copy of these scripts.

Available today:

| Launcher | What it does | Connects to a database? |
| --- | --- | --- |
| `RunSchemaExport.bat` | Exports a database's schema and object definitions to files | Yes, read-only, catalog metadata only |
| `RunSchemaCompare.bat` | Compares two exports of the same database (e.g. Test vs Production) | No, reads the exported files only |
| `RunAnalysis.bat` | Checks a database's data quality: refreshes the export, then runs automatic and declared checks | Yes, read-only; reads table data, but its report holds only counts, keys, codes, and dates |
| `RunSuggestions.bat` | Suggests what the declarations file lacks: guesses from the export, and the tables to choose an `include` from | Yes, read-only, catalog metadata only (refreshes the export) |

The export, compare, and suggestions never read table rows. The analysis does, under the [output and privacy rules](docs/ANALYSIS-REFERENCE.md#output-and-privacy-rules). It was built and tested against PhoneLists and ClinicalScheduler, then tried on Effort and on one project each in SIS (CareerSelection) and VIPER (the migrated `phones` schema), comparing with those migrations' own analysis scripts where they exist. [docs/ANALYSIS-REFERENCE.md](docs/ANALYSIS-REFERENCE.md) describes every check and the declarations file in full.

## Prerequisites

- .NET 10 SDK
- AWS credentials that can read Parameter Store (the same access the other migration scripts use), when the connection string comes from there. A connection string in `appsettings` or an environment variable needs none (see [Connection names](#connection-names)).
- Network or VPN access to the SQL Server, for the export and the analysis
- For `identifiesOnePerson` checks that look people up in `VIPER.users.Person`: the VIPER connection string for the same environment (`/<Environment>/ConnectionStrings/VIPER`), resolved the same way as the legacy one
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

# 3. Suggest declarations to copy into analysis.json (scope it first with include if it moves only some tables)
.\RunSuggestions.bat ClinicalScheduler Production

# 4. Check data quality (refreshes that environment's export first)
.\RunAnalysis.bat ClinicalScheduler Production
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

## Analyzing data quality

```
RunAnalysis.bat <ConnectionName> <Development|Test|Production> [Project]
```

Each run:

1. Reads the declarations file, `Areas\<ConnectionName>\analysis.json`, if there is one. Without it, only the automatic checks run. A database holding several projects (VIPER's schemas, SIS's apps) can give each its own file, `Areas\<ConnectionName>\<Project>\analysis.json`, chosen by the optional project argument, with results in a `<Project>` folder of their own; the schema export is shared. A connection with project folders but no `analysis.json` of its own refuses a run without a project, which would otherwise check every table.
2. Refreshes the schema export for that environment, exactly as `RunSchemaExport.bat` does, so names are checked against the database as it is now.
3. Checks every table, view, and column the declarations name against the export. All mistakes are reported together, with a suggestion when a name differs only in case, and no checks run until they are fixed.
4. Runs the checks. A check that fails is reported as failed to run, and the rest carry on. That includes a fault in a check's own code, which is reported with where it happened.

### What it checks

| Kind | Checks | Details |
| --- | --- | --- |
| Problems (automatic) | Unenforced foreign keys (and after cleanup), Duplicate rows (and in tables with no key), Rows differing only in audit columns, Leading or trailing spaces, Implausible dates, Unusual characters, Identity headroom | [Automatic checks](docs/ANALYSIS-REFERENCE.md#automatic-checks) |
| Context (automatic) | Column profile, Objects that change data, Rules the schema enforces | [Automatic checks](docs/ANALYSIS-REFERENCE.md#automatic-checks) |
| Design observations (automatic) | Text that is always numbers or dates, Lengths in short code columns, Text columns with few distinct values, Lists stored in one column, Bit columns holding NULL, Date and time columns at midnight, Zero or negative IDs, Placeholder text, Tables with no recent rows, Repeating column groups, Tables that look like copies, Deprecated types, Related columns with different types, Person columns | [Design observations](docs/ANALYSIS-REFERENCE.md#automatic-checks) |
| Declared in `analysis.json` | `valuesMustExistIn`, `noDuplicates`, `notBlank`, `startBeforeEnd`, `withinRange`, `canConvertTo`, `matchesPattern`, `doesNotMatchPattern`, `matchesOtherTable`, `listValues`, `oneValuePerKey`, `sameKeysAs`, `identifiesOnePerson`, `customSql` | [Declared checks reference](docs/ANALYSIS-REFERENCE.md#declared-checks-reference) |

The declarations file can limit the automatic checks to named tables, views, and schemas (`include`, for a database that holds more than the migration moves, such as unrelated sensitive records or other projects' schemas; declared checks may still name any table), exclude tables and views, name the date column that dates each table's rows, change an automatic check's severity for one table or view, or skip it, and list columns that hold codes so the report shows their values (`safeColumns`). When a table has a date column, every declared check on it reports the earliest and latest date of the rows it flags; `listValues` also gives each listed value its own range, and `sameKeysAs` gives each side's range from that side's date column. The [reference](docs/ANALYSIS-REFERENCE.md#the-declarations-file) has the format and every check's fields. Working examples: [`Areas/PhoneLists/analysis.json`](Areas/PhoneLists/analysis.json) (small), [`Areas/ClinicalScheduler/analysis.json`](Areas/ClinicalScheduler/analysis.json) (large), [`Areas/Effort/analysis.json`](Areas/Effort/analysis.json) (a database with no foreign keys, organized by the old analysis script's phases, with two `customSql` checks), and two projects of shared databases, [`Areas/SIS/CareerSelection/analysis.json`](Areas/SIS/CareerSelection/analysis.json) (tables) and [`Areas/VIPER/PhoneLists/analysis.json`](Areas/VIPER/PhoneLists/analysis.json) (a schema).

Declarations files, and the `.sql` files of `customSql` checks, are committed, so they are reviewed like other code.

### Output

Results go to `Output\<ConnectionName>\analysis\<Environment>\` (plus `<Project>\` for a project), gitignored, beside the export folder rather than inside it (each export clears its own folder):

- `report.txt`: a header with the `include` entries, if any, and how many tables and views the automatic checks visited out of how many (the rest outside `include` or excluded), then decisions for the migration (one line per choice a result raises, such as whether to trim leading and trailing spaces, pointing to the full result), then changes since the last run in the same folder (results found for the first time or no longer, and what moved in those found both times), then problems, most severe first (including checks that were skipped or failed to run), then the checks that found no problems, then context, then design observations. The column profile shows only each table's row count and its columns that are always NULL, always blank, or hold one value. Each design observation is one table across all tables, with its description once, rather than a block per table; a table where the check was skipped or overridden keeps its own block.
  Counts that come in pairs per category (such as IDs and rows) are shown as one table with a row per category, its columns in the order of the fullest category (so IDs come before Rows even when a NULL row, which has no IDs, is listed first). Zero counts are left out of a result that counted anything else. A result where everything is zero still says what it counted: all its plain counts (both "Rows below 0" and "Rows above 100", both NULL and blank rows), or the first of its paired counts. A check that says it had nothing to check gives the reason, such as a database with no foreign keys.
- `results.json`: the same results, with every count including zeros, plus every column's full profile. The next run reads it before overwriting it, to write CHANGES SINCE THE LAST RUN near the top of `report.txt`, and every run also keeps a copy as `history/results-<date>-<time>.json`. A field is left out when it does not apply to the column's type, or when there is nothing to report (no lowest value in a column that is always NULL, which `nulls` shows); a number column's lowest and highest that are withheld under the privacy rules are left out, with a `rangeWithheld` reason in their place. Ranges are shown for columns whose values can be shown, with a `rangeShownBecause` reason when a declaration made them so (a `valuesMustExistIn` lookup into a column the database made, or a `safeColumns` entry). `maskedColumns` lists the columns whose values were described by shape, marking person ID columns with `isPersonId` and the shapes of their hidden values.

What results may hold, in short (the [output and privacy rules](docs/ANALYSIS-REFERENCE.md#output-and-privacy-rules) have them in full):

- **Never:** names, email addresses, phone numbers, free text, or whole rows.
- **A value is shown only when its column's origin makes it safe**, never because of how it looks (an SSN is all digits too): values the database made (identity, rowversion), columns whose foreign key points at those, person ID columns (digits only), and columns declared under `safeColumns`. Anything else, row keys included, is described by shape, such as `<letters only, 8 chars>`.
- **Dates** in the column profile are years only, since the earliest or latest date in a column such as a birth date is often one person's; dates that place a problem in time are year and month only.
- **`report.txt` lists the columns it described by shape**, so those holding codes can be declared under `safeColumns`, and, apart, person ID columns holding values that are not IDs.
- **`safeColumns` and `listValues` are refused on a column whose name looks personal**, unless the entry says why with `looksPersonal`.

### Connections

The analysis reads the legacy database over its own connection, read-only. `identifiesOnePerson` checks also open the connection of the database they look people up in, for the same environment, read-only, only when needed: VIPER (`ConnectionStrings:VIPER`) for `VIPER.users.Person`, and AAUD (`ConnectionStrings:AAUD`) for `AAUD.dbo.aaudUser`. Legacy logins cannot be assumed to read either, so the lookups do not go through the legacy connection.

### Suggesting declarations

```powershell
.\RunSuggestions.bat ClinicalScheduler Test
.\RunSuggestions.bat VIPER Production PhoneLists
```

Refreshes the export, reads `Areas\<ConnectionName>\analysis.json` (or the project's, given a project name) if there is one, then writes `analysis.suggestions.json` to the same output folder as the analysis: commented guesses from table and column names and keys that the declarations do not have yet, for you to keep, edit, or delete before copying into `analysis.json`. It never changes that committed file. Run it before the first analysis, and again whenever the schema changes, since new tables and columns get guesses while what is already declared is left out.

When the migration moves only some of the database's tables, the guesses should cover only those, through an `include` in `analysis.json`. If you know which tables they are, write that `include` before the first run. If you do not, run it once without: with no `include` and no checks, it also lists every schema, table, and view with its approximate row count, as a commented `include` to copy from. Then add the `include` and run it again. It proposes:

- **`exclude`**: tables whose names look like backups (the words `bk`, `bak`, `bkup`, `backup`, `old`, or `copy`, or a date such as `_2022_12_30`).
- **`dateColumns`**: per table, a date column named for creation (preferring "created" over "inserted", "added", "entered", and "submitted"), else one named for modification, else its only date column. Tables with several date columns and none of those names are listed in a comment.
- **`valuesMustExistIn` checks**: columns named exactly like another table's single-column primary key, with no foreign key already on them. Generic key names such as `ID`, and names matching several tables, are left out.
- **`identifiesOnePerson` checks**: the export's person ID columns in tables, looked up in `VIPER.users.Person`, or `AAUD.dbo.aaudUser` for PIDMs, which `users.Person` does not hold.

Before reporting success, it reads the file back and binds it against the export, as the analysis would.

Declarations files allow `//` comments and trailing commas. Standard JSON does not, so the repo's VS Code settings open `analysis.json` and `analysis.suggestions.json` as JSON with Comments; other editors may flag the comments as errors.

## Permissions

The catalog only shows a login the objects it has some permission on. So:

- **Without `VIEW DEFINITION`**, an object's definition can come back empty. The export still writes a file for that object, saying the definition is not available, and lists it under `missingDefinitions`.
- **Objects the login cannot see at all** are absent from the catalog. The export cannot know they exist. In a comparison they show up as "only in" the other environment.
- **Without `VIEW DEFINITION` on the database and `SELECT` on `sys.sql_expression_dependencies`** (by default only `db_owner` has the `SELECT`), `externalReferences` and `unresolvedReferences` cannot be read. The export carries on with both empty, records why in `referencesError`, and warns.

If two environments' exports disagree, check whether the logins' permissions differ before assuming the schemas do.

The analysis also needs `SELECT` on the tables and views it checks. A table the login cannot read shows as failed to run.

## What these tools do not do

- **Read table data, for the export and compare.** They query catalog views only, and row counts come from partition metadata. The analysis does read table data, and its report holds only what its privacy rules allow.
- **Find inbound references.** `externalReferences` is outbound only: what this database uses elsewhere. Other databases, SQL Agent jobs, and applications that use this database are not visible from inside it.
- **See application queries.** References made from application code (e.g. inline queries in a ColdFusion app) never reach the database catalog.
- **See dynamic SQL.** Names built into strings and run with `EXEC` or `sp_executesql`, or passed through `OPENQUERY`, are not tracked as dependencies. `moduleDependencies` marks the objects that run dynamic SQL, and `linkedServerMentions` lists those that use `OPENQUERY` and similar, but not what they query.
- **Write to any database.** Every connection is opened with `ApplicationIntent=ReadOnly`, and the tools only run `SELECT`s. On a standalone server `ApplicationIntent` does not itself block writes, so this read-only guarantee comes from the code.
- **Fix anything.** The analysis only reports. Deciding what to do about each problem stays with the developers and the people who sign off on the migration.

## For developers

- `Program.cs` routes each command (`schema-export`, `schema-compare`, `analysis`, `suggestions`) to its class. Each `.bat` launcher is a thin wrapper that calls `dotnet run -- <command>`.
- `LegacyMigrationScriptHelper.cs` holds the shared configuration, connection, and argument handling.
- `PersonIdRules.cs` holds the word lists behind `personIdColumns`. Adding an ID type or a "who did it" verb is one line.
- The analysis is in `Analysis/`. Checks build SQL only from `TableRef` and `ColumnRef`, which come only from the schema export, so a name that is not in the export can never reach a query; values go in as parameters, apart from `canConvertTo`'s `type` (checked against a fixed list) and the tool's own quoted constants, such as the placeholder words. The one query run as written is a `customSql` file, which `CustomSqlValidator` (Microsoft's T-SQL parser, ScriptDom) must first show to be a single read-only `SELECT` of tables in the export.
- To add a check: an automatic check implements `IAutomaticCheck`; a declared check is a `CheckDeclaration` (its fields, and binding them to the export) plus a `DeclaredCheck` (running it). Register either in `CheckRegistry` in `Checks.cs`. `ResultSql.cs` has the shared pieces, such as counting affected rows with example keys and dates, and describing values by shape. `AutomaticResult.cs` applies the declarations' overrides and keeps only the tables with findings. A result that raises a choice for the migration sets `CheckResult.Decision`, which the report lists first, under DECISIONS FOR THE MIGRATION. `noDuplicates` declarations are run by the automatic Duplicate rows check (`DuplicateRowsCheck` calls `NoDuplicatesCheck.AddSection`), so each table's duplicates are one result. `AnalysisReport` prints each design observation as one table across tables; `results.json` keeps one result per table.
- `TableProfile.cs` is the shared read of each table (one query, split into a few for a wide table so none passes SQL Server's 4,096-item select list), cached by `AnalysisContext.Profile`. A check that needs per-column counts should add its aggregates there rather than read the table again; `ProfileProblemCheck` and `ProfileObservationCheck` turn those counts into results, and its shared conditions (`LeadingSpacesWhere`, `UnusualCharacterKinds`, and the like) list the rows behind a count, so counts and rows always agree. `ColumnRef.IsPadded` marks `char` and `nchar` columns, whose padding checks trim or ignore.
- `Suggestions.cs` writes the suggested declarations.
- The project is excluded from the web build (`Viper.csproj`) and linted at pre-commit by `scripts/lint-staged-dotnet.js`.
- **Tests** are in `test/LegacyMigration/`, in the main test project, which references this one. Run them with `npm run test:backend -- LegacyMigration`. This is a tool meant to be run again and again, so add tests with every change.
  - **What they cover:** everything that runs without a database. The export's scripts and `summary.json`, written from a catalog the tests build in memory (`SchemaExport.Write`); binding `analysis.json` to the schema export (every declared check, `include`, `exclude`, `dateColumns`, `automaticChecks`, `safeColumns`, and the `customSql` validator and its key tracing); which columns count as person columns; which columns' values are shown and how others are masked; how object names are quoted; the name rules behind several checks; the report's layout; the comparison with the last run; the suggestions (each kind of guess, what is left out, and that what is written binds); the Parameter Store fallback; and the schema comparison (against two export folders the tests write).
  - **The checks themselves** run against `FakeDatabase.cs`, which stands in for SQL Server through `IAnalysisDatabase`: it answers each query by a fragment of its SQL, records every query with its parameters, fails a test on a query it has no answer for, and refuses a parameter reused across queries, as SQL Server does. `TestSchema.cs` holds a small schema export shaped like the legacy databases, and `ProfileRow.cs` builds the profile read's one wide row in the order `TableProfile` asks for it. A new check gets tests of what it reports from known rows, and of what it must keep out of the report.
  - **Culture:** `Program.Main` runs the toolkit under the invariant culture, so numbers and dates format the same on every machine (`1,234`, `10.6%`); test classes carry `[InvariantCulture]` to match, and a new one should too.
  - **Not covered:** whether the SQL itself is right for SQL Server, including the catalog queries in `Catalog.Load`, which running the tool verifies.
- If pre-commit reports `IDE0005: Using directive is unnecessary` on a `using` of one of the project's packages (such as `Microsoft.Data.SqlClient` or `Microsoft.SqlServer.TransactSql.ScriptDom`), the project has not been restored since the package was added. Run any launcher once, or `dotnet restore`, and commit again.
