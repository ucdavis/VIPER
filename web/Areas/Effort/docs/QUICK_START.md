# Effort Migration - Quick Start Guide

**Quick reference for running the migration scripts**

---

## Step 1: Create Database Schema

```powershell
# Set environment and navigate to scripts
$env:ASPNETCORE_ENVIRONMENT="Test"
cd web\Areas\Effort\Scripts
```

### Dry-Run Mode (Recommended First Step)

**Test SQL commands without permanent changes:**

```powershell
# Option A: Using batch file (recommended)
.\RunCreateDatabase.bat

# Option B: Using dotnet script directly
dotnet script CreateEffortDatabase.cs
```

**Expected**:

- Creates [VIPER].[effort] schema (permanent, idempotent)
- Tests all 20 table creation statements in a transaction
- Rolls back tables (no permanent table changes)
- Validates SQL syntax and database connectivity

### Execute Mode (After Dry-Run Validation)

**Actually create the tables:**

```powershell
# Option A: Using batch file with execute flag
.\RunCreateDatabase.bat --execute

# Option B: Using dotnet script with execute flag
dotnet script CreateEffortDatabase.cs --execute
```

**Expected**: Creates [VIPER].[effort] schema with 20 tables and automatically validates:

- All 20 tables created
- Roles table seeded (3 rows)
- SessionTypes table seeded (35 rows)
- EffortTypes table seeded (26 rows)
- Months table seeded (12 rows)
- Records.Role column type correct (char(1))

---

## Step 2: Data Cleanup (REQUIRED before migration)

```powershell
# Run remediation to fix all data quality issues
dotnet script EffortDataRemediation.cs

# Review the remediation report
# This resolves ALL unmapped MothraIds, invalid data, and duplicates
```

**Expected**: Fixes data quality issues across 8 categories:
- Task 1: Merge duplicate courses (same CRN/Term/Units)
- Task 2: Fix missing department codes
- Task 3: Generate placeholder CRNs where needed
- Task 4: Delete negative hours records
- Task 5: Delete invalid MothraIds
- Task 6: Consolidate guest instructors → VETGUEST
- Task 7: Add missing persons to VIPER
- Task 8: Remap duplicate person records

**IMPORTANT**: This step ensures 0% unmapped MothraIds in the migration.

---

## Step 3: Migrate Data

### Dry-Run Mode - Data Migration (Recommended First Step)

**Test migration without permanent changes:**

```powershell
# Option A: Using batch file (recommended)
.\RunMigrateData.bat

# Option B: Using dotnet script directly
dotnet script MigrateEffortData.cs
```

**Expected**:

- Validates prerequisites and connectivity
- Tests all migration SQL in a transaction
- Rolls back transaction (no permanent changes)
- Shows preview of records that would be migrated
- Automatically validates data quality

### Execute Mode - Data Migration (After Dry-Run Validation)

**Actually migrate the data:**

```powershell
# Option A: Using batch file with execute flag
.\RunMigrateData.bat --execute

# Option B: Using dotnet script with execute flag
dotnet script MigrateEffortData.cs --execute
```

**Expected**: Migrates all data from Efforts → [VIPER].[effort] and automatically validates:

- All table record counts match legacy database
- Data quality summary (unmapped MothraIds should be 0)
- Role column values correct ('1', '2', '3')
- Sabbaticals and Audits data preserved
- UserAccess authorization data complete

---

## Troubleshooting

### Error: "Effort schema not found"
```powershell
# Run CreateEffortDatabase.cs first
dotnet script CreateEffortDatabase.cs
```

### Error: "Legacy Efforts database not found"
- Check connection string in appsettings.json
- Verify Efforts database exists and is accessible

### Error: "Foreign key constraint violation"
- This means CreateEffortDatabase.cs didn't complete successfully
- Drop and recreate: `dotnet script CreateEffortDatabase.cs --force`

### Warning: "X persons could not be mapped"

- **This should NOT happen** if EffortDataRemediation.cs ran successfully
- If you see this warning, run EffortDataRemediation.cs first to resolve all MothraId mapping issues
- Review the remediation report to verify all 8 tasks completed

---

## Rollback

### Test changes first (dry-run):

```powershell
# Always test schema with dry-run before executing
.\RunCreateDatabase.bat
# Review output, then execute if everything looks good
.\RunCreateDatabase.bat --execute

# Always test data migration with dry-run before executing
.\RunMigrateData.bat
# Review output, then execute if everything looks good
.\RunMigrateData.bat --execute
```

### Drop data only (keep schema):

```sql
EXEC sp_MSforeachtable 'ALTER TABLE [effort].? NOCHECK CONSTRAINT ALL';
-- Truncate tables (see MIGRATION_GUIDE.md for order)
EXEC sp_MSforeachtable 'ALTER TABLE [effort].? WITH CHECK CHECK CONSTRAINT ALL';
```

### Drop everything and start over:

```powershell
dotnet script CreateEffortDatabase.cs --drop
# Test with dry-run first
dotnet script CreateEffortDatabase.cs
# Then execute
dotnet script CreateEffortDatabase.cs --execute
```

---

## Next Steps After Migration

1. ✅ Verify 0 unmapped PersonId records (remediation should have resolved all)
2. ✅ Run CreateEffortShadow.cs (creates compatibility layer for ColdFusion)
3. ✅ DBA configures database permissions for applications
4. ✅ Test ColdFusion application with EffortShadow database
5. ✅ Begin Sprint 1 (VIPER2 development)

---

## Documentation

| File | Purpose |
|------|---------|
| **MIGRATION_GUIDE.md** | Complete testing guide, validation queries, and Sprint 0 plan |
| **EFFORT_MASTER_PLAN.md** | 16-sprint roadmap |
| **Shadow_Database_Guide.md** | ColdFusion compatibility strategy |
| **Technical_Reference.md** | Schema mappings and field transformations |

---

## Support

If something goes wrong:
1. Read error message carefully
2. Check MIGRATION_GUIDE.md for validation queries and troubleshooting
3. Try rollback and retry
4. Document specific error for further analysis

**All scripts are safe to run multiple times** - they check for existing data before inserting.

---
