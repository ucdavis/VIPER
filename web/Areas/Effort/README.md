# Effort Database Modernization

This folder contains the Effort system migration scripts and documentation.

## Quick Start

**New to this project? Start here:**

1. **[Quick Start Guide](Docs/QUICK_START.md)** - Simple commands to run the migration scripts
2. **[Migration Guide](Docs/MIGRATION_GUIDE.md)** - Complete testing, validation, and troubleshooting guide

## Scripts

All migration scripts are in the [`Scripts/`](Scripts/) folder:

- **EffortDataAnalysis.cs** / **RunDataAnalysis.bat** - Analyzes data quality issues in legacy database (run FIRST)
- **EffortDataRemediation.cs** / **RunDataRemediation.bat** - Fixes identified data quality issues in legacy database
- **CreateEffortDatabase.cs** / **RunCreateDatabase.bat** - Creates the modern `[VIPER].[effort]` schema with 20 tables
- **MigrateEffortData.cs** / **RunMigrateData.bat** - Migrates all data from legacy Efforts database
- **CreateEffortReportingProcedures.cs** / **RunCreateReportingProcedures.bat** - Creates reporting stored procedures in `[effort]` schema
- **CreateEffortShadow.cs** / **RunCreateShadow.bat** - Creates shadow schema for ColdFusion compatibility
- **VerifyShadowProcedures.cs** / **RunVerifyShadow.bat** - Compares legacy vs shadow schema stored procedures to verify compatibility layer

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

**Shadow Schema**: `[VIPER].[EffortShadow]` (schema within VIPER database for ColdFusion compatibility)

**Migration Approach**:
1. Analyze data quality issues in legacy Efforts database (RunDataAnalysis.bat)
2. Remediate identified issues in legacy Efforts database (RunDataRemediation.bat)
3. Verify 0 critical issues remain (re-run RunDataAnalysis.bat)
4. Create modern schema in VIPER database
5. Migrate all data from legacy Efforts database
6. Create reporting stored procedures
7. Create shadow schema with views/procedures for ColdFusion
8. Update ColdFusion datasource to VIPER database ([EffortShadow] schema)
9. Begin VIPER2 development using modern schema

**Timeline**: Sprint 0 (2-3 weeks) for shadow schema setup + ColdFusion testing

## Support

If you encounter issues:
1. Check troubleshooting section in [QUICK_START.md](docs/QUICK_START.md)
2. Review validation queries in [MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
3. Check rollback procedures if needed
4. Document specific errors for analysis
