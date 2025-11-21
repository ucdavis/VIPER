# Effort Database Migration Guide

## Overview

This guide covers the complete migration process from the legacy Efforts database to the modern `[VIPER].[effort]` schema.

**Architecture**: The new effort schema is created within the VIPER database as `[VIPER].[effort]`, not as a separate database. This simplifies foreign key management to `VIPER.users.Person`.

---

## Quick Start

See [QUICK_START.md](QUICK_START.md) for simple command reference.

**Step 1: Test Schema Creation (Dry-Run)**:

```powershell
# Test SQL without permanent changes (recommended first step)
.\RunCreateDatabase.bat
# or: dotnet script CreateEffortDatabase.cs
```

**Step 2: Create Schema (Execute)**:

```powershell
# Actually create the tables
.\RunCreateDatabase.bat --execute
# or: dotnet script CreateEffortDatabase.cs --execute
```

**Step 3: Preview Data Migration (Dry-Run)**:

```powershell
# Test migration without permanent changes (recommended first step)
.\RunMigrateData.bat
# or: dotnet script MigrateEffortData.cs
```

**Step 4: Execute Data Migration**:

```powershell
# Actually migrate the data
.\RunMigrateData.bat --execute
# or: dotnet script MigrateEffortData.cs --execute
```

---

## Validation & Testing

### Dry-Run Mode for Schema Creation (Recommended First Step)

**Always test with dry-run before executing in any environment:**

```powershell
# Test schema creation without permanent changes
dotnet script CreateEffortDatabase.cs
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
dotnet script CreateEffortDatabase.cs --execute
```

### Dry-Run Mode for Data Migration (Recommended Before Actual Migration)

**Always test data migration with dry-run before executing:**

```powershell
# Preview migration without permanent changes
dotnet script MigrateEffortData.cs
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
dotnet script MigrateEffortData.cs --execute
```

---

## Rollback Procedures

### Rollback Schema Creation

```powershell
# Drop the schema completely
dotnet script CreateEffortDatabase.cs --drop

# Test recreation with dry-run first
dotnet script CreateEffortDatabase.cs

# Then execute if dry-run succeeds
dotnet script CreateEffortDatabase.cs --execute
```

### Rollback Data Migration

```powershell
# Drop and recreate schema
dotnet script CreateEffortDatabase.cs --force
# Type "DELETE" to confirm

# Test with dry-run first
dotnet script MigrateEffortData.cs

# Then execute if dry-run succeeds
dotnet script MigrateEffortData.cs --execute
```

---

## Post-Migration Tasks

After successful data migration, proceed with these tasks in order:

### 1. Create Shadow Database (EffortShadow)

```powershell
# Test with dry-run first
dotnet script CreateEffortShadow.cs

# Then execute if dry-run succeeds
dotnet script CreateEffortShadow.cs --execute
```

This creates a compatibility layer for the ColdFusion application with:

- Views that map legacy table/column names to the new `[VIPER].[effort]` schema
- Automatic migration of 87 stored procedures from legacy database

### 2. DBA Configures Database Permissions

**Note:** Database permissions are configured by the DBA outside of migration scripts.

The DBA will grant:
- ColdFusion application login access to EffortShadow database
- ColdFusion application login access to Effort database (via wrappers)
- ColdFusion application login access to VIPER.users.Person
- VIPER2 application service account access to Effort schema

**Authorization in VIPER2:** The VIPER2 application uses a trusted service account with full schema access. Authorization is enforced at the application layer using RAPS permissions and the EffortPermissionService. See [Authorization_Design.md](Authorization_Design.md) for details.

### 3. Update ColdFusion Datasource

**Test environment only first!**

Update ColdFusion datasource from "Efforts" to "EffortShadow".

### 4. ColdFusion Testing

Complete testing checklist:

- [ ] All CRUD operations
- [ ] Authorization queries
- [ ] Reports generation
- [ ] Search functionality
- [ ] Performance validation (< 20% slower than baseline)

### 5. Begin VIPER2 Development (Sprint 1)

Once ColdFusion is running successfully on EffortShadow:

- Scaffold EF entities from [effort] schema
- Create first API endpoints
- Build Vue.js UI components

---

## Sprint 0 Timeline (2-3 Weeks)

### Week 1: Database Creation & Data Migration

**Day 1**: Create modern schema

- Test with dry-run: `dotnet script CreateEffortDatabase.cs`
- Execute schema creation: `dotnet script CreateEffortDatabase.cs --execute`
- Validate 20 tables created

**Day 2-3**: Data quality & remediation

- Run data analysis scripts
- Apply remediation
- Verify fixes

**Day 4-5**: Migrate data

- Test with dry-run: `dotnet script MigrateEffortData.cs`
- Execute migration: `dotnet script MigrateEffortData.cs --execute`

### Week 2: Shadow Database & ColdFusion

**Day 6-7**: Create EffortShadow

- Test with dry-run: `dotnet script CreateEffortShadow.cs`
- Execute: `dotnet script CreateEffortShadow.cs --execute`
- Automatically migrates views and stored procedures

**Day 8-9**: Configure security

- DBA configures database permissions
- Grant application access to databases

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
# Run CreateEffortDatabase.cs first
dotnet script CreateEffortDatabase.cs
```

**Error: "Legacy Efforts database not found"**
- Check connection string in appsettings.json
- Verify Efforts database exists and is accessible

**Error: "Foreign key constraint violation"**
- CreateEffortDatabase.cs didn't complete successfully
- Drop and recreate: `dotnet script CreateEffortDatabase.cs --force`

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
