# Effort Migration Toolkit

Consolidated command-line tools for the Effort System migration from ColdFusion to .NET/Vue.js.

## Overview

This project provides three main operations:

1. **Data Analysis** - Analyze legacy Effort database for migration planning
2. **Data Remediation** - Fix data quality issues before migration
3. **Schema Export** - Document legacy database schema for reference

---

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Access to legacy Effort database (SQL Server)

### Common Commands

```bash
cd web/Areas/Effort/Scripts

# Data analysis (Point 1 from PLAN.md)
dotnet run -- analysis

# Data remediation
dotnet run -- remediation --dry-run    # Preview changes
dotnet run -- remediation              # Apply fixes

# Export legacy schema documentation
dotnet run -- schema-export --output ../docs/Legacy_Schema.md
```

---

## Architecture

### Modern Architecture (3-Database Design)

```
+----------------------------------------------------------+
|                  VIPER Database                          |
|  - users.Person table (master person records)            |
|  - Contains MothraId -> PersonId mapping                 |
+----------------------------------------------------------+
                        |
                        | Foreign Keys
                        v
+----------------------------------------------------------+
|              Effort Database (Modern)                    |
|  - 19 tables with modern schema design                   |
|  - Uses PersonId (int) instead of MothraId (varchar)     |
|  - NO stored procedures (uses Entity Framework)          |
|  - All CRUD operations via EF Core                       |
+----------------------------------------------------------+
                        |
                        | Views Query
                        v
+----------------------------------------------------------+
|    EffortShadow Database (Compatibility Layer)           |
|  - 19 views mapping Effort -> legacy schema              |
|  - 122 wrapper stored procedures for ColdFusion          |
|  - Maps PersonId -> MothraId for legacy compatibility    |
|  - NO DATA STORAGE (all views and SPs)                   |
+----------------------------------------------------------+
                        |
                        v
              ColdFusion Application
              (no code changes required)
```

---

## Scripts Execution Order

### Prerequisites

1. **SQL Server** installed and accessible
2. **.NET 8 SDK** installed
3. **dotnet-script** tool installed:
   ```bash
   dotnet tool install -g dotnet-script
   ```
4. **Existing databases**:
   - `VIPER` database with `users.Person` table
   - `Efforts` database (legacy database with existing data)

### Step-by-Step Execution

#### Step 1: Create Modern Effort Database

```bash
cd web\Areas\Effort\Scripts
dotnet script CreateEffortDatabase.cs
```

**What this does:**
- Creates the `Effort` database
- Creates all 19 tables with modern schema
- Sets up foreign keys to `VIPER.users.Person`
- Creates indexes for performance

**Tables created:**
- Core tables: Roles, EffortTypes, Status, Persons, Courses, Records, Percentages, Sabbatics
- Authorization: UserAccess (CRITICAL)
- Lookup tables: Units, JobCodes, ReportUnits, ReviewYears, AlternateTitles, Months, Workdays
- Relationships: CourseRelationships, AdditionalQuestions
- Audit: AuditLog

**Expected output:**
```
============================================
Creating Effort Database
============================================
Creating database...
  ✓ Effort database created successfully.

Creating tables...
  ✓ Roles table created.
  ✓ EffortTypes table created.
  ...
  ✓ UserAccess table created (CRITICAL).
  ...
✓ All 19 tables created successfully
```

---

#### Step 2: Migrate Data from Legacy Database

```bash
dotnet script MigrateEffortData.cs
```

**What this does:**
- Migrates all data from `Efforts` database to `Effort` database
- Performs MothraId -> PersonId mapping via `VIPER.users.Person`
- Validates all migrations
- Preserves all data relationships

**Security Note:** The migration script automatically adds `ApplicationIntent=ReadOnly` to the legacy Effort database connection to prevent accidental modifications. You'll see this notification during execution:
```
ℹ Added ApplicationIntent=ReadOnly to Effort connection for safety
```

**Migration order:**
1. Lookup tables (Roles, EffortTypes, Status, Units, etc.)
2. Core tables (Persons, Courses, Records, Percentages)
3. Relationship tables (CourseRelationships, AdditionalQuestions)

**Expected output:**
```
============================================
Migrating Data from Efforts to Effort Database
============================================
Verifying prerequisites...
  ✓ Effort database found
  ✓ Legacy Efforts database found
  ✓ Legacy database contains 1234 effort records

Step 1: Migrate Lookup Tables
  ✓ Migrated 5 roles
  ✓ Migrated 3 effort types
  ...

Step 2: Migrate Core Tables
  ✓ Migrated 456 persons
  ✓ Migrated 789 courses
  ✓ Migrated 1234 effort records
  ...

Step 4: Validate Migration
  ✓ Roles: 5 records (matches legacy)
  ✓ Records: 1234 records (matches legacy)
  ...
```

**⚠ IMPORTANT:** Review warnings about unmapped records:
```
  ⚠ WARNING: 12 persons could not be mapped to VIPER.users.Person
```
These records will have `PersonId = 0` and need manual remediation.

---

#### Step 3: Create Shadow Database for ColdFusion

```bash
dotnet script CreateEffortShadow.cs
```

**What this does:**
- Creates `EffortShadow` database
- Creates 19 compatibility views mapping modern schema to legacy naming
- Creates 122 wrapper stored procedures for ColdFusion
- Maps PersonId -> MothraId for legacy compatibility

**Views created:**
- `tblEffort` -> maps to `Effort.dbo.Records`
- `tblPerson` -> maps to `Effort.dbo.Persons`
- `userAccess` -> maps to `Effort.dbo.UserAccess` (CRITICAL)
- 16 additional views for all legacy tables

**Expected output:**
```
============================================
Creating EffortShadow Compatibility Database
============================================
Verifying prerequisites...
  ✓ Effort database found
  ✓ Effort database contains 1234 records

Creating EffortShadow database...
  ✓ EffortShadow database created successfully.

Step 1: Create Compatibility Views
Creating view: tblEffort (effort records)...
  ✓ View created
Creating view: userAccess (CRITICAL - user authorization)...
  ✓ View created
...
✓ All 19 views created successfully

Step 2: Create Wrapper Stored Procedures
⚠ MANUAL STEP REQUIRED: Create the 122 wrapper stored procedures
```

**⚠ MANUAL STEP:** The 122 wrapper stored procedures need to be created based on the legacy `Efforts` database stored procedures. See [`Shadow_Database_Guide.md`](../docs/Shadow_Database_Guide.md) for the complete list.

---

#### Step 4: Configure Security Permissions

**Note:** Database permissions are configured by the DBA outside of migration scripts.

**DBA will grant:**
- ColdFusion application login access to EffortShadow database
- ColdFusion application login access to Effort database (via wrappers)
- ColdFusion application login access to VIPER.users.Person
- VIPER2 application service account access to [VIPER].[effort] schema

**VIPER2 Authorization:**
- Uses trusted application service account with full schema access
- Authorization enforced at application layer using RAPS permissions
- Department-level access controlled via UserAccess table
- See [Authorization_Design.md](../Docs/Authorization_Design.md) for complete architecture

**Example SQL for DBA reference:**

```sql
-- Example - actual commands will vary by environment
USE master;
CREATE LOGIN [DOMAIN\ViperAppService] FROM WINDOWS;
CREATE LOGIN [DOMAIN\ColdFusionService] FROM WINDOWS;

-- Add users to Effort database
USE Effort;
CREATE USER [DOMAIN\ViperAppService] FOR LOGIN [DOMAIN\ViperAppService];
EXEC sp_addrolemember 'EffortEditor', 'DOMAIN\ViperAppService';

-- Add users to EffortShadow database
USE EffortShadow;
CREATE USER [DOMAIN\ColdFusionService] FOR LOGIN [DOMAIN\ColdFusionService];
EXEC sp_addrolemember 'ColdFusionApp', 'DOMAIN\ColdFusionService';
```

---

## Verification Steps

### 1. Verify Effort Database

```sql
USE Effort;

-- Check record counts
SELECT 'Records' as TableName, COUNT(*) as RecordCount FROM Records
UNION ALL
SELECT 'Persons', COUNT(*) FROM Persons
UNION ALL
SELECT 'Courses', COUNT(*) FROM Courses
UNION ALL
SELECT 'UserAccess', COUNT(*) FROM UserAccess;

-- Check MothraId mapping
SELECT COUNT(*) as UnmappedPersons
FROM Persons
WHERE PersonId = 0;

-- Should return 0 or very few records
```

### 2. Verify EffortShadow Database

```sql
USE EffortShadow;

-- Check views exist
SELECT name FROM sys.views ORDER BY name;
-- Should return 19 views

-- Check stored procedures exist
SELECT name FROM sys.procedures ORDER BY name;
-- Should return 122 procedures

-- Test a view
SELECT TOP 10 * FROM tblEffort;
-- Should return effort records with MothraId (not PersonId)

-- Test userAccess view (CRITICAL)
SELECT COUNT(*) FROM userAccess;
-- Should match count from Effort.dbo.UserAccess WHERE IsActive = 1
```

### 3. Test ColdFusion Compatibility

```sql
USE EffortShadow;

-- Test the exact query ColdFusion uses
SELECT *
FROM tblEffort
WHERE effort_MothraID = 'test123';

-- Test authorization query
SELECT *
FROM userAccess
WHERE mothraID = 'test123';
```

---

## Rollback Procedures

If you need to rollback the migration:

### Rollback Step 4 (Permissions)
```sql
-- Drop roles from Effort database
USE Effort;
DROP ROLE IF EXISTS EffortReadOnly;
DROP ROLE IF EXISTS EffortEditor;
DROP ROLE IF EXISTS EffortAdmin;

-- Drop role from EffortShadow database
USE EffortShadow;
DROP ROLE IF EXISTS ColdFusionApp;
```

### Rollback Step 3 (Shadow Database)
```sql
USE master;
DROP DATABASE IF EXISTS EffortShadow;
```

### Rollback Step 2 (Data Migration)
```sql
USE Effort;

-- Truncate all tables in reverse dependency order
TRUNCATE TABLE AdditionalQuestions;
TRUNCATE TABLE CourseRelationships;
TRUNCATE TABLE AuditLog;
TRUNCATE TABLE UserAccess;
TRUNCATE TABLE Sabbatics;
TRUNCATE TABLE Percentages;
TRUNCATE TABLE Records;
TRUNCATE TABLE Courses;
TRUNCATE TABLE Persons;
-- ... continue for all tables
```

### Rollback Step 1 (Database Creation)
```sql
USE master;
DROP DATABASE IF EXISTS Effort;
```

---

## Next Steps After Migration

1. **Test ColdFusion Application**
   - Update ColdFusion datasource to point to `EffortShadow`
   - Test all CRUD operations
   - Verify authorization queries work

2. **Create Entity Framework Entities**
   - Scaffold EF entities from `Effort` database
   - Create DbContext class
   - Add to VIPER2 application

3. **Build New API Endpoints**
   - Create ASP.NET Core controllers
   - Implement business logic services
   - Add permission attributes

4. **Create Vue.js Frontend**
   - Build new UI components
   - Implement API service layer
   - Add Quasar components for forms/tables

5. **Data Validation**
   - Run comprehensive data validation queries
   - Fix any unmapped PersonId records
   - Verify all relationships are intact

6. **Performance Testing**
   - Test query performance
   - Add additional indexes if needed
   - Optimize slow queries

7. **Create 122 Wrapper Stored Procedures**
   - Export all 122 stored procedures from legacy `Efforts` database
   - Create wrapper versions in `EffortShadow` database
   - Each wrapper should query the views, not the `Effort` database directly

---

## Additional Resources

- **Planning Documents:**
  - [Effort Master Plan](../docs/EFFORT_MASTER_PLAN.md)
  - [Migration Guide](../docs/MIGRATION_GUIDE.md)
  - [Shadow Database Guide](../docs/Shadow_Database_Guide.md)
  - [Database Schema](../docs/Effort_Database_Schema.sql)
  - [Technical Reference](../docs/Technical_Reference.md)

- **Data Analysis:**
  - [Data Analysis Script](./EffortDataRemediation.cs)
  - [SQL Remediation Scripts](./SQL/)
