# Data Quality Analysis Reference

What `RunAnalysis.bat` and `RunSuggestions.bat` in the Legacy Migration Toolkit do: every check, the declarations file, the output and privacy rules, and how the analysis stays read-only and safe from SQL injection. The [README](../README.md) covers running the tools.

## Why

Every legacy migration so far (Effort, CareerSelection, PhoneLists) wrote its own data quality analysis script before migrating. Those scripts ask many of the same questions in different words:

- Does every value in this column exist in the table it points at?
- Are there duplicates on columns that should be unique?
- Is this required column ever blank?
- Does each person ID in this column identify exactly one person?

Each script wrote its own SQL for each question, by hand, even where the database already knew the answer was worth checking. The analysis replaces most of that hand-written SQL with:

1. **Automatic checks** that run on any legacy database, using the schema export the toolkit already produces.
2. **Declared checks**, listed in a per-database JSON file, for questions only someone who knows the app can ask ("these three columns should never repeat together").

A rule those cannot express can be written as a [`customSql`](#customsql) query, checked before it runs. What SQL cannot express stays in the area's own scripts.

The analysis is **read-only and reports only**. It never changes data, and nothing it reports is "fixed" by running it. Deciding what to do about each problem stays with the developers and the people who sign off on the migration.

Results come in three kinds:

- **Problems**: data that is wrong or will break the migration (a reference to a row that does not exist, a duplicate where there should be none).
- **Design observations**: things that are not wrong but are worth considering when designing the new schema (a text column with only a few distinct values, which may belong in a lookup table, or a yes/no flag that is sometimes NULL).
- **Context**: facts to read the rest by, which nothing can fail (each table's column profile, the stored objects that change each table's data, a `listValues` listing with no `allowedValues`).

Each kind has its own section of the report, so observations and context do not bury the problems.

Some results of either kind also raise a **decision for the migration**: a choice someone has to make once, such as what NULL means in a yes/no flag, rather than rows to fix. `report.txt` lists these first, under DECISIONS FOR THE MIGRATION, one line per question and check with how many columns and tables it covers, so they are not lost further down a long report. The full results stay in their own sections.

## Contents

- [Using it for a new migration](#using-it-for-a-new-migration)
- [Output and privacy rules](#output-and-privacy-rules)
- [Severity](#severity)
- [Automatic checks](#automatic-checks)
- [The declarations file](#the-declarations-file)
- [Declared checks reference](#declared-checks-reference)
- [Suggestions](#suggestions)
- [Safety](#safety)
- [What stays in per-area scripts](#what-stays-in-per-area-scripts)

## Using it for a new migration

1. **Add the legacy database's connection string** to Parameter Store, as for the schema export.
2. **Decide the scope.** If the migration moves only some of the database's tables (the rest belong to other projects, or hold sensitive data it does not need, such as student records), the analysis should be limited to them with an [`include`](#include) in `analysis.json`. When the database holds several projects, give this one its own [project](#the-declarations-file) folder and name it in every command below.
   - **If you know which tables**, for example from the legacy app or an earlier migration script, create `analysis.json` with just that `include`.
   - **If you do not**, run the suggestions once without it (next step): with no `include` and no checks, they list every schema, table, and view with its row count, as an `include` to copy from. Then create `analysis.json` with the `include`.
   - **If the migration moves the whole database**, skip this step.
3. **Run the suggestions**: `RunSuggestions.bat <ConnectionName> <Environment> [Project]` (see [Suggestions](#suggestions)). They read only the catalog, never table data, and guess declarations for the tables in scope.
4. **Copy the suggestions you keep into `analysis.json`** and edit them as you learn the app: exclude backup tables, say which column dates each table's rows, and declare the keys and relationships the schema never enforced.
5. **Run the analysis**: `RunAnalysis.bat <ConnectionName> <Environment> [Project]`. It first refreshes the schema export, so the checks always match the database as it is now, then runs the [automatic checks](#automatic-checks) and the declared ones. The automatic checks need no declarations, so a run before step 4 also works, as a first picture of the data, but only without a project: a project's analysis stops unless its `analysis.json` exists (the suggestions do not need it). `RunSchemaExport.bat` still exists on its own, for when only the schema is wanted, for example before comparing environments.
6. **Re-run whenever you like.** The legacy app is usually still live during a migration, so re-running the analysis shows whether a problem is still being created or stopped long ago: the report's CHANGES SINCE THE LAST RUN lists what was found for the first time or no longer, and, for results found both times, counts that moved, a latest affected date that moved later, and newly affected keys. Re-running the suggestions after a schema change proposes guesses for new tables and columns only.

Declarations files are committed, so the team can review them like other code. Analysis results are not.

## Output and privacy rules

Results go to `Output\<ConnectionName>\analysis\<Environment>\` (with a `<Project>` folder below it for one project of a database holding several), which is gitignored. They sit beside the schema export folder rather than inside it, because each export clears its folder and would delete the results kept for comparing runs. Even so, gitignored files can end up staged (for example after switching to a branch where the ignore rule does not exist yet), so the analysis follows strict rules about what goes into its results.

The analysis never changes the database.

**May appear in the results:**

- Counts of rows and values.
- Values of a column whose origin makes them safe to show (see [Which values are shown](#which-values-are-shown)), wherever they appear: row keys, values a check found, and the column profile's lowest and highest value. Each shown or withheld range says why in `results.json`.
- Codes and other non-personal values, from columns you explicitly declare (see [`safeColumns`](#safecolumns) and `listValues`).
- Person IDs that are numbers (MothraIds, IAM IDs, PIDMs, and VIPER PersonIds, the key of `users.Person`), separated from personally identifying context.
- Dates that place a problem in time (see `dateColumns`), as **year and month only** (`2019-06`), since a declared date column may be personal and one affected row's dates are its own. In the column profile, a date column's earliest and latest values are reported as **year only**, because in a column such as a birth date the earliest or latest date is often one person's.

**Never appear in the results:**

- Names, email addresses, phone numbers.
- Free text (comments, statements, notes).
- Whole rows.
- LoginIds and MailIds (often derived from people's names), unless a specific case needs them for the report to be readable and that exception is agreed first.

### Which values are shown

Whether a value is shown depends on where its column's values come from, never on how the value looks: a social security number is all digits too. A column's values are shown when:

1. **The database made them**: an identity column or a rowversion. Such a value means nothing outside its table.
2. **The column's foreign key points at one of those**, followed along a chain of foreign keys. A declared [`valuesMustExistIn`](#valuesmustexistin) lookup into such a column counts as a foreign key, since legacy databases often declare none. Unlike a foreign key, nothing makes a lookup's types match, so a text column in one shows only its values of digits; any other value, which matches no ID and may be a LoginId or a name, is described by shape.
3. **The column holds person IDs** (one the export flags in `personIdColumns` as a MothraId, IAM ID, or PIDM). Only values of digits are shown; anything else in the column, such as a LoginId typed into a MothraId column, is described by shape.
4. **The column is listed under [`safeColumns`](#safecolumns)**, for everything else that is safe: code lookups, term codes, natural numeric keys.

NULL and blank values are always named. Any other value is described by shape, such as `<letters only, 8 chars>`, which is enough to tell a LoginId from a name or a typo. Rows are listed by their key, so a key in a column with none of these origins is described by shape too; the rows can then be found by declaring the column under `safeColumns`, or by the other columns of the result.

When a run describes any column's values by shape, report.txt lists those columns near the top, in two lists, and `results.json` holds them as `maskedColumns` (with `isPersonId` and `shapes` for the second):

- **VALUES DESCRIBED BY SHAPE: declare under safeColumns to show**, by table: columns with none of the origins above, all of whose values were hidden, and text columns in a declared lookup that hold values other than digits. Declaring those that hold codes, and re-running, shows them.
- **PERSON ID COLUMNS WITH VALUES THAT ARE NOT IDS**: person ID columns whose IDs were shown as written, but which also hold values that are not all digits, each with the shapes of those values and how many different values have each (such as `letters only, 6-8 chars (4 distinct values)`, likely LoginIds). These are data to fix, not columns to declare.

A [`customSql`](#customsql) key is shown only when the query selects it straight from a table column (`t.column`, or `t.column AS name`), and then as that column's values are; any other key is described by shape (see `customSql`).

[`listValues`](#listvalues) is the one exception: declaring it on a column is the approval to list that column's values in that check.

Declaring a column under `safeColumns`, or `listValues` on it, is refused when its name looks personal (it has a word such as name, email, login, user, phone, ssn, birth, address, grade, score, gpa, balance, gender, age, note, or health, or the export flags it as a person column), unless the entry also has `looksPersonal`, saying in one sentence why its values are safe anyway. The run reports every such entry at once, before any table's data is read.

### What a result looks like

A check produces one result per subject: usually a table, but a declared column for [`notBlank`](#notblank) and [`oneValuePerKey`](#onevalueperkey), and all tables together for the five checks that report across tables (see [`automaticChecks`](#automaticchecks)). Each result has:

- **name** and **description** (from the declaration, or generated for automatic checks)
- **status**: `no problems found`, `problems found`, `skipped` (with why), or `failed to run` (with the error; some SQL Server and .NET errors quote the value that failed, so quoted text is described by shape unless it is a schema, table, view, or column name, or a .NET type such as `System.Int32`; a fault in the check's own code also names the exception's type and where it was thrown)
- **counts**: how many rows or values have the problem
- **examples**: the keys of affected rows, up to 50 per check, with the total always shown
- **earliest / latest**: the year and month range of affected rows, when the table has a [date column](#datecolumns) declared
- **tables and detail lines**: issues laid out by key or column, and notes such as rows listed by key under each kind of problem
- **decision** (some results): a choice the migration has to make that the result raises, listed again at the top of `report.txt`

Results are written two ways:

- **`report.txt`**, a readable report. Each design observation is one table across all tables, with its description once, so that it reads on its own without repeating itself.
- **`results.json`**, every result on its own. The next run in the same folder compares itself with it, and every run's copy is also kept under `history/` by run time.

The comparison, **CHANGES SINCE THE LAST RUN**, follows the decisions at the top of `report.txt`:

- Results are matched by check name and table (or label), so a check renamed or reworded between runs shows as one result gone and one new.
- It lists results found now and not in the last run, found then and not now, and other status changes (such as a check that now fails to run).
- For results found both times: the counts that changed; table rows added, removed, or changed, named by their key (such as the column); the latest affected date; and example keys listed now and not before, or the reverse (only when the other run listed all of its keys, since a result keeps its first 50).
- Context results are not compared, since their counts change with every write; unchanged results are only counted.

## Severity

Every result carries one of three labels for triage, so the report can be read most-important first:

| Severity   | Meaning                                                                                                                           |
| ---------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `critical` | Wrong data, or data that will stop the migration (a missing referenced row, a duplicate where the new schema requires uniqueness) |
| `warning`  | Likely a problem, or something that needs a decision before migrating (values that only match after trimming spaces)              |
| `info`     | Context and design observations; nothing is necessarily wrong                                                                     |

Automatic checks have the default severities listed in their tables below. Declared checks default to `warning` unless the declaration says otherwise; the exception is `listValues` without `allowedValues`, which cannot fail and defaults to `info`. Defaults can be changed for a specific table or object in the declarations file (see [`automaticChecks`](#automaticchecks)).

## Automatic checks

These run on every analysis, with no declarations. They are driven by the schema export's `summary.json`. They visit only the tables and views named by [`include`](#include), when the declarations file has one, and skip those listed under [`exclude`](#exclude). Unenforced foreign keys follow the child table, so a key from an included table into a table outside `include` is still checked. Every problem check and design observation reports only the tables (or foreign keys) where it found something; when it finds nothing anywhere, one result says how much it checked. The context entries (the column profile, Objects that change data, and Rules the schema enforces) cover every table. Duplicate rows also reports tables whose `noDuplicates` declarations all passed, since every declared check is reported.

**Problems:**

| Check                                          | What it reports                                                                                                                                                                                     | Severity           | Cost                                                      |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------ | --------------------------------------------------------- |
| **Unenforced foreign keys**                    | For every untrusted or disabled foreign key, the rows with no parent row, even once spaces are trimmed and case is ignored, listed by the child table's key. [Details](#unenforced-foreign-keys) | `critical`         | One query per foreign key; cheap                          |
| **Unenforced foreign keys, after cleanup**     | Rows of an untrusted or disabled foreign key whose values match a parent row only after trimming spaces or ignoring case; not counted in the check above. | `warning`          | Same query                                                |
| **Duplicate rows in tables with no key**       | For tables with no primary key, identity column, or enforced unique key over every row (a disabled or filtered one does not count): groups of fully identical rows and how many extra copies they hold, with groups whose rows match only under the columns' collation (differing in case or trailing spaces) counted apart. The rows have no key, so only counts and dates are reported. | `critical`         | One grouped read per such table                           |
| **Duplicate rows**                             | One result per table holding every kind of duplicate: exact copies (rows identical but for their surrogate key), and each `noDuplicates` declaration's groups that share its key but differ elsewhere. [Details](#duplicate-rows) | `warning`          | One grouped read of every table with a surrogate key, plus one per `noDuplicates` declaration; heavier than the profile |
| **Rows differing only in audit columns**       | Groups of rows identical but for their surrogate key and their audit columns (when or by whom a row was created or changed), whose audit columns differ: usually one record saved again. [Details](#rows-differing-only-in-audit-columns) | `warning` / `info` | One grouped read of each table with a surrogate key and audit columns |
| **Leading or trailing spaces**                 | Per text column, how many values start or end with spaces, with the column's type. SQL Server ignores trailing spaces when comparing with `=`, but C# does not, so these break lookups in migration code wherever the values are matched; in free text they break nothing, but the migration still has to choose whether to keep or trim them (leading spaces can be deliberate, such as indentation), so the result raises that decision. The type is shown for information only, since it cannot tell a matched column from free text. Values of only spaces count as blank instead, and trailing spaces in `char` and `nchar` columns are padding, so not counted. Leading and trailing are each listed by row key with their dates. | `warning`          | Counted in the profile read; one more query per kind in each column with findings |
| **Implausible dates**                          | Per date column, how many values fall before 1950 or more than 10 years in the future, which often means placeholder dates such as `1900-01-01`. Each kind is listed by row key with its dates. | `warning`          | Counted in the profile read; one more query per kind in each column with findings |
| **Unusual characters**                         | Per text column, how many values contain control characters (other than tab and line breaks) or common signs of encoding damage: UTF-8 text read as Windows-1252 (such as `Ã©` in place of `é`), or the replacement character `�`. The rows are listed by key and by kind, since each kind needs a different fix. Each flagged column also lists which control characters it holds, by hex code with how many rows hold each (codes only, never text); codes that are a Unicode curly quote or dash cut to its low byte (0x13 –, 0x14 —, 0x18 ‘, 0x19 ’, 0x1C “, 0x1D ”) name the character they likely were, since text pasted from a word processor can be damaged this way; a value can fall under more than one. Never the values. | `warning`          | Counted in the profile read; one more query per kind in each column with findings, and one for its control-character codes |
| **Identity headroom**                          | For each identity column, how much of its type's range (from the seed, in the direction of the increment) its last value has used. A `warning` per table once 80% is used; the 10 most used are listed as context. | `info` / `warning` | Metadata only; no table reads                             |
| **Objects that change data**                   | For each table, the stored views, procedures, functions, and triggers that write to it, and the tables no stored object uses; a map of what keeps changing the data, not a finding. [Details](#objects-that-change-data) | `info` (context)   | Metadata only; no table reads                             |
| **Rules the schema enforces**                  | The CHECK constraints on the tables checked, each with its rule and whether SQL Server trusts it: rules the new schema should keep, as constraints or validation. A trusted rule holds for every row; an untrusted one was added or re-enabled without checking the rows already there, and a disabled one is not enforced, so existing rows may break either. Those rows are not checked. | `info` (context)   | Metadata only; no table reads                             |

### Unenforced foreign keys

- Covers every foreign key the export marks untrusted or disabled. Trusted foreign keys are skipped, because SQL Server already guarantees them.
- Rows are counted and listed by the child table's key, never by the values, which can be anything the parent is keyed on.
- Rows that match a parent only once spaces are trimmed and case is ignored are under **Unenforced foreign keys, after cleanup** instead, so no row is counted twice, as in [`valuesMustExistIn`](#valuesmustexistin).
- Untrusted keys with no missing parents are listed together, since re-enabling them `WITH CHECK` would succeed. Keys whose only missing parents match after cleanup are listed apart, since re-enabling them fails until those values are cleaned.

### Duplicate rows

- **Exact copies**, for tables keyed by an identity column or a single whole-number primary key: groups of rows identical in every other column, which usually means the same record was entered twice. The first 10 groups are listed by their keys, with the declared keys' values when the table has `noDuplicates` declarations.
- **Case and trailing spaces:** rows are grouped under each column's collation, so in a case-insensitive database "Smith" and "smith " are copies too. Those groups are counted apart, as copies differing only in case or trailing spaces, and marked in the list with the columns that differ that way, so "exact" means byte for byte.
- **Each [`noDuplicates`](#noduplicates) declaration** on the table (or view) adds its own part: groups sharing the declared key whose rows differ elsewhere, with how many groups differ in each combination of columns (such as a modified-date column only, the same record saved twice), and its groups that are copies (mentioned when the table's exact-copy part already lists them, counted and listed otherwise). A table whose declarations all pass is still reported, under no problems found, with a line per declaration.
- **Counts** are a table of groups and rows per part.
- **Left out:** computed columns and types that cannot be compared (such as `xml` and `rowversion`), which the result names.
- **Severity:** an `automaticChecks` override changes or skips only the exact-copy part; a declaration's part keeps its own severity, and the result takes the most severe of the parts that found something.

### Rows differing only in audit columns

- For tables keyed by an identity column or a single whole-number primary key: groups of rows identical in every other column except their audit columns, whose audit columns differ. Groups identical in the audit columns too are exact copies, under Duplicate rows.
- **Audit columns** record when or by whom a row was created or changed, by name:
  - person columns that say who did something (`ModBy`, `CreatedBy`, `Who_Mod`)
  - date columns named for creation or modification (`Create_Date`, `Modified_date`, `LastMod`), as whole name words, so `ModuleDate` and `AdditionalDate` are compared
  - the table's declared [`dateColumns`](#datecolumns) column, when its name also says it records when the row was saved (those words, or `timestamp`, `stamp`, `logged`, `generated`, `snapshot`). A declared column that records when something happened, such as an appointment's start, is compared, since two rows differing in it are two events.
- Approval, review, and signature columns are compared, since a difference there is a real one.
- As in Duplicate rows, the other columns are compared under their collation; groups whose other columns also differ in case or trailing spaces are counted, and marked in the list.
- A table named like a log (a whole name word `audit`, `audits`, `log`, `logs`, `hist`, `history`, or `histories`, such as `RequestAudit` or `ChangeLog`, but not `Histology` or `Login`) is reported only as `info`, with a note, since its rows are usually separate events.
- **Reports** the groups and rows with their dates, the audit columns ignored, and the first 10 groups by key.

### Objects that change data

- For each table, the views, procedures, functions, and triggers stored in the database that insert, update, or delete its rows, as SQL Server binds them (the export's `moduleDependencies`), with how many stored objects use the table at all.
- Then the tables no stored object uses, each with the latest year in its [`dateColumns`](#datecolumns) column. Where an app's queries live in its own code, most tables are on that list, so being on it does not mean a table is unused; the latest year is the stronger sign.
- A reference SQL Server records as neither reading nor writing the table is listed among the writers, marked as a use not recorded: SQL Server cannot bind a statement that also uses a temporary table until it runs, such as `DELETE FROM t WHERE id IN (SELECT id FROM #ids)`, so it records only that the table is referenced.
- **Blind spots are named:** objects running dynamic SQL and objects calling a linked server (when only part of the database is checked, those using a table checked are named and the rest counted), objects SQL Server could not resolve, with why (for its usual error, 2020, the likely causes: something that no longer exists, a temporary table, or another database or server; any other error as SQL Server gave it), objects with no references recorded at all, and objects with no definition to read (CLR, encrypted, or hidden from the login), in which dynamic SQL and linked server calls cannot be looked for. Changes made by application code never reach the database's catalog.
- **Without `VIEW DEFINITION`**, the login sees only the objects it has some permission on, so the result says it is incomplete, and with no objects visible it does not say the database has none.

### Column profile

`info`, context. For every column:

- **Counts:** rows, NULLs, blanks (empty or only spaces), the **number** of distinct values, and the longest text length. Never the text values themselves. Distinct counts follow each column's collation, so under a case-insensitive one `ABC` and `abc` count once.
- **Ranges:** the lowest and highest value for number columns whose values can be shown (see [Which values are shown](#which-values-are-shown)); for any other number column, such as a score or a balance, the lowest or highest is often one person's value. For dates, the lowest and highest **year**.
- **In `report.txt`:** each table's row count, and the columns always NULL, always blank, or holding only one value, and empty tables.
- **In `results.json`:** the full per-column profile, leaving out fields that do not apply to a column's type or have nothing to report (such as the lowest value of a column that is always NULL). A number column whose range is withheld says why in `rangeWithheld`; one shown because of a declaration (a lookup, or an entry under [`safeColumns`](#safecolumns)) carries the reason as `rangeShownBecause`.
- **Not profiled:** computed columns. Types that cannot be compared (such as `xml` and `varbinary`) get NULL counts only.
- **Cost:** one query per table (a few for a wide table, so none passes SQL Server's limit of 4,096 select-list items), which most of the checks above share. It is not a single pass: SQL Server works out each column's distinct count separately, so it reads the table, or a copy it keeps, several times. The console shows how long each table's profile took.

**Design observations** (all `info`):

| Observation                                | What it reports                                                                                                                                                                                                                           | Cost                                                                                                                                  |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| **Text that is always numbers or dates**   | Text columns whose every non-blank value (trimmed) is a number or a date: candidates for a proper type in the new schema. A number is digits with at most one point and an optional leading minus; a date is what `ISDATE` accepts, which follows the session's language and date format. Values that start with 0 are counted separately. Converting a column to a number type would drop those zeros, so columns that have them, such as zero-padded codes, usually need to stay text. Shows each column's current type. Person columns are left out: person IDs are numbers kept as text for their leading zeros, and Person columns already covers them. | Included in the profile read                                                                                                          |
| **Lengths in short code columns**          | For short text columns (20 characters or fewer), how many values have each length, reported when one length holds at least 80% of values but not all of them, so an ID column mixing 8- and 9-character values stands out. Counts only.                                                                    | Included in the profile read                                                                                                          |
| **Text columns with few distinct values**  | Text columns with 2 to 50 distinct values, at least 10 values for each distinct value on average, and no foreign key. A match on counts only, so the report asks for a review of whether each holds codes (a candidate for a lookup table), references to something else, or a sequence. Person columns are left out, since a column changed by a few people matches too. | Included in the profile read                                                                                                          |
| **Lists stored in one column**             | Per text column, how many values contain commas or semicolons, which often means several values packed into one. `max`, `text`, and `ntext` columns are left out, since free text holds commas anyway. Each column's share is given: a small share usually means a few exceptions in a single-value column, and a share near 100% a consistent format such as "Last, First". | Included in the profile read                                                                                                          |
| **Bit columns holding NULL**               | `bit` columns that are NULL on some rows, with how many: three-valued flags, where `WHERE Active = 0` misses the NULL rows. The migration has to decide what NULL means before the new column can be `NOT NULL`. | Included in the profile read |
| **Date and time columns at midnight**      | Columns of a date type that holds a time (`datetime`, `datetime2`, `smalldatetime`, `datetimeoffset`) whose values are all, or at least 95%, at midnight, with how many have a time of day: candidates for `date`. A few values with a time among many at midnight often mean a second writer, such as a procedure storing the current time where the application stores a date only. A column recording when a row changed that is always at midnight means the time is not being kept. Each column shows its earliest and latest year, and those of its values with a time, which show whether such values are still being written. Counts and years only. | Included in the profile read |
| **Zero or negative IDs**                   | Whole-number columns that identify or reference rows, with how many rows hold 0 and how many a negative value: usually "none" or "unknown" rather than a real row, either a reference to row 0 or a placeholder row in the table it points at. A column qualifies by name (ending in `ID` as a word: `Didactic_ID`, `CourseId`, but not `Paid`), foreign key, declared [`valuesMustExistIn`](#valuesmustexistin) lookup, or primary key, and the result says which. Person columns are left out, since [`identifiesOnePerson`](#identifiesoneperson) reports their placeholders, and so are identity columns seeded at 0 or below, whose 0 is an ordinary row. Raises the decision of what 0 or a negative value means in such a column. Counts only. | Included in the profile read |
| **Placeholder text**                       | Text columns where some values are, as a whole, a word that stands in for a missing value: `None`, `N/A`, `NA`, `NULL`, `Unknown`, `TBD`, `TBA`, `test`, `?`, or `-`, ignoring case and surrounding spaces (a word inside longer text, such as "Unknown donor", is not counted). Each column lists the words as stored, case variants apart, with their row counts, and their share of the column's non-blank values. The new schema may use NULL or a lookup row instead; some may be real values, such as a "None" option a person chose, so the result is for review. Person columns are included; [`identifiesOnePerson`](#identifiesoneperson) counts the same words as placeholders. | Counted in the profile read; one more query per column with findings |
| **Tables with no recent rows**             | Tables whose [`dateColumns`](#datecolumns) column's latest year is 2 or more years before the run (the profile keeps years only, so a 2026 run lists 2024 and earlier), and empty tables, dated or not: candidates for tables no longer written, which may not need migrating. Each with its date column, latest year, and row count. A date far in the future, which [Implausible dates](#automatic-checks) flags, raises the latest year and can hide such a table. Tables with no declared date column, or none filled in, cannot be judged unless empty; the result counts them. Excluded tables are not read, so not covered. | From the profile read; no extra queries |
| **Deprecated types**                       | Columns of `text`, `ntext`, or `image`, which Microsoft has deprecated, with their replacements (`varchar(max)`, `nvarchar(max)`, `varbinary(max)`). | Metadata only |
| **Related columns with different types**   | Columns that hold the same thing in different tables but with different types, such as a MothraId that is `varchar(8)` in one table and `nchar(10)` in another, so joins between them need conversions. When a group includes `char` or `nchar`, the result says that these pad shorter values with spaces: SQL comparisons ignore the padding, but C# comparisons after loading do not, and copying the values into a `varchar` or `nvarchar` column keeps the spaces, so the migration should trim them when reading. Limited to columns that get joined: person ID columns, grouped by ID type whatever their names, and columns named like a key (primary, unique, identity, or either side of a foreign key) in some table, grouped by name ignoring case and underscores. Generic names such as `ID` are left out. Also the column pairs that declarations compare or join (`valuesMustExistIn`'s `columns` and `otherColumns`, `matchesOtherTable`'s `joinOn` and compared columns), listed as pairs, which catches related columns with different names, such as a reference column and the differently named key it points at; a pair whose columns already share a group is reported there. | Metadata only |
| **Repeating column groups**                | Columns in one table whose names differ only by a number (`Choice1`, `Choice2`, `Choice3`, or `Phone1Type`, `Phone2Type`): candidates for a separate table with one row per item.                                                                                        | Metadata only                                                                                                                         |
| **Tables that look like copies**           | Tables whose columns (name, ignoring case, and type with its length or precision) are all in another table that has at most 2 more: backups, snapshots, and leftover copies, whatever their names say. Nullability, identity, defaults, and keys are not compared, since `SELECT INTO` and hand-made backups drop or change them. Matching tables are grouped, joining chains of matches, and each table shows its approximate row count from the export, the latest year in its [`dateColumns`](#datecolumns) column, and the columns it has beyond those the whole group shares. Tables of fewer than 3 columns are left out, since small link tables match by chance. Tables under [`exclude`](#exclude) are compared too and marked, which confirms why they were excluded; tables outside [`include`](#include) are not. Copies with renamed columns, copies missing more than 2 columns, and copies in other databases are not found, and the rows themselves are not compared. | Metadata, plus the latest year from the profile read |
| **Person columns**                         | Columns in tables and views that hold a person, built from `personIdColumns` in the schema export: those whose names contain a word for an ID type (`mothra`, `iam`, `pidm`), or whose foreign key references such a column, and those whose names say who did something (`ModBy`, `CreatedBy`, `UpdatedBy`, `addUser`, `Who_Mod`: a verb followed by "by" or "user", or "who" followed by a verb) but not with which ID (ID type "not stated", for review). For finding everything that depends on person identifiers before they change. Each column's shape gives how many non-blank values are not numbers, so a column mixing ID types, as Effort's `audit_ModBy` mixed MothraIds and LoginIds, or holding placeholders, stands out without a declaration; a declared [`identifiesOnePerson`](#identifiesoneperson) check says what those values are. Views are not read (see [Views in declared checks](#common-fields-on-every-check)), so a view's person column shows "view, not read"; an `identifiesOnePerson` declaration on it checks its values. | Metadata, plus counts from the profile read (tables only) |

**About cost.** The full reads take shared locks, which can briefly delay a live application's writes. The analysis reads without locks (`READ UNCOMMITTED`): counts may be slightly off while the application is mid-write, which is acceptable for analysis. Running against a Test copy avoids the issue entirely. For a database of a few million rows, all automatic checks together should take minutes, not hours.

## The declarations file

One file per legacy database, committed at:

```
web/Scripts/LegacyMigration/Areas/<ConnectionName>/analysis.json
```

The folder name is the same connection name used for the schema export, so the analysis finds the file automatically.

**Projects.** A database can hold several projects, such as the many schemas in VIPER, or the separate apps' tables in SIS. Each can have its own file, named by a project folder:

```
web/Scripts/LegacyMigration/Areas/<ConnectionName>/<Project>/analysis.json
```

The project name is the launchers' optional third argument (`RunAnalysis.bat VIPER Production PhoneLists`), and its results and suggestions go to their own folder, `Output\<ConnectionName>\analysis\<Environment>\<Project>\`. The schema export is shared by every project of a connection. A project's file usually starts with an [`include`](#include) naming its schema or tables. When a connection has project folders but no `analysis.json` of its own, a run without a project is refused, since with no declarations it would check every table, including those each project leaves out on purpose.

Top-level shape:

```json
{
  "include": [ ... ],
  "exclude": [ ... ],
  "dateColumns": { ... },
  "automaticChecks": [ ... ],
  "safeColumns": [ ... ],
  "checks": [ ... ]
}
```

All six are optional. An empty file (or no file) runs only the automatic checks, on every table and view, with their default severities.

### `include`

The tables, views, and whole schemas the automatic checks visit. Without it they visit every table and view in the database. Use it when the database holds more than the migration moves: a legacy database shared with other apps, a database holding sensitive records the migration does not touch, or one project's schema inside a large shared database such as VIPER.

```json
"include": [
  "[dbo].[tb_CareerSelection]",
  "[dbo].[LK_careers]",
  "[cts]"
]
```

An entry is a table or view, written `[schema].[name]`, or a schema, written `[schema]`, which takes in every table and view in it. Every entry must exist in the schema export, and an empty list is an error rather than "everything", so the file can only narrow a run.

- **`exclude` still applies** inside it, for backups in an included schema.
- **Declared checks are not limited by it.** A declaration names its tables on purpose, such as a `valuesMustExistIn` lookup into a shared table, so it may name any table that is not excluded. A `noDuplicates` declaration on a table outside `include` reports only its own part of [Duplicate rows](#automatic-checks), without the exact-copy part.
- **`automaticChecks` entries** must name included objects, since nothing would use them otherwise. `safeColumns` entries may name any table or view, since declared checks reach outside it.
- **The schema export is not limited by it.** It reads only the catalog, never table data, and declarations must be able to name tables in other schemas.

The top of `report.txt` lists the entries and how many tables and views the automatic checks visited, such as "4 of 312 checked automatically (308 not in the include list)", with an excluded count when anything included is also excluded; `results.json` holds the same counts.

### `exclude`

Tables and views to leave out of every check, automatic and declared, except that [Tables that look like copies](#automatic-checks) still compares their columns (without reading them) and marks them excluded, since a match confirms why they were left out. Use it for backups and abandoned objects, which would otherwise produce results nobody acts on.

```json
"exclude": [
  "[dbo].[tblCourses_backup]"
]
```

Names are written as `[schema].[name]`, exactly as they appear in the schema export.

### `dateColumns`

For each table, the one column that says when a row was created or happened. When given, every result for that table reports the **earliest and latest** affected row, and Rows differing only in audit columns ignores it, as the [automatic checks](#automatic-checks) describe.

```json
"dateColumns": {
  "[dbo].[tblPercent]": "percent_modifiedOn"
}
```

**Optional**, and it cannot be inferred reliably: a table can have several date columns that mean different things (created, modified, the start of the thing the row describes), and only someone who knows the table can pick the one that dates the row. The [suggestions](#suggestions) guess one per table from the column names, for you to confirm.

Why it matters: a result like "38 duplicate placements, dated 2019-03 to 2021-06" says the problem stopped years ago, while "dated 2019-03 to 2026-09" (last month) says the legacy app is still creating it. Without `dateColumns` the analysis still runs; results just lack that time range.

### `safeColumns`

Columns whose values the report may show wherever they appear (row keys, values a check found, the column profile's lowest and highest value), because they hold codes (a term code, a course number, a status) rather than anything personal. Columns the schema already shows to be safe need no entry (see [Which values are shown](#which-values-are-shown)); the report's **VALUES DESCRIBED BY SHAPE** list names the columns a run described by shape, to choose from. Every entry needs a `reason`, printed beside the column's range in `results.json` as `rangeShownBecause`.

```json
"safeColumns": [
  {
    "table": "[dbo].[<table>]",
    "column": "<course number column>",
    "reason": "Course numbers are codes shared by every student in a course."
  }
]
```

| Field          | Required | Meaning                                                                                                                 |
| -------------- | -------- | ----------------------------------------------------------------------------------------------------------------------- |
| `table`        | Yes      | The table or view                                                                                                       |
| `column`       | Yes      | The column                                                                                                              |
| `reason`       | Yes      | Why its values are not personal, in one sentence                                                                        |
| `looksPersonal`| No       | Required when the column's name looks personal (see [Which values are shown](#which-values-are-shown)): why its values are safe anyway |

A column used in a declared `valuesMustExistIn` lookup into a column the database made is shown without an entry, as a foreign key column is (a text column only its values of digits); its `rangeShownBecause` names the lookup.

> **Caution:** a column listed here has its values copied from the database into the report wherever a check reaches it. Only list columns whose values say nothing about a person.

### `automaticChecks`

Changes how an [automatic check](#automatic-checks) treats one table or view: a different severity, or skipping it. Every entry needs a `reason`, which is printed in the report so nobody wonders later why a result is missing or downgraded.

```json
"automaticChecks": [
  {
    "automaticCheck": "Duplicate rows",
    "table": "[dbo].[<audit table>]",
    "severity": "info",
    "reason": "Audit rows repeat when the same change is made twice."
  },
  {
    "automaticCheck": "Unusual characters",
    "table": "[dbo].[<table copied from another system>]",
    "skip": true,
    "reason": "These values are copied from another system and checked there."
  }
]
```

| Field            | Required         | Meaning                                                |
| ---------------- | ---------------- | ------------------------------------------------------ |
| `automaticCheck` | Yes              | The check name, exactly as in the tables above         |
| `table`          | Yes              | The table or view it applies to                        |
| `severity`       | One of these two | The severity to use instead of the default             |
| `skip`           | One of these two | `true` to leave this check out for this table          |
| `reason`         | Yes              | Why, in one sentence                                   |

Five checks report one result across tables rather than one per table, so they cannot be overridden for one table, and an entry naming one is an error: Related columns with different types, Tables that look like copies, Tables with no recent rows, Objects that change data, and Rules the schema enforces.

### Common fields on every check

| Field         | Required                                            | Meaning                                                                                                                                      |
| ------------- | --------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `check`       | Yes                                                 | Which check to run (see the [reference](#declared-checks-reference))                                                                         |
| `description` | No, but recommended                                 | One sentence on why this check exists. Printed with its results, so someone who did not write the check knows what failed and why it matters |
| `table`       | Yes, except for `customSql`                         | `[schema].[table]`, or a view (see below). A [`customSql`](#customsql) check reads the tables its query names instead                        |
| `severity`    | No                                                  | `critical`, `warning` (default), or `info` (see [Severity](#severity))                                                                       |

Every table and column named in a declaration must exist in the schema export, except a `customSql` check's `keyColumns` and `occurredAtColumn`, which name the query's own columns and are checked when it runs (see [`customSql`](#customsql)). A misspelled name stops the analysis before anything runs (see [Safety](#safety)).

**Views in declared checks.** `table` can name a view. Automatic checks never read views (a view stores no data of its own, so profiling one mostly re-reads its tables and repeats their findings, and a view reading another database, as many legacy views read AAUD, would profile data outside the migration), but a declared check on a view can catch what no table check sees: for example, `noDuplicates` on a view's output finds joins that multiply rows. Keep in mind that a check on a view runs the view's whole query, which can be slow for views that read other databases.

## Declared checks reference

Each check below answers one question. The examples use real tables and columns from the existing Effort, CareerSelection, and PhoneLists migrations, showing how their hand-written checks would be declared instead.

### `valuesMustExistIn`

**Asks:** does every value in these columns exist in that other table?

Unenforced foreign keys are already checked [automatically](#automatic-checks). Declare this for relationships the schema never declared at all.

| Field          | Required | Meaning                                                |
| -------------- | -------- | ------------------------------------------------------ |
| `columns`      | Yes      | Column(s) in `table` holding the reference             |
| `otherTable`   | Yes      | The table the values should exist in                   |
| `otherColumns` | Yes      | The matching column(s), in the same order as `columns` |

```json
{
    "check": "valuesMustExistIn",
    "description": "Every effort record must point at a course that exists.",
    "table": "[dbo].[tblEffort]",
    "columns": ["effort_CourseID"],
    "otherTable": "[dbo].[tblCourses]",
    "otherColumns": ["course_ID"],
    "severity": "critical"
}
```

**Behavior:** a NULL value is not counted as missing. With several columns, a row with any NULL among them is skipped, matching how SQL Server treats multi-column foreign keys. Text values are compared both exactly and after trimming spaces and ignoring case, and values that match only the second way are reported separately.

**Reports:** for values with no match, and separately for values that match only after cleanup: how many values and rows, their date range, and up to 50 values, each with how many rows have it, most rows first. A value is shown only when its columns' values can be (see [Which values are shown](#which-values-are-shown)); any other is described by shape (such as "letters only, 3 chars"), and its rows are listed by key. When `otherColumns` were made by the database (an identity), `columns` count as a foreign key into them, so their values are shown everywhere.

**Existing equivalents:** Effort's checks of `tblCourses.course_TermCode` against `tblStatus.status_TermCode`, `tblEffort.effort_Role` against `tblRoles.Role_ID`, and `tblPercent.percent_TypeID` against `tblEffortType_LU.type_ID`; CareerSelection's checks of `career`, `firstSpecies`, `secondSpecies`, and `postGrad` against their lookup tables.

### `noDuplicates`

**Asks:** do any rows share the same values in these columns?

| Field                     | Required             | Meaning                                                                             |
| ------------------------- | -------------------- | ----------------------------------------------------------------------------------- |
| `columns`                 | Yes                  | The column(s) that together should be unique                                        |
| `ignoreCase`              | No (default `false`) | Treat `ABC` and `abc` as the same                                                   |
| `ignoreSurroundingSpaces` | No (default `false`) | Treat `ABC` and `ABC` with a leading or trailing space as the same                  |
| `ignoreLeadingZeros`      | No (default `false`) | Treat `0123` and `123` as the same, for IDs stored as text that will become numbers |

```json
{
    "check": "noDuplicates",
    "description": "A person has one record per term.",
    "table": "[dbo].[tblPerson]",
    "columns": ["person_MothraID", "person_TermCode"],
    "severity": "critical"
}
```

**Behavior:** text compares exactly by default: case, accents, and trailing spaces all count. Rows with a NULL in any of the columns are skipped (use `notBlank` for those). With `ignoreLeadingZeros`, a value of only zeros counts as `0`.

**Reports:** as a part of the table's [Duplicate rows](#automatic-checks) result, not on its own, so rows that are exact copies are not reported twice. The repeated combinations split in two. Those whose rows differ in some other column (conflicting versions of one record): how many groups and rows, their date range, how many groups differ in each combination of columns (up to 10 combinations), and the 10 groups with the most rows, one per line, each with its rows by key and the columns that differ (never their values). A column whose values differ only in case or trailing spaces is named that way, such as `notes (case or trailing spaces only)`. Those whose rows hold one value in every other column, byte for byte or differing only in case or trailing spaces: when the table has an exact-copy part, they are already there, so they are only mentioned (such as "40 of the 50 groups sharing it; the other 10 are exact copies, above"); otherwise they are counted and listed here, exact copies and copies differing only in case or trailing spaces apart.

Each group's repeated values are shown as its columns' values are (see [Which values are shown](#which-values-are-shown)), and described by shape otherwise.

**Existing equivalents:** Effort's `tblPerson` (`person_MothraID`, `person_TermCode`) and `tblCourses` (`course_CRN`, `course_TermCode`, `course_Units`); CareerSelection's `tb_CareerSelection.PIDM`, including PIDMs that only collide once read as numbers (`ignoreLeadingZeros`), and its lookup labels compared ignoring case (`ignoreCase`).

### `notBlank`

**Asks:** is this column ever NULL, empty, or only spaces?

The column profile already reports every column's blank count. Declare `notBlank` for columns that must never be blank, so a violation is reported as a problem rather than just a number in the profile.

| Field     | Required | Meaning                                      |
| --------- | -------- | -------------------------------------------- |
| `columns` | Yes      | One or more columns, each checked separately |

```json
{
    "check": "notBlank",
    "description": "The migration needs a name and department for every person.",
    "table": "[dbo].[tblPerson]",
    "columns": ["person_FirstName", "person_LastName", "person_EffortDept"]
}
```

**Reports:** per column, as its own result: how many rows are NULL and how many are blank, the affected rows' keys, and their date range.

**Existing equivalents:** Effort's required-field checks (the example above, plus `tblCourses.course_SubjCode` and `tblEffort.effort_Role` / `effort_SessionType`); CareerSelection's lookup labels; PhoneLists' `SVM_Phones.SectionID` and `unitID`.

### `withinRange`

**Asks:** is every value within a minimum and/or maximum?

| Field         | Required     | Meaning                                                                                  |
| ------------- | ------------ | ---------------------------------------------------------------------------------------- |
| `column`      | Yes          | A number or date column                                                                  |
| `min` / `max` | At least one | The limits, inclusive: a value equal to a limit is within range. Dates written as `"2000-01-01"` |

```json
{
    "check": "withinRange",
    "description": "A percentage of effort must be between 0 and 100.",
    "table": "[dbo].[tblPercent]",
    "column": "percent_Percent",
    "min": 0,
    "max": 100
}
```

**Behavior:** NULLs are ignored (use `notBlank` for those). Text columns are rejected, because comparing text alphabetically gives misleading results (`"9"` sorts after `"10"`).

**Reports:** how many rows fall below and above the limits, the affected rows' keys, and their date range. When the column's values can be shown (see [Which values are shown](#which-values-are-shown)), also the lowest value found below the range and the highest above it.

> **Caution:** values are reported next to their rows' keys, so each belongs to a row and often a person. List a column under `safeColumns` only when it says nothing personal (percentages, hours, units), never for scores, grades, balances, or counts of absences and the like.

**Existing equivalents:** Effort's percentage checks (the example, plus `person_PercentAdmin` and `person_PercentClinical`), `effort_Hours`, `effort_Weeks`, and `course_Units`.

### `startBeforeEnd`

**Asks:** in each row, does the start come before (or on) the end?

For tables with a pair of columns describing a period, such as a start date and an end date, or a start and end period ID that sort in time order. A row where the end comes first usually means the values were swapped or mistyped.

| Field         | Required | Meaning                      |
| ------------- | -------- | ---------------------------- |
| `startColumn` | Yes      | The column holding the start |
| `endColumn`   | Yes      | The column holding the end   |

```json
{
    "check": "startBeforeEnd",
    "description": "A period cannot end before it starts.",
    "table": "[dbo].[<table>]",
    "startColumn": "<start column>",
    "endColumn": "<end column>"
}
```

**Behavior:** both columns must be dates or both numbers; text is rejected, since it compares alphabetically. Rows where either column is NULL are skipped, and a start equal to the end is fine.

**Reports:** how many rows end before they start, their keys, and their date range.

None of the existing migrations has an equivalent.

### `listValues`

**Asks:** what values does this column actually contain?

| Field           | Required             | Meaning                                                                           |
| --------------- | -------------------- | --------------------------------------------------------------------------------- |
| `column`        | Yes                  | A code-like column (a status, type, or category)                                  |
| `allowedValues` | No                   | The values that are valid, as strings. When given, every other value is reported as a problem. For a number column each must be a number, and for a bit column `0`, `1`, `true`, or `false` |
| `ignoreCase`    | No (default `false`) | Compare text with `allowedValues` ignoring case                                   |
| `looksPersonal` | No                   | Required when the column's name looks personal: why its values are safe anyway, as for [`safeColumns`](#safecolumns) |

```json
{
    "check": "listValues",
    "description": "Interim must map onto the new Acting/Interim/Vice values.",
    "table": "[dbo].[SVM_Phones]",
    "column": "InterimAdmin"
}
```

**Behavior:** text compares with `allowedValues` as written, with the padding of `char` and `nchar` columns dropped. A number or bit column compares by value, so `"1"` matches a stored `1.00`, and a bit matches `"1"` or `"true"`; an entry that is not a number (or, for a bit, not `0`, `1`, `true`, or `false`) is a declaration error, since it could never match. Numbers are listed as SQL Server writes them, without the trailing zeros of the column's scale, and a bit as `0` or `1`.

**Reports:** each distinct value and how many rows have it, most common first. Text values are quoted, so leading and trailing spaces show, and are grouped exactly: `Interim`, `interim`, and `Interim ` are listed separately even under a case-insensitive collation. NULL and blank are shown as `<null>` and `<blank>`, and are not checked against `allowedValues` (use `notBlank` for those). At most 100 values are listed. When the table has a [date column](#datecolumns), each value also shows the earliest and latest date of its rows, so a value no longer being written stands out. Without `allowedValues` nothing can fail, so the listing is reported as context with severity `info`.

> **Caution:** this check copies the column's actual values from the database into the report: declaring it is the approval to show them, in this check only. Only use it on columns holding codes, never on names or free text.

**Existing equivalents:** PhoneLists' `InterimAdmin` and `InterimDirector`; CareerSelection's `classYear`; Effort's `effort_SessionType` checked against its list of valid session types (with `allowedValues`), and course relationship types checked against parent/child/crosslist/section.

### `canConvertTo`

**Asks:** will every value in this column convert to this type?

For columns stored as text that the new schema will store as a number or date.

| Field    | Required | Meaning                                                      |
| -------- | -------- | ------------------------------------------------------------ |
| `column` | Yes      | The column to test                                           |
| `type`   | Yes      | One of `int`, `bigint`, `date`, `datetime`, `bit`, or `decimal(precision, scale)` such as `decimal(10, 2)`. A bare `decimal` is not accepted: it means `decimal(18, 0)`, which rounds fractions instead of failing on them |

```json
{
    "check": "canConvertTo",
    "description": "Roles are stored as text but the new schema uses an int foreign key.",
    "table": "[dbo].[tblEffort]",
    "column": "effort_Role",
    "type": "int"
}
```

**Behavior:** text columns only, since a number or date column converts silently (`1.5` becomes `1` as an `int`). NULLs and blanks are ignored. Uses SQL Server's own conversion (`TRY_CAST`), so the result matches what a conversion in SQL would do.

**Reports:** how many rows and distinct values will not convert, the rows' keys and date range, and the values themselves, most rows first, when the column's values can be shown (see [Which values are shown](#which-values-are-shown)); otherwise the values described by shape (such as "letters only, 3 chars").

**Existing equivalents:** Effort's `effort_Role` (the example); CareerSelection's `PIDM`.

### `matchesPattern` and `doesNotMatchPattern`

**Asks:** is every value in this column in the expected format (`matchesPattern`)? Does any value look like something it should not be (`doesNotMatchPattern`)?

For formats a list of values cannot express, such as term codes, academic years, or email addresses, and for values that give themselves away, such as test data.

| Field        | Required             | Meaning                                                                                                                         |
| ------------ | -------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `column`     | Yes                  | The column to test                                                                                                              |
| `patterns`   | Yes                  | SQL Server `LIKE` patterns (`%` any text, `_` one character, `[0-9]` one character from a set). `matchesPattern` flags values matching none of them; `doesNotMatchPattern` flags values matching any of them |

```json
{
    "check": "matchesPattern",
    "description": "Academic years are written as 2019-2020.",
    "table": "[dbo].[tblPercent]",
    "column": "percent_AcademicYear",
    "patterns": ["[12][0-9][0-9][0-9]-[12][0-9][0-9][0-9]"]
}
```

```json
{
    "check": "doesNotMatchPattern",
    "description": "Last names that look like test data.",
    "table": "[dbo].[tblPerson]",
    "column": "person_LastName",
    "patterns": ["%test%", "%demo%", "%xxx%", "%delete%"]
}
```

**Behavior:** text columns only. NULLs and blanks are ignored. Matching is SQL Server's own `LIKE`, under the column's collation, so in a case-insensitive database `%test%` also matches `Test`. The padding of `char` and `nchar` columns is trimmed before matching; other trailing spaces count (Leading or trailing spaces reports them). `LIKE` has no repetition, so a format whose length varies needs one pattern per length.

**Reports:** how many rows and distinct values are flagged, the rows' keys and date range, and the values described by shape (such as "digits only, 8 chars"; a value holding an `@` is described by what keeps it from being an email address, such as "contains @, with no dot after the @"). `doesNotMatchPattern` with several patterns also counts the rows matching each; a value can match more than one. When the column's values can be shown (see [Which values are shown](#which-values-are-shown)), the values themselves, most rows first, instead of their shapes.

**Existing equivalents:** Effort's academic years (the first example), its test-data patterns in names (the second), and the migration's rule for course relationship types (Parent, Child, CrossList, Section, or anything containing "cross" or "list").

### `matchesOtherTable`

**Asks:** does a copied column still agree with the column it was copied from?

Legacy tables often store a copy of a value that also lives in a related table (for example, a code copied onto each row so reports do not need a join). Over time the copy and the original can drift apart, and a migration has to decide which one to trust.

| Field         | Required             | Meaning                                                                                            |
| ------------- | -------------------- | -------------------------------------------------------------------------------------------------- |
| `column`      | Yes                  | The copied column in `table`                                                                       |
| `otherTable`  | Yes                  | The table holding the original                                                                     |
| `joinOn`      | Yes                  | How rows match: a list of `{ "column": ..., "otherColumn": ... }` pairs                            |
| `otherColumn` | Yes                  | The original column in `otherTable`                                                                |

```json
{
    "check": "matchesOtherTable",
    "description": "<the copied column> should always equal <the original column> on the related row.",
    "table": "[dbo].[<table>]",
    "column": "<copied column>",
    "otherTable": "[dbo].[<other table>]",
    "joinOn": [{ "column": "<key column>", "otherColumn": "<other key column>" }],
    "otherColumn": "<original column>"
}
```

**Behavior:** for each row of `table`, finds the row of `otherTable` whose `joinOn` columns are equal, then compares `column` with `otherColumn`. Both must be text, both numbers, or both dates. Two NULLs agree; NULL against a value is reported on its own (see below). Text compares exactly except for trailing spaces, which SQL Server ignores. Rows with a NULL in a `joinOn` column are skipped.

**Reports:** how many rows disagree, their keys, and their date range. Separately, each with its rows' keys: values that differ only in case or surrounding spaces (the copy needs cleaning, not a decision), rows NULL in `table` with a value in `otherTable` and the reverse (usually a copy never filled in, rather than drift; still a problem, since the migration must pick a side), rows with no matching row in `otherTable` (a missing reference, not a disagreement), and rows matching several rows in `otherTable` (nothing single to compare with). When the values of both `column` and `otherColumn` can be shown (see [Which values are shown](#which-values-are-shown)), also the 10 differing value pairs shared by the most rows.

None of the existing migrations has an equivalent.

### `oneValuePerKey`

**Asks:** when a value is repeated on every row that shares a key, is it the same on all of them?

| Field        | Required             | Meaning                                                                            |
| ------------ | -------------------- | ---------------------------------------------------------------------------------- |
| `keyColumns` | Yes                  | The column(s) that group the rows                                                  |
| `columns`    | Yes                  | Columns that should hold a single value within each group, each checked separately |

```json
{
    "check": "oneValuePerKey",
    "description": "UnitOrder and Fax are stored on every row of a unit and should be the same on all of them.",
    "table": "[dbo].[SVM_Phones]",
    "keyColumns": ["SectionID", "unitID"],
    "columns": ["UnitOrder", "Fax"]
}
```

**Behavior:** a NULL next to a value counts as a difference. Values compare as SQL Server compares them (the column's collation, trailing spaces ignored). Rows with a NULL in any key column belong to no key and are skipped. Each column is reported as its own result.

**Reports:** how many keys have more than one value in a column, and for up to 10 of them, the key's rows split into groups that share a value: each group's row count, row keys, and date range. When one group is larger than every other, it is only counted, and the smaller groups are the rows to look at; an even split lists every group. The values themselves are shown only when the column's values can be (see [Which values are shown](#which-values-are-shown)); NULL is always named.

```
Keys with more than one Fax value (2):
  SectionID 2, unitID 2: 5 rows, 2 different Fax values
    4 rows: the most common value (dated 2014-02 to 2019-07)
    1 row: a different value, phoneID 1234 (dated 2017-05)
  SectionID 2, unitID 3: 4 rows, 2 different Fax values
    2 rows: one value, phoneID 1240, 1241 (dated 2015-01 to 2016-03)
    2 rows: another value, phoneID 1242, 1243 (dated 2021-08)
```

The `keyColumns` values are shown as those columns' values are, and described by shape otherwise.

**Existing equivalent:** PhoneLists' unit consistency check (the example). Unless `Fax` is listed under `safeColumns`, which it should not be, the Fax numbers themselves stay out of the report, unlike the PhoneLists report.

### `sameKeysAs`

**Asks:** do two tables contain the same set of keys?

| Field          | Required | Meaning                                       |
| -------------- | -------- | --------------------------------------------- |
| `columns`      | Yes      | The key column(s) in `table`                  |
| `otherTable`   | Yes      | The table to compare with                     |
| `otherColumns` | Yes      | The matching key column(s), in the same order |

```json
{
    "check": "sameKeysAs",
    "description": "dvtUnit should describe exactly the units in SVM_Phones, or it cannot be dropped.",
    "table": "[dbo].[dvtUnit]",
    "columns": ["dvtUnit_sectionID", "dvtUnit_unitID"],
    "otherTable": "[dbo].[SVM_Phones]",
    "otherColumns": ["SectionID", "unitID"]
}
```

**Behavior:** keys with a NULL in any column are skipped, as SQL Server does for a multi-column foreign key. Text columns with different collations are compared using the database default.

**Reports:** keys found only in `table`, and keys found only in `otherTable`, each side with the date range of its rows from its own [date column](#datecolumns). Key values are shown as their columns' values are (see [Which values are shown](#which-values-are-shown)), as in row keys.

> **Caution:** the key values are copied from the database into the report. Key columns must be codes, surrogate keys, or numeric person IDs (MothraId, IAM ID, PIDM, PersonId), never names or LoginIds.

**Existing equivalent:** PhoneLists' `dvtUnit` coverage check (the example).

### `identifiesOnePerson`

**Asks:** does each ID in this column identify exactly one person?

| Field    | Required | Meaning                                                                          |
| -------- | -------- | -------------------------------------------------------------------------------- |
| `column` | Yes      | The column holding person IDs                                                    |
| `idType` | Yes      | What the column is meant to hold: `MothraId`, `IamId`, `PersonId` (the key of VIPER's `users.Person`, which is AAUD's `aaudUserID`), `Pidm` (Banner's person number), `LoginId`, or `MailId` |
| `lookIn` | Yes      | Where to look people up: `VIPER.users.Person` or `AAUD.dbo.aaudUser`. `users.Person` holds no PIDM, so `Pidm` needs `AAUD.dbo.aaudUser` |

```json
{
    "check": "identifiesOnePerson",
    "description": "Every mentor must be a person the app can look up.",
    "table": "[dbo].[tb_CareerSelection]",
    "column": "facultyMothraID",
    "idType": "MothraId",
    "lookIn": "AAUD.dbo.aaudUser"
}
```

**What happens when it runs:** the analysis reads each distinct ID in the column, looks it up where `lookIn` says, and puts it in exactly one category describing what the lookup found. Nothing is changed or chosen.

| Category                         | Meaning                                                                                                          | A problem?                                                                   |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| **Found one person**             | Exactly one person matches                                                                                       | No                                                                           |
| **Not found**                    | No person matches                                                                                                | Yes: the migration cannot link these rows to anyone                          |
| **Found several people**         | More than one person record matches (`aaudUser` can hold several rows for one MothraId)                          | Yes: someone has to decide which is meant. The analysis does not choose      |
| **Found only after cleanup**     | Matches one person only after trimming spaces or ignoring case                                                   | Yes: the stored values need trimming, or the migration has to normalize them |
| **Found only after cleanup, several people** | As above, but the cleaned value matches more than one person. A person found under two spellings of the value counts once | Yes: the values need cleaning, and then someone has to decide which person is meant |
| **Placeholder**                  | Blank, all zeros such as `00000000`, or a placeholder word, the same list as [Placeholder text](#automatic-checks) (`None`, `N/A`, `NA`, `NULL`, `Unknown`, `TBD`, `TBA`, `test`, `?`, `-`) | Usually not, but counted so it is visible                                    |
| **Found as a different ID type** | Does not match as the declared `idType`, but does match as another, naming one person (for example, a LoginId in a MothraId column). The other types are MothraId, IAM ID, LoginId, and MailId; each value is tried as every one it is shaped like (see below), never as a PersonId or PIDM, since both are dense runs of numbers that MothraIds and other numbers share the shape of, so a match would usually be a coincidence. A value can match one person by more than one type (a LoginId that is also that person's MailId) | Yes: the column mixes ID types, or holds a different type than declared |
| **Found as a different ID type, several people** | As above, but the matches name more than one person, by one type or across types (one person's LoginId and another's MailId) | Yes: the migration cannot tell which person is meant, and resolving by trying the types in order would pick one silently |

**Padded columns.** In a `char` or `nchar` column every value shorter than the column carries trailing padding, so it says nothing about the data: IDs are matched without it, and the result says so once, with what the migration has to do (trim the IDs when reading them, since C# comparisons after loading and copies into `varchar` or `nvarchar` keep the spaces).

**Which lookup to use.** `VIPER.users.Person` asks whether the migration can link each row to a person in VIPER, which is usually the question before migrating: an ID that is fine in AAUD but missing from `users.Person` still breaks the migration. `AAUD.dbo.aaudUser` asks whether each ID names a real person by the source most legacy apps use, which is a legacy data integrity question.

**Reaching the lookup table.** `VIPER.users.Person` is read over VIPER's own connection (`ConnectionStrings:VIPER`, for the same environment as the analysis), since legacy logins cannot be assumed to read VIPER; the PhoneLists login cannot. It opens only when a check needs it, read-only like the legacy connection. `AAUD.dbo.aaudUser` is read by three-part name (`[AAUD].[dbo].[aaudUser]`) over the legacy connection, so AAUD must be on the same server and readable by the legacy login. Each result says where it looked.

**Shapes.** Each ID type has a shape, checked after trimming spaces:

| `idType`   | Shape               | Cleanup beyond trimming and ignoring case                                   |
| ---------- | ------------------- | --------------------------------------------------------------------------- |
| `MothraId` | Up to 8 digits      | Left-padding with zeros to 8 digits, which catches MothraIds stored as numbers |
| `IamId`    | 10 digits           | None                                                                        |
| `PersonId` | A positive whole number | Dropping leading zeros                                                  |
| `Pidm`     | Up to 8 digits      | Dropping leading zeros, which catches PIDMs stored with them (`aaudUser.PIDM` is text) |
| `LoginId`  | Any text            | None                                                                        |
| `MailId`   | Any text            | None                                                                        |

**Cleanup** means trimming spaces, ignoring case, and the type's own cleanup above. NULLs are counted but not looked up.

**MailIds** are compared as full addresses on both sides, since a column, or `aaudUser.mailID`, can hold either a mailbox alone or the full address: a mailbox with no domain counts as a `@ucdavis.edu` address. Both forms are proper ways to write a MailId, so a match through them is found exactly, not after cleanup. The domain must match: `jdoe@gmail.com` is not the UC Davis mailbox `jdoe`. When a column holds full addresses, the result says how many, so a not-found address at another domain is not mistaken for a missing person.

LoginIds and MailIds are often derived from people's names, so they are never shown, even in a column declared to hold them: values that do not match are described by shape and listed by row key. Declaring one is how to test what a text column is believed to hold, after a run with another `idType` has suggested it.

**Reports:**

- **Per category**, how many distinct IDs and how many rows; empty categories are left out.
- **For the problem categories, the IDs themselves**, but only IDs shaped like the declared type, and never LoginIds or MailIds, so a column holding names, LoginIds, or phone numbers cannot leak them into the report (a 10-digit phone number in a MothraId column is withheld; in an `IamId` column it has the right shape and would show).
- **Other IDs are described by shape** (the kinds of characters they hold and their length, such as "letters and digits, 7 chars"), and their rows are listed by key with their date range. Rows are found by each ID exactly as it was counted, including its length, so an ID's rows never include those of a variant with a trailing space, which may be in another category.
- **Found as a different ID type** (and its several-people form) gives a table of how many IDs and rows matched each combination of types ("LoginId and MailId", "MailId only"), which add up to the whole and show how many matched more than one type. When any did, it adds each type's own coverage ("Any MailId"), so the single type that explains the most values stands out. It reports the affected rows' keys, because those values may be LoginIds or MailIds.
- **Placeholder** shows each value as stored, since blanks, zeros, and the fixed placeholder words reveal nothing about anyone.

**Existing equivalents:** Effort's MothraId checks on `tblPerson`, `tblEffort`, `tblPercent`, and `tblSabbatic` against `users.Person`, and its `tblAudit.audit_ModBy` analysis (which found MothraIds and LoginIds mixed); PhoneLists' `Dean_Director_MothraID`, `Admin_MothraID`, and `vmdoPeople_mothraID`; CareerSelection's `facultyMothraID` (the example). Unlike those scripts, this check never puts names in the report, and never compares legacy names with the people found.

### `customSql`

For a rule the other checks cannot express, such as "X is required when Y" or a count on only the rows that matter: a SQL query, in a file of its own, that returns **one row per problem**.

| Field              | Required | Meaning                                                                                                   |
| ------------------ | -------- | --------------------------------------------------------------------------------------------------------- |
| `name`             | Yes      | Short name shown in the results                                                                           |
| `file`             | Yes      | The `.sql` file, in the same folder as `analysis.json`, named without a folder                            |
| `keyColumns`       | Yes      | The query's columns that identify each row, listed as examples                                            |
| `occurredAtColumn` | No       | A date or time column of the query that dates each row, for the earliest and latest                       |

There is no `table`: the tables are the ones the query reads.

```json
{
  "check": "customSql",
  "name": "Percentages with a start date but no academic year",
  "description": "The rows the migration's analysis counts: those the legacy procedures' filter on the academic year silently skips.",
  "file": "null-academic-year-with-start.sql",
  "keyColumns": ["percent_ID"],
  "occurredAtColumn": "percent_modifiedOn",
  "severity": "critical"
}
```

`null-academic-year-with-start.sql`:

```sql
SELECT p.percent_ID, p.percent_modifiedOn
FROM dbo.tblPercent AS p
WHERE (p.percent_AcademicYear IS NULL OR p.percent_AcademicYear = '')
  AND p.percent_start IS NOT NULL
```

**Rules for the query**, checked with Microsoft's T-SQL parser before anything runs (see [Safety](#safety)):

- It must be a **single `SELECT` statement**. Subqueries, `UNION` / `EXCEPT` / `INTERSECT`, common table expressions (`WITH ... SELECT`), and joins are all fine.
- Not allowed: more than one statement (including `DECLARE` before the `SELECT`; a common table expression usually does the same job), `SELECT ... INTO`, temporary tables, table variables and variables, `NEXT VALUE FOR`, user-defined functions, scalar or table-valued (their code is not part of the query, so what they read or do cannot be checked; methods on a column, such as `t.col.value(...)`, are refused with them, since the two look alike; built-in functions such as `LEN` or `CONVERT` are fine), table hints other than `NOLOCK`, `READUNCOMMITTED`, `READPAST`, `NOWAIT`, `INDEX`, `FORCESEEK`, `FORCESCAN`, and `NOEXPAND` (the others, such as `READCOMMITTEDLOCK` or `UPDLOCK`, take locks or wait for them, which the analysis's reads avoid), and anything that reaches another server or a file (`OPENQUERY`, `OPENROWSET`, `OPENDATASOURCE`, four-part names).
- Every table and view it reads must be in this database's schema export, written `schema.name` or just `name` (for `dbo`), with or without this database's name in front. Names compare ignoring case, as in the database. Only the names are checked, not a view's own query (see [Safety](#safety)).
- It is parsed as the database's compatibility level.

**Reports:** how many rows the query returned, the first 50 by their key columns, and the earliest and latest `occurredAtColumn`, as year and month. No other column is read from the rows, so a `SELECT *` cannot leak anything into the report. The parser traces each key column to the table column it is selected from, when it is a plain column reference (`t.percent_ID`, `t.percent_ID AS id`, or `percent_ID` when the query reads one table); its values are then shown as that column's are (see [Which values are shown](#which-values-are-shown)). A key that cannot be traced (an expression, a column of a common table expression or derived table, a `UNION`) is described by its shape, and the result says which keys are and how to show them: select the table's column itself. A query whose result lacks a declared column, or whose `occurredAtColumn` is not a date, shows as failed to run.

**Existing equivalents:** Effort's NULL academic years on percentages with a start date (the example) and its blank CRNs only on courses effort records use, both in [`Areas/Effort/`](../Areas/Effort/).

## Suggestions

`RunSuggestions.bat` suggests what the declarations file does not have yet, from the schema export, so nobody starts from a blank page and new tables are noticed after the schema changes. It reads the committed `analysis.json`, if any, and never changes it.

- **Scope comes first.** When the file has no [`include`](#include) and no checks (a new database), the suggestions open with every schema, table, and view and its approximate row count, as a commented `include` to copy from.
- **Guesses cover only included tables**, when the file has an `include`; a guessed relationship may still point to a table outside it.
- **What is already declared is left out:** excluded tables, tables with a date column, and columns already checked by a `valuesMustExistIn` or `identifiesOnePerson` declaration.

It proposes, for the developer to keep, edit, or delete:

- **`exclude`**: tables whose names look like backups (the words `bk`, `bak`, `bkup`, `backup`, `old`, or `copy`, or a date such as `_2022_12_30`).
- **`dateColumns`**: for each table, a date column named for creation (preferring "created" over "inserted", "added", "entered", and "submitted"), else one named for modification, else its only date column; tables with several and none of those names are listed in a comment. Names match by whole words, so `ModuleDate` is not taken for a modification date.
- **Likely relationships**, as `valuesMustExistIn` checks: columns named exactly like another table's single-column primary key (such as `Course_ID` matching `tblCourses.Course_ID`), with no foreign key already on them; generic names such as `ID` and names matching several tables are left out.
- **Likely person ID columns**: the MothraId, IAM ID, and PIDM columns in tables, from [Person columns](#automatic-checks), as `identifiesOnePerson` checks against `VIPER.users.Person`, or `AAUD.dbo.aaudUser` for PIDMs.

They are written to `Output\<ConnectionName>\analysis\<Environment>\analysis.suggestions.json`, read back, and bound against the export before they are reported, so every guess names a table and column that exist.

## Safety

**Declared and automatic checks cannot inject SQL.** Table and column names cannot be passed to SQL Server as parameters, so the analysis has to place them in the query text. Before it does:

1. Every name must exist in the schema export. Anything else stops the analysis before any SQL is built.
2. Every name is bracket-quoted, with any `]` inside it escaped.
3. Every value is sent as a real SQL parameter (`min`, `max`, `patterns`) or compared in memory (`allowedValues`), never placed in query text. The one declared value that is placed in query text is `canConvertTo`'s `type`, and only after it matches the fixed list of type names the check accepts. The analysis's own constants, such as the placeholder words and the years that make a date implausible, are written into query text, quoted; no value read from the database ever is.

**A `customSql` query runs as written, so it is checked instead.** Running a developer's own query is that check's purpose, so it cannot be made safe by quoting. The tools' read-only promise rests on their own code (`ApplicationIntent=ReadOnly` does not block writes on a standalone server), so:

1. The file is parsed with Microsoft's T-SQL parser (ScriptDom) and must be a single `SELECT` that follows the [rules](#customsql). Anything else stops the analysis before the query reaches the database.
2. Every table and view it names must be in the schema export, as for any other declaration, so it cannot name another database's objects. This checks names only: a view's own query is not followed, so a view that reads another database passes, and the query reads that data if the login can. The report can then hold counts from it, and the values of any of the view's columns listed in `safeColumns` (see [Views in declared checks](#common-fields-on-every-check)).
3. Key values reach the report only when traced to a table column whose values can be shown; others are described by shape.
4. The files are committed beside `analysis.json`, so they are reviewed like other code.

The first three are covered by automated tests (`test/LegacyMigration/`), as are binding every declared name to the export and deciding which columns' values are shown. The checks' queries are tested too, against a stand-in database that answers with known rows: values reach SQL only as parameters, and a value in a column with no safe origin never reaches the report.

## What stays in per-area scripts

The declared checks and `customSql` express most of what earlier migrations' analysis scripts checked: CareerSelection's PIDM, label, mentor, and classYear checks ([`Areas/SIS/CareerSelection/analysis.json`](../Areas/SIS/CareerSelection/analysis.json)), and Effort's people, term, reference, required-field, range, code, format, and test-data checks, including its counts on only the rows that matter ([`Areas/Effort/analysis.json`](../Areas/Effort/analysis.json)). These stay in an area's own scripts:

- **Conditions across columns over free text**, such as how each CareerSelection option column records "Other" (no option beside free text, a real option beside leftover text, or the form's placeholder prompt saved as text). Each encoding can be written as a [`customSql`](#customsql) check.
- **Name matching**, such as suggesting VIPER people for IDs not found, by first and last name, as Effort's script does. It would read names from both databases, and exact names give false matches.
- **Migration decisions**, such as which lookup row becomes CareerSelection's catch-all "Other" (`listValues` shows the labels it is decided from), or which Effort audit rows the migration can link to a record.
- **The destination schema**, such as lengths against the destination's columns. The analysis reads only the legacy database; the column profile's longest text measures against the legacy widths.
- **Rules of the new app**, such as whether a mentor is a current affiliate or a student a current DVM student. What "current" means differs between apps.
