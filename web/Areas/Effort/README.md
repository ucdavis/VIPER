# Effort Database Modernization

This folder contains the Effort system migration scripts and documentation.

## Quick Start

**New to this project? Start here:**

1. **[Quick Start Guide](Docs/QUICK_START.md)** - Simple commands to run the migration scripts
2. **[Migration Guide](Docs/MIGRATION_GUIDE.md)** - Complete testing, validation, and troubleshooting guide

## Scripts

All migration scripts are in the [`Scripts/`](Scripts/) folder:

- **CreateEffortDatabase.cs** - Creates the modern `[VIPER].[effort]` schema with 20 tables
- **MigrateEffortData.cs** - Migrates all data from legacy Efforts database
- **CreateEffortShadow.cs** - Creates shadow database for ColdFusion compatibility
- **RunCreateDatabase.bat** - User-friendly launcher for CreateEffortDatabase.cs
- **RunMigrateData.bat** - User-friendly launcher for MigrateEffortData.cs
- **RunCreateShadow.bat** - User-friendly launcher for CreateEffortShadow.cs

## Documentation

All documentation is in the [`Docs/`](Docs/) folder:

### Migration Documentation
- **[QUICK_START.md](Docs/QUICK_START.md)** - Quick reference for running scripts (commands only)
- **[MIGRATION_GUIDE.md](Docs/MIGRATION_GUIDE.md)** - Complete guide:
  - Testing and validation procedures
  - Rollback procedures
  - Success criteria
  - Sprint 0 execution plan (2-3 weeks)

### Planning Documentation
- **[EFFORT_MASTER_PLAN.md](Docs/EFFORT_MASTER_PLAN.md)** - 16-sprint roadmap

### Technical Documentation
- **[Effort_Database_Schema.sql](Docs/Effort_Database_Schema.sql)** - Complete database schema (for review)
- **[Technical_Reference.md](Docs/Technical_Reference.md)** - Schema mappings and field transformations
- **[Shadow_Database_Guide.md](Docs/Shadow_Database_Guide.md)** - ColdFusion compatibility layer
- **[Authorization_Design.md](Docs/Authorization_Design.md)** - VIPER2 authorization integration architecture
- **[CreateEffortDatabase.cs](Scripts/CreateEffortDatabase.cs)** - Schema creation script (executable)

## Architecture

**Modern Database**: `[VIPER].[effort]` schema (within VIPER database, not separate database)

**Shadow Database**: `EffortShadow` (separate database for ColdFusion compatibility)

**Migration Approach**:
1. Create modern schema in VIPER database
2. Migrate all data from legacy Efforts database
3. Create shadow database with views/procedures for ColdFusion
4. Update ColdFusion datasource to EffortShadow
5. Begin VIPER2 development using modern schema

**Timeline**: Sprint 0 (2-3 weeks) for shadow database setup + ColdFusion testing

## Support

If you encounter issues:
1. Check troubleshooting section in [QUICK_START.md](docs/QUICK_START.md)
2. Review validation queries in [MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
3. Check rollback procedures if needed
4. Document specific errors for analysis
