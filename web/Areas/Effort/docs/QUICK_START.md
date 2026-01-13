# Effort Migration - Quick Start Guide

**Quick reference for running the migration scripts**

For detailed validation queries, troubleshooting, and comprehensive documentation, see [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md).

---

## Prerequisites

```powershell
# Set environment and navigate to scripts
$env:ASPNETCORE_ENVIRONMENT="Test"  # or "Development" or "Production"
cd web\Areas\Effort\Scripts
```

---

## Step 1: Data Analysis & Remediation

**IMPORTANT: Run this BEFORE creating the new database to fix data quality issues at the source.**

```powershell
# Step 1a: Analyze data quality in legacy Efforts database
.\RunDataAnalysis.bat

# Review the analysis report

# Step 1b: Fix the identified issues
.\RunDataRemediation.bat

# Step 1c: Verify 0 critical issues remain
.\RunDataAnalysis.bat
```

**What it does:**

**RunDataAnalysis.bat** - Identifies data quality issues and creates analysis report

**RunDataRemediation.bat** - Fixes 8 categories of issues:
- Merges duplicate courses, fixes missing data, removes invalid records
- Consolidates guest instructors, adds missing persons to VIPER

**Expected Result:** Final analysis shows 0% unmapped MothraIds and no critical issues.

See [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for detailed remediation task descriptions.

---

## Step 2: Create Database Schema

```powershell
# Test schema creation (dry-run, recommended first)
.\RunCreateDatabase.bat

# Actually create the schema
.\RunCreateDatabase.bat --apply
```

**What it creates:**
- [VIPER].[effort] schema with 20 tables
- Reference data: Roles (3), EffortTypes (35), PercentAssignTypes (26), Months (12)

See [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for detailed validation queries.

---

## Step 3: Migrate Data

```powershell
# Test migration (dry-run, recommended first)
.\RunMigrateData.bat

# Actually migrate the data
.\RunMigrateData.bat --apply
```

**What it does:**

- Clears all existing data from [effort] tables (with confirmation prompt)
- Migrates all data from legacy Efforts → [VIPER].[effort]
- Reseeds IDENTITY columns for consistent IDs
- Validates record counts and data quality
- Should report 0% unmapped MothraIds (due to Step 1 remediation)

See [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for detailed validation queries.

---

## Step 4: Create Reporting Stored Procedures

```powershell
# Test procedure creation (dry-run, recommended first)
.\RunCreateReportingProcedures.bat

# Actually create the procedures
.\RunCreateReportingProcedures.bat --apply
```

**What it creates:**
- 16 reporting stored procedures in [effort] schema
- Instructor reports, course summaries, term analytics

---

## Step 5: Create Shadow Schema (ColdFusion Compatibility)

```powershell
# Test shadow schema creation (dry-run, recommended first)
.\RunCreateShadow.bat

# Actually create the shadow schema
.\RunCreateShadow.bat --apply
```

**What it creates:**
- [EffortShadow] schema with 19 compatibility views
- 87 stored procedures rewritten to work with modern [effort] tables
- Allows ColdFusion app to work with new database structure

```powershell
# Verify shadow procedures work correctly
.\RunVerifyShadow.bat
```

See [Shadow_Database_Guide.md](./Shadow_Database_Guide.md) for architectural details.

---

## Next Steps After Migration

1. ✅ DBA configures database permissions for applications
2. ✅ Test ColdFusion application with [EffortShadow] schema
3. ✅ Begin VIPER2 development (Sprint 1)

---

## Troubleshooting

### Error: "Legacy Efforts database not found"
Check connection string in `appsettings.json` and verify database accessibility.

### Error: "Foreign key constraint violation"
Run `.\RunCreateDatabase.bat --force` to drop and recreate schema.

### Warning: "X persons could not be mapped"
This should NOT happen if Step 1 (RunDataRemediation.bat) completed successfully. Re-run data remediation.

### Rollback Instructions

See [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for complete rollback procedures.

---

## All Commands At A Glance

```powershell
# Complete migration workflow
$env:ASPNETCORE_ENVIRONMENT="Test"
cd web\Areas\Effort\Scripts

# Step 1: Analyze and fix data quality issues in legacy database
.\RunDataAnalysis.bat
.\RunDataRemediation.bat
.\RunDataAnalysis.bat  # Verify 0 critical issues

# Step 2: Create new schema
.\RunCreateDatabase.bat
.\RunCreateDatabase.bat --apply

# Step 3: Migrate data
.\RunMigrateData.bat
.\RunMigrateData.bat --apply

# Step 4: Create reporting procedures
.\RunCreateReportingProcedures.bat
.\RunCreateReportingProcedures.bat --apply

# Step 5: Create shadow schema for ColdFusion
.\RunCreateShadow.bat
.\RunCreateShadow.bat --apply
.\RunVerifyShadow.bat
```

---

## Documentation

| File | Purpose |
|------|---------|
| **MIGRATION_GUIDE.md** | Complete testing guide, validation queries, and Sprint 0 plan |
| **EFFORT_MASTER_PLAN.md** | 16-sprint roadmap |
| **Shadow_Database_Guide.md** | ColdFusion compatibility schema strategy |
| **Technical_Reference.md** | Schema mappings and field transformations |

---

## Support

If something goes wrong, see [MIGRATION_GUIDE.md](./MIGRATION_GUIDE.md) for detailed troubleshooting steps.
