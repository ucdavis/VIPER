# Effort Data Migration Analysis Script

## Purpose
This C# script analyzes the Effort database to identify all potential data integrity issues that need to be addressed before migrating to the VIPER database with foreign key constraints.

## What It Analyzes

### 1. MothraId Mapping Issues

- Identifies all MothraIds in Effort tables that don't exist in VIPER.users.Person
- Reports count and percentage of affected records in each table
- Lists specific unmapped MothraIds with their record counts

### 2. Referential Integrity Issues

- Orphaned courses (referencing non-existent terms)
- Effort records without valid courses
- Effort records with invalid roles
- Percentage records without valid person/term combinations

### 3. Business Rule Violations

- Duplicate key violations (e.g., duplicate PersonId+TermCode)
- Required fields with null values
- Check constraint violations (percentages outside 0-100, invalid hours)

### 4. Data Quality Issues

- Suspicious data patterns (test data, demo data)
- Date range analysis (oldest to newest terms)
- Stale or inactive records

## How to Run

### Using the Batch File

```bash
# Navigate to Scripts folder
cd C:\path\to\VIPER3\web\Areas\Effort\Scripts

# Run with Development environment (default)
RunDataAnalysis.bat

# Run with Production environment
RunDataAnalysis.bat Production

# Run with custom config file
RunDataAnalysis.bat Test custom-config.json
```

## Output

The script generates a detailed text report in the `AnalysisOutput` folder:

**`EffortAnalysis_[timestamp].txt`**

- Human-readable detailed report
- Executive summary with risk assessment
- Detailed breakdown of each issue type
- Recommended remediation actions

## Understanding the Results

### Severity Levels

**CRITICAL** - Migration will fail without fixing these:

- Unmapped MothraIds (no corresponding PersonId in VIPER)
- Orphaned records (invalid foreign key references)
- Duplicate key violations
- Required fields with nulls

**HIGH** - Should be fixed but migration might succeed:

- Check constraint violations
- Business rule violations

**WARNING** - Should review but may be acceptable:

- Suspicious data patterns
- Very old data
- Test/demo data

### Risk Assessment

- **HIGH** - More than 100 critical issues
- **MEDIUM** - 50-100 critical issues
- **LOW** - 1-49 critical issues
- **MINIMAL** - No critical issues

## Next Steps After Analysis

Based on the analysis results, you'll need to:

1. **For Unmapped MothraIds:**

   - Option A: Create PersonId entries in VIPER for missing MothraIds
   - Option B: Map to a default migration user
   - Option C: Exclude these records from migration

2. **For Orphaned Records:**

   - Delete orphaned records
   - Create missing parent records
   - Update references to valid values

3. **For Business Rule Violations:**

   - Fill in required fields with default values
   - Merge or delete duplicate records
   - Correct invalid data values

4. **For Data Quality Issues:**

   - Clean up test/demo data
   - Archive old data separately
   - Standardize data formats


---

# Effort Data Remediation Script

> **🛡️ SAFETY FIRST**: This script defaults to **preview mode** (no changes). You must explicitly use `--apply` to modify the database.

## Purpose

This C# script automatically fixes critical data quality issues identified by the EffortDataAnalysis script. It prepares the Efforts database for migration to VIPER by resolving data integrity problems.

**✨ Key Features**:

- **Safe by Default**: Runs in dry-run mode unless you explicitly use `--apply`
- **Idempotent**: Safe to run multiple times - skips already-fixed records
- **Transaction Rollback Dry-Run**: Executes actual SQL in transactions then rolls back (validates SQL correctness)
- **Backup System**: Creates SQL backup files before modifications (when using --apply)
- **Transaction Safety**: All database operations wrapped in transactions for atomicity

## What It Fixes

### Task 1: Fix Duplicate Courses

Identifies courses with duplicate (CRN + Term + Units) combinations and consolidates them:

- Keeps the course with the most effort assignments
- Remaps effort records to the kept course
- Deletes duplicate courses
- **Example**: CRN 83036 Term 201010 Units 15 (2 duplicates) → keeps 1

### Task 2: Fix Missing Department Codes

Fills in missing `person_EffortDept` values:

- Looks up most recent valid department from person's other terms
- Updates to found dept or "UNK" if none found
- **Impact**: Prevents NULL constraint violations in migration

### Task 3: Generate Placeholder CRNs

Assigns sequential placeholder CRNs to all courses with blank/null CRN values:

- **Courses with effort records**: CRN range 90001+ (critical for migration)
- **Courses without effort records**: CRN range 99001+ (cleanup)
- Sequential assignment based on course_id order
- **Impact**: Ensures all courses have valid CRNs for migration

### Task 4: Delete Negative Hours Records

Removes invalid effort records with negative hours:

- Identifies any records where effort_Hours < 0
- **Impact**: Fixes check constraint violations

### Task 5: Delete Invalid MothraIds

Removes records with invalid/corrupted MothraIds:

- Identifies MothraIds that contain invalid characters or patterns
- Examples: `#URL.Mot` (data entry error), invalid IDs from data imports
- **Impact**: Removes unmappable records

### Task 6: Consolidate Guest Instructors

Creates unified guest instructor account:

- Creates VETGUEST person in VIPER if needed
- Remaps various guest instructor accounts to VETGUEST:
  - UNKGuest → VETGUEST
  - VETGuest → VETGUEST
  - VMDOGues → VETGUEST
- **Impact**: Provides valid PersonId for all guest instructors

### Task 7: Add Missing Person Records

Creates VIPER person records for valid historical instructors:

- Identifies instructors with effort records but no VIPER person record
- Creates person entries with their historical MothraId data
- **Impact**: Allows migration of their effort records

### Task 8: Remap Duplicate Person Records

Consolidates duplicate person records identified in data analysis:

- Remaps effort/percent records to the canonical MothraId
- Preserves all historical effort data
- **Impact**: Prevents duplicate persons in VIPER

## How to Run

### Using Batch File

#### Dry-Run Mode (Default - Safe)

```bash
cd web\Areas\Effort\Scripts
RunDataRemediation.bat
```

This **executes actual SQL in transactions then rolls back** - validates SQL correctness without persisting changes.

**How it works:**

- Executes all UPDATE/DELETE/INSERT statements using real SQL
- Validates SQL syntax and catches runtime errors
- Acquires table locks (brief, released on rollback)
- Rolls back all transactions at the end
- No data is permanently modified
- Catches SQL errors that preview-only mode would miss

#### Apply Fixes

```bash
RunDataRemediation.bat --apply
```

This **actually applies the changes** to the database (with backups).

#### Fast Mode (No Backup)

```bash
RunDataRemediation.bat --apply --skip-backup
```

⚠️ **Warning**: Only use if you have a database backup!

#### With Different Environment

```bash
RunDataRemediation.bat Production          # Preview in Production
RunDataRemediation.bat --apply Production  # Apply in Production
```

## Output

All outputs are saved in the `RemediationOutput/` directory:

**`RemediationReport_[timestamp].txt`**

- Summary of all tasks executed
- Records affected per task
- Detailed list of changes made
- Final statistics

**`Backups/` folder** (created when using `--apply`)

- SQL files with INSERT statements for original data
- Format: `{TableName}_{timestamp}_{uniqueid}.sql`
- Contains only affected records
- Created automatically unless `--skip-backup` used
- Example: `tblCourses_20251029_143022_a1b2c3d4.sql`
- Easy to restore - just execute the SQL file

## Safety Features

### Idempotent Operations

All tasks are **idempotent** - safe to run multiple times:

- ✅ Checks if issues still exist before fixing
- ✅ Skips records that are already corrected
- ✅ No errors if re-run after completion
- ✅ Can resume after interruption

**Example**: If Task 3 completes but Task 4 fails, you can re-run the script. Tasks 1-3 will be skipped, and execution continues from Task 4.

### Backup System

When using `--apply` mode, creates backup SQL files:

- **Not created in dry-run mode** (changes are rolled back, no backup needed)
- Contains only affected records (not entire table)
- Saved to `RemediationOutput/Backups/` folder
- Each file contains INSERT statements to restore data
- Automatically named with timestamp and unique ID
- Ready to execute - no import wizards needed
- Disable with `--skip-backup` for faster execution (only works with `--apply`)

**Backup File Naming**:

```
RemediationOutput/
  └── Backups/
      ├── tblCourses_20251029_143022_a1b2c3d4.sql
      ├── tblPerson_20251029_143022_b2c3d4e5.sql
      └── tblEffort_20251029_143022_c3d4e5f6.sql
```

### Transaction Safety

All database operations are wrapped in transactions:

- ✅ **All operations** use SqlTransaction (not just multi-statement tasks)
- ✅ **Dry-run mode**: Executes SQL then rolls back (validates without persisting)
- ✅ **Apply mode**: Executes SQL then commits (persists changes)
- ✅ Operations are atomic (all succeed or all rollback)
- ✅ Task 1 (Fix Duplicates): UPDATE + DELETE in single transaction
- ✅ Task 2-8: All UPDATEs/DELETEs/INSERTs wrapped in transactions
- ✅ Idempotent design allows safe re-runs
- ✅ Backup SQL files provide additional rollback capability (apply mode only)

**Dry-Run Advantages:**

- Catches SQL syntax errors before applying changes
- Validates NULL handling and data type conversions
- Shows actual row counts that will be affected
- Brief table locks (released on rollback) - minimal impact on running systems

**Recommendation**: Always run dry-run first, then apply during maintenance window.

## Rollback Procedure

If you need to rollback changes:

### Restore from Backup SQL Files

The backup SQL files contain INSERT statements that can be executed directly:

**Manual execution:**

```sql
-- Run the contents of the backup SQL file
-- Example from tblCourses_20251029_143022_a1b2c3d4.sql:

INSERT INTO tblCourses ([course_id], [course_CRN], [course_TermCode], ...)
VALUES (123, '12345', 202503, ...);

INSERT INTO tblCourses ([course_id], [course_CRN], [course_TermCode], ...)
VALUES (456, '67890', 202503, ...);
```

## Recommended Workflow

### Complete Migration Process

#### 1. Initial Analysis

```bash
cd web\Areas\Effort\Scripts
RunDataAnalysis.bat
```

Review the analysis report: `AnalysisOutput/EffortAnalysis_[timestamp].txt`

Note the number of critical issues that need remediation.

#### 2. Dry-Run Remediation

```bash
RunDataRemediation.bat
```

⚠️ **Note**: No `--apply` flag = safe dry-run mode (default)

Review the dry-run report: `RemediationOutput/RemediationReport_[timestamp].txt`

**What happens in dry-run:**

- Executes all SQL statements in transactions
- Validates syntax and catches runtime errors (e.g., NULL handling, type mismatches)
- Acquires table locks briefly (released when transaction rolls back)
- Shows actual row counts that would be affected
- Rolls back all changes - no data is modified
- **Advantage**: Catches SQL errors before applying changes to production

#### 3. Backup Database (Recommended)

```sql
BACKUP DATABASE Efforts
TO DISK = 'C:\Backups\Efforts_PreRemediation.bak'
WITH COMPRESSION;
```

#### 4. Apply Remediation

```bash
RunDataRemediation.bat --apply
```

The script will ask for confirmation before applying changes.

#### 5. Verify Results

```bash
RunDataAnalysis.bat
```

Review the analysis report.

Should show: **0 critical issues** ✅

#### 6. Proceed with Migration

Once analysis shows 0 critical issues, proceed with:

- Entity Framework migration generation
- Database schema migration
- Data migration from Efforts → VIPER


## Notes

### VETGUEST Account

The consolidated guest instructor account:

- **MothraId**: VETGUEST
- **Name**: Guest Instructor
- **Email**: vetguest@ucdavis.edu
- **Purpose**: Historical placeholder for guest instructors
- **Records**: Consolidates UNKGuest, VETGuest, VMDOGues

### CRN Numbering

Placeholder CRNs use distinct ranges:

- **Real CRNs**: 5 digits (e.g., 12345)
- **90001-90999**: Courses with effort records (must migrate)
- **99001-99999**: Courses without effort records (cleanup)
- Easily identifiable as system-generated
- Sequential assignment based on course_id order

### Data Integrity

After remediation:

- All MothraIds map to valid VIPER PersonIds
- No duplicate courses
- No NULL department codes
- No blank CRNs (for courses with effort records)
- No negative hours
- All check constraints satisfied
