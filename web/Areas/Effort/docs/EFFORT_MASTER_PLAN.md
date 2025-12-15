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
- **Efforts database** with 21 tables, 93 active stored procedures
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

## Database Access Strategy - Hybrid Approach

### Overview
The migration will use a **hybrid approach** optimized for performance and maintainability:
- **Entity Framework with LINQ** will replace simple CRUD stored procedures (~37 SPs)
- **Complex reporting SPs** will be migrated and modernized (~16 SPs)
- **Unused procedures** will not be migrated (~30 SPs)
- **Term Management**: Leverage existing VIPER vwTerms view for term data, create [effort].[TermStatus] for workflow tracking

### Rationale
- Simple CRUD operations: EF/LINQ provides better maintainability and type safety
- Complex reporting: Stored procedures are 10-30x faster for aggregations with temp tables
- Term data reuse: Avoid duplicating term reference data that already exists in VIPER
- This approach balances modern development practices with proven performance

### Term Management Strategy
Instead of creating a duplicate [effort].[Terms] table, we will:
1. **Use existing vwTerms** for term reference data (TermCode, Description, StartDate, EndDate, etc.)
2. **Create [effort].[TermStatus]** table for Effort-specific workflow tracking:
   - TermCode (PK, references vwTerms.TermCode)
   - Status (Harvested/Opened/Closed)
   - HarvestedDate, OpenedDate, ClosedDate
   - ModifiedBy, ModifiedDate for audit trail
3. **Benefits**:
   - No duplication of term data
   - Leverages existing VIPER infrastructure
   - Maintains Effort-specific workflow tracking
   - Better integration with other VIPER modules

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

**Total Stored Procedures in Legacy System:** 83

**Hybrid Approach - Best of Both Worlds:**
- **Replace with EF Repositories:** ~37 CRUD operation SPs
- **Migrate & Modernize:** 16 complex reporting SPs (for performance)
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

## Implementation Phases & Sprint Structure

### Phase 1: Database Updates (Sprint 0 - Pre-Development)
**Timeline:** Prior to Sprint 1 start
**Focus:** Database preparation and schema migration

#### Database Migration Decisions
- [x] Validate table usage in codebase (COMPLETED)
- [ ] Tables NOT to migrate (leave in legacy database):
  - [ ] AdditionalQuestion (no references found)
  - [ ] Months (no references found)
  - [ ] Sheet1 (no references found)
  - [ ] Workdays (no references found)
  - [ ] tblReviewYears (referenced but appears unused)
  - [ ] userAccess (disabled in UI, replaced by VIPER Application Approvers)
- [ ] Tables TO MIGRATE to new schema:
  - [ ] tblSabbatic → [effort].[Sabbaticals] (ACTIVELY USED for faculty leave tracking - critical for merit reports)
  - [ ] tblEffort → [effort].[Records]
  - [ ] tblPerson → [effort].[Persons]
  - [ ] tblCourses → [effort].[Courses]
  - [ ] tblPercent → [effort].[Percentages]
  - [ ] tblStatus → [effort].[Terms]
  - [ ] tblEffortType_LU → [effort].[EffortTypes]
  - [ ] tblRoles → [effort].[Roles]
- [ ] Create VIPER.effort schema in VIPER database
- [ ] Migrate tables with PersonId integration to [VIPER].[users].[Person]
- [ ] Rename tblEffortType_LU to PercentType
- [ ] Create foreign key constraints and unique constraints
- [ ] Replace MothraId with PersonId throughout (FK to [VIPER].[users].[Person])

#### Stored Procedure Migration Preparation
- [ ] Analyze and categorize all 83 stored procedures
- [ ] Create migration script templates for reporting SPs
- [ ] Document table/field name mappings for SP updates:
  - tblEffort → [effort].[Records]
  - effort_MothraID → PersonId
  - tblPerson → [effort].[Persons]
  - person_MothraID → PersonId
  - tblSabbatic → [effort].[Sabbaticals]
  - sab_MothraID → PersonId
- [ ] Migrate helper functions to new schema:
  - [ ] getFirstTermInYear - Converts year to first term code
  - [ ] getLastTermInYear - Converts year to last term code
- [ ] Create test harness for validating SP migrations
- [ ] Identify SP dependencies on external databases (AAUD, Banner)

#### Data Migration Requirements
- [ ] Map effort_MothraID → PersonId (int FK to [VIPER].[users].[Person])
- [ ] Map person_MothraID → PersonId (int FK to [VIPER].[users].[Person])
- [ ] Map percent_MothraID → PersonId (int FK to [VIPER].[users].[Person])
- [ ] Map ModifiedBy fields → PersonId references
- [ ] Create repeatable migration scripts (RedGate or SQL scripts)

### Agile Sprint Structure

Each 2-week sprint delivers a complete, testable feature with database, API, and UI components. This vertical slicing approach enables continuous stakeholder feedback and reduces integration risk.

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
- RAPS integration and claims
- Department-based restrictions
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
