# Effort Database Migration Guide

## Overview

This guide covers the complete migration process from the legacy Efforts database to the modern `[VIPER].[effort]` schema.

**Architecture**: The new effort schema is created within the VIPER database as `[VIPER].[effort]`, not as a separate database. This simplifies foreign key management to `VIPER.users.Person`.

---

## Quick Start

See [QUICK_START.md](QUICK_START.md) for simple command reference.

**Step 1: Data Analysis & Remediation**:

```powershell
# Step 1a: Analyze data quality in legacy Efforts database
.\RunDataAnalysis.bat

# Step 1b: Fix data quality issues in legacy Efforts database BEFORE migration
.\RunDataRemediation.bat

# Step 1c: Verify 0 critical issues remain
.\RunDataAnalysis.bat
```

**IMPORTANT**: This step **analyzes** then **fixes** 8 categories of data quality issues in the **legacy Efforts database** before you create the new schema. This ensures:
- 0% unmapped MothraIds
- No duplicate courses or invalid data
- Clean migration with no rollback needed

**RunDataAnalysis.bat** identifies issues, **RunDataRemediation.bat** fixes them, then re-run analysis to verify.

**Step 2: Test Schema Creation (Dry-Run)**:

```powershell
# Test SQL without permanent changes (recommended first step)
.\RunCreateDatabase.bat
```

**Step 3: Create Schema (Execute)**:

```powershell
# Actually create the tables
.\RunCreateDatabase.bat --apply
```

**Step 4: Preview Data Migration (Dry-Run)**:

```powershell
# Test migration without permanent changes (recommended first step)
.\RunMigrateData.bat
```

**Step 5: Execute Data Migration**:

```powershell
# Actually migrate the data
.\RunMigrateData.bat --apply
```

**Step 6: Create Reporting Stored Procedures**:

```powershell
# Test stored procedure creation (dry-run)
.\RunCreateReportingProcedures.bat

# Actually create the stored procedures
.\RunCreateReportingProcedures.bat --apply
```

**Step 7: Create Shadow Schema (ColdFusion Compatibility)**:

```powershell
# Test shadow schema creation (dry-run)
.\RunCreateShadow.bat

# Actually create the shadow schema
.\RunCreateShadow.bat --apply

# Verify shadow procedures
.\RunVerifyShadow.bat
```

---

## Validation & Testing

### Dry-Run Mode for Schema Creation (Recommended First Step)

**Always test with dry-run before executing in any environment:**

```powershell
# Test schema creation without permanent changes
.\RunCreateDatabase.bat
```

**What dry-run does**:

- ✅ Creates [VIPER].[effort] schema (idempotent, safe)
- ✅ Tests all 20 table creation SQL statements
- ✅ Validates database connectivity and permissions
- ✅ Wraps all table operations in a transaction
- ✅ **Rolls back transaction** - no permanent table changes
- ✅ Automatically validates schema and seeded data

**When dry-run succeeds, you can safely execute**:

```powershell
# Actually create the tables
.\RunCreateDatabase.bat --apply
```

### Dry-Run Mode for Data Migration (Recommended Before Actual Migration)

**Always test data migration with dry-run before executing:**

```powershell
# Preview migration without permanent changes
.\RunMigrateData.bat
```

**What dry-run does**:

- ✅ Validates prerequisites (schema exists, legacy database accessible)
- ✅ Tests all data migration SQL statements
- ✅ Previews record counts that would be migrated per table
- ✅ Wraps all operations in a transaction
- ✅ **Rolls back transaction** - no permanent data changes
- ✅ Automatically validates data quality and shows warnings about unmapped MothraIds (if any)

**When dry-run succeeds, you can safely execute**:

```powershell
# Actually migrate the data
.\RunMigrateData.bat --apply
```

---

## Rollback Procedures

### Rollback Schema Creation

```powershell
# Drop the schema completely
.\RunCreateDatabase.bat --drop

# Test recreation with dry-run first
.\RunCreateDatabase.bat

# Then execute if dry-run succeeds
.\RunCreateDatabase.bat --apply
```

### Rollback Data Migration

```powershell
# Drop and recreate schema
.\RunCreateDatabase.bat --force
# Type "DELETE" to confirm

# Test with dry-run first
.\RunMigrateData.bat

# Then execute if dry-run succeeds
.\RunMigrateData.bat --apply
```

---

## Post-Migration Tasks

After successful data migration, proceed with these tasks in order:

### 1. Create Shadow Schema (EffortShadow)

```powershell
# Test with dry-run first
.\RunCreateShadow.bat

# Then execute if dry-run succeeds
.\RunCreateShadow.bat --apply
```

This creates a compatibility layer for the ColdFusion application within the VIPER database:

- Creates `[VIPER].[EffortShadow]` schema
- Views that map legacy table/column names to the `[effort]` schema
- 87 stored procedures rewritten to work with modern tables

### 2. Verify Shadow Schema Procedures

```powershell
# Compare legacy vs shadow schema procedures
.\RunVerifyShadow.bat
```

This verifies that [EffortShadow] schema procedures return identical results to legacy procedures, ensuring the compatibility layer works correctly.

### 3. DBA Configures Database Permissions

**Note:** Database permissions are configured by the DBA outside of migration scripts.

The DBA will grant:

- ColdFusion application login access to VIPER database ([EffortShadow] schema)
- ColdFusion application login access to VIPER database ([effort] schema via EffortShadow views)
- ColdFusion application login access to VIPER.users.Person
- VIPER2 application service account access to [effort] schema

**Authorization in VIPER2:** The VIPER2 application uses a trusted service account with full schema access. Authorization is enforced at the application layer using RAPS permissions and the EffortPermissionService. See [Authorization_Design.md](Authorization_Design.md) for details.

### 4. Update ColdFusion Datasource

**Test environment only first!**

Update ColdFusion datasource to point to VIPER database and use [EffortShadow] schema. The ColdFusion application will continue to reference the same table names (e.g., tblEffort), but they will now resolve to [EffortShadow].[tblEffort] views in the VIPER database.

### 5. ColdFusion Testing

Complete testing checklist:

- [ ] All CRUD operations
- [ ] Authorization queries
- [ ] Reports generation
- [ ] Search functionality
- [ ] Performance validation (< 20% slower than baseline)

### 6. Begin VIPER2 Development (Sprint 1)

Once ColdFusion is running successfully on EffortShadow:

- Scaffold EF entities from [effort] schema
- Create first API endpoints
- Build Vue.js UI components

---

## Sprint 0 Timeline (2-3 Weeks)

### Week 1: Database Creation & Data Migration

**Day 1**: Data quality & remediation

- Run data analysis: `.\RunDataRemediation.bat`
- Review remediation report
- Verify all 8 data quality tasks completed
- Confirm 0% unmapped MothraIds

**Day 2**: Create modern schema

- Test with dry-run: `.\RunCreateDatabase.bat`
- Execute schema creation: `.\RunCreateDatabase.bat --apply`
- Validate 20 tables created

**Day 3-4**: Migrate data and create reporting procedures

- Test with dry-run: `.\RunMigrateData.bat`
- Execute migration: `.\RunMigrateData.bat --apply`
- Test reporting procedures: `.\RunCreateReportingProcedures.bat`
- Execute reporting procedures: `.\RunCreateReportingProcedures.bat --apply`

### Week 2: Shadow Schema & ColdFusion

**Day 6-7**: Create EffortShadow schema and verify

- Test with dry-run: `.\RunCreateShadow.bat`
- Execute: `.\RunCreateShadow.bat --apply`
- Automatically creates views and rewrites stored procedures
- Verify shadow procedures: `.\RunVerifyShadow.bat`

**Day 8-9**: Configure security

- DBA configures database permissions
- Grant application access to VIPER database schemas

**Day 10-14**: ColdFusion testing

- Update datasource
- Test CRUD operations
- Test reports and authorization
- Performance validation

### Week 3: Buffer for Issues

- Fix any discovered issues
- Performance tuning
- Final stakeholder approval

---

## Risk Assessment

| Component | Risk Level | Mitigation |
|-----------|-----------|------------|
| Schema Creation | 🟢 Low | Scripts tested and validated |
| Data Migration | 🟡 Medium | May have unmapped records; documented |
| Data Loss | 🟢 Low | Source data unchanged |
| Rollback | 🟢 Low | Simple procedures documented |
| Performance | 🟢 Low | One-time migration operation |

---

## Support

### Related Documentation
- [QUICK_START.md](QUICK_START.md) - Quick command reference
- [EFFORT_MASTER_PLAN.md](EFFORT_MASTER_PLAN.md) - 16-sprint roadmap
- [Shadow_Database_Guide.md](Shadow_Database_Guide.md) - ColdFusion compatibility strategy
- [Technical_Reference.md](Technical_Reference.md) - Schema mappings
- [Effort_Database_Schema.sql](Effort_Database_Schema.sql) - Complete database schema (for review)

### Troubleshooting

**Error: "Effort schema not found"**

```powershell
# Run RunCreateDatabase.bat first
.\RunCreateDatabase.bat
```

**Error: "Legacy Efforts database not found"**

- Check connection string in appsettings.json
- Verify Efforts database exists and is accessible

**Error: "Foreign key constraint violation"**

- RunCreateDatabase.bat didn't complete successfully
- Drop and recreate: `.\RunCreateDatabase.bat --force`

**Warning: "X persons could not be mapped"**

- Expected if some MothraIds don't exist in VIPER.users.Person
- Document these for stakeholder review

---

## Pre-Production Checklist

Before running in production:

- [ ] Test environment validated successfully
- [ ] Unmapped PersonId strategy decided with stakeholders
- [ ] Rollback procedures tested in dev environment
- [ ] Backup of legacy Efforts database created
- [ ] DBAs informed and available for support
- [ ] ColdFusion team ready for subsequent testing
- [ ] Sprint 0 timeline confirmed (2-3 weeks)

---

**Document Status**: ✅ Ready for Testing
**Last Updated**: 2025-11-06
