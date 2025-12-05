# Shadow Schema Guide

Implementation guide for the EffortShadow compatibility layer.

---

## Architecture

```
[VIPER].[effort] schema (modern)
        ↑
        │
[VIPER].[EffortShadow] schema (views + stored procedures)
        ↑
        │
ColdFusion app (no changes required)
```

**[EffortShadow]** provides a compatibility layer between legacy ColdFusion code and the modern database schema. It exists as a schema within the VIPER database, not as a separate database.

---

## Implementation

### Step 1: Create Shadow Schema

```powershell
.\RunCreateShadow.bat --apply
```

Creates [EffortShadow] schema with:
- Views mapping legacy table/column names to modern schema
- 87 stored procedures rewritten to work with modern tables

### Step 2: Migrate Stored Procedures

**87 active stored procedures** need migration from legacy Efforts database to EffortShadow (89 total procedures, 2 obsolete).

**Note**: The 2 obsolete procedures (`dt_displayoaerror*`) are legacy Visual Studio database designer tools and should not be migrated.

**See [Technical_Reference.md](Technical_Reference.md#stored-procedures)** for complete procedure list and categorization.

**Process**:
1. Export procedure from legacy database
2. Update table references (tblEffort → vw_tblEffort)
3. Update column references (effort_ID → Id, wrap in compatibility view if needed)
4. Test with sample data
5. Deploy to EffortShadow

**Estimated effort**: 3-5 days

**Priority groups** (see Stored Procedure Analysis):
- **High priority** (35 procedures): Frequently used CRUD operations
- **Medium priority** (31 procedures): Reports and complex queries
- **Low priority** (20 procedures): Rarely used utilities

### Step 3: Update ColdFusion Datasource

**Test environment only first!**

In ColdFusion Administrator:
1. Navigate to Data & Services > Data Sources
2. Find "Effort" or "Efforts" datasource
3. Change database from "Efforts" to "VIPER"
4. Set default schema to "EffortShadow"
5. Test connection
6. Save and restart ColdFusion

### Step 4: Test ColdFusion Application

**Critical tests**:
- CRUD operations (create, read, update, delete effort records)
- Authorization (userAccess table queries)
- Reports (merit report, department activity, evaluations)
- Course imports (from Banner)
- Clinical imports (from scheduler)
- Email notifications

**Performance baseline**: Capture query times before/after

**Success criteria**: All functionality works, <20% performance degradation

---

## INSTEAD OF Triggers

The shadow schema uses **INSTEAD OF triggers** to intercept INSERT/UPDATE/DELETE operations on views and redirect them to the modern schema tables. This allows legacy ColdFusion code to write to "tables" that are actually views.

### Why Triggers Are Needed

Legacy ColdFusion code expects to INSERT/UPDATE/DELETE directly on tables like `tblEffort`, `tblPercent`, etc. Since these are now views over the modern schema, we use INSTEAD OF triggers to:

1. **Map identifiers**: Convert MothraId → PersonId, AcademicYear → TermCode
2. **Handle derived columns**: Skip columns that are computed in the view
3. **Maintain data integrity**: Ensure foreign keys are resolved correctly

### Trigger-Enabled Views

| View | Triggers | Purpose |
|------|----------|---------|
| **tblEffort** | INSERT, UPDATE, DELETE | Effort records CRUD |
| **tblPerson** | INSERT, UPDATE, DELETE | Instructor person records |
| **tblPercent** | INSERT, UPDATE, DELETE | Percentage allocation records |
| **tblAudit** | INSERT | Audit log entries (insert-only) |

### Internal Columns

Some views include **internal columns** that are not in legacy tables but are required by triggers:

| View | Internal Column | Purpose |
|------|-----------------|---------|
| **tblPercent** | `percent_PersonId_Internal` | Allows trigger to use PersonId directly |
| **tblAudit** | `audit_TableName` | Maps action to table for modern schema |
| **tblAudit** | `audit_RecordID` | Record ID for audit trail |
| **tblAudit** | `audit_ChangedBy_PersonId` | PersonId of modifier |

These columns appear as "extra columns" in verification but are required for correct trigger operation.

---

## View Mappings

### Core Views

**vw_tblEffort** → `SELECT * FROM [VIPER].[effort].[Records]`
- Maps PersonId → MothraId (JOIN to VIPER.users.Person)
- Maps modern column names to legacy names

**vw_tblPerson** → `SELECT * FROM [VIPER].[effort].[Persons]`
- Maps PersonId → MothraId
- Maps decimal → float for percentages (ColdFusion compatibility)

**vw_tblCourses** → `SELECT * FROM [VIPER].[effort].[Courses]`
- Direct mapping (minimal changes)

**vw_tblPercent** → `SELECT * FROM [VIPER].[effort].[Percentages]`
- Denormalizes narrow format to wide format (PIVOT)
- PersonId → MothraId mapping

**vw_tblStatus** → `SELECT * FROM [VIPER].[effort].[TermStatus]`
- Direct mapping with status workflow fields

**vw_userAccess** → `SELECT * FROM [VIPER].[effort].[UserAccess] WHERE IsActive = 1`
- Filters to active users only
- PersonId → MothraId mapping

### Lookup Views

**vw_tblRoles** → `SELECT * FROM [VIPER].[effort].[Roles]`
**vw_tblEffortType_LU** → `SELECT * FROM [VIPER].[effort].[EffortTypes]`
**vw_tblUnits_LU** → `SELECT * FROM [VIPER].[effort].[Units]`
**vw_tblJobCode** → `SELECT * FROM [VIPER].[effort].[JobCodes]`

---

## Stored Procedure Categories

### CRUD Operations (35 procedures)
Create, read, update, delete operations for effort records, persons, courses.

**Examples**:
- `usp_createInstructorEffort` - Create new effort record
- `usp_updateInstructorEffort` - Update effort record
- `usp_deleteInstructorEffort` - Delete effort record
- `usp_getInstructorEffortByID` - Get single effort record
- `usp_getInstructorsByTerm` - List instructors for term

**Migration strategy**: Rewrite INSERT/UPDATE/DELETE to use views (views must be updatable)

### Reports (31 procedures)
Generate merit reports, department activity, evaluations, summaries.

**Examples**:
- `usp_getEffortReportMerit` - Merit report
- `usp_getEffortDeptActivityTotal` - Department activity report
- `usp_getInstructorEvalsAverageWithExcludeTerms` - Evaluation report with sabbatical exclusions
- `usp_getEffortSummaryByDept` - Summary by department

**Migration strategy**: Update table/column references, test output matches legacy

### Utilities (20 procedures)
Data validation, imports, exports, cleanup.

**Examples**:
- `usp_importCoursesFromBanner` - Course import
- `usp_importClinicalFromScheduler` - Clinical import
- `usp_validateEffortData` - Data validation
- `usp_cleanupOrphanedRecords` - Cleanup utility

**Migration strategy**: Low priority, migrate as needed

---

## ColdFusion Usage Analysis

**68 actively used stored procedures** identified in ColdFusion codebase.

**High-frequency calls**:
- `userAccess` table: 20+ references (authorization queries)
- `tblUnits_LU`: 8 references (effort unit lookups)
- `usp_getInstructorsByTerm`: 15+ references
- `usp_createInstructorEffort`: 10+ references

**Tables to prioritize in shadow views**:
1. userAccess (critical for authorization)
2. tblEffort / Records (core data)
3. tblPerson / Persons (instructor info)
4. tblUnits_LU / Units (effort units)

---

## Performance Testing

### Baseline Queries

```sql
SET STATISTICS TIME ON;

-- Test 1: Core effort query
SELECT * FROM tblEffort WHERE effort_termcode = 202401;

-- Test 2: Authorization query
SELECT * FROM userAccess WHERE mothraID = 'testuser';

-- Test 3: Complex report
EXEC usp_getEffortReportMerit @StartYear = 2022, @EndYear = 2024;

SET STATISTICS TIME OFF;
```

**Acceptance criteria**: Query time increase <20%

### Indexing Strategy

**Add indexes to [VIPER].[effort] tables**:
- `Records` (TermCode, PersonId, CourseId)
- `Persons` (PersonId, TermCode composite PK already indexed)
- `UserAccess` (PersonId, DepartmentCode, IsActive)

**Monitor with**:
```sql
SELECT * FROM sys.dm_db_missing_index_details
WHERE database_id = DB_ID('VIPER')
  AND OBJECT_SCHEMA_NAME(object_id, database_id) = 'effort';
```

---

## External Dependencies

**courses database** (Banner):
- Used in LEFT OUTER JOINs for course titles
- EffortShadow views may need cross-database joins

**dictionary database**:
- Used for employee title lookups
- Function calls may need updating

**UCDBanner linked server** (Oracle):
- Used in `usp_importCoursesFromBanner`
- Requires linked server configuration

---

## Rollback

**If issues discovered**, revert ColdFusion datasource:
1. Change datasource from "VIPER" back to "Efforts"
2. Restart ColdFusion
3. Verify legacy app works
4. Fix [EffortShadow] schema issues
5. Retry

**Data safety**: Modern [VIPER].[effort] schema remains untouched, legacy Efforts database remains available.

---

## Security

**Database permissions** (configured by DBA):
- ColdFusion application login granted access to VIPER database ([EffortShadow] schema)
- ColdFusion application login granted access to VIPER database ([effort] schema via EffortShadow views)
- ColdFusion application login granted access to VIPER.users.Person
- VIPER2 application service account granted access to [VIPER].[effort] schema

**VIPER2 Authorization**:
- Uses trusted application service account with full schema access
- Authorization enforced at application layer using RAPS permissions
- Department-level access controlled via UserAccess table
- See [Authorization_Design.md](Authorization_Design.md) for complete architecture

---

## Next Steps After Shadow Setup

1. Test all ColdFusion functionality
2. Monitor performance in production
3. Begin VIPER2 development (Sprint 1)
4. Gradually migrate features from ColdFusion to VIPER2
5. When ColdFusion fully replaced, retire [EffortShadow] schema
