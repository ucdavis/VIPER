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

## Agile Sprint Structure

### Sprint Planning Overview

Each 2-week sprint delivers a complete, testable feature with database, API, and UI components. This vertical slicing approach enables continuous stakeholder feedback and reduces integration risk.

## Sprint 1: Foundation & Core Data Model

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
  - [ ] EffortPerson entity with department relationships
  - [ ] EffortCourse entity with enrollment tracking
  - [ ] EffortRecord entity for effort assignments

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

### Success Criteria
- ✅ Can view existing terms in the system
- ✅ VIPER2 recognizes Effort area
- ✅ Entity Framework successfully connects and queries

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

## Sprint 3: Instructor Management

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

### Success Criteria
- ✅ View and manage instructor records by department
- ✅ Role-based access working correctly
- ✅ Basic instructor import functional

---

## Sprint 4: Course Management

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

## Sprint 5: Basic Effort Entry

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

## Sprint 6: Data Import - Phase 1

### Objectives
- Automate course and instructor data synchronization
- Implement comprehensive import validation
- Support bulk data operations

### Deliverables
- [ ] **CREST Integration**
  - [ ] Course session offering import
  - [ ] Personnel data synchronization
  - [ ] Import scheduling and automation

- [ ] **Import Validation**
  - [ ] Data integrity checks
  - [ ] Duplicate detection and resolution
  - [ ] Error reporting and logging

- [ ] **Import UI**
  - [ ] Import status dashboard
  - [ ] Manual import triggers
  - [ ] Import history and logs

### Success Criteria
- ✅ Import courses and instructors from external systems
- ✅ Data validation prevents corruption
- ✅ Import process is reliable and auditable

---

## Remaining Sprints Summary

**Sprint 7:** Effort Verification Workflow
- Self-service verification portal
- Email notifications and deadlines
- Verification status tracking

**Sprint 8:** Permission & Access Control
- RAPS integration and claims
- Department-based restrictions
- Role enforcement across all features

**Sprint 9:** Percentage Assignments
- Admin/Clinical percentage allocation
- Academic year tracking
- Historical percentage management

**Sprint 10:** Course Relationships & Advanced Features
- Parent/child course relationships
- Guest instructor support
- Additional questions and comments

**Sprint 11:** Basic Reporting Suite
- Instructor effort summary
- Department reports
- Zero effort validation

**Sprint 12:** Clinical Integration
- Clinical scheduler import
- Clinical effort tracking
- Volunteer and clinical percentages

**Sprint 13:** Advanced Reporting - Merit & Evaluation
- Merit review reports
- Multi-year analysis
- Complex aggregations

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
