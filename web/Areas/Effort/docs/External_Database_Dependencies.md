# External Database Dependencies

## Overview

The Effort shadow schema (`[EffortShadow]`) includes 92 stored procedures that were migrated from the legacy Effort database. Some of these procedures require access to **external databases** on the same SQL Server instance. These dependencies are documented below.

## Required Database Permissions

For the shadow schema to function completely, the application's SQL Server user needs **READ (SELECT) permissions** on the following databases:

### 1. `dictionary` Database (20 procedures affected)

**Purpose**: Contains reference data for job titles, departments, and employee information.

**Required Permissions**:
```sql
USE dictionary;
GRANT SELECT TO [YourApplicationUser];
```

**Affected Stored Procedures**:
- `usp_getDepartmentCountByJobGroup`
- `usp_getDepartmentCountByJobGroupWithExcludeTerms`
- `usp_getEffortDeptActivityTotal2`
- `usp_getEffortDeptActivityTotalBackup`
- `usp_getEffortDeptActivityTotalWithExclude`
- `usp_getEffortDeptActivityTotalWithExcludeTerms_old`
- `usp_getEffortDeptActivityTotalWithExcludeTerms2`
- `usp_getEffortReportKass`
- `usp_getEffortReportMeritAverage`
- `usp_getEffortReportMeritSummary`
- `usp_getEffortReportMeritSummaryForLairmore`
- `usp_getEffortReportMeritWithClinPercent`
- `usp_getInstructors`
- `usp_getInstructorsInAcademicYear`
- `usp_getInstructorsInAcademicYear2`
- `usp_getInstructorsRay` (also requires `dvtTitle_Ray` view)
- `usp_getJobGroups`
- `usp_getNonAdminEffort`
- `usp_getNonAdminEffort2`
- `usp_getNonAdminEffortMultiYear`

**Tables/Views Used**:
- `dictionary.dbo.dvtTitle` - Job titles and job group information
- `dictionary.dbo.dvtTitle_Ray` - Special view for Ray's reports

---

### 2. `EvalHarvest` Database (7 procedures affected)

**Purpose**: Contains course evaluation data from students.

**Required Permissions**:
```sql
USE EvalHarvest;
GRANT SELECT TO [YourApplicationUser];
```

**Affected Stored Procedures**:
- `usp_getInstructorEvals`
- `usp_getInstructorEvalsAverage`
- `usp_getInstructorEvalsAverageWithExclude`
- `usp_getInstructorEvalsAverageWithExcludeTerms`
- `usp_getInstructorEvalsMultiYear`
- `usp_getInstructorEvalsMultiYear_fromEvalTable`
- `usp_getInstructorEvalsMultiYearWithExclude`

**Tables Used**:
- `EvalHarvest.dbo.[evaluations table]` - Student course evaluations

---

### 3. `idcards` Database (1 procedure affected)

**Purpose**: Contains employee ID card data.

**Required Permissions**:
```sql
USE idcards;
GRANT SELECT TO [YourApplicationUser];
```

**Affected Stored Procedures**:
- `usp_getEffortReportMeritUnit`

**Tables Used**:
- `idcards.dbo.idcard` - Employee ID card information

---

## Linked Server Dependencies

Two procedures require access to a **linked server** named `UCDPPS`:

- `usp_getEffortDeptActivityTotal`
- `usp_getEffortDeptActivityTotalWorking`

These procedures were **not migrated** to the shadow schema because the linked server is not available in the development environment. They are marked as obsolete and have alternative versions that don't require the linked server.

---

## Testing Considerations

### Development/Test Environments

In development or test environments where these external databases may not be available or the service account may not have permissions:

1. **Expected Failures**: 28 stored procedures will fail with "access denied" errors during verification testing
2. **Impact**: These are **environmental limitations**, not shadow schema defects
3. **Workaround**: These procedures can be tested individually once permissions are granted, or skipped during automated verification

### Production Environment

In production, the application service account should already have the necessary permissions to these databases, as the legacy Effort application required the same access.

**Verification**: To confirm permissions are correctly configured, run the following as the application service account:

```sql
-- Check dictionary access
USE dictionary;
SELECT HAS_PERMS_BY_NAME(SCHEMA_NAME(), 'SCHEMA', 'SELECT') AS has_select_permission;

-- Check EvalHarvest access
USE EvalHarvest;
SELECT HAS_PERMS_BY_NAME(SCHEMA_NAME(), 'SCHEMA', 'SELECT') AS has_select_permission;

-- Check idcards access
USE idcards;
SELECT HAS_PERMS_BY_NAME(SCHEMA_NAME(), 'SCHEMA', 'SELECT') AS has_select_permission;
```

All three queries should return `1` (true) for `has_select_permission`.

---

## Summary

- **Total procedures requiring external databases**: 28 (31% of all procedures)
- **Procedures working without external dependencies**: 62 (69% of all procedures)
- **Required action**: Grant SELECT permissions on `dictionary`, `EvalHarvest`, and `idcards` databases to the application service account

**Note**: The shadow schema itself is fully functional. The external database dependencies are inherited from the legacy Effort application and are not limitations of the migration.
