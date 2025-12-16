# VIPER Effort System Migration - Master Plan

**Project:** Complete modernization of Effort System from ColdFusion to VueJS/.NET
**Target Platform:** VIPER2 (VueJS + .NET 8 + Entity Framework + SQL Server)
**Strategy:** Agile feature-based vertical slicing with continuous stakeholder feedback
**Timeline:** 16 two-week sprints delivering incremental value

---

## Executive Summary

This master plan outlines an agile, feature-driven migration strategy for transforming the legacy ColdFusion Effort System into a modern VueJS/.NET application within the existing VIPER2 architecture. The approach uses vertical slicing to deliver complete features incrementally, enabling continuous stakeholder feedback and value delivery throughout the migration process.

## Current State Analysis

### Legacy System (ColdFusion)
- **85 CFM files** including 40+ reports and **8 CFC components**
- **Complex workflows**: Term lifecycle, multi-system imports, effort verification
- **Efforts database** with 21 tables, 87 active stored procedures (89 total, 2 obsolete)
- **Missing database constraints**: 0 foreign keys, 4 tables without primary keys
- **Security concerns**: Limited input validation, SQL injection risks
- **Integration dependencies**: CREST, Banner, Clinical Scheduler, AAUD systems
- **Stored procedure dependencies**: Extensive SP usage for CRUD, business logic, and reporting

### Target System (VIPER2)
- **Modern stack**: Vue 3 + TypeScript + .NET 8 + Entity Framework
- **Areas-based architecture** for modular organization (existing: ClinicalScheduler, CTS, RAPS, etc.)
- **Integrated security** with RAPS authentication
- **API-first design** with comprehensive validation
- **Quasar UI framework** for consistent, accessible components

---

## Database Strategy - Shadow Schema Approach

### Architecture Overview

The migration uses a **Shadow Schema** pattern to enable parallel operation of ColdFusion and VIPER2:

**Two Schemas Within VIPER Database:**
1. **[effort] Schema** - Modernized schema (single source of truth)
2. **[EffortShadow] Schema** - Compatibility layer (views + rewritten SPs only)

**Key Benefits:**
- ✅ ColdFusion continues running unchanged during entire VIPER2 development
- ✅ Single source of truth - no data synchronization needed
- ✅ Both applications share same data in real-time
- ✅ Easy cleanup - drop [EffortShadow] schema when ColdFusion retired
- ✅ Low risk - can rollback by changing one datasource configuration
- ✅ Same-database foreign keys enable referential integrity to [users].[Person]

### [effort] Schema (Primary)

**Purpose:** Modern schema with proper constraints and relationships

**Schema Design:**
- Clean table names: Records, Persons, Courses, Percentages, Terms, etc.
- MothraId (varchar) → PersonId (int FK to VIPER.users.Person)
- Decimal types for precision (percentages, units)
- Comprehensive audit fields (CreatedDate, ModifiedDate, ModifiedBy)
- Proper foreign keys and unique constraints
- Optimized indexes for EF Core queries

**Data Access:**

- CRUD operations replaced with Entity Framework Core + LINQ repositories
- 16 complex reporting stored procedures for performance-critical aggregations
- MothraId → PersonId mapping handled by Entity Framework navigation properties

### [EffortShadow] Schema (Compatibility Layer)

**Purpose:** Enable ColdFusion to use new schema transparently

**Contains:** Views and wrapper SPs only (NO DATA)

**Compatibility Views:**
```
tblEffort        → VIEW of [Effort].[dbo].[Records]
tblPerson        → VIEW of [Effort].[dbo].[Persons]
tblCourses       → VIEW of [Effort].[dbo].[Courses]
tblPercent       → VIEW of [Effort].[dbo].[Percentages]
tblStatus        → VIEW of [Effort].[dbo].[Terms]
tblSabbatic      → VIEW of [Effort].[dbo].[Sabbaticals]
tblRoles         → VIEW of [Effort].[dbo].[Roles]
tblEffortType_LU → VIEW of [Effort].[dbo].[EffortTypes]
```

**Wrapper Stored Procedures:**
- 37 wrapper SPs with legacy signatures
- Each delegates to corresponding Effort database SP
- ColdFusion calls wrappers, wrappers call real SPs
- Transparent to ColdFusion application

**ColdFusion Configuration Change:**
- Update datasource: `Efforts` → `EffortShadow`
- That's the ONLY change needed in ColdFusion!

**Detailed Implementation:** See `Shadow_Database_Implementation_Plan.md`

### Data Access Strategy

**VIPER2 Application:**
- Connects directly to **Effort** database
- Uses Entity Framework with LINQ for CRUD operations
- Modern repository and service layer patterns
- Type-safe, testable, maintainable code

**ColdFusion Application:**
- Connects to **EffortShadow** database
- Uses views (read operations) and wrapper SPs (write operations)
- Completely unchanged code
- Views/wrappers handle all translation to new schema

**Both Applications:**
- Read and write to same underlying Effort database
- See changes immediately (real-time consistency)
- No dual-write complexity
- Single source of truth

### Migration Approach by Category

#### 1. CRUD Operations (37 SPs) → Entity Framework Repositories
Replace with repository pattern and services:
- **Term Management:** OpenTermAsync(), CloseTermAsync(), etc.
- **Instructor Operations:** Standard CRUD via EF repositories
- **Course Operations:** Standard CRUD via EF repositories
- **Effort Operations:** Standard CRUD via EF repositories
- **Benefits:** Type safety, easier testing, better maintainability

#### 2. Complex Reporting (16 Active SPs) → Migrate & Modernize
Keep as stored procedures but update to new schema:

**Merit & Promotion Reports (6 SPs to migrate):**
- `usp_getEffortReportMeritSummaryForLairmore` → `[effort].[sp_merit_summary_report]`
- `usp_getEffortReportMeritSummary` → `[effort].[sp_merit_summary]`
- `usp_getEffortReportMerit` → `[effort].[sp_merit_report]`
- `usp_getEffortReportMeritMultiYearWithExcludeTerms` → `[effort].[sp_merit_multiyear]`
- `usp_getEffortReportMeritAverage` → `[effort].[sp_merit_average]`
- `usp_getEffortReportMeritWithClinPercent` → `[effort].[sp_merit_clinical_percent]`

**Department Analysis (3 SPs to migrate):**
- `usp_getEffortDeptActivityTotalWithExcludeTerms` → `[effort].[sp_dept_activity_summary]`
- `usp_getDepartmentCountByJobGroupWithExcludeTerms` → `[effort].[sp_dept_job_group_count]`
- `usp_getEffortReportDeptSummary` → `[effort].[sp_dept_summary]`

**Instructor Reports (5 SPs to migrate):**
- `usp_getEffortInstructorActivity` → `[effort].[sp_instructor_activity]`
- `usp_getEffortInstructorActivityWithExcludeTerms` → `[effort].[sp_instructor_activity_exclude]`
- `usp_getInstructorEvals` → `[effort].[sp_instructor_evals]`
- `usp_getInstructorEvalsMultiYearWithExclude` → `[effort].[sp_instructor_evals_multiyear]`
- `usp_getInstructorEvalsAverageWithExcludeTerms` → `[effort].[sp_instructor_evals_average]`

**Other Reports (2 SPs to migrate):**
- `usp_getEffortReport` → `[effort].[sp_effort_general_report]`
- `usp_getZeroEffortInstructors` → `[effort].[sp_zero_effort_check]`

**Benefits:** Proven performance (10-30x faster), maintains complex logic, optimized for large datasets

#### 3. Service Layer Wrapper Pattern
All stored procedures will be wrapped in service classes:
```csharp
public class MeritReportingService
{
    public async Task<List<MeritSummaryDto>> GetMeritSummaryAsync(string termCode, string dept)
    {
        return await _context.Set<MeritSummaryDto>()
            .FromSqlRaw("EXEC effort.sp_merit_summary_report @p0, @p1", termCode, dept)
            .ToListAsync();
    }
}
```

#### 4. Unused SPs (~30 SPs) - NOT MIGRATING
Development artifacts and unused procedures will not be migrated

### Migration Summary

**Total Stored Procedures in Legacy System:** 87 active (89 total, 2 obsolete)

**Hybrid Approach - Best of Both Worlds:**

- **Replace with Entity Framework Core:** ~37 CRUD operation SPs replaced with EF repositories + LINQ
- **Migrate & Modernize:** 16 complex reporting SPs (for performance-critical aggregations)
- **Not Migrating (Unused):** ~30 development iterations and unused SPs

**Key Benefits:**
- **Performance:** Complex reports remain 10-30x faster with SPs
- **Maintainability:** CRUD operations use modern EF patterns
- **Type Safety:** Service layer provides strongly-typed interfaces
- **Testability:** Repository pattern enables unit testing
- **Proven Logic:** Critical reporting logic remains optimized

### Migration Naming Convention
| Old Name Pattern | New Name Pattern | Example |
|-----------------|------------------|---------|
| usp_get*Report* | sp_effort_* | usp_getEffortReportMeritSummaryForLairmore → sp_effort_merit_summary_report |
| usp_insert/update/delete* | (Remove - use EF) | usp_insertInstructor → InstructorRepository.Add() |
| WithExcludeTerms | _exclude | usp_getEffortInstructorActivityWithExcludeTerms → sp_effort_instructor_activity_exclude |

---

## Authorization Design

The Effort system integrates with VIPER2's existing RAPS-based permission architecture to provide both system-wide and department-level authorization.

### Two-Layer Authorization Model

1. **RAPS Permissions (System-Wide)**: Controls what actions users can perform
   - `SVMSecure.Effort.Admin` - Full administrative access
   - `SVMSecure.Effort.Manage` - Manage all departments
   - `SVMSecure.Effort.ViewDept` - View department-level data
   - `SVMSecure.Effort.EditDept` - Edit department-level data
   - `SVMSecure.Effort.VerifyDept` - Verify department effort records
   - `SVMSecure.Effort.ViewOwn` - View own effort records
   - `SVMSecure.Effort.EditOwn` - Edit own effort records
   - `SVMSecure.Effort.RunReports` - Access reporting functionality

2. **UserAccess Table (Department-Specific)**: Controls which departments users can access
   - Users with department-level permissions (ViewDept, EditDept, VerifyDept) are filtered by their UserAccess assignments
   - Admin and Manage permissions bypass department filtering

### Implementation Components

- **EffortPermissions.cs**: Strongly-typed permission constants
- **IEffortPermissionService**: Service interface for permission checks
- **EffortPermissionService**: Implementation following VIPER2's ClinicalScheduler pattern
- **PermissionAttribute**: Applied to controllers/actions for RAPS authorization
- **Service Layer**: All controllers use EffortPermissionService for department-level filtering

### Key Benefits Over Legacy System

- ✅ Consistent with VIPER2 architecture (follows ClinicalScheduler pattern)
- ✅ Centralized permission management through RAPS
- ✅ Type-safe permission constants prevent typos
- ✅ Department-level access preserved from legacy system
- ✅ Permission hierarchy (Admin > Manage > Department-specific)
- ✅ Comprehensive audit trail (ModifiedBy, ModifiedDate, IsActive)
- ✅ Service layer enables testability and maintainability
- ✅ Defense in depth (application layer + optional row-level security)

**Detailed Documentation**: See [Authorization_Design.md](Authorization_Design.md) for complete architecture, implementation guide, code examples, and migration path.

---

## Implementation Phases & Sprint Structure

### Phase 1: Shadow Schema Setup (Sprint 0 - Pre-Development)
**Timeline:** Prior to Sprint 1 start (1-2 weeks)
**Focus:** Database preparation, data migration, and shadow schema creation
**Detailed Plan:** See `Shadow_Database_Guide.md`

#### Week 1: Data Quality & Migration

**Step 1: Data Quality Analysis & Remediation (BEFORE creating new database)**
- [ ] Run RunDataAnalysis.bat to identify critical data quality issues in legacy Efforts database
- [ ] Review analysis report
- [ ] Run RunDataRemediation.bat to fix all issues in legacy Efforts database:
  - [ ] Task 1: Merge duplicate courses (same CRN/Term/Units)
  - [ ] Task 2: Fix missing department codes
  - [ ] Task 3: Generate placeholder CRNs where needed
  - [ ] Task 4: Delete negative hours records
  - [ ] Task 5: Delete invalid MothraIds
  - [ ] Task 6: Consolidate guest instructors → VETGUEST
  - [ ] Task 7: Add missing persons to VIPER
  - [ ] Task 8: Remap duplicate person records
- [ ] Re-run RunDataAnalysis.bat to verify 0 critical issues remain (0% unmapped MothraIds)

**Step 2: Create Effort Database**
- [ ] Create [VIPER].[effort] schema (modernized schema within VIPER database)
- [ ] Create all 20 tables with proper constraints:
  - [ ] Records (tblEffort)
  - [ ] Persons (tblPerson)
  - [ ] Courses (tblCourses)
  - [ ] Percentages (tblPercent)
  - [ ] Terms (tblStatus)
  - [ ] Sabbaticals (tblSabbatic)
  - [ ] Roles (tblRoles)
  - [ ] EffortTypes (tblEffortType_LU)
  - [ ] CourseRelationships (new)
  - [ ] AdditionalQuestions (new)
  - [ ] Audits (new)
  - [ ] UserAccess (authorization)
  - [ ] Plus 8 additional tables

**Tables NOT Migrated:**

- AdditionalQuestion - legacy table with 0 rows, unused
- Months - no references found in legacy code or stored procedures
- Sheet1 - no references found
- Workdays - no references found in legacy code or stored procedures
- tblReviewYears - partially implemented feature, never fully deployed (only 1 test record), superseded by dynamic year selection in reports

**Tables Migrated (verified in active use):**

- userAccess - **MIGRATED** (actively used in Access.cfc and manageAccess.cfm for department-level authorization)

**Step 3: Migrate Data**
- [ ] Migrate reference data (Roles, EffortTypes, Terms)
- [ ] Migrate courses with variable-unit support
- [ ] Migrate persons with PersonId mapping
- [ ] Migrate effort records
- [ ] Migrate percentages
- [ ] Migrate sabbaticals
- [ ] Validate record counts match

**Step 4: Create Reporting Stored Procedures in Effort Database**
- [ ] Run CreateEffortReportingProcedures.cs to create 16 reporting SPs
- [ ] Test each reporting procedure with sample data
- [ ] Validate performance meets requirements
- [ ] Document SP parameters and return values

**Note:** CRUD operations will be handled by Entity Framework repositories, not stored procedures.

#### Week 2: Shadow Schema Creation and ColdFusion Update

**Step 5: Create EffortShadow Schema**
- [ ] Create [EffortShadow] schema in VIPER database
- [ ] Create 19 compatibility views:
  - [ ] tblEffort → Records view
  - [ ] tblPerson → Persons view
  - [ ] tblCourses → Courses view
  - [ ] tblPercent → Percentages view
  - [ ] tblStatus → Terms view
  - [ ] tblSabbatic → Sabbaticals view
  - [ ] tblRoles → Roles view
  - [ ] tblEffortType_LU → EffortTypes view

**Step 6: Create Wrapper Stored Procedures**
- [ ] Create 87 wrapper SPs in EffortShadow (rewritten from legacy)
- [ ] Test each wrapper delegates correctly
- [ ] Verify all CRUD operations work through shadow layer

**Step 7: Database Permissions (DBA Responsibility)**
- [ ] DBA grants ColdFusion login access to EffortShadow
- [ ] DBA grants ColdFusion login access to Effort (via wrappers)
- [ ] DBA grants ColdFusion login access to VIPER.users.Person
- [ ] DBA grants VIPER2 application service account access to Effort database

**Note:** Database permissions are configured by the DBA outside of migration scripts. The VIPER2 application uses a trusted service account with full schema access, and authorization is enforced at the application layer (see Authorization Design below).

**Step 8: Update ColdFusion**
- [ ] Change datasource from "Efforts" to "EffortShadow"
- [ ] Test all ColdFusion functionality:
  - [ ] CRUD operations
  - [ ] Reporting
  - [ ] Term management
  - [ ] Imports
  - [ ] Verification workflow

**Step 9: Performance Validation**
- [ ] Benchmark view query performance
- [ ] Benchmark wrapper SP performance
- [ ] Ensure no degradation from baseline

#### Success Criteria for Phase 1
✅ [effort] schema contains all migrated data
✅ [EffortShadow] schema created with all views and rewritten SPs
✅ ColdFusion updated to use [EffortShadow] schema
✅ All ColdFusion functionality working correctly
✅ Performance meets or exceeds baseline
✅ Both ColdFusion and VIPER2 can access same data

---

### Agile Sprint Structure

Each 2-week sprint delivers a complete, testable feature with database, API, and UI components. This vertical slicing approach enables continuous stakeholder feedback and reduces integration risk.

**Note:** ColdFusion continues running on EffortShadow throughout all sprints. VIPER2 development proceeds incrementally while both systems share the same underlying Effort database.

## Sprint 1: Foundation & Core Data Model (Phase 2)

### Objectives
- Create Effort area in VIPER2 project structure
- Establish core Entity Framework models
- Set up basic API infrastructure
- Implement simple term viewing functionality

### Deliverables
- [ ] **Effort Area Setup**
  - [ ] Create /web/Areas/Effort directory structure
  - [ ] Set up Models, Controllers, Services folders
  - [ ] Configure area routing and dependencies

- [ ] **Core Data Models**
  - [ ] EffortTerm entity with status management
  - [ ] EffortPerson entity with PersonId FK to [VIPER].[users].[Person]
  - [ ] EffortCourse entity with enrollment tracking
  - [ ] EffortRecord entity with PersonId references

- [ ] **Database Infrastructure**
  - [ ] EffortDbContext configuration
  - [ ] Initial Entity Framework migration
  - [ ] Seed data for reference tables (roles, session types)

- [ ] **Basic API**
  - [ ] TermsController with GET endpoints
  - [ ] Basic repository pattern implementation
  - [ ] Service layer foundation

- [ ] **Simple UI**
  - [ ] Vue components for term listing (read-only)
  - [ ] Basic navigation in VIPER2 structure
  - [ ] Quasar components integration

- [ ] **Stored Procedure Replacements**
  - [ ] Replace usp_getAllTerms with TermRepository.GetAllAsync()
  - [ ] Replace usp_getTermByCode with TermRepository.GetByCodeAsync()
  - [ ] Replace usp_getCurrentTerm with TermService.GetCurrentTermAsync()

### Success Criteria
- ✅ Can view existing terms in the system
- ✅ VIPER2 recognizes Effort area
- ✅ Entity Framework successfully connects and queries
- ✅ Basic CRUD SPs replaced with EF repositories

---

## Sprint 2: Term Management (Critical Path)

### Objectives
- Complete term lifecycle management workflow
- Implement business rules for term state transitions
- Enable administrators to control effort entry periods
- Establish audit logging for administrative actions

### Deliverables
- [ ] **Term Lifecycle API**
  - [ ] Open Term endpoint with validation
  - [ ] Close Term with business rule checks (no zero enrollment)
  - [ ] Reopen Term functionality
  - [ ] Unopen Term capability

- [ ] **Term Management Service**
  - [ ] TermService with business logic
  - [ ] Term status validation
  - [ ] Audit trail creation
  - [ ] Permission checking (ManageTerms role)

- [ ] **Vue Components**
  - [ ] TermManagement.vue with action buttons
  - [ ] Term status display with color coding
  - [ ] Confirmation dialogs for term actions
  - [ ] Success/error notifications

- [ ] **Stored Procedure Migration**
  - [ ] Replace usp_openTerm with TermService.OpenTermAsync
  - [ ] Replace usp_closeTerm with TermService.CloseTermAsync
  - [ ] Replace usp_reopenTerm with TermService.ReopenTermAsync
  - [ ] Replace usp_unopenTerm with TermService.UnopenTermAsync

### Success Criteria
- ✅ Administrators can manage complete term lifecycle
- ✅ Business rules enforced (can't close term with issues)
- ✅ All term actions logged for audit
- ✅ Legacy stored procedures replaced with service layer

---

## Sprint 3: Core Operations - Instructors (Phase 3)

### Objectives
- Enable comprehensive instructor data management
- Implement department-based access control
- Support instructor import from external systems

### Deliverables
- [ ] **Instructor CRUD Operations**
  - [ ] InstructorController with full CRUD endpoints
  - [ ] InstructorService with business logic
  - [ ] Department assignment validation

- [ ] **Permission-Based Access**
  - [ ] ViewDept vs ViewAllDepartments role checking
  - [ ] Department filtering in API responses
  - [ ] Security policies for instructor access

- [ ] **Basic Import Integration**
  - [ ] CREST personnel data synchronization
  - [ ] Import validation and error handling
  - [ ] Instructor creation from external data

- [ ] **Vue Components**
  - [ ] InstructorList.vue with search and filtering
  - [ ] InstructorDetail.vue showing effort history
  - [ ] Basic instructor editing interface

- [ ] **Stored Procedure Replacements**
  - [ ] Replace usp_insertInstructor with InstructorRepository.AddAsync()
  - [ ] Replace usp_updateInstructor with InstructorRepository.UpdateAsync()
  - [ ] Replace usp_deleteInstructor with InstructorRepository.DeleteAsync()
  - [ ] Replace usp_getInstructorByID with InstructorRepository.GetByIdAsync()
  - [ ] Replace usp_getInstructorsByDept with InstructorRepository.GetByDepartmentAsync()

### Success Criteria
- ✅ View and manage instructor records by department
- ✅ Role-based access working correctly
- ✅ Basic instructor import functional
- ✅ Instructor CRUD SPs replaced with EF repositories

---

## Sprint 4: Core Operations - Courses & Relationships (Phase 3)

### Objectives
- Complete course catalog management
- Support enrollment tracking and department ownership
- Handle course-term relationships

### Deliverables
- [ ] **Course CRUD Operations**
  - [ ] CourseController with full CRUD endpoints
  - [ ] Course validation (enrollment, department)
  - [ ] Term-specific course management

- [ ] **Course Import**
  - [ ] Banner course data synchronization
  - [ ] CRN validation and conflict resolution
  - [ ] Enrollment tracking updates

- [ ] **Vue Components**
  - [ ] CourseList.vue with term filtering
  - [ ] CourseDetail.vue with enrollment info
  - [ ] Course editing with validation

### Success Criteria
- ✅ Manage complete course catalog for each term
- ✅ Import courses from Banner system
- ✅ Enrollment tracking accurate

---

## Sprint 5: Basic Effort Entry (Phase 4)

### Objectives
- Enable core effort recording functionality
- Support session type classification and hours/weeks tracking
- Implement effort validation rules

### Deliverables
- [ ] **Core Effort Recording**
  - [ ] EffortController with CRUD operations
  - [ ] Hours vs Weeks logic (CLI = Weeks, others = Hours)
  - [ ] Instructor-Course assignment validation

- [ ] **Business Rules**
  - [ ] Session type validation
  - [ ] Effort calculation logic
  - [ ] Term status checking (only open terms)

- [ ] **Vue Components**
  - [ ] EffortEntry.vue with session type dropdown
  - [ ] Dynamic hours/weeks switching
  - [ ] Effort validation feedback

### Success Criteria
- ✅ Record basic teaching effort assignments
- ✅ Session type logic working (CLI vs others)
- ✅ Business rules enforced

---

## Sprint 6: Import Processes (Phase 5)

### Objectives
- Automate course and instructor data synchronization
- Implement comprehensive import validation
- Support bulk data operations
- Replace loop-based imports with efficient bulk operations

### Deliverables
- [ ] **CREST Integration Service**
  - [ ] Course session offering import from CREST database
  - [ ] Personnel data synchronization from AAUD
  - [ ] Replace Import.cfm with .NET service
  - [ ] Bulk insert operations (replace individual usp_createCourse calls)

- [ ] **Clinical Scheduler Integration**
  - [ ] Clinical rotation import service
  - [ ] Replace importClinical.cfm with .NET service
  - [ ] Weekly schedule synchronization
  - [ ] Unmatched rotation handling

- [ ] **Import Infrastructure**
  - [ ] Staging tables for validation
  - [ ] Bulk operations with EF Core
  - [ ] Transaction rollback capability
  - [ ] Import orchestration service

- [ ] **Import UI**
  - [ ] Import status dashboard
  - [ ] Manual import triggers
  - [ ] Import history and audit logs
  - [ ] Validation error reporting

### Success Criteria
- ✅ Import courses and instructors from external systems
- ✅ Data validation prevents corruption
- ✅ Import process is reliable and auditable
- ✅ Bulk operations improve performance over legacy loop-based approach

---

## Remaining Sprints Summary

**Sprint 7: Effort Verification (Phase 6)**
- Self-service verification portal
- Email notifications and deadlines
- Verification status tracking

**Sprint 8: Permission & Access Control**
- Implement EffortPermissionService (see [Authorization_Design.md](Authorization_Design.md))
- RAPS permission integration (SVMSecure.Effort.*)
- Department-based access control via UserAccess table
- Role enforcement across all features

**Sprint 9: Percent Assignment (Phase 7)**
- Admin/Clinical percentage allocation
- Academic year tracking
- Historical percentage management

**Sprint 10:** Course Relationships & Advanced Features
- Parent/child course relationships
- Guest instructor support
- Additional questions and comments

**Sprint 11: Basic Reports with LINQ (Phase 8)**
- Instructor effort summary reports
- Department comparison reports
- Zero effort validation
- **Create Reporting Services:**
  - `InstructorReportingService` with LINQ queries
  - `DepartmentReportingService` with optimized queries
  - Implement caching strategy for expensive reports
  - Add performance monitoring

**Sprint 12: Clinical Integration**
- Clinical scheduler import
- Clinical effort tracking
- Volunteer and clinical percentages
- **Clinical Reporting Services:**
  - Clinical effort calculations using LINQ
  - Integration with evaluation data
  - Volunteer hour tracking

**Sprint 13: Merit and Promotion Multi-Year Reports (Phase 9)**
- Merit review reports with sabbatical exclusions
- Multi-year analysis spanning academic periods
- Complex aggregations and comparisons
- **Create MeritReportingService:**
  - Implement complex merit calculations with LINQ
  - Use compiled queries for performance
  - Add sabbatical term exclusion logic
  - Implement year-to-term conversions using TermCodeService
  - Consider raw SQL for most complex aggregations if needed

**Sprint 14:** Data Migration & Parallel Running
- Historical data migration
- Validation and reconciliation
- Parallel system operation

**Sprint 15:** Performance & Polish
- Performance optimization
- UI/UX refinements
- Documentation

**Sprint 16:** Production Deployment
- Security audit
- Load testing
- Go-live support

---

## Risk Management & Mitigation

### High Priority Risks
1. **Feature Scope Creep**
   - *Risk*: Sprints expanding beyond 2-week capacity
   - *Mitigation*: Strict scope management + backlog grooming + stakeholder agreement

2. **Integration Complexity**
   - *Risk*: External system dependencies causing delays
   - *Mitigation*: Early integration testing + mock services + parallel development

3. **Data Migration Complexity**
   - *Risk*: Historical data issues during Sprint 14
   - *Mitigation*: Early data analysis + incremental migration + comprehensive testing

4. **User Adoption Challenges**
   - *Risk*: Resistance to new workflows
   - *Mitigation*: Sprint demos + early feedback + gradual feature introduction

### Medium Priority Risks
1. **Performance Issues**
   - *Risk*: EF Core performance not meeting expectations
   - *Mitigation*: Performance testing each sprint + optimization backlog items

---

## Post-Deployment Cleanup

After the legacy ColdFusion Effort system is fully decommissioned and all features are migrated to the new VueJS/.NET application, the following cleanup steps should be performed:

### Database Cleanup

1. **Drop Legacy Preservation Columns from `effort.Audits`**:
   ```sql
   ALTER TABLE [effort].[Audits] DROP COLUMN LegacyAction;
   ALTER TABLE [effort].[Audits] DROP COLUMN LegacyCRN;
   ALTER TABLE [effort].[Audits] DROP COLUMN LegacyMothraID;
   -- Note: TermCode is retained as it provides term context for audit records
   ```

2. **Drop `[EffortShadow]` Schema** (views, triggers, wrapper stored procedures):
   ```sql
   -- Drop all views, triggers, and stored procedures in EffortShadow schema
   DROP SCHEMA [EffortShadow];
   ```

3. **Update ColdFusion Datasource Configuration**:
   - Remove the `EffortShadow` datasource from ColdFusion (if still configured)
   - Ensure no remaining references to the legacy Efforts database

4. **Archive/Drop Legacy Efforts Database**:
   - Take a final backup of the legacy `Efforts` database for historical reference
   - Drop the legacy database after confirming all data is safely migrated

### Code Cleanup

1. Remove any shadow view verification scripts that are no longer needed
2. Update documentation to remove references to the legacy compatibility layer
3. Remove any feature flags or conditional logic for legacy support

### Verification

Before performing cleanup:
- [ ] Confirm all ColdFusion Effort pages are disabled/retired
- [ ] Verify all users are using the new VueJS interface
- [ ] Run final data integrity checks between legacy and modern tables
- [ ] Take complete database backups

---

## Timeline Summary

| Sprint | Focus Area | Key Deliverables | Value Delivered |
|--------|------------|------------------|-----------------|
| **Sprint 1** | Foundation | Core models, basic term viewing | Infrastructure ready |
| **Sprint 2** | Term Management | Complete term lifecycle | Administrators can control system |
| **Sprint 3** | Instructors | Instructor CRUD, permissions | Manage faculty records |
| **Sprint 4** | Courses | Course management, import | Maintain course catalog |
| **Sprint 5** | Basic Effort | Core effort entry | Record teaching assignments |
| **Sprint 6** | Import | Automated data sync | Reduce manual data entry |
| **Sprints 7-16** | Advanced Features | Verification, reporting, polish | Complete system replacement |

**Implementation Approach**: Agile with vertical slicing
**Sprint Duration**: 2 weeks each
**Total Timeline**: 32 weeks (8 months)
**Value Delivery**: Starts Sprint 2, continuous thereafter

---

**Plan Status**: Draft v2.0 - Agile Sprint-Based Approach
**Last Updated**: September 18, 2025
**Next Review**: After each sprint retrospective
