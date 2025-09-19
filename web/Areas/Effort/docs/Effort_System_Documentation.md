# Effort System Documentation

**Project:** VIPER (UC Davis School of Veterinary Medicine)
**System:** Faculty Effort Tracking and Reporting
**Technology:** ColdFusion (CFML) / SQL Server
**Analysis Date:** September 17, 2025

---

## Executive Summary

The Effort System is a comprehensive faculty workload tracking application that manages instructor teaching assignments, clinical effort, administrative responsibilities, and generates detailed reports for merit reviews and promotion processes. The system handles complex workflows including term management, data import from external systems, effort verification, and multi-dimensional reporting.

## System Architecture

### Technology Stack
- **Backend**: ColdFusion (CFML) with extensive stored procedure usage
- **Frontend**: Server-rendered pages with jQuery for dynamic interactions
- **Database**: SQL Server with multiple datasource connections
- **Authentication**: Integrated with RAPS (Role-based Access Permissions System)
- **Styling**: CSS with table-based layouts and custom styling

### Database Architecture
- **Primary Database**: Effort (21 tables, 93 active stored procedures)
- **Integration Databases**:
  - AAUD (Personnel data)
  - Courses (Academic course information)
  - EvalHarvest (Course evaluations)
  - ClinicalScheduler (Clinical assignments)
  - Logins (User access control)

### Stored Procedures Overview
The system relies heavily on 93 stored procedures for data operations:
- **CRUD Operations**: 37 procedures for basic create, read, update, delete operations
- **Business Logic**: 33 procedures handling complex calculations and validations
- **Reporting**: 23 procedures generating various reports and analytics

## Core Business Entities

### 1. **Instructors (tblPerson)**
Faculty members with department affiliations and term-specific records
- **Key Fields**: person_MothraID, person_TermCode, person_FirstName, person_LastName
- **Business Logic**: Term-based records allowing historical tracking
- **Relationships**: Links to effort records, percentage assignments

### 2. **Courses (tblCourses)**
Academic courses with enrollment and scheduling information
- **Key Fields**: course_id, course_CRN, course_TermCode, course_SubjCode
- **Business Logic**: Unique CRN per term, custodial department assignments
- **Relationships**: Links to effort records, course relationships

### 3. **Effort Records (tblEffort)**
Core tracking of instructor teaching assignments
- **Key Fields**: effort_ID, effort_MothraID, effort_CourseID, effort_Hours/Weeks
- **Business Logic**: Hours or weeks tracking, session type classification
- **Relationships**: Links instructors to courses with role assignments

### 4. **Terms (tblStatus)**
Academic term lifecycle management
- **Key Fields**: status_TermCode, status_TermName, status_AcademicYear
- **Business Logic**: Term states (Harvested, Opened, Closed)
- **Workflow**: Controls when effort can be entered and verified

### 5. **Percentage Assignments (tblPercent)**
Administrative and clinical effort percentages by academic year
- **Key Fields**: percent_MothraID, percent_TermCode, percent_EffortType
- **Business Logic**: Percentage allocations across effort types
- **Purpose**: Merit review and promotion documentation

## File Structure Analysis

### Application Layer (65 CFM Files)

#### **Core Management Files**
- **InstructorList.cfm** (4,434 bytes) - Main instructor listing and navigation
- **InstructorEffort.cfm** (18,918 bytes) - Primary effort entry and editing interface
- **CourseList.cfm** (4,434 bytes) - Course management and listing
- **EditInstructorPercent.cfm** (13,290 bytes) - Complex percentage assignment interface
- **TermManagement.cfm** (Term lifecycle controls)

#### **Import and Data Management**
- **Import.cfm** (Complex data import from Banner/Mothra systems)
- **importClinical.cfm** (Clinical scheduler integration)
- **ImportCourse.cfm** (Course data import)
- **EmailInstructor.cfm** (12,370 bytes) - Instructor notification system

#### **Reporting Files (40+ Report CFM Files)**
- **rpt_ReportSetup.cfm** - Report parameter configuration
- **rpt_EffortByUnit.cfm** - Unit-based effort reporting
- **rpt_InstructorsWithZeroEffort.cfm** - Validation reporting
- **rpt_ClinicalMPInstructors.cfm** - Clinical instructor reporting

### Component Layer (8 CFC Files)

#### **Core Business Logic Components**

**1. effort.cfc**
```coldfusion
component {
    property string dsn;

    // Core methods:
    function updateEffort() - Updates instructor effort records
    function createEffort() - Creates new effort assignments
}
```

**2. Instructor.cfc**
- Instructor CRUD operations
- Integration with personnel systems
- Validation and business rule enforcement

**3. Course.cfc**
- Course management operations
- Enrollment data handling
- Department assignment logic

**4. percentAssignment.cfc**
- Complex percentage calculation logic
- Academic year effort allocation
- Administrative vs. clinical effort tracking

**5. Access.cfc**
- Permission checking and validation
- Department-level access control
- Role-based operation authorization

**6. Audit.cfc**
- Comprehensive change tracking
- User action logging
- Data integrity verification

## Business Workflows

### 1. **Term Management Workflow**
```
1. Create New Term → 2. Harvest Data → 3. Open Term → 4. Effort Entry → 5. Verification → 6. Close Term
```

**Term States:**
- **Harvested**: Data imported from external systems
- **Opened**: Faculty can enter/edit effort
- **Closed**: No further changes allowed

### 2. **Data Import Process**
```
External Systems (Banner, Mothra, Clinical Scheduler) → Import Scripts → Validation → Database Updates
```

**Import Sources:**
- **Banner**: Course and enrollment data
- **Mothra**: Personnel and demographic information
- **Clinical Scheduler**: Clinical assignment data
- **AAUD**: Employee status and job information

### 3. **Effort Assignment Workflow**
```
Course Creation → Instructor Assignment → Effort Entry → Session Type Classification → Hours/Weeks Recording
```

**Session Types:**
- **LEC**: Lecture
- **LAB**: Laboratory
- **CLI**: Clinical
- **SEM**: Seminar
- **Others**: Various specialized types

### 4. **Verification Process**
```
Instructor Login → Review Assignments → Verify Accuracy → Submit Verification → Audit Trail
```

**Verification Features:**
- Self-service instructor verification
- Email notifications
- Deadline management
- Completion tracking

### 5. **Reporting Workflow**
```
Parameter Selection → Data Aggregation → Report Generation → Export Options (PDF, Excel)
```

## Permission System

### User Roles
- **ViewDept**: View department-specific data
- **ViewAllDepartments**: View cross-departmental data
- **EditEffort**: Modify effort records
- **ManageTerms**: Control term lifecycle
- **SU**: Super user access
- **MSO**: Manager/supervisor operations

### Access Patterns
- **Department-based**: Users see only their department's data
- **Term-based**: Access restricted to active terms
- **Role-based**: Operations available based on assigned permissions

## Database Schema Details

### Tables with Primary Keys (17 tables)
- **tblRoles**: Role definitions and descriptions
- **tblEffort**: Core effort tracking records
- **tblPerson**: Instructor information (composite PK: MothraID + TermCode)
- **tblCourses**: Course catalog and scheduling
- **tblPercent**: Percentage assignments
- **tblSabbatic**: Sabbatical term exclusions
- **tblStatus**: Term status and lifecycle management

### Tables Missing Primary Keys (4 tables)
- **tblReportUnits**: Reporting unit definitions
- **tblReviewYears**: Review period definitions
- **Sheet1**: Import/temporary table
- **tblSabbatic_backup_2017_08_25**: Archive table

### Key Relationships (No Foreign Keys Currently)
```
tblEffort → tblPerson (effort_MothraID, effort_termCode)
tblEffort → tblCourses (effort_CourseID)
tblEffort → tblRoles (effort_Role)
tblPercent → tblPerson (percent_MothraID, percent_TermCode)
```

## Stored Procedures Analysis & Migration Strategy

### Active Procedures (93 total)

The current system utilizes 93 stored procedures that require migration to Entity Framework and service layer architecture. These procedures are categorized by complexity and migration approach:

#### Category 1: Simple CRUD Operations (37 procedures - 40%)
These procedures perform basic create, read, update, delete operations and can be directly replaced with Entity Framework repository patterns.

**Create Operations:**
- usp_createInstructorEffort → EffortRepository.CreateEffortAsync()
- usp_createInstructor → InstructorRepository.CreateInstructorAsync()
- usp_createCourse → CourseRepository.CreateCourseAsync()
- usp_createInstructorPercent → PercentageRepository.CreatePercentageAsync()
- usp_createCourseRelationship → CourseRepository.CreateRelationshipAsync()
- usp_createAudit → AuditRepository.CreateAuditAsync()

**Update Operations:**
- usp_updateInstructorEffort → EffortRepository.UpdateEffortAsync()
- usp_updateInstructor → InstructorRepository.UpdateInstructorAsync()
- usp_updateCourse → CourseRepository.UpdateCourseAsync()
- usp_updateInstructorPercent → PercentageRepository.UpdatePercentageAsync()

**Delete Operations:**
- usp_delInstructorEffort → EffortRepository.DeleteEffortAsync()
- usp_delInstructor → InstructorRepository.DeleteInstructorAsync()
- usp_delCourse → CourseRepository.DeleteCourseAsync()
- usp_delInstructorPercent → PercentageRepository.DeletePercentageAsync()
- usp_delCourseRelationship → CourseRepository.DeleteRelationshipAsync()

**Simple Retrieval:**
- usp_getInstructor → InstructorRepository.GetInstructorAsync()
- usp_getCourse → CourseRepository.GetCourseAsync()
- usp_getTermStatus → TermRepository.GetTermStatusAsync()
- usp_findCourseID → CourseRepository.FindCourseIdAsync()
- usp_getPossibleInstructors → InstructorRepository.GetPossibleInstructorsAsync()

#### Category 2: Complex Business Logic (33 procedures - 35%)
These procedures contain business rules, calculations, and validations that should be migrated to C# service layer methods.

**Term Management:**
- usp_openTerm → TermService.OpenTermAsync()
- usp_closeTerm → TermService.CloseTermAsync()
- usp_reopenTerm → TermService.ReopenTermAsync()
- usp_unopenTerm → TermService.UnopenTermAsync()

**Effort Calculations:**
- usp_VerifyEffort → EffortService.VerifyEffortAsync()
- usp_getEffortPercentsForInstructor → EffortService.GetEffortPercentsAsync()
- usp_getSumEffortPercentsForInstructor → EffortService.GetSumEffortPercentsAsync()
- usp_getListOfEffortPercentsForInstructor → EffortService.GetListEffortPercentsAsync()

**Business Logic Operations:**
- usp_getAcademicYearFromTermCode → TermService.GetAcademicYearAsync()
- usp_getJobGroupName → JobGroupService.GetJobGroupNameAsync()
- usp_getJobGroups → JobGroupService.GetJobGroupsAsync()
- usp_getEffortTypes → EffortService.GetEffortTypesAsync()
- usp_getEffortUnits → EffortService.GetEffortUnitsAsync()

**Complex Retrieval:**
- usp_getInstructors → InstructorService.GetInstructorsAsync()
- usp_getCourses → CourseService.GetCoursesAsync()
- usp_getCourseEffort → CourseService.GetCourseEffortAsync()
- usp_getInstructorEffort → InstructorService.GetInstructorEffortAsync()
- usp_getDistinctCourseInstructors → CourseService.GetDistinctInstructorsAsync()
- usp_getDistinctInstructorCourses → InstructorService.GetDistinctCoursesAsync()

#### Category 3: High-Performance Reports (23 procedures - 25%)
These procedures generate complex reports and analytics. They require careful migration with performance considerations, potentially using EF Core raw SQL and caching.

**Merit Reports:**
- usp_getEffortReportMerit → ReportingService.GetEffortReportMeritAsync()
- usp_getEffortReportMeritMultiYear → ReportingService.GetEffortReportMeritMultiYearAsync()
- usp_getEffortReportMeritAverage → ReportingService.GetEffortReportMeritAverageAsync()
- usp_getEffortReportMeritMedian → ReportingService.GetEffortReportMeritMedianAsync()
- usp_getEffortReportMeritSummary → ReportingService.GetEffortReportMeritSummaryAsync()
- usp_getEffortReportMeritUnit → ReportingService.GetEffortReportMeritUnitAsync()
- usp_getEffortReportMeritWithClinPercent → ReportingService.GetEffortReportMeritWithClinPercentAsync()

**Department Activity Reports:**
- usp_getEffortDeptActivityTotal → ReportingService.GetEffortDeptActivityTotalAsync()
- usp_getEffortDeptActivityTotalWithExcludeTerms → ReportingService.GetEffortDeptActivityTotalWithExcludeTermsAsync()
- usp_getDepartmentCountByJobGroup → ReportingService.GetDepartmentCountByJobGroupAsync()
- usp_getDepartmentCountByJobGroupWithExcludeTerms → ReportingService.GetDepartmentCountByJobGroupWithExcludeTermsAsync()

**Instructor Activity Reports:**
- usp_getEffortInstructorActivity → ReportingService.GetEffortInstructorActivityAsync()
- usp_getEffortInstructorActivityWithExcludeTerms → ReportingService.GetEffortInstructorActivityWithExcludeTermsAsync()
- usp_getInstructorsWithClinicalEffort → ReportingService.GetInstructorsWithClinicalEffortAsync()
- usp_getZeroEffortInstructors → ReportingService.GetZeroEffortInstructorsAsync()

**Evaluation Reports:**
- usp_getInstructorEvals → ReportingService.GetInstructorEvalsAsync()
- usp_getInstructorEvalsAverage → ReportingService.GetInstructorEvalsAverageAsync()
- usp_getInstructorEvalsMultiYear → ReportingService.GetInstructorEvalsMultiYearAsync()

**School-Level Reports:**
- usp_getEffortReportSchoolSummary → ReportingService.GetEffortReportSchoolSummaryAsync()
- usp_getEffortReportDeptSummary → ReportingService.GetEffortReportDeptSummaryAsync()

### Migration Implementation Timeline

**Agile Sprint-Based Approach (16 two-week sprints)**

**Sprint 1-2: Foundation & Core Features**
- Sprint 1: Core EF models, basic infrastructure, term viewing
- Sprint 2: Term lifecycle management, replacing usp_openTerm/closeTerm/etc.

**Sprint 3-6: Core Entity Management**
- Sprint 3: Instructor CRUD, department permissions, basic import
- Sprint 4: Course management, Banner integration
- Sprint 5: Basic effort entry with session type logic
- Sprint 6: Comprehensive data import validation

**Sprint 7-10: Advanced Features**
- Sprint 7: Effort verification workflow
- Sprint 8: Complete permission system integration
- Sprint 9: Percentage assignments (admin/clinical)
- Sprint 10: Course relationships, guest instructors

**Sprint 11-13: Reporting & Integration**
- Sprint 11: Basic reporting suite (instructor, department summaries)
- Sprint 12: Clinical scheduler integration
- Sprint 13: Advanced reporting (merit review, multi-year analysis)

**Sprint 14-16: Production Readiness**
- Sprint 14: Historical data migration, parallel running
- Sprint 15: Performance optimization, UI polish
- Sprint 16: Security audit, production deployment

**Stored Procedure Migration Schedule:**
- **Simple CRUD (37 procedures)**: Sprints 1-6 as needed per feature
- **Business Logic (33 procedures)**: Sprints 2-10 integrated with features
- **Reporting (23 procedures)**: Sprints 11-13 with reporting implementation
- **Cleanup & Validation**: Sprint 14 during data migration phase

**Benefits of Sprint Approach:**
- Term management working after Sprint 2 (immediate value)
- Core system functional after Sprint 6 (12 weeks)
- Continuous stakeholder feedback and validation
- Risk mitigation through incremental delivery

### Performance Considerations

**EF Core Optimizations:**
- Use compiled queries for frequently executed operations
- Implement query splitting for complex joins
- Add appropriate database indexes based on LINQ query patterns
- Use raw SQL for complex aggregations where necessary

**Caching Strategy:**
- Memory cache for reference data (roles, terms, job groups)
- Distributed cache for expensive report results
- Cache invalidation on data updates

**Monitoring:**
- Application Insights for query performance tracking
- Custom metrics for business operation timings
- Database performance monitoring for index effectiveness

### Unused Procedures (27)
- Backup versions (_backup, _old)
- Person-specific procedures (Ray, Kass, Lairmore)
- Superseded versions (v2, alternative implementations)

## Integration Points

### External System Integrations
1. **RAPS Authentication**: Role and permission management
2. **Banner System**: Course and enrollment data
3. **Mothra Directory**: Personnel information
4. **Clinical Scheduler**: Clinical assignment data
5. **Email System**: Notification delivery

### Data Flow Patterns
```
External Systems → Import Jobs → Validation → Database → UI → Reports
```

## User Interface Patterns

### Navigation Structure
- **Main Menu**: Term selection and primary navigation
- **Instructor Views**: List → Detail → Edit workflows
- **Course Views**: List → Assignment → Management
- **Administrative**: Term management, user access, reporting

### Form Patterns
- **Master-Detail**: Instructor → Efforts → Details
- **Wizard-style**: Multi-step processes for complex operations
- **Validation**: Client and server-side validation
- **AJAX**: Dynamic updates without page refresh

### Reporting Interface
- **Parameter Selection**: Flexible report configuration
- **Preview**: Report preview before generation
- **Export**: Multiple format support (PDF, Excel, CSV)

## Security Considerations

### Authentication
- **CAS Integration**: Central Authentication Service
- **Session Management**: Secure session handling
- **Timeout**: Automatic session expiration

### Authorization
- **Granular Permissions**: Fine-grained access control
- **Department Isolation**: Data segregation by department
- **Role-based Access**: Operation-level permissions

### Data Protection
- **Audit Trails**: Comprehensive change logging
- **Input Validation**: SQL injection prevention
- **Error Handling**: Secure error reporting

## Performance Characteristics

### Database Performance
- **Stored Procedures**: Extensive use for complex operations
- **Indexing**: Primary keys only (missing performance indexes)
- **Query Patterns**: Complex joins and aggregations

### Application Performance
- **Server-side Rendering**: Traditional page-based architecture
- **Caching**: Minimal caching implementation
- **Optimization**: Room for improvement in query efficiency

## Known Issues and Technical Debt

### Database Issues
- **Missing Foreign Keys**: No referential integrity constraints
- **Missing Indexes**: Performance impact on large datasets
- **Orphaned Tables**: Unused import and backup tables

### Code Issues
- **Mixed Coding Styles**: Inconsistent formatting and patterns
- **Hard-coded Values**: Configuration values embedded in code
- **Error Handling**: Inconsistent error management

### Architecture Issues
- **Tight Coupling**: Database logic mixed with presentation
- **Limited Reusability**: Monolithic page structures
- **Maintenance Complexity**: Large, complex files difficult to maintain

## Migration Considerations

### Data Migration
- **Preserve Historical Data**: Maintain all audit trails and historical records
- **Relationship Integrity**: Ensure data consistency during migration
- **Validation**: Comprehensive data validation post-migration

### Functional Parity
- **Business Logic**: Preserve all current functionality
- **User Workflows**: Maintain familiar user experience
- **Integration**: Preserve external system connections

### Performance Improvements
- **Database Optimization**: Add missing constraints and indexes
- **Query Optimization**: Convert stored procedures to efficient queries
- **Caching Strategy**: Implement appropriate caching layers

## Success Metrics

### Functional Metrics
- **Feature Completeness**: 100% functional parity
- **Data Integrity**: Zero data loss or corruption
- **User Acceptance**: Positive feedback on usability improvements

### Technical Metrics
- **Performance**: Improved page load times
- **Maintainability**: Reduced code complexity
- **Reliability**: Decreased error rates and downtime

---

**Documentation Status**: Complete
**Next Phase**: Migration Plan Development
**Dependencies**: Database schema analysis, VIPER2 architecture review